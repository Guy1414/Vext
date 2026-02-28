import { spawn, ChildProcessWithoutNullStreams } from "child_process";
import * as path from "path";
import * as fs from "fs";
import * as readline from "readline";

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason?: any) => void;
}

interface QueueItem {
  payload: Omit<any, "id">;
  timeoutMs: number;
  resolve: (v: any) => void;
  reject: (r?: any) => void;
}

export class CompilerBridge {
  private proc: ChildProcessWithoutNullStreams;
  private nextId = 1;
  private pending = new Map<number, PendingRequest>();

  private queue: QueueItem[] = [];
  private busy = false;

  constructor() {
    const compilerDir = path.resolve(__dirname, "..", "..", "compiler");
    const dllPath = path.resolve(compilerDir, "Vext.LSP.dll");
    const exePath = path.resolve(compilerDir, "Vext.LSP.exe");
    const devProjPath = path.resolve(__dirname, "..", "..", "..", "..", "Vext.LSP", "Vext.LSP.csproj");

    if (fs.existsSync(devProjPath)) {
      // Dev mode: use dotnet run to avoid Application Control blocks in the extension folder
      console.log(`[compiler] Dev environment detected. Starting via: ${devProjPath}`);
      this.proc = spawn("dotnet", ["run", "--project", devProjPath, "--", "--lsp"], {
        windowsHide: true,
        shell: false,
      });
    } else if (fs.existsSync(dllPath)) {
      // Packaged mode (DLL): use dotnet <dll>
      console.log(`[compiler] Starting via DLL: ${dllPath}`);
      this.proc = spawn("dotnet", [dllPath], {
        windowsHide: true,
        shell: false,
      });
    } else if (fs.existsSync(exePath)) {
      // Packaged mode (EXE): use the signed executable
      console.log(`[compiler] Starting via EXE: ${exePath}`);
      this.proc = spawn(exePath, ["--lsp"], {
        windowsHide: true,
        shell: false,
      });
    } else {
      throw new Error(`Compiler missing in: ${compilerDir}. Please build Vext.LSP.`);
    }

    // Use readline to safely parse JSON lines
    readline.createInterface({ input: this.proc.stdout }).on("line", (line) => this.onStdout(line));

    let stderrLog = "";
    this.proc.stderr.on("data", (d) => {
      const msg = d.toString();
      console.error("STDERR:", msg);
      stderrLog += msg;
      // Keep stderrLog from growing infinitely
      if (stderrLog.length > 5000) {
        stderrLog = stderrLog.substring(stderrLog.length - 5000);
      }
    });

    this.proc.on("error", (err) => {
      console.error("[compiler] process error:", err);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });

    this.proc.on("exit", (code, signal) => {
      const err = new Error(`Compiler exited (code=${code}, signal=${signal})\nStderr: ${stderrLog}`);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });

    this.proc.on("close", () => {
      const err = new Error(`Compiler process closed\nStderr: ${stderrLog}`);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });
  }

  private onStdout(line: string) {
    if (!line.trim()) return;

    let msg: any;
    try {
      msg = JSON.parse(line);
    } catch (err) {
      console.error("[compiler] failed to parse JSON from stdout:", line, err);
      return;
    }

    const pending = this.pending.get(msg.id);
    if (!pending) return;

    this.pending.delete(msg.id);
    pending.resolve(msg.result);

    // Process next queued request
    this.busy = false;
    this.processNext();
  }

  private processNext() {
    if (this.busy || this.queue.length === 0) return;

    const item = this.queue.shift()!;
    this.busy = true;

    const id = this.nextId++;
    const msg = JSON.stringify({ id, ...item.payload }) + "\n";

    if (item.payload.type === "compile") {
      const preview = item.payload.code.substring(0, 100).replace(/\n/g, "\\n");
      console.log(`[compiler] Sending request ${id} (compile): ${preview}...`);
    } else {
      console.log(`[compiler] Sending request ${id} (${item.payload.type})`);
    }

    const timer = setTimeout(() => {
      if (this.pending.has(id)) {
        this.pending.delete(id);
        item.reject(new Error("Compiler request timed out"));
        this.busy = false;
        this.processNext();
      }
    }, item.timeoutMs);

    this.pending.set(id, {
      resolve: (v) => {
        clearTimeout(timer);
        item.resolve(v);
      },
      reject: (r) => {
        clearTimeout(timer);
        item.reject(r);
      },
    });

    try {
      this.proc.stdin.write(msg, (err) => {
        if (err) {
          this.pending.delete(id);
          clearTimeout(timer);
          item.reject(err instanceof Error ? err : new Error(String(err)));
          this.busy = false;
          this.processNext();
        }
      });
    } catch (err) {
      this.pending.delete(id);
      clearTimeout(timer);
      item.reject(err instanceof Error ? err : new Error(String(err)));
      this.busy = false;
      this.processNext();
    }
  }

  request<T>(payload: Omit<any, "id">, timeoutMs = 10000): Promise<T> {
    if (!this.proc || this.proc.exitCode !== null) {
      return Promise.reject(new Error("Compiler process has already exited"));
    }

    return new Promise<T>((resolve, reject) => {
      if (payload.type === "compile" && !payload.run) {
        for (let i = this.queue.length - 1; i >= 0; i--) {
          if (this.queue[i].payload.type === "compile" && !this.queue[i].payload.run) {
            const old = this.queue.splice(i, 1)[0];
            old.reject(new Error("Superseded by newer compile request"));
          }
        }
      }

      this.queue.push({ payload, timeoutMs, resolve, reject });
      this.processNext();
    });
  }

  dispose() {
    if (!this.proc.killed) this.proc.kill();
    try {
      this.proc.stdin.end();
    } catch { }
  }
}
