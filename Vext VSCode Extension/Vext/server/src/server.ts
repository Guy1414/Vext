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
  CompletionItem,
  CompletionItemKind
} from "vscode-languageserver/node";
import { InsertTextFormat, RequestType } from 'vscode-languageserver';
import { TextDocument } from "vscode-languageserver-textdocument";
import { spawn } from "child_process";
import * as path from "path";
import * as fs from 'fs';
import { CompilerBridge } from "./compilerBridge";

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
  stdout: string;
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

interface KeywordInfo {
  label: string;
  kind: typeof CompletionItemKind.Keyword;
  insertTextFormat: 2;
  insertText: string;
}

interface CompileResult {
  success: boolean;
  errors: ErrorInfo[];
  output?: RunOutput;
  tokens?: TokenInfo[];
  keywords: KeywordInfo[];
}

const compileCounter = new Map<string, number>();

const compiler = new CompilerBridge();

namespace RunCodeRequest {
  export const type = new RequestType<{ code: string }, RunOutput, void>('vext/runCode');
}

// --- Compile helper ---
function compileVextFromText(code: string, run = false): Promise<CompileResult> {
  return compiler.request<CompileResult>({
    type: "compile",
    run,
    code
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
  }
}

// --- LSP Handlers ---
connection.onInitialize((_params: InitializeParams) => {
  return <InitializeResult>{
    capabilities: {
      textDocumentSync: TextDocumentSyncKind.Incremental,
      completionProvider: {
        resolveProvider: false,
        triggerCharacters: ["."]
      },
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

const compileTimeouts = new Map<string, NodeJS.Timeout>();

documents.onDidChangeContent((change) => {
  const uri = change.document.uri;

  // clear any previous debounce timeout
  if (compileTimeouts.has(uri)) clearTimeout(compileTimeouts.get(uri)!);

  // set a new debounce (e.g., 200ms after last keystroke)
  compileTimeouts.set(uri, setTimeout(async () => {
    const code = change.document.getText();

    // increment document compile counter
    const counter = (compileCounter.get(uri) ?? 0) + 1;
    compileCounter.set(uri, counter);

    try {
      const result = await compileVextFromText(code, false);

      // only act if this is the latest compile
      if (compileCounter.get(uri) !== counter) return;

      const diagnostics = errorsToDiagnostics(result.errors ?? []);
      connection.sendDiagnostics({ uri, diagnostics });
    } catch (err) {
      // clear diagnostics on compiler error
      connection.sendDiagnostics({ uri, diagnostics: [] });
      console.error("compile error", err);
      connection.window.showErrorMessage("Compiler error: " + err);
    }
  }, 200)); // 200ms debounce delay
});

documents.onDidClose((e) => {
  const uri = e.document.uri;
  compileCounter.delete(uri);
  if (compileTimeouts.has(uri)) {
    clearTimeout(compileTimeouts.get(uri)!);
    compileTimeouts.delete(uri);
  }
});

connection.onRequest(RunCodeRequest.type, async (params) => {
  try {
    const result = await compileVextFromText(params.code, true);
    return result.output ?? { time: 0, finalState: [] };
  } catch (err) {
    throw new Error(err as string);
  }
});

connection.languages.semanticTokens.on(async (params) => {
  try {
    const doc = documents.get(params.textDocument.uri);
    if (!doc) return { data: [] };

    const builder = new SemanticTokensBuilder();
    const code = doc.getText();

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

      const length = t.endColumn - t.startColumn;
      if (length <= 0) continue;

      builder.push(
        t.line,
        t.startColumn,
        length,
        tokenType,
        modifiers
      );
    }

    return builder.build();
  } catch (err) {
    console.error("Error building semantic tokens:", err);
    return { data: [] };
  }
});

connection.onCompletion(async (params) => {
  const doc = documents.get(params.textDocument.uri);
  if (!doc) return [];

  const code = doc.getText();
  try {
    const result = await compileVextFromText(code, false);
    if (!result) return [];

    const items: CompletionItem[] = [];
    const range = getWordRangeAtPosition(doc, params.position);

    // 1. Keywords
    const keywords = result.keywords;

    for (const k of keywords) {
      items.push({
        label: k.label,
        kind: k.kind,
        insertTextFormat: InsertTextFormat.Snippet,
        textEdit: {
          range,
          newText: k.insertText
        },
        sortText: "0" + k.label
      });
    }

    // 2. Variables / Functions from semantic tokens
    if (result.tokens) {
      const fullText = doc.getText();
      for (const t of result.tokens) {
        if (!t.isDeclaration) continue;
        const name = extractIdentifierFromDocument(doc, fullText, t);

        if (t.type === "variable") {
          items.push({
            label: name,
            kind: CompletionItemKind.Variable,
            textEdit: {
              range,
              newText: name
            },
            sortText: "1" + name
          });
        }

        if (t.type === "function") {
          items.push({
            label: name,
            kind: CompletionItemKind.Function,
            textEdit: {
              range,
              newText: name
            },
            sortText: "1" + name
          });
        }
      }
    }

    const unique = new Map<string, CompletionItem>();

    for (const item of items) {
      unique.set(item.label, item);
    }

    return Array.from(unique.values());
  } catch {
    return [];
  }
});

function getWordRangeAtPosition(doc: TextDocument, position: Position): Range {
  const text = doc.getText();
  const offset = doc.offsetAt(position);

  let start = offset;
  let end = offset;

  while (start > 0 && /\w/.test(text[start - 1])) start--;
  while (end < text.length && /\w/.test(text[end])) end++;

  return Range.create(doc.positionAt(start), doc.positionAt(end));
}

function extractIdentifierFromDocument(doc: TextDocument, fullText: string, token: TokenInfo): string {
  const startOffset = doc.offsetAt(Position.create(token.line, token.startColumn));
  const endOffset = doc.offsetAt(Position.create(token.line, token.endColumn));
  const name = fullText.substring(startOffset, endOffset);
  return name;
}

connection.onShutdown(() => {
  try { compiler.dispose(); } catch {}
});

connection.onExit(() => {
  try { compiler.dispose(); } catch {}
});

// --- Listen ---
documents.listen(connection);
connection.listen();
