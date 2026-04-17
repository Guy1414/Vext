import * as vscode from 'vscode';
import * as path from 'path';
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

interface TokenInfo {
    line: number;
    startColumn: number;
    endColumn: number;
    type: string;
    isDeclaration: boolean;
}

let client: LanguageClient;
let statusBarItem: vscode.StatusBarItem;

export async function activate(context: ExtensionContext) {
  // 1. Setup Status Bar
  statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
  statusBarItem.command = 'vext.showControlPanel';
  statusBarItem.text = `$(sync~spin) Vext: Starting...`;
  statusBarItem.tooltip = "Vext Language Server is initializing";
  statusBarItem.show();
  context.subscriptions.push(statusBarItem);

  // 2. Fetch/Update the compiler binary
  let compilerPath: string;
  try {
      compilerPath = await ensureCompiler(context);
      const tag = context.globalState.get<string>("vext.compilerVersion") || "unknown";
      statusBarItem.text = `$(zap) Vext ${tag}`;
      statusBarItem.tooltip = `Vext Compiler ${tag} is ready`;
  } catch (err: any) {
      const msg = err instanceof Error ? err.message : String(err);
      vscode.window.showErrorMessage(`Vext: Failed to initialize compiler. ${msg}`);
      statusBarItem.text = `$(error) Vext: Error`;
      statusBarItem.backgroundColor = new vscode.ThemeColor('statusBarItem.errorBackground');
      return;
  }

  // 3. Setup LSP
  const serverModule = context.asAbsolutePath('server/out/server.js');
  const spawnOptions = { env: { ...process.env, VEXT_COMPILER_PATH: compilerPath } };

  const serverOptions: ServerOptions = {
    run: { command: 'node', args: [serverModule], transport: TransportKind.stdio, options: spawnOptions },
    debug: { command: 'node', args: [serverModule, '--debug'], transport: TransportKind.stdio, options: spawnOptions },
  };

  const outputChannel = vscode.window.createOutputChannel('Vext');
  const runOutputChannel = vscode.window.createOutputChannel('Vext Output');
  
  const clientOptions: LanguageClientOptions = {
    documentSelector: [
        { scheme: "file", language: "vext" }, 
        { scheme: "file", language: "vxt" }
    ],
    synchronize: { fileEvents: workspace.createFileSystemWatcher("**/.clientrc") },
    outputChannel
  };

  client = new LanguageClient("vextLanguageServer", "Vext Language Server", serverOptions, clientOptions);

  try {
    await client.start();
  } catch (err: any) {
    const msg = err instanceof Error ? err.message : String(err);
    vscode.window.showErrorMessage(`Vext: Failed to start Language Server. ${msg}`);
    return;
  }

  // --- Sidebar Views ---
  
  const infoProvider = new VextInfoProvider(context);
  vscode.window.registerTreeDataProvider('vext-info', infoProvider);

  const symbolsProvider = new VextSymbolsProvider();
  vscode.window.registerTreeDataProvider('vext-symbols', symbolsProvider);

  // Auto-refresh symbols on change
  context.subscriptions.push(
    workspace.onDidChangeTextDocument(() => symbolsProvider.refresh()),
    vscode.window.onDidChangeActiveTextEditor(() => symbolsProvider.refresh())
  );

  // --- Notification Handlers ---

  client.onNotification("vext/needInput", async () => {
    const input = await vscode.window.showInputBox({
      prompt: "Vext: Input Required",
      placeHolder: "Enter input for the program..."
    });
    client.sendRequest("vext/submitInput", { input: input ?? "" });
  });

  // --- Command Registrations ---

  context.subscriptions.push(
    vscode.commands.registerCommand('vext.forceUpdateCheck', async () => {
        await vscode.window.withProgress({
            location: vscode.ProgressLocation.Notification,
            title: "Vext: Checking for updates...",
            cancellable: false
        }, async () => {
            await context.globalState.update("vext.lastCheckTime", 0);
            await ensureCompiler(context);
        });
        vscode.window.showInformationMessage("Vext: Update check complete. Restart VS Code to apply changes.");
        infoProvider.refresh();
    }),

    vscode.commands.registerCommand('vext.openWalkthrough', () => {
        vscode.commands.executeCommand('workbench.action.openWalkthrough', 'guy1414.vext#vext-welcome', false);
    }),

    vscode.commands.registerCommand('vext.showControlPanel', async () => {
        const tag = context.globalState.get<string>("vext.compilerVersion") || "unknown";
        const selection = await vscode.window.showQuickPick([
            { label: "$(play) Run Active File", id: 'run' },
            { label: "$(book) Open Welcome Guide", id: 'walkthrough' },
            { label: "$(cloud-download) Check for Updates...", id: 'update' },
            { label: "$(output) Show Extension Logs", id: 'logs' }
        ], { placeHolder: `Vext Control Panel (${tag})` });

        if (!selection) return;
        switch (selection.id) {
            case 'run': vscode.commands.executeCommand('vext.runCode'); break;
            case 'walkthrough': vscode.commands.executeCommand('vext.openWalkthrough'); break;
            case 'update': vscode.commands.executeCommand('vext.forceUpdateCheck'); break;
            case 'logs': outputChannel.show(); break;
        }
    }),

    vscode.commands.registerCommand('vext.runCode', async () => {
      const editor = vscode.window.activeTextEditor;
      if (!editor) return;

      const diagnostics = vscode.languages.getDiagnostics(editor.document.uri);
      const errors = diagnostics.filter(d => d.severity === vscode.DiagnosticSeverity.Error);

      if (errors.length > 0) {
        const selection = await vscode.window.showErrorMessage(
          "Vext: Execution Blocked",
          { modal: true, detail: `Please fix the ${errors.length} critical error(s) in your code before running.` },
          "Show Problems"
        );
        if (selection === "Show Problems") vscode.commands.executeCommand("workbench.actions.view.problems");
        return; 
      }

      await vscode.window.withProgress({
          location: vscode.ProgressLocation.Notification,
          title: "Vext: Running code...",
          cancellable: false
      }, async () => {
        try {
          runOutputChannel.clear();
          runOutputChannel.show(true);
          runOutputChannel.appendLine(`[Vext] Running ${path.basename(editor.document.uri.fsPath)}...`);
          
          const code = editor.document.getText();
          const result = await client.sendRequest<RunOutput>('vext/runCode', { code });

          runOutputChannel.appendLine("--------------------------------------------------");
          runOutputChannel.appendLine(result.stdout?.trim() ? result.stdout : '(no output)');
          runOutputChannel.appendLine("--------------------------------------------------");
          runOutputChannel.appendLine(`[Vext] Finished in ${result.time}ms`);
          
          vscode.window.showInformationMessage(`Vext: Run completed in ${result.time}ms`);
        } catch (err: any) {
          const msg = err.message || err;
          vscode.window.showErrorMessage(`Vext: Run failed. ${msg}`);
          runOutputChannel.appendLine(`[Vext] Error: ${msg}`);
        }
      });
    })
  );
}

