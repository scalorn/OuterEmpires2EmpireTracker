#!/usr/bin/env node
/**
 * fix-sa1107.js — Fix SA1107 (multiple statements on one line)
 * 
 * Reads _sa1107_lines.txt (file|lineNum format) and splits multi-statement lines.
 * Processes each file once, fixing all flagged lines.
 * 
 * Strategy: For each flagged line, find semicolons that are NOT inside strings,
 * char literals, or parenthesized expressions. If there are multiple top-level
 * semicolons, split the line at those points.
 * 
 * Special handling for brace-enclosed blocks on one line:
 *   { stmt1; stmt2; } → expand to multi-line block
 */
const fs = require('fs');
const path = require('path');

const lines = fs.readFileSync('_sa1107_lines.txt', 'utf8').trim().split('\n');
const fileLines = {};

// Group by file, deduplicate line numbers
for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    const [filePath, lineNum] = trimmed.split('|');
    const fp = filePath.trim();
    const ln = parseInt(lineNum.trim(), 10);
    if (!fileLines[fp]) fileLines[fp] = new Set();
    fileLines[fp].add(ln);
}

let totalFixed = 0;
let totalSkipped = 0;

for (const [filePath, lineNums] of Object.entries(fileLines)) {
    if (filePath.includes('.Designer.cs')) {
        console.log(`SKIP (Designer): ${filePath}`);
        continue;
    }
    
    const content = fs.readFileSync(filePath, 'utf8');
    const fileLinesList = content.split('\n');
    let changed = false;
    
    // Process lines in reverse order so line numbers stay valid
    const sortedLines = [...lineNums].sort((a, b) => b - a);
    
    for (const lineNum of sortedLines) {
        const idx = lineNum - 1;
        if (idx < 0 || idx >= fileLinesList.length) continue;
        
        const originalLine = fileLinesList[idx];
        const fixed = fixLine(originalLine);
        
        if (fixed !== null && fixed.length > 1) {
            fileLinesList.splice(idx, 1, ...fixed);
            changed = true;
            totalFixed++;
        } else {
            totalSkipped++;
        }
    }
    
    if (changed) {
        fs.writeFileSync(filePath, fileLinesList.join('\n'), 'utf8');
        console.log(`Fixed in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}, Skipped: ${totalSkipped}`);

/**
 * Find top-level semicolons in a line (not inside strings, chars, or nested parens/brackets).
 * Returns array of indices where semicolons occur at the top level.
 */
function findTopLevelSemicolons(text) {
    const positions = [];
    let inString = false;
    let inChar = false;
    let inVerbatim = false;
    let parenDepth = 0;
    let braceDepth = 0;
    
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        const prev = i > 0 ? text[i - 1] : '';
        
        if (inVerbatim) {
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') {
                i++; // skip escaped quote in verbatim string
            } else if (ch === '"') {
                inVerbatim = false;
            }
            continue;
        }
        
        if (inString) {
            if (ch === '\\') {
                i++; // skip escaped char
            } else if (ch === '"') {
                inString = false;
            }
            continue;
        }
        
        if (inChar) {
            if (ch === '\\') {
                i++; // skip escaped char
            } else if (ch === '\'') {
                inChar = false;
            }
            continue;
        }
        
        // Check for string/char start
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') {
            inVerbatim = true;
            i++; // skip the quote
            continue;
        }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') {
            inString = true;
            i++; // skip the quote  
            continue;
        }
        if (ch === '"') {
            inString = true;
            continue;
        }
        if (ch === '\'') {
            inChar = true;
            continue;
        }
        
        // Track nesting
        if (ch === '(' || ch === '[') parenDepth++;
        if (ch === ')' || ch === ']') parenDepth--;
        if (ch === '{') braceDepth++;
        if (ch === '}') braceDepth--;
        
        // Top-level semicolon (inside braces is OK, but not inside parens)
        if (ch === ';' && parenDepth === 0) {
            positions.push(i);
        }
    }
    
    return positions;
}

