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
} from "vscode-languageserver/node";
import { TextDocument } from "vscode-languageserver-textdocument";
import { spawn } from "child_process";
import * as path from "path";

// --- Setup connection & documents ---
const connection = createConnection(ProposedFeatures.all);
const documents: TextDocuments<TextDocument> = new TextDocuments(TextDocument);

// --- Helper types for C# JSON result ---
interface ErrorInfo {
  Message: string;
  Line: number;
  Column: number;
}

interface RunOutput {
  Time: number;
  FinalState: any[];
}

interface CompileResult {
  Success: boolean;
  Errors: ErrorInfo[];
  Output?: RunOutput;
}

// --- Compile helper using stdin ---
function compileVextFromText(code: string, run = false): Promise<CompileResult> {
    return new Promise((resolve, reject) => {
    const bridgePath = path.resolve(__dirname, '..', '..', 'compiler', 'Vext.LSP.exe');
    const args = ["--stdin"];
    if (run) args.push("--run");

    const proc = spawn(bridgePath, args, { windowsHide: true });

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
      Position.create(Math.max(0, e.Line - 1), Math.max(0, e.Column - 1)),
      Position.create(Math.max(0, e.Line - 1), Number.MAX_VALUE)
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

// --- Listen ---
documents.listen(connection);
connection.listen();
