const { context, build: _build } = require('esbuild');

const isWatch = process.argv.includes('--watch');

async function build() {
    const logPlugin = {
        name: 'log-plugin',
        setup(build) {
            build.onStart(() => {
                console.log('Build started...');
            });
            build.onEnd((result) => {
                if (result.errors.length > 0) {
                    console.log(`Build failed with ${result.errors.length} errors.`);
                } else {
                    console.log('Build finished.');
                }
            });
        },
    };

    const contextSettings = [
        {
            entryPoints: ['client/src/extension.ts'],
            bundle: true,
            platform: 'node',
            target: 'node20',
            outfile: 'client/out/extension.js',
            external: ['vscode'],
            minify: !isWatch,
            plugins: [logPlugin],
        },
        {
            entryPoints: ['server/src/server.ts'],
            bundle: true,
            platform: 'node',
            target: 'node20',
            outfile: 'server/out/server.js',
            minify: !isWatch,
            plugins: [logPlugin],
        }
    ];

    if (isWatch) {
        // No need for a global log here, the plugin handles it per context
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