class VextInfoProvider implements vscode.TreeDataProvider<InfoItem> {
    private _onDidChangeTreeData: vscode.EventEmitter<InfoItem | undefined | void> = new vscode.EventEmitter<InfoItem | undefined | void>();
    readonly onDidChangeTreeData: vscode.Event<InfoItem | undefined | void> = this._onDidChangeTreeData.event;

    constructor(private context: vscode.ExtensionContext) {}

    refresh(): void { this._onDidChangeTreeData.fire(); }
    getTreeItem(element: InfoItem): vscode.TreeItem { return element; }

    async getChildren(element?: InfoItem): Promise<InfoItem[]> {
        if (element) return [];
        const tag = this.context.globalState.get<string>("vext.compilerVersion") || "Unknown";
        return [
            new InfoItem("Status", "Connected", vscode.TreeItemCollapsibleState.None, "$(check)"),
            new InfoItem("Version", tag, vscode.TreeItemCollapsibleState.None, "$(tag)"),
            new InfoItem("Compiler", "Installed", vscode.TreeItemCollapsibleState.None, "$(file-binary)"),
        ];
    }
}

class VextSymbolsProvider implements vscode.TreeDataProvider<InfoItem> {
    private _onDidChangeTreeData: vscode.EventEmitter<InfoItem | undefined | void> = new vscode.EventEmitter<InfoItem | undefined | void>();
    readonly onDidChangeTreeData: vscode.Event<InfoItem | undefined | void> = this._onDidChangeTreeData.event;

    refresh(): void { this._onDidChangeTreeData.fire(); }
    getTreeItem(element: InfoItem): vscode.TreeItem { return element; }

    async getChildren(element?: InfoItem): Promise<InfoItem[]> {
        if (element || !client) return [];
        const editor = vscode.window.activeTextEditor;
        if (!editor || (editor.document.languageId !== "vext" && editor.document.languageId !== "vxt")) return [];

        try {
            const symbols = await client.sendRequest<TokenInfo[]>('vext/getSymbols', { uri: editor.document.uri.toString() });
            return symbols.map(s => {
                const icon = s.type === "function" ? "$(symbol-function)" : "$(symbol-variable)";
                const name = editor.document.getText(new vscode.Range(s.line, s.startColumn, s.line, s.endColumn));
                return new InfoItem(name, s.type, vscode.TreeItemCollapsibleState.None, icon);
            });
        } catch {
            return [];
        }
    }
}

class InfoItem extends vscode.TreeItem {
    constructor(
        public readonly label: string,
        private value: string,
        public readonly collapsibleState: vscode.TreeItemCollapsibleState,
        public readonly icon?: string
    ) {
        super(label, collapsibleState);
        this.description = this.value;
        if (icon) this.iconPath = new vscode.ThemeIcon(icon.replace(/\$|\(|\)/g, ''));
    }
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) return undefined;
  return client.stop();
}