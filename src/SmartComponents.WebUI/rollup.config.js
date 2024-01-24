import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';

const config = {
    input: 'typescript/main.ts',
    output: {
        format: 'es',
        plugins: []
    },
    plugins: [typescript()]
};

if (process.env.BUILD === 'Debug') {
    config.output.sourcemap = 'inline';
} else {
    config.output.plugins.push(terser());
}

export default config;
