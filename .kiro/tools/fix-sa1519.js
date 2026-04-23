#!/usr/bin/env node
/**
 * fix-sa1519.js — Fix SA1519 (braces should not be omitted from multi-line child statement)
 * 
 * Adds braces to if/else/foreach/while/for statements that span multiple lines
 * but don't have braces.
 * 
 * Pattern:
 *   if (condition)
 *       DoSomething();
 * becomes:
 *   if (condition)
 *   {
 *       DoSomething();
 *   }
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
        
        // The warning is on the line with the keyword (if/else/foreach/while/for)
        // We need to find the statement body that follows and wrap it in braces
        
        // Check if this line already has an opening brace
        if (trimmed.endsWith('{')) continue;
        
        // Find the statement body - it's the next non-empty line(s)
        // For SA1519, the body spans multiple lines (that's why braces are needed)
        const indent = line.match(/^(\s*)/)[1];
        
        // Find the end of the body statement
        let bodyStart = idx + 1;
        let bodyEnd = bodyStart;
        
        // Skip to first non-empty line
        while (bodyStart < lines.length && !lines[bodyStart].trim()) bodyStart++;
        if (bodyStart >= lines.length) continue;
        
        bodyEnd = bodyStart;
        
        // The body might be a single statement spanning multiple lines
        // or a chain of statements. Find where it ends by tracking
        // semicolons and indentation.
        const bodyIndent = lines[bodyStart].match(/^(\s*)/)[1];
        
        // Find the end of the statement
        let foundSemicolon = false;
        for (let i = bodyStart; i < lines.length; i++) {
            const bodyLine = lines[i].trim();
            if (!bodyLine) continue;
            
            bodyEnd = i;
            if (bodyLine.endsWith(';') || bodyLine.endsWith(';)')) {
                foundSemicolon = true;
                break;
            }
        }
        
        if (!foundSemicolon) continue;
        
        // Insert braces
        // Add closing brace after bodyEnd
        lines.splice(bodyEnd + 1, 0, indent + '}');
        // Add opening brace after the keyword line
        lines.splice(idx + 1, 0, indent + '{');
        
        changed = true;
        totalFixed++;
    }
    
    if (changed) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}`);
