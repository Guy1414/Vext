import * as fs from "fs";
import * as path from "path";
import fetch from "node-fetch";
import * as vscode from "vscode";

type GitHubRelease = {
    tag_name?: string;
    assets?: Array<{ name: string; browser_download_url: string }>;
};

const OWNER = "guy1414";
const REPO = "Vext";
const CACHE_KEY = "vext.latestVersion";
const TIMESTAMP_KEY = "vext.lastCheckTime";
const CHECK_INTERVAL = 24 * 60 * 60 * 1000; // 24 hours in milliseconds

export async function ensureCompiler(context: vscode.ExtensionContext): Promise<string> {
    const storagePath = context.globalStorageUri.fsPath;
    const compilerDir = path.join(storagePath, "compiler");
    await fs.promises.mkdir(compilerDir, { recursive: true });

    const now = Date.now();
    const lastCheck = context.globalState.get<number>(TIMESTAMP_KEY) || 0;
    let tag = context.globalState.get<string>(CACHE_KEY);

    // 1. Determine if we need to fetch from GitHub
    if (!tag || (now - lastCheck) > CHECK_INTERVAL) {
        try {
            const releaseRes = await fetch(`https://api.github.com/repos/${OWNER}/${REPO}/releases/latest`);
            if (!releaseRes.ok) throw new Error("GitHub API unreachable");

            const release = await releaseRes.json() as GitHubRelease;
            if (!release?.tag_name) {
                throw new Error("Invalid GitHub release response");
            }
            tag = release.tag_name;

            // Update cache
            await context.globalState.update(CACHE_KEY, tag);
            await context.globalState.update(TIMESTAMP_KEY, now);
        } catch (err) {
            console.error("Vext: Failed to check for updates, falling back to cache/local.", err);
            // If we have no tag even in cache, we can't proceed
            if (!tag) throw new Error("Could not determine Vext version.");
        }
    }

    const targetExeName = `Vext.LSP-${tag}.exe`;
    const exePath = path.join(compilerDir, targetExeName);

    // 2. Cleanup old versions (remove any Vext.LSP-*.exe that IS NOT the current target)
    const files = await fs.promises.readdir(compilerDir).catch(() => []);
    for (const file of files) {
        if (file.startsWith("Vext.LSP-") && file.endsWith(".exe") && file !== targetExeName) {
            try {
                await fs.promises.unlink(path.join(compilerDir, file));
            } catch (err) { /* ignore cleanup errors */ }
        }
    }

    // 3. Check local existence
    if (fs.existsSync(exePath)) {
        return exePath;
    }

    // 4. Download if missing (this happens if cache is updated or file was deleted)
    // We need the download URL, so we have to fetch release info if we don't have it
    const releaseRes = await fetch(`https://api.github.com/repos/${OWNER}/${REPO}/releases/tags/${tag}`);
    if (!releaseRes.ok) throw new Error(`Could not fetch release info for tag ${tag}`);
    
    const release = await releaseRes.json() as GitHubRelease;
    
    if (!Array.isArray(release.assets)) {
        throw new Error("Invalid release assets response");
    }

    // Look for the LSP exe specifically (should be named Vext.LSP-v{tag}.exe or similar)
    const asset = release.assets.find((a: any) => 
        a.name.includes("Vext.LSP") && a.name.endsWith(".exe")
    );
    if (!asset) throw new Error(`LSP binary not found for version ${tag}. Available assets: ${release.assets.map((a: any) => a.name).join(", ")}`);

    await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: `Installing Vext Language Server (${tag})...`,
        cancellable: false
    }, async () => {
        const downloadRes = await fetch(asset.browser_download_url);
        const downloadStream = downloadRes.body;
        if (!downloadStream) {
            throw new Error("Download stream unavailable");
        }

        const fileStream = fs.createWriteStream(exePath);
        
        await new Promise((resolve, reject) => {
            downloadStream.pipe(fileStream);
            downloadStream.on("error", (err) => {
                fs.promises.unlink(exePath).catch(() => {});
                reject(err);
            });
            fileStream.on("error", (err) => {
                reject(err);
            });
            fileStream.on("finish", resolve);
        });
    });

    return exePath;
}
