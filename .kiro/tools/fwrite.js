#!/usr/bin/env node
/**
 * fwrite.js — Reliable file write/append/replace tool for large content.
 * 
 * Usage:
 *   node .kiro/tools/fwrite.js write  <filepath>  (reads stdin, overwrites file)
 *   node .kiro/tools/fwrite.js append <filepath>  (reads stdin, appends to file)
 *   node .kiro/tools/fwrite.js replace <filepath> <oldFile> <newFile>
 *       (reads oldFile and newFile as temp files containing the old/new strings,
 *        performs a single replacement in filepath, deletes temp files)
 *   node .kiro/tools/fwrite.js writefile <filepath> <srcFile>
 *       (reads srcFile content and writes to filepath, deletes srcFile)
 *   node .kiro/tools/fwrite.js appendfile <filepath> <srcFile>
 *       (reads srcFile content and appends to filepath, deletes srcFile)
 *
 * The writefile/appendfile modes avoid piping large content through stdin,
 * which can cause PowerShell pipe buffering issues where the shell appears
 * to hang. Write content to a temp file first, then use writefile/appendfile.
 *
 * Examples:
 *   # Write file from heredoc (small content):
 *   @"
 *   line1
 *   line2
 *   "@ | node .kiro/tools/fwrite.js write path/to/file.md
 *
 *   # Write file via temp file (large content — preferred):
 *   @"
 *   large content here
 *   "@ | Out-File -NoNewline -Encoding utf8 _content.tmp
 *   node .kiro/tools/fwrite.js writefile path/to/file.md _content.tmp
 *
 *   # Append via temp file:
 *   @"
 *   extra content
 *   "@ | Out-File -NoNewline -Encoding utf8 _content.tmp
 *   node .kiro/tools/fwrite.js appendfile path/to/file.md _content.tmp
 *
 *   # Replace (using temp files for old/new strings):
 *   @"
 *   old text
 *   "@ | Out-File -NoNewline -Encoding utf8 _old.tmp
 *   @"
 *   new text
 *   "@ | Out-File -NoNewline -Encoding utf8 _new.tmp
 *   node .kiro/tools/fwrite.js replace path/to/file.md _old.tmp _new.tmp
 */

const fs = require('fs');
const path = require('path');

const [,, mode, filePath, ...rest] = process.argv;

if (!mode || !filePath) {
    console.error('Usage: fwrite.js <write|append|replace|writefile|appendfile> <filepath> [args...]');
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

function readTempFile(tmpPath) {
    const content = fs.readFileSync(tmpPath, 'utf8').replace(/^\uFEFF/, '');
    try { fs.unlinkSync(tmpPath); } catch(e) {}
    return content;
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

        } else if (mode === 'writefile') {
            const [srcFile] = rest;
            if (!srcFile) {
                console.error('writefile mode requires: fwrite.js writefile <filepath> <srcFile>');
                process.exit(1);
            }
            const content = readTempFile(srcFile);
            fs.mkdirSync(path.dirname(filePath), { recursive: true });
            fs.writeFileSync(filePath, content, 'utf8');
            console.log(`Wrote ${content.length} chars to ${filePath}`);

        } else if (mode === 'appendfile') {
            const [srcFile] = rest;
            if (!srcFile) {
                console.error('appendfile mode requires: fwrite.js appendfile <filepath> <srcFile>');
                process.exit(1);
            }
            const content = readTempFile(srcFile);
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

            const normalizedContent = fileContent.replace(/\r\n/g, "\n");
            const idx = normalizedContent.indexOf(oldStr);
            if (idx === -1) {
                console.error(`Old string not found in ${filePath} (${oldStr.length} chars)`);
                process.exit(1);
            }
            const secondIdx = normalizedContent.indexOf(oldStr, idx + 1);
            if (secondIdx !== -1) {
                console.error(`Old string found multiple times in ${filePath}`);
                process.exit(1);
            }

            const result = normalizedContent.substring(0, idx) + newStr + normalizedContent.substring(idx + oldStr.length);
            const hasCRLF = fileContent.includes("\r\n");
            const finalResult = hasCRLF ? result.replace(/(?<!\r)\n/g, "\r\n") : result;
            fs.writeFileSync(filePath, finalResult, "utf8");

            try { fs.unlinkSync(oldFile); } catch(e) {}
            try { fs.unlinkSync(newFile); } catch(e) {}

            console.log(`Replaced ${oldStr.length} chars with ${newStr.length} chars in ${filePath}`);

        } else {
            console.error(`Unknown mode: ${mode}. Use write, append, replace, writefile, or appendfile.`);
            process.exit(1);
        }
    } catch (err) {
        console.error(`Error: ${err.message}`);
        process.exit(1);
    }
}

main();
