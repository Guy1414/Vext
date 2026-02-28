import * as path from "path";
import * as vscode from 'vscode';
import { workspace, ExtensionContext } from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  TransportKind,
} from "vscode-languageclient/node";
import { exec } from "child_process";
import { promisify } from "util";

const execAsync = promisify(exec);

async function checkDotnetExists(): Promise<boolean> {
  try {
    await execAsync("dotnet --version");
    return true;
  } catch {
    return false;
  }
}

interface RunOutput {
  time: number;
  finalState: any[];
  stdout?: string;
}

let client: LanguageClient;

export async function activate(context: ExtensionContext) {
  // Check for dotnet before doing anything else
  const dotnetExists = await checkDotnetExists();
  if (!dotnetExists) {
    const choice = await vscode.window.showErrorMessage(
      "The .NET SDK/Runtime is required to run the Vext Language Server but was not found.",
      "Install .NET"
    );
    if (choice === "Install .NET") {
      vscode.env.openExternal(vscode.Uri.parse("https://dotnet.microsoft.com/download"));
    }
    return;
  }

  // The server is implemented in node
  const serverModule = context.asAbsolutePath(
    path.join("server", "out", "server.js")
  );

  // If the extension is launched in debug mode then the debug server options are used
  // Otherwise the run options are used
  const serverOptions: ServerOptions = {
    run: { module: serverModule, transport: TransportKind.ipc },
    debug: {
      module: serverModule,
      transport: TransportKind.ipc,
    },
  };

  // Options to control the language client
  const clientOptions: LanguageClientOptions = {
    // Register the server for all documents by default
    documentSelector: [{ scheme: "file", language: "vext" }, { scheme: "file", language: "vxt" }],
    synchronize: {
      // Notify the server about file changes to '.clientrc files contained in the workspace
      fileEvents: workspace.createFileSystemWatcher("**/.clientrc"),
    },
  };

  // Create the language client and start the client.
  client = new LanguageClient(
    "vextLanguageServer",
    "Vext Language Server",
    serverOptions,
    clientOptions
  );

  // Start the client. This will also launch the server
  await client.start();

  let outputChannel: vscode.OutputChannel | undefined;

  const runCommand = vscode.commands.registerCommand('vext.runCode', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    const code = editor.document.getText();

    if (!outputChannel) {
      outputChannel = vscode.window.createOutputChannel('Vext');
    }

    outputChannel.clear();
    outputChannel.show(true); // true = preserve focus in the editor

    try {
      const activeEditor = vscode.window.activeTextEditor;
      if (activeEditor) {
        const diagnostics = vscode.languages.getDiagnostics(activeEditor.document.uri);

        const hasErrors = diagnostics.some(d => d.severity === vscode.DiagnosticSeverity.Error);

        if (hasErrors) {
          // 1. Filter to get ONLY the actual errors for the count
          const errorCount = diagnostics.filter(d => d.severity === vscode.DiagnosticSeverity.Error).length;

          // 2. Show the Modal
          vscode.window.showErrorMessage(
            "Execution Blocked",
            {
              modal: true,
              detail: `You have ${errorCount} critical error(s) in this file. Please fix them before running.`
            },
            "Show Problems"
          ).then(selection => {
            if (selection === "Show Problems") {
              // 3. This built-in command opens the Problems view automatically
              vscode.commands.executeCommand("workbench.actions.view.problems");
            }
          });

          return; // Stop execution
        }
      }

      const result = await client.sendRequest<RunOutput>('vext/runCode', { code });

      outputChannel.appendLine(`✅ Ran successfully in ${result.time}ms`);

      const stdout = result.stdout ?? '';

      if (stdout.trim().length > 0) {
        outputChannel.appendLine(stdout);
      } else {
        outputChannel.appendLine('(no output)');
      }
    } catch (err: any) {
      vscode.window.showErrorMessage('Failed to run Vext code: ' + (err.message || err));
      outputChannel.appendLine('❌ Run failed: ' + (err.message || err));
    }
  });

  context.subscriptions.push(runCommand);
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}
