import * as path from "path";
import * as vscode from 'vscode';
import { workspace, ExtensionContext } from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  ServerOptions,
  TransportKind,
} from "vscode-languageclient/node";

interface RunOutput {
  time: number;
  finalState: any[];
  stdout?: string;
}

let client: LanguageClient;

export function activate(context: ExtensionContext) {
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
  client.start();

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
