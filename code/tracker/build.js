const { readFileSync, writeFileSync, mkdirSync, existsSync } = require('fs');
const { minify } = require('terser');
const JavaScriptObfuscator = require('javascript-obfuscator');
const path = require('path');

const inputPath = 'track.js';
const outputPath = 'track.min.js';

async function processCode() {
    try {
        // 1. Read the source
        if (!existsSync(inputPath)) {
            console.error(`File not found: ${inputPath}`);
            return;
        }
        const sourceCode = readFileSync(inputPath, 'utf8');

        // 2. Minify with Terser
        const terserResult = await minify(sourceCode, {
            compress: { passes: 2, drop_console: true },
            mangle: { toplevel: true }
        });

        // 3. Obfuscate the minified code
        const obfuscationResult = JavaScriptObfuscator.obfuscate(terserResult.code, {
            compact: true,
            controlFlowFlattening: true, // Makes the logic flow hard to follow
            numbersToExpressions: true,  // Converts numbers to complex math (e.g., 10 becomes 2*5)
            simplify: true,
            stringArray: true,           // Hides strings/URLs in an array
            stringArrayThreshold: 0.75
        });

        // 4. Write output
        writeFileSync(outputPath, obfuscationResult.getObfuscatedCode());
        
        console.log('--- Build Complete ---');
        console.log(`Output saved to: ${outputPath}`);
        console.log(`Final size: ${obfuscationResult.getObfuscatedCode().length} bytes`);

    } catch (error) {
        console.error('Build failed:', error);
    }
}

processCode();