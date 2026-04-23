#!/usr/bin/env node
/**
 * fix-formatting.js — Fix common formatting issues across all .cs files
 * 
 * Fixes:
 * - SA1028: Trailing whitespace
 * - SA1509: Opening brace preceded by blank line
 * 
 * Usage: node fix-formatting.js [--all]
 *   --all: Fix all .cs files (not just those in build log)
 *   default: Fix files mentioned in _build.log for SA1028/SA1509
 */
const fs = require('fs');
const path = require('path');

const mode = process.argv[2] || 'log';
let filesToFix = new Set();

if (mode === '--all') {
    // Find all .cs files recursively
    function findCs(dir) {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
            const full = path.join(dir, entry.name);
            if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules' && entry.name !== 'bin' && entry.name !== 'obj' && entry.name !== 'packages') {
                findCs(full);
            } else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.includes('.Designer.cs')) {
                filesToFix.add(full);
            }
        }
    }
    findCs('OE2EmpireTracker');
} else {
    // Read from build log - extract file paths from SA1028/SA1509/SA1513 warnings
    const log = fs.readFileSync('_build.log', 'utf8');
    for (const line of log.split('\n')) {
        if (/warning SA(1028|1509|1513|1504)/.test(line)) {
            // Extract file path - it's the first thing before the (line,col)
            const match = line.match(/^(.+?\.cs)\(/);
            if (match) {
                const absPath = match[1].trim();
                if (!absPath.includes('.Designer.cs') && fs.existsSync(absPath)) {
                    filesToFix.add(absPath);
                }
            }
        }
    }
}

console.log(`Processing ${filesToFix.size} files...`);
let totalChanges = 0;

for (const fullPath of filesToFix) {
    let content = fs.readFileSync(fullPath, 'utf8');
    const original = content;
    
    // Fix SA1028: Remove trailing whitespace from all lines
    content = content.replace(/[ \t]+(\r?\n)/g, '$1');
    content = content.replace(/[ \t]+$/, ''); // trailing whitespace at end of file
    
    // Fix SA1509: Remove blank lines before opening braces
    // A blank line followed by a line containing only whitespace+{
    content = content.replace(/(\r?\n)([ \t]*\r?\n)+([ \t]*\{\s*(?:\r?\n))/g, '$1$3');
    
    if (content !== original) {
        fs.writeFileSync(fullPath, content, 'utf8');
        totalChanges++;
        console.log(`Fixed: ${path.basename(fullPath)}`);
    }
}

console.log(`\nTotal files fixed: ${totalChanges}`);
