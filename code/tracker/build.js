const { readFileSync, writeFileSync, mkdirSync, existsSync } = require('fs');
const { minify } = require('terser');
const JavaScriptObfuscator = require('javascript-obfuscator');
const path = require('path');

const PRODUCTION_URL = 'https://api.witnes.io/v1/events';
const DEV_URL = 'http://localhost:7070/api/v1/events';

const inputPath = 'w.js';
const distDir = 'dist';
const prodOutput = path.join(distDir, 'w.min.js');
const devOutput = path.join('..', 'fe', 'public', 'w.js');

async function processCode() {
    try {
        if (!existsSync(inputPath)) {
            console.error(`File not found: ${inputPath}`);
            return;
        }

        const sourceCode = readFileSync(inputPath, 'utf8');

        // --- Production build (dist/w.min.js) ---
        if (!existsSync(distDir)) {
            mkdirSync(distDir);
        }

        const terserResult = await minify(sourceCode, {
            compress: { passes: 2, drop_console: true },
            mangle: { toplevel: true }
        });

        const obfuscationResult = JavaScriptObfuscator.obfuscate(terserResult.code, {
            compact: true,
            controlFlowFlattening: true,
            numbersToExpressions: true,
            simplify: true,
            stringArray: true,
            stringArrayThreshold: 0.75
        });

        writeFileSync(prodOutput, obfuscationResult.getObfuscatedCode());
        console.log(`Production: ${prodOutput} (${obfuscationResult.getObfuscatedCode().length} bytes)`);

        // --- Dev build (code/fe/public/w.js) ---
        const devCode = sourceCode.replace(PRODUCTION_URL, DEV_URL);
        writeFileSync(devOutput, devCode);
        console.log(`Dev:        ${devOutput}`);

        console.log('--- Build Complete ---');
    } catch (error) {
        console.error('Build failed:', error);
    }
}

processCode();
