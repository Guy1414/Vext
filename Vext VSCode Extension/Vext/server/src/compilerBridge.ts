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
  private currentTimer?: NodeJS.Timeout;
  private currentTimeoutMs: number = 10000;

  private queue: QueueItem[] = [];
  private busy = false;
  private onNotification?: (method: string, params: any) => void;

  public setNotificationHandler(handler: (method: string, params: any) => void) {
    this.onNotification = handler;
  }

  constructor() {
    console.error(`[compiler] Starting Vext compiler`);
    
    // Try to get compiler path from environment variable (set by extension)
    const compilerPathEnv = process.env.VEXT_COMPILER_PATH;
    
    // Fallback paths for development
    const compilerDir = path.resolve(__dirname, "..", "..", "compiler");
    const dllPath = path.resolve(compilerDir, "Vext.LSP.dll");
    const exePath = path.resolve(compilerDir, "Vext.LSP.exe");
    const devProjPath = path.resolve(__dirname, "..", "..", "..", "..", "Vext.LSP", "Vext.LSP.csproj");

    if (compilerPathEnv && fs.existsSync(compilerPathEnv)) {
      console.error(`[compiler] Starting via downloaded EXE: ${compilerPathEnv}`);
      this.proc = spawn(compilerPathEnv, [], {
        windowsHide: true,
        shell: false,
      });
    } else if (fs.existsSync(devProjPath)) {
      console.error(`[compiler] Dev environment detected. Starting via: ${devProjPath}`);
      this.proc = spawn("dotnet", ["run", "--project", devProjPath, "--", "--lsp"], {
        windowsHide: true,
        shell: false,
      });
    } else if (fs.existsSync(dllPath)) {
      console.error(`[compiler] Starting via DLL: ${dllPath}`);
      this.proc = spawn("dotnet", [dllPath], {
        windowsHide: true,
        shell: false,
      });
    } else if (fs.existsSync(exePath)) {
      console.error(`[compiler] Starting via EXE: ${exePath}`);
      this.proc = spawn(exePath, [], {
        windowsHide: true,
        shell: false,
      });
    } else {
      throw new Error(`Compiler not found. Set VEXT_COMPILER_PATH or place exe in: ${compilerDir}`);
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
    if (!pending) {
      if (msg.method && this.onNotification) {
        if (msg.method === "vext/needInput" && this.currentTimer) {
          clearTimeout(this.currentTimer);
          // Just disable timeout while waiting for input, it will be reset by the next request
          this.currentTimer = undefined;
        }
        this.onNotification(msg.method, msg.params);
      }
      return;
    }

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
      console.error(`[compiler] Sending request ${id} (compile): ${preview}...`);
    } else {
      console.error(`[compiler] Sending request ${id} (${item.payload.type})`);
    }

    this.currentTimeoutMs = item.timeoutMs;
    this.currentTimer = setTimeout(() => {
      if (this.pending.has(id)) {
        this.pending.delete(id);
        item.reject(new Error("Compiler request timed out"));
        this.busy = false;
        this.processNext();
      }
    }, item.timeoutMs);

    this.pending.set(id, {
      resolve: (v) => {
        if (this.currentTimer) clearTimeout(this.currentTimer);
        this.currentTimer = undefined;
        item.resolve(v);
      },
      reject: (r) => {
        if (this.currentTimer) clearTimeout(this.currentTimer);
        this.currentTimer = undefined;
        item.reject(r);
      },
    });

    try {
      this.proc.stdin.write(msg, (err) => {
        if (err) {
          this.pending.delete(id);
          if (this.currentTimer) clearTimeout(this.currentTimer);
          this.currentTimer = undefined;
          item.reject(err instanceof Error ? err : new Error(String(err)));
          this.busy = false;
          this.processNext();
        }
      });
    } catch (err) {
      this.pending.delete(id);
      if (this.currentTimer) clearTimeout(this.currentTimer);
      this.currentTimer = undefined;
      item.reject(err instanceof Error ? err : new Error(String(err)));
      this.busy = false;
      this.processNext();
    }
  }

  request<T>(payload: Omit<any, "id">, timeoutMs = this.currentTimeoutMs): Promise<T> {
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

  notify(payload: any) {
    if (!this.proc || this.proc.exitCode !== null) return;
    const msg = JSON.stringify(payload) + "\n";
    try {
      this.proc.stdin.write(msg);
    } catch { }
  }

  dispose() {
    if (!this.proc.killed) this.proc.kill();
    try {
      this.proc.stdin.end();
    } catch { }
  }
}
