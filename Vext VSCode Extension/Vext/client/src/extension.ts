import * as vscode from 'vscode';
import { workspace, ExtensionContext } from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  TransportKind,
} from "vscode-languageclient/node";
import { ensureCompiler } from "./compilerDownloader";

interface RunOutput {
  time: number;
  finalState: any[];
  stdout?: string;
}

let client: LanguageClient;

export async function activate(context: ExtensionContext) {
  // 1. Fetch/Update the compiler binary and get its path
  let compilerPath: string;
  try {
      compilerPath = await ensureCompiler(context);
  } catch (err: any) {
      vscode.window.showErrorMessage(`Vext failed to start: ${err.message}`);
      return;
  }

  // 2. Setup Server Options for the Node.js LSP server
  const serverModule = context.asAbsolutePath('server/out/server.js');

  const spawnOptions = {
    env: { ...process.env, VEXT_COMPILER_PATH: compilerPath }
  };

  const serverOptions: ServerOptions = {
    run: { 
        command: 'node',
        args: [serverModule],
        transport: TransportKind.stdio,
        options: spawnOptions
    },
    debug: { 
        command: 'node',
        args: [serverModule, '--debug'],
        transport: TransportKind.stdio,
        options: spawnOptions
    },
  };

  const outputChannel = vscode.window.createOutputChannel('Vext');

  const clientOptions: LanguageClientOptions = {
    documentSelector: [
        { scheme: "file", language: "vext" }, 
        { scheme: "file", language: "vxt" }
    ],
    synchronize: {
      fileEvents: workspace.createFileSystemWatcher("**/.clientrc"),
    },
    outputChannel
  };

  client = new LanguageClient(
    "vextLanguageServer",
    "Vext Language Server",
    serverOptions,
    clientOptions
  );


  try {
    await client.start();
  } catch (err: any) {
    const msg = err instanceof Error ? err.message : String(err);
    vscode.window.showErrorMessage(`Vext failed to start: ${msg}`);
    console.error("Vext startup error:", err);
    return;
  }

  // --- Notification Handlers ---

  client.onNotification("vext/needInput", async () => {
    const input = await vscode.window.showInputBox({
      prompt: "Vext Input Required",
      placeHolder: "Enter input for the program..."
    });
    client.sendRequest("vext/submitInput", { input: input ?? "" });
  });

  // --- Command Registrations ---

  // Register Force Update
  const forceUpdateCommand = vscode.commands.registerCommand('vext.forceUpdateCheck', async () => {
      await context.globalState.update("vext.lastCheckTime", 0);
      await ensureCompiler(context);
      vscode.window.showInformationMessage("Vext update check complete. Restart VS Code to use the new version.");
  });

  // Register Run Code
  const runCommand = vscode.commands.registerCommand('vext.runCode', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;

    outputChannel.clear();
    outputChannel.show(true);

    try {
      const diagnostics = vscode.languages.getDiagnostics(editor.document.uri);
      const errors = diagnostics.filter(d => d.severity === vscode.DiagnosticSeverity.Error);

      if (errors.length > 0) {
        const selection = await vscode.window.showErrorMessage(
          "Execution Blocked",
          {
            modal: true,
            detail: `You have ${errors.length} critical error(s) in this file. Please fix them before running.`
          },
          "Show Problems"
        );

        if (selection === "Show Problems") {
          vscode.commands.executeCommand("workbench.actions.view.problems");
        }
        return; 
      }

      const code = editor.document.getText();
      const result = await client.sendRequest<RunOutput>('vext/runCode', { code });

      outputChannel.appendLine(`✅ Ran successfully in ${result.time}ms`);
      outputChannel.appendLine(result.stdout?.trim() ? result.stdout : '(no output)');

    } catch (err: any) {
      vscode.window.showErrorMessage('Failed to run Vext code: ' + (err.message || err));
      outputChannel.appendLine('❌ Run failed: ' + (err.message || err));
    }
  });

  context.subscriptions.push(forceUpdateCommand, runCommand);
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}