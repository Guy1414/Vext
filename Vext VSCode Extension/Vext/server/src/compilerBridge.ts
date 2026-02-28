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
    const exePath = path.resolve(__dirname, "..", "..", "compiler", "Vext.LSP.exe");

    if (!fs.existsSync(exePath)) {
      throw new Error(`Compiler missing at: ${exePath}`);
    }

    this.proc = spawn(exePath, ["--lsp"], {
      windowsHide: true,
      shell: false,
    });

    // Use readline to safely parse JSON lines
    readline.createInterface({ input: this.proc.stdout }).on("line", (line) => this.onStdout(line));
    this.proc.stderr.on("data", (d) => console.error("[compiler]", d.toString()));

    this.proc.on("error", (err) => {
      console.error("[compiler] process error:", err);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });

    this.proc.on("exit", (code, signal) => {
      const err = new Error(`Compiler exited (code=${code}, signal=${signal})`);
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });

    this.proc.on("close", () => {
      const err = new Error("Compiler process closed");
      for (const p of this.pending.values()) p.reject(err);
      this.pending.clear();
    });

    this.proc.stderr.on("data", (d) => console.error("STDERR:", d.toString()));
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
      this.queue.push({ payload, timeoutMs, resolve, reject });
      this.processNext();
    });
  }

  dispose() {
    if (!this.proc.killed) this.proc.kill();
    try {
      this.proc.stdin.end();
    } catch {}
  }
}