function fixLine(line) {
    const trimmed = line.trim();
    const indent = line.match(/^(\s*)/)[1];
    
    // Skip empty lines, comments, for loops
    if (!trimmed || trimmed.startsWith('//') || trimmed.startsWith('/*')) return null;
    if (/\bfor\s*\(/.test(trimmed)) return null;
    
    const semiPositions = findTopLevelSemicolons(trimmed);
    
    // Need at least 2 semicolons for SA1107
    if (semiPositions.length < 2) {
        // Special case: single statement inside braces on same line as control flow
        // e.g., "lock (_x) { return new List<T>(_list); }"
        // This is SA1107 because the lock statement and return are on one line
        // But there's only one semicolon. Check if it's a brace-enclosed single statement.
        return tryExpandBraceBlock(trimmed, indent);
    }
    
    // Multiple semicolons — split into separate lines
    // But first check if they're all inside a brace block
    const firstBrace = trimmed.indexOf('{');
    const lastBrace = trimmed.lastIndexOf('}');
    
    if (firstBrace >= 0 && lastBrace > firstBrace) {
        // There's a brace block. Check if all semicolons are inside it.
        const allInside = semiPositions.every(p => p > firstBrace && p < lastBrace);
        
        if (allInside) {
            // Expand the brace block
            return expandBraceBlock(trimmed, indent, firstBrace, lastBrace);
        }
    }
    
    // Plain multiple statements — split at semicolons
    return splitAtSemicolons(trimmed, indent, semiPositions);
}

function tryExpandBraceBlock(trimmed, indent) {
    // Match: prefix { body }
    // where prefix is like "lock (_x)" or "public void Foo()"
    const match = trimmed.match(/^(.+?)\s*\{(.*)\}\s*$/);
    if (!match) return null;
    
    const prefix = match[1].trim();
    const body = match[2].trim();
    
    if (!body) return null; // empty braces
    
    // Don't expand property initializers like "new Foo { X = 1 }"
    if (/^new\s+/.test(prefix)) return null;
    // Don't expand lambda expressions
    if (/=>\s*$/.test(prefix)) return null;
    // Don't expand array/collection initializers
    if (/=\s*$/.test(prefix)) return null;
    
    const result = [];
    result.push(indent + prefix);
    result.push(indent + '{');
    
    // Split body into statements
    const bodySemis = findTopLevelSemicolons(body);
    if (bodySemis.length === 0) {
        result.push(indent + '    ' + body);
    } else {
        let lastEnd = 0;
        for (const pos of bodySemis) {
            const stmt = body.substring(lastEnd, pos + 1).trim();
            if (stmt) result.push(indent + '    ' + stmt);
            lastEnd = pos + 1;
        }
        // Any trailing content after last semicolon
        const trailing = body.substring(lastEnd).trim();
        if (trailing) result.push(indent + '    ' + trailing);
    }
    
    result.push(indent + '}');
    return result;
}

function expandBraceBlock(trimmed, indent, firstBrace, lastBrace) {
    const prefix = trimmed.substring(0, firstBrace).trim();
    const body = trimmed.substring(firstBrace + 1, lastBrace).trim();
    const suffix = trimmed.substring(lastBrace + 1).trim();
    
    const result = [];
    result.push(indent + prefix);
    result.push(indent + '{');
    
    // Split body into statements
    const bodySemis = findTopLevelSemicolons(body);
    let lastEnd = 0;
    for (const pos of bodySemis) {
        const stmt = body.substring(lastEnd, pos + 1).trim();
        if (stmt) result.push(indent + '    ' + stmt);
        lastEnd = pos + 1;
    }
    const trailing = body.substring(lastEnd).trim();
    if (trailing) result.push(indent + '    ' + trailing);
    
    if (suffix) {
        result.push(indent + '}' + ' ' + suffix);
    } else {
        result.push(indent + '}');
    }
    return result;
}

function splitAtSemicolons(trimmed, indent, semiPositions) {
    const result = [];
    let lastEnd = 0;
    
    for (const pos of semiPositions) {
        const stmt = trimmed.substring(lastEnd, pos + 1).trim();
        if (stmt) result.push(indent + stmt);
        lastEnd = pos + 1;
    }
    
    const trailing = trimmed.substring(lastEnd).trim();
    if (trailing) result.push(indent + trailing);
    
    return result.length > 1 ? result : null;
}
