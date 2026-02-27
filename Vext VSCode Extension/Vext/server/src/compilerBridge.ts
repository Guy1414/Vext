import { spawn, ChildProcessWithoutNullStreams } from "child_process";
import * as path from "path";
import * as fs from "fs";

interface PendingRequest {
  resolve: (value: any) => void;
  reject: (reason?: any) => void;
}

export class CompilerBridge {
  private proc: ChildProcessWithoutNullStreams;
  private buffer = "";
  private nextId = 1;
  private pending = new Map<number, PendingRequest>();

  constructor() {
    const exePath = path.resolve(__dirname, "..", "..", "compiler", "Vext.LSP.exe");

    if (!fs.existsSync(exePath)) {
      throw new Error(`Compiler missing at: ${exePath}`);
    }

    this.proc = spawn(exePath, ["--lsp"], {
      windowsHide: true,
      shell: false,
    });

    this.proc.stdout.on("data", (d) => this.onStdout(d.toString()));
    this.proc.stderr.on("data", (d) => console.error("[compiler]", d.toString()));

    this.proc.on("exit", (code) => {
      for (const p of this.pending.values()) {
        p.reject(`Compiler exited (${code})`);
      }
      this.pending.clear();
    });
  }

  private onStdout(chunk: string) {
    this.buffer += chunk;

    let idx;
    while ((idx = this.buffer.indexOf("\n")) !== -1) {
      const line = this.buffer.slice(0, idx).trim();
      this.buffer = this.buffer.slice(idx + 1);

      if (!line) continue;

      const msg = JSON.parse(line);
      const pending = this.pending.get(msg.id);
      if (!pending) continue;

      this.pending.delete(msg.id);
      pending.resolve(msg.result);
    }
  }

  request<T>(payload: Omit<any, "id">): Promise<T> {
    const id = this.nextId++;

    const msg = JSON.stringify({ id, ...payload }) + "\n";

    return new Promise<T>((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.proc.stdin.write(msg);
    });
  }

  dispose() {
    this.proc.kill();
  }
}
