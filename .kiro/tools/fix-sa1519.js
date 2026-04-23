#!/usr/bin/env node
/**
 * fix-sa1519.js — Fix SA1519 (braces should not be omitted from multi-line child statement)
 * 
 * Only adds braces after: if, else, foreach, while, for
 * Does NOT add braces after lambda arrows or other constructs.
 */
const fs = require('fs');
const path = require('path');

const log = fs.readFileSync('_build.log', 'utf8');
const fileWarnings = {};

for (const line of log.split('\n')) {
    if (!line.includes('warning SA1519')) continue;
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
let totalSkipped = 0;

for (const [filePath, warnLines] of Object.entries(fileWarnings)) {
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    let changed = false;
    
    const sorted = [...warnLines].sort((a, b) => b - a);
    
    for (const warnLine of sorted) {
        const idx = warnLine - 1;
        if (idx < 0 || idx >= lines.length) continue;
        
        const line = lines[idx];
        const trimmed = line.trim();
        const indent = line.match(/^(\s*)/)[1];
        
        // Only handle known control flow keywords
        const isControlFlow = /^\s*(if\s*\(|else\s+if\s*\(|else\s*$|foreach\s*\(|while\s*\(|for\s*\()/.test(line);
        if (!isControlFlow) {
            totalSkipped++;
            continue;
        }
        
        // Already has opening brace
        if (trimmed.endsWith('{')) {
            totalSkipped++;
            continue;
        }
        
        // Find the body: next non-empty line(s) until we hit a semicolon
        let bodyStart = idx + 1;
        while (bodyStart < lines.length && !lines[bodyStart].trim()) bodyStart++;
        if (bodyStart >= lines.length) { totalSkipped++; continue; }
        
        // Find end of statement - track parens and look for semicolon
        let bodyEnd = bodyStart;
        let parenDepth = 0;
        let foundEnd = false;
        
        for (let i = bodyStart; i < Math.min(lines.length, bodyStart + 20); i++) {
            const bodyLine = lines[i].trim();
            if (!bodyLine) continue;
            
            for (const ch of bodyLine) {
                if (ch === '(') parenDepth++;
                if (ch === ')') parenDepth--;
            }
            
            bodyEnd = i;
            
            if (parenDepth <= 0 && bodyLine.endsWith(';')) {
                foundEnd = true;
                break;
            }
        }
        
        if (!foundEnd) { totalSkipped++; continue; }
        
        // Insert closing brace after bodyEnd
        lines.splice(bodyEnd + 1, 0, indent + '}');
        // Insert opening brace after the keyword line
        lines.splice(idx + 1, 0, indent + '{');
        
        changed = true;
        totalFixed++;
    }
    
    if (changed) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}, Skipped: ${totalSkipped}`);
