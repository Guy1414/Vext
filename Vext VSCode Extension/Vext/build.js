const { context, build: _build } = require('esbuild');

const isWatch = process.argv.includes('--watch');

async function build() {
    const contextSettings = [
        {
            entryPoints: ['client/src/extension.ts'],
            bundle: true,
            platform: 'node',
            target: 'node20',
            outfile: 'client/out/extension.js',
            external: ['vscode'],
            minify: !isWatch,
        },
        {
            entryPoints: ['server/src/server.ts'],
            bundle: true,
            platform: 'node',
            target: 'node20',
            outfile: 'server/out/server.js',
            minify: !isWatch,
        }
    ];

    if (isWatch) {
        console.log('Starting build in watch mode...');
        for (const settings of contextSettings) {
            const ctx = await context(settings);
            await ctx.watch();
        }
    } else {
        await Promise.all(contextSettings.map(settings => _build(settings)));
        console.log('Build complete.');
    }
}

build().catch((err) => {
    console.error(err);
    process.exit(1);
});