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
  message: string;
  line: number;
  startColumn: number;
  endColumn: number;
  severity: "error" | "warning" | "info" | "hint";
}

interface RunOutput {
  time: number;
  finalState: any[];
}

interface TokenInfo {
  line: number;
  startColumn: number;
  endColumn: number;
  type: string;
  isDeclaration: boolean;
}

const enum TokenType {
  keyword = 0,
  variable = 1,
  function = 2,
  type = 3,
  string = 4,
  number = 5,
  comment = 6,
  boolean = 7,
  operator = 8,
}

const enum TokenModifier {
  declaration = 1 << 0,
  readonly = 1 << 1,
}

interface CompileResult {
  success: boolean;
  errors: ErrorInfo[];
  output?: RunOutput;
  tokens?: TokenInfo[];
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
  return errors
    .filter(e =>
      Number.isInteger(e.line) &&
      Number.isInteger(e.startColumn) &&
      Number.isInteger(e.endColumn)
    )
    .map((e) => ({
      severity:
        e.severity === "hint" ? DiagnosticSeverity.Hint :
        e.severity === "warning" ? DiagnosticSeverity.Warning :
        e.severity === "info" ? DiagnosticSeverity.Information :
        DiagnosticSeverity.Error,
      range: Range.create(
        Position.create(e.line, e.startColumn),
        Position.create(e.line, e.endColumn)
      ),
      message: e.message ?? "Unknown error",
      source: "vext-compiler",
    }));
}

function assertNoOverlappingTokens(tokens: TokenInfo[], uri: string) {
  const sorted = [...tokens].sort((a, b) => {
    if (a.line !== b.line) return a.line - b.line;
    return a.startColumn - b.startColumn;
  });

  const diagnostics: Diagnostic[] = [];

  for (let i = 1; i < sorted.length; i++) {
    const prev = sorted[i - 1];
    const curr = sorted[i];

    if (curr.line === prev.line && curr.startColumn < prev.endColumn) {
      diagnostics.push({
        severity: DiagnosticSeverity.Error,
        range: Range.create(
          Position.create(curr.line, curr.startColumn),
          Position.create(curr.line, curr.endColumn + 1)
        ),
        message: `Semantic token overlap detected: '${prev.type}' [${prev.startColumn}-${prev.endColumn}] overlaps with '${curr.type}' [${curr.startColumn}-${curr.endColumn}]`,
        source: "vext-compiler",
      });
    }
    if (curr.endColumn - curr.startColumn == 0) {
      diagnostics.push({
        severity: DiagnosticSeverity.Error,
        range: Range.create(
          Position.create(curr.line, curr.startColumn),
          Position.create(curr.line, curr.endColumn)
        ),
        message: `length is 0`,
        source: "vext-compiler",
      });
    }
  }

  if (diagnostics.length > 0) {
    // Send diagnostics so they appear as red squiggles in the editor
    connection.sendDiagnostics({ uri, diagnostics });
    // Also throw to stop further processing if desired
    throw new Error("Semantic token overlap detected; see editor for details.");
  }
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
    const diagnostics = errorsToDiagnostics(result.errors || []);
    connection.sendDiagnostics({ uri, diagnostics });

    if (result.success && result.output) {
      connection.window.showInformationMessage(
        `Program ran in ${result.output.time.toFixed(2)}ms`
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
    if (!result.tokens) return builder.build();

    const tokens = [...result.tokens].sort((a, b) => {
      if (a.line !== b.line) return a.line - b.line;
      return a.startColumn - b.startColumn;
    });

    assertNoOverlappingTokens(tokens, params.textDocument.uri);

    for (const t of tokens) {
      let tokenType: number;

      switch (t.type) {
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
        t.isDeclaration ? TokenModifier.declaration : 0;

      builder.push(
        t.line,
        t.startColumn,
        (t.endColumn - t.startColumn),
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
