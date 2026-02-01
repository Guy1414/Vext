import {
  createConnection,
  TextDocuments,
  ProposedFeatures,
  InitializeParams,
  TextDocumentSyncKind,
  InitializeResult,
  Diagnostic,
  DiagnosticSeverity,
  Range,
  Position,
  SemanticTokensBuilder,
  SemanticTokens
} from "vscode-languageserver/node";
import { TextDocument } from "vscode-languageserver-textdocument";
import { spawn } from "child_process";
import * as path from "path";
import * as fs from 'fs';

// --- Setup connection & documents ---
const connection = createConnection(ProposedFeatures.all);
const documents: TextDocuments<TextDocument> = new TextDocuments(TextDocument);

// --- Helper types for C# JSON result ---
interface ErrorInfo {
  Message: string;
  Line: number;
  StartColumn: number;
  EndColumn: number;
}

interface RunOutput {
  Time: number;
  FinalState: any[];
}

interface TokenInfo {
  Line: number;
  StartColumn: number;
  EndColumn: number;
  Type: string;
  IsDeclaration: boolean;
}

const enum TokenType {
  keyword = 0,
  variable = 1,
  function = 2,
  type = 3,
  string = 4,
  number = 5,
  boolean = 6,
  comment = 7,
  operator = 8,
}

const enum TokenModifier {
  declaration = 1 << 0,
  readonly = 1 << 1,
}

interface CompileResult {
  Success: boolean;
  Errors: ErrorInfo[];
  Output?: RunOutput;
  Tokens?: TokenInfo[];
}

// --- Compile helper using stdin ---
function compileVextFromText(code: string, run = false): Promise<CompileResult> {
  return new Promise((resolve, reject) => {
    const bridgePath = path.resolve(__dirname, '..', '..', 'compiler', 'Vext.LSP.exe');
    if (!fs.existsSync(bridgePath)) {
      connection.window.showErrorMessage(`Compiler missing at: ${bridgePath}`);
      return reject(`File not found: ${bridgePath}`);
    }
    const args = ["--stdin"];
    if (run) args.push("--run");

    const proc = spawn(`"${bridgePath}"`, args, { windowsHide: true, shell: true });

    let stdout = "";
    let stderr = "";

    proc.stdout.on("data", (data) => (stdout += data.toString()));
    proc.stderr.on("data", (data) => (stderr += data.toString()));

    proc.on("error", (err) => reject(err.message));

    proc.on("close", (code) => {
      if (code !== 0) {
        reject(stderr || `Bridge exited with code ${code}`);
        return;
      }
      try {
        const result: CompileResult = JSON.parse(stdout);
        resolve(result);
      } catch (parseErr) {
        reject("Failed to parse compiler output: " + parseErr);
      }
    });

    // Send code to stdin
    proc.stdin.write(code);
    proc.stdin.end();
  });
}

// --- Convert compiler errors to LSP diagnostics ---
function errorsToDiagnostics(errors: ErrorInfo[]): Diagnostic[] {
  return errors.map((e) => ({
    severity: DiagnosticSeverity.Error,
    range: Range.create(
      Position.create(e.Line, e.StartColumn),
      Position.create(e.Line, e.EndColumn)
    ),
    message: e.Message,
    source: "vext-compiler",
  }));
}

// --- LSP Handlers ---
connection.onInitialize((_params: InitializeParams) => {
  return <InitializeResult>{
    capabilities: {
      textDocumentSync: TextDocumentSyncKind.Incremental,
      semanticTokensProvider: {
        legend: {
          tokenTypes: [
            "keyword",    // 0
            "variable",   // 1
            "function",   // 2
            "type",       // 3
            "string",     // 4
            "number",     // 5
            "comment",    // 6
            "boolean",    // 7
            "operator"    // 8
          ],
          tokenModifiers: ["declaration", "readonly"]
        },
        full: true
      }
    },
  };
});

documents.onDidChangeContent(async (change) => {
  const uri = change.document.uri;
  const code = change.document.getText();

  try {
    const result = await compileVextFromText(code, false);

    // Send diagnostics
    const diagnostics = errorsToDiagnostics(result.Errors);
    connection.sendDiagnostics({ uri, diagnostics });

    if (result.Success && result.Output) {
      connection.window.showInformationMessage(
        `Program ran in ${result.Output.Time.toFixed(2)}ms`
      );
    }
  } catch (err: any) {
    connection.window.showErrorMessage("Compiler error: " + err);
  }
});

connection.languages.semanticTokens.on(async (params) => {
  const doc = documents.get(params.textDocument.uri);
  if (!doc) return { data: [] };

  const builder = new SemanticTokensBuilder();
  const code = doc.getText();

  try {
    const result = await compileVextFromText(code, false);
    if (!result.Tokens) return builder.build();

    const tokens = [...result.Tokens].sort((a, b) => {
      if (a.Line !== b.Line) return a.Line - b.Line;
      return a.StartColumn - b.StartColumn;
    });

    for (const t of tokens) {
      let tokenType: number;

      switch (t.Type) {
        case "keyword":
          tokenType = TokenType.keyword;
          break;
        case "variable":
          tokenType = TokenType.variable;
          break;
        case "function":
          tokenType = TokenType.function;
          break;
        case "type":
          tokenType = TokenType.type;
          break;
        case "string":
          tokenType = TokenType.string;
          break;
        case "number":
          tokenType = TokenType.number;
          break;
        case "boolean":
          tokenType = TokenType.boolean;
          break;
        case "comment":
          tokenType = TokenType.comment;
          break;
        case "operator":
          tokenType = TokenType.operator;
          break;
        default:
          continue;
      }

      const modifiers =
        t.IsDeclaration ? TokenModifier.declaration : 0;

      builder.push(
        t.Line,
        t.StartColumn,
        t.EndColumn - t.StartColumn,
        tokenType,
        modifiers
      );
    }

    return builder.build();
  } catch (err) {
    console.error("Error building semantic tokens:", err);
    return builder.build();
  }
});

// --- Listen ---
documents.listen(connection);
connection.listen();
