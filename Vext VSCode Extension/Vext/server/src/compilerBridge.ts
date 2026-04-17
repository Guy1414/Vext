import { spawn, ChildProcessWithoutNullStreams } from "child_process";
import * as path from "path";
import * as fs from "fs";
import * as readline from "readline";

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason?: any) => void;
  id: number;
}

interface QueueItem {
  payload: Omit<any, "id">;
  timeoutMs: number;
  resolve: (v: any) => void;
  reject: (r?: any) => void;
}

export class CompilerBridge {
  private proc: ChildProcessWithoutNullStreams | null = null;
  private nextId = 1;
  private pending = new Map<number, PendingRequest>();
  private currentTimer?: NodeJS.Timeout;
  private currentTimeoutMs: number = 20000;

  private queue: QueueItem[] = [];
  private busy = false;
  private waitingForInput = false;
  private onNotification?: (method: string, params: any) => void;

  public setNotificationHandler(handler: (method: string, params: any) => void) {
    this.onNotification = handler;
  }

  constructor() {
    this.spawn();
  }

  private spawn() {
    if (this.proc && this.proc.exitCode === null) return;

    console.error(`[compiler] Spawning Vext compiler...`);
    
    const compilerPathEnv = process.env.VEXT_COMPILER_PATH;
    const compilerDir = path.resolve(__dirname, "..", "..", "compiler");
    const dllPath = path.resolve(compilerDir, "Vext.LSP.dll");
    const exePath = path.resolve(compilerDir, "Vext.LSP.exe");
    const devProjPath = path.resolve(__dirname, "..", "..", "..", "..", "Vext.LSP", "Vext.LSP.csproj");

    if (compilerPathEnv && fs.existsSync(compilerPathEnv)) {
      this.proc = spawn(compilerPathEnv, [], { windowsHide: true, shell: false });
    } else if (fs.existsSync(devProjPath)) {
      this.proc = spawn("dotnet", ["run", "--project", devProjPath, "--", "--lsp"], { windowsHide: true, shell: false });
    } else if (fs.existsSync(dllPath)) {
      this.proc = spawn("dotnet", [dllPath], { windowsHide: true, shell: false });
    } else if (fs.existsSync(exePath)) {
      this.proc = spawn(exePath, [], { windowsHide: true, shell: false });
    } else {
      throw new Error(`Compiler not found. Checked: ${compilerPathEnv}, ${exePath}, ${dllPath}`);
    }

    readline.createInterface({ input: this.proc.stdout }).on("line", (line) => this.onStdout(line));

    let stderrLog = "";
    this.proc.stderr.on("data", (d) => {
      stderrLog = (stderrLog + d.toString()).slice(-5000);
      console.error("STDERR:", d.toString());
    });

    const cleanup = (reason: string) => {
      console.error(`[compiler] Process ${reason}`);
      this.proc = null;
      this.waitingForInput = false;
      this.busy = false;
      const err = new Error(`Compiler ${reason}\nStderr: ${stderrLog}`);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
      this.processNext(); // Try to restart if there's a queue
    };

    this.proc.on("error", (err) => cleanup(`error: ${err.message}`));
    this.proc.on("exit", (code, sig) => cleanup(`exited (code=${code}, sig=${sig})`));
  }

  private onStdout(line: string) {
    if (!line.trim()) return;

    let msg: any;
    try {
      msg = JSON.parse(line);
    } catch (err) {
      console.error("[compiler] JSON parse error:", line);
      return;
    }

    // Handle Notifications (no ID)
    if (msg.id === undefined || msg.id === 0) {
      if (msg.method === "vext/needInput") {
        console.error("[compiler] Input requested, suspending timeout.");
        this.waitingForInput = true;
        if (this.currentTimer) {
          clearTimeout(this.currentTimer);
          this.currentTimer = undefined;
        }
      }
      if (this.onNotification) this.onNotification(msg.method, msg.params);
      return;
    }

    // Handle Responses
    const pending = this.pending.get(msg.id);
    if (!pending) return;

    this.pending.delete(msg.id);
    if (this.currentTimer) {
      clearTimeout(this.currentTimer);
      this.currentTimer = undefined;
    }

    this.waitingForInput = false;
    this.busy = false;
    pending.resolve(msg.result);
    this.processNext();
  }

  private processNext() {
    // If waiting for input, don't send new requests (avoid overlapping)
    if (this.busy || this.waitingForInput || this.queue.length === 0) return;

    if (!this.proc || this.proc.exitCode !== null) {
      try {
        this.spawn();
      } catch (err) {
        const item = this.queue.shift();
        item?.reject(err);
        return;
      }
    }

    const item = this.queue.shift()!;
    this.busy = true;
    const id = this.nextId++;
    const msg = JSON.stringify({ id, ...item.payload }) + "\n";

    this.currentTimeoutMs = item.timeoutMs;
    this.currentTimer = setTimeout(() => {
      if (this.pending.has(id)) {
        console.error(`[compiler] Request ${id} timed out after ${this.currentTimeoutMs}ms`);
        this.pending.delete(id);
        this.busy = false;
        item.reject(new Error("Compiler request timed out"));
        this.processNext();
      }
    }, item.timeoutMs);

    this.pending.set(id, { resolve: item.resolve, reject: item.reject, id });

    try {
      this.proc!.stdin.write(msg, (err) => {
        if (err) {
          this.pending.delete(id);
          this.busy = false;
          item.reject(err);
          this.processNext();
        }
      });
    } catch (err) {
      this.pending.delete(id);
      this.busy = false;
      item.reject(err);
      this.processNext();
    }
  }

  request<T>(payload: any, timeoutMs?: number): Promise<T> {
    const isRun = payload.type === "compile" && payload.run;
    const tMs = timeoutMs ?? (isRun ? 300000 : this.currentTimeoutMs); // 5 min for runs

    return new Promise<T>((resolve, reject) => {
      // Supersede background compiles
      if (payload.type === "compile" && !payload.run) {
        for (let i = this.queue.length - 1; i >= 0; i--) {
          if (this.queue[i].payload.type === "compile" && !this.queue[i].payload.run) {
            this.queue.splice(i, 1)[0].reject(new Error("Superseded"));
          }
        }
      }

      this.queue.push({ payload, timeoutMs: tMs, resolve, reject });
      this.processNext();
    });
  }

  notify(payload: any) {
    if (!this.proc || this.proc.exitCode !== null) return;

    // If submitting input, we expect the compiler to finish soon, so restart a normal timeout
    if (payload.method === "vext/submitInput" && this.waitingForInput) {
        console.error("[compiler] Input submitted, resuming timeout.");
        this.waitingForInput = false;
        // The active request is still in 'pending'. We need to restart its timer.
        const active = Array.from(this.pending.values())[0];
        if (active) {
            if (this.currentTimer) clearTimeout(this.currentTimer);
            this.currentTimer = setTimeout(() => {
                if (this.pending.has(active.id)) {
                    this.pending.delete(active.id);
                    this.busy = false;
                    active.reject(new Error("Compiler request timed out after input"));
                    this.processNext();
                }
            }, 30000); // Give it another 30s after input
        }
    }

    try {
      this.proc.stdin.write(JSON.stringify(payload) + "\n");
    } catch { }
  }

  dispose() {
    if (this.proc && !this.proc.killed) this.proc.kill();
  }
}
