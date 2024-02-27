import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';
import { nodeResolve } from '@rollup/plugin-node-resolve';

const config = {
    input: 'typescript/main.ts',
    output: {
        format: 'es',
        plugins: []
    },
    plugins: [typescript({
        noEmitOnError: true,
    }), nodeResolve()]
};

if (process.env.BUILD === 'Debug') {
    config.output.sourcemap = 'inline';
} else {
    config.output.plugins.push(terser());
}

export default config;
