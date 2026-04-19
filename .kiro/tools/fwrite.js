#!/usr/bin/env node
/**
 * fwrite.js — Reliable file write/append tool for large content.
 * 
 * Usage:
 *   node .kiro/tools/fwrite.js write  <filepath>  (reads stdin, overwrites file)
 *   node .kiro/tools/fwrite.js append <filepath>  (reads stdin, appends to file)
 *   node .kiro/tools/fwrite.js replace <filepath> <oldFile> <newFile>
 *       (reads oldFile and newFile as temp files containing the old/new strings,
 *        performs a single replacement in filepath, deletes temp files)
 *
 * Designed to be called from executePwsh with content piped via stdin or temp files.
 * Handles arbitrarily large content without the size limits of built-in IDE tools.
 *
 * Examples:
 *   # Write file from heredoc:
 *   @"
 *   line1
 *   line2
 *   "@ | node .kiro/tools/fwrite.js write path/to/file.md
 *
 *   # Append:
 *   @"
 *   extra content
 *   "@ | node .kiro/tools/fwrite.js append path/to/file.md
 *
 *   # Replace (using temp files for old/new strings):
 *   "old text" | Out-File -Encoding utf8 _old.tmp
 *   "new text" | Out-File -Encoding utf8 _new.tmp
 *   node .kiro/tools/fwrite.js replace path/to/file.md _old.tmp _new.tmp
 */

const fs = require('fs');
const path = require('path');

const [,, mode, filePath, ...rest] = process.argv;

if (!mode || !filePath) {
    console.error('Usage: fwrite.js <write|append|replace> <filepath> [oldFile newFile]');
    process.exit(1);
}

function readStdin() {
    return new Promise((resolve, reject) => {
        const chunks = [];
        process.stdin.setEncoding('utf8');
        process.stdin.on('data', chunk => chunks.push(chunk));
        process.stdin.on('end', () => resolve(chunks.join('')));
        process.stdin.on('error', reject);
    });
}

async function main() {
    try {
        if (mode === 'write') {
            const content = await readStdin();
            fs.mkdirSync(path.dirname(filePath), { recursive: true });
            fs.writeFileSync(filePath, content, 'utf8');
            console.log(`Wrote ${content.length} chars to ${filePath}`);

        } else if (mode === 'append') {
            const content = await readStdin();
            fs.appendFileSync(filePath, content, 'utf8');
            console.log(`Appended ${content.length} chars to ${filePath}`);

        } else if (mode === 'replace') {
            const [oldFile, newFile] = rest;
            if (!oldFile || !newFile) {
                console.error('replace mode requires: fwrite.js replace <file> <oldFile> <newFile>');
                process.exit(1);
            }
            const fileContent = fs.readFileSync(filePath, 'utf8');
            const oldStr = fs.readFileSync(oldFile, 'utf8').replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').trimEnd();
            const newStr = fs.readFileSync(newFile, 'utf8').replace(/^\uFEFF/, '').replace(/\r\n/g, '\n').trimEnd();

            const idx = fileContent.indexOf(oldStr);
            if (idx === -1) {
                console.error(`Old string not found in ${filePath} (${oldStr.length} chars)`);
                process.exit(1);
            }
            // Check uniqueness
            const secondIdx = fileContent.indexOf(oldStr, idx + 1);
            if (secondIdx !== -1) {
                console.error(`Old string found multiple times in ${filePath}`);
                process.exit(1);
            }

            const result = fileContent.substring(0, idx) + newStr + fileContent.substring(idx + oldStr.length);
            fs.writeFileSync(filePath, result, 'utf8');

            // Clean up temp files
            try { fs.unlinkSync(oldFile); } catch(e) {}
            try { fs.unlinkSync(newFile); } catch(e) {}

            console.log(`Replaced ${oldStr.length} chars with ${newStr.length} chars in ${filePath}`);

        } else {
            console.error(`Unknown mode: ${mode}. Use write, append, or replace.`);
            process.exit(1);
        }
    } catch (err) {
        console.error(`Error: ${err.message}`);
        process.exit(1);
    }
}

main();
