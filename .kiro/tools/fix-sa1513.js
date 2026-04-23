#!/usr/bin/env node
/**
 * fix-sa1513.js — Fix SA1513 (closing brace must be followed by blank line)
 * 
 * A closing brace } must be followed by a blank line, EXCEPT when followed by:
 * - Another closing brace }
 * - else, catch, finally
 * - A comma (object initializer)
 * - A semicolon
 * - while (do-while)
 * 
 * This adds blank lines after closing braces where needed.
 */
const fs = require('fs');
const path = require('path');

const log = fs.readFileSync('_build.log', 'utf8');
const fileWarnings = {};

for (const line of log.split('\n')) {
    if (!line.includes('warning SA1513')) continue;
    const match = line.match(/^(.+?\.cs)\((\d+),/);
    if (!match) continue;
    const fp = match[1].trim();
    if (fp.includes('.Designer.cs')) continue;
    const ln = parseInt(match[2], 10);
    if (!fileWarnings[fp]) fileWarnings[fp] = new Set();
    fileWarnings[fp].add(ln);
}

console.log(`Files to process: ${Object.keys(fileWarnings).length}`);
let totalFixed = 0;

for (const [filePath, warnLines] of Object.entries(fileWarnings)) {
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    let changed = false;
    
    // Process in reverse order
    const sorted = [...warnLines].sort((a, b) => b - a);
    
    for (const warnLine of sorted) {
        const idx = warnLine - 1;
        if (idx < 0 || idx >= lines.length) continue;
        
        const line = lines[idx];
        const trimmed = line.trim();
        
        // This line should have a closing brace
        if (!trimmed.includes('}')) continue;
        
        // Check the next non-empty line
        let nextIdx = idx + 1;
        if (nextIdx >= lines.length) continue;
        
        const nextLine = lines[nextIdx].trim();
        
        // Don't add blank line if next line is }, else, catch, finally, etc.
        if (!nextLine || nextLine.startsWith('}') || nextLine.startsWith('else') || 
            nextLine.startsWith('catch') || nextLine.startsWith('finally') ||
            nextLine.startsWith(',') || nextLine.startsWith(');')) continue;
        
        // Add a blank line
        lines.splice(idx + 1, 0, '');
        changed = true;
        totalFixed++;
    }
    
    if (changed) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed ${sorted.length} in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}`);
