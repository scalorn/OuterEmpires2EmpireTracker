#!/usr/bin/env node
/**
 * fix-misc.js — Fix miscellaneous small SA warnings
 * 
 * SA1009: Closing parenthesis should not be preceded by a space
 * SA1111: Closing parenthesis should be on line of last parameter
 * SA1137: Elements should have the same indentation
 * SA1119: Statement should not use unnecessary parenthesis
 * SA1214: Readonly fields should appear before non-readonly fields
 * SA1115: Parameter should follow comma
 * SA1203: Constants should appear before fields
 * SA1504: All accessors should be single-line or multi-line
 * SA1003: Operator should not be followed by whitespace
 * SA1520: Use braces consistently
 * SA1118: Parameter should not span multiple lines
 * SA1008: Opening parenthesis should not be preceded by a space
 * SA1114: Parameter list should follow declaration
 * SA1108: Block statements should not contain embedded comments
 * SA1001: Commas should not be preceded by whitespace
 */
const fs = require('fs');
const path = require('path');

const log = fs.readFileSync('_build.log', 'utf8');

// SA1009: Remove space before closing paren
// Pattern: "( expr )" -> "(expr)" or "Method( )" -> "Method()"
fixSA1009();

// SA1111: Closing paren on line of last parameter
// This is about standalone ) lines from our param reformatter
fixSA1111();

function fixSA1009() {
    const fileWarnings = {};
    for (const line of log.split('\n')) {
        if (!line.includes('warning SA1009')) continue;
        const match = line.match(/^(.+?\.cs)\((\d+),(\d+)\)/);
        if (!match) continue;
        const fp = match[1].trim();
        if (fp.includes('.Designer.cs')) continue;
        const ln = parseInt(match[2], 10);
        const col = parseInt(match[3], 10);
        if (!fileWarnings[fp]) fileWarnings[fp] = [];
        fileWarnings[fp].push({ ln, col });
    }
    
    let fixed = 0;
    for (const [filePath, warnings] of Object.entries(fileWarnings)) {
        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n');
        let changed = false;
        
        // Process in reverse order
        warnings.sort((a, b) => b.ln - a.ln || b.col - a.col);
        
        for (const { ln, col } of warnings) {
            const idx = ln - 1;
            if (idx < 0 || idx >= lines.length) continue;
            
            const line = lines[idx];
            const c = col - 1; // 0-indexed
            
            if (c >= 0 && c < line.length && line[c] === ')') {
                // Check if preceded by space
                if (c > 0 && line[c - 1] === ' ') {
                    // Remove the space before )
                    lines[idx] = line.substring(0, c - 1) + line.substring(c);
                    changed = true;
                    fixed++;
                }
            }
        }
        
        if (changed) {
            fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        }
    }
    console.log(`SA1009: Fixed ${fixed}`);
}

function fixSA1111() {
    const fileWarnings = {};
    for (const line of log.split('\n')) {
        if (!line.includes('warning SA1111')) continue;
        const match = line.match(/^(.+?\.cs)\((\d+),(\d+)\)/);
        if (!match) continue;
        const fp = match[1].trim();
        if (fp.includes('.Designer.cs')) continue;
        const ln = parseInt(match[2], 10);
        if (!fileWarnings[fp]) fileWarnings[fp] = new Set();
        fileWarnings[fp].add(ln);
    }
    
    let fixed = 0;
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
            
            // If this line is just ")" or ");" or ")," etc.
            if (/^\s*\)[;,]?\s*$/.test(line)) {
                // Move the ) to the end of the previous non-empty line
                let prevIdx = idx - 1;
                while (prevIdx >= 0 && !lines[prevIdx].trim()) prevIdx--;
                
                if (prevIdx >= 0) {
                    const prevLine = lines[prevIdx];
                    // Append ) to previous line (removing trailing comma if present)
                    let prevTrimmed = prevLine.trimEnd();
                    if (prevTrimmed.endsWith(',')) {
                        prevTrimmed = prevTrimmed.substring(0, prevTrimmed.length - 1);
                    }
                    lines[prevIdx] = prevTrimmed + trimmed.trim();
                    lines.splice(idx, 1);
                    changed = true;
                    fixed++;
                }
            }
        }
        
        if (changed) {
            fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        }
    }
    console.log(`SA1111: Fixed ${fixed}`);
}
