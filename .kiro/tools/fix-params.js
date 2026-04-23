#!/usr/bin/env node
/**
 * fix-params.js — Fix SA1116 + SA1117 (parameter formatting)
 * 
 * When parameters span multiple lines, each parameter should be on its own line.
 * The first parameter should be on the line after the opening parenthesis.
 * 
 * Key: Properly handles interpolated strings $"...{expr}..." to avoid
 * splitting on commas inside string literals.
 */
const fs = require('fs');
const path = require('path');

const log = fs.readFileSync('_build.log', 'utf8');
const fileWarnings = {};

for (const line of log.split('\n')) {
    if (!/warning SA111[67]/.test(line)) continue;
    const match = line.match(/^(.+?\.cs)\((\d+),(\d+)\)/);
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
    let content = fs.readFileSync(filePath, 'utf8');
    let lines = content.split('\n');
    const processed = new Set();
    let fileFixed = 0;
    
    const sorted = [...warnLines].sort((a, b) => a - b);
    
    for (const warnLine of sorted) {
        if (processed.has(warnLine)) continue;
        
        const idx = warnLine - 1;
        if (idx < 0 || idx >= lines.length) continue;
        
        const callStart = findCallStart(lines, idx);
        if (!callStart) { totalSkipped++; continue; }
        
        const callEnd = findCallEnd(lines, callStart.line, callStart.col);
        if (!callEnd) { totalSkipped++; continue; }
        
        if (callStart.line === callEnd.line) { totalSkipped++; continue; }
        
        let skip = false;
        for (let i = callStart.line; i <= callEnd.line; i++) {
            if (processed.has(i + 1)) { skip = true; break; }
        }
        if (skip) continue;
        
        for (let i = callStart.line; i <= callEnd.line; i++) {
            processed.add(i + 1);
        }
        
        const innerText = extractInner(lines, callStart.line, callStart.col, callEnd.line, callEnd.col);
        const params = splitParams(innerText);
        if (params.length < 2) { totalSkipped++; continue; }
        
        if (isAlreadyFormatted(lines, callStart, callEnd, params)) { totalSkipped++; continue; }
        
        const startLine = lines[callStart.line];
        const prefix = startLine.substring(0, callStart.col + 1);
        const endLine = lines[callEnd.line];
        const suffix = endLine.substring(callEnd.col);
        
        const baseIndent = startLine.match(/^(\s*)/)[1];
        const paramIndent = baseIndent + '    ';
        
        const newLines = [];
        newLines.push(prefix.trimEnd());
        for (let i = 0; i < params.length; i++) {
            const p = params[i].trim();
            const comma = (i < params.length - 1) ? ',' : '';
            if (i === params.length - 1) {
                // Last parameter: append closing paren and suffix on same line
                newLines.push(paramIndent + p + suffix.trim());
            } else {
                newLines.push(paramIndent + p + comma);
            }
        }
        
        // Validate paren count
        const oldText = lines.slice(callStart.line, callEnd.line + 1).join('\n');
        const newText = newLines.join('\n');
        
        if (countParens(oldText, '(') !== countParens(newText, '(') ||
            countParens(oldText, ')') !== countParens(newText, ')')) {
            totalSkipped++;
            continue;
        }
        
        lines.splice(callStart.line, callEnd.line - callStart.line + 1, ...newLines);
        fileFixed++;
        totalFixed++;
        
        const delta = newLines.length - (callEnd.line - callStart.line + 1);
        if (delta !== 0) {
            for (let i = sorted.indexOf(warnLine) + 1; i < sorted.length; i++) {
                if (sorted[i] > callEnd.line + 1) {
                    sorted[i] += delta;
                }
            }
        }
    }
    
    if (fileFixed > 0) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed ${fileFixed} in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}, Skipped: ${totalSkipped}`);

// ---- Helper functions ----

function findCallStart(lines, warnIdx) {
    for (let i = warnIdx; i >= Math.max(0, warnIdx - 30); i--) {
        const line = lines[i];
        const col = findUnmatchedOpenParen(line);
        if (col >= 0) return { line: i, col };
        if (i < warnIdx && !/[()]/.test(stripStringsSimple(line))) break;
    }
    return null;
}

function findUnmatchedOpenParen(line) {
    // Forward scan tracking depth, find the last ( that leaves depth > 0
    let depth = 0;
    let inStr = false, inChar = false, inVerbatim = false, inInterp = false;
    let interpBrace = 0;
    const positions = [];
    
    for (let i = 0; i < line.length; i++) {
        const ch = line[i];
        
        if (inVerbatim) {
            if (ch === '"' && i + 1 < line.length && line[i + 1] === '"') { i++; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inInterp) {
            if (interpBrace > 0) {
                if (ch === '{') interpBrace++;
                else if (ch === '}') interpBrace--;
                else if (ch === '"') {
                    for (let j = i + 1; j < line.length; j++) {
                        if (line[j] === '\\') { j++; continue; }
                        if (line[j] === '"') { i = j; break; }
                    }
                }
            } else {
                if (ch === '\\') { i++; }
                else if (ch === '{' && i + 1 < line.length && line[i + 1] === '{') { i++; }
                else if (ch === '{') { interpBrace = 1; }
                else if (ch === '"') { inInterp = false; }
            }
            continue;
        }
        if (inStr) {
            if (ch === '\\') { i++; }
            else if (ch === '"') { inStr = false; }
            continue;
        }
        if (inChar) {
            if (ch === '\\') { i++; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        
        if (ch === '$' && i + 1 < line.length && line[i + 1] === '"') { inInterp = true; interpBrace = 0; i++; continue; }
        if (ch === '$' && i + 1 < line.length && line[i + 1] === '@' && i + 2 < line.length && line[i + 2] === '"') { inVerbatim = true; i += 2; continue; }
        if (ch === '@' && i + 1 < line.length && line[i + 1] === '$' && i + 2 < line.length && line[i + 2] === '"') { inVerbatim = true; i += 2; continue; }
        if (ch === '@' && i + 1 < line.length && line[i + 1] === '"') { inVerbatim = true; i++; continue; }
        if (ch === '"') { inStr = true; continue; }
        if (ch === '\'') { inChar = true; continue; }
        
        if (ch === '(') { depth++; positions.push(i); }
        if (ch === ')') { depth--; }
    }
    
    if (depth > 0 && positions.length > 0) {
        // Find the ( that is unmatched - walk from end
        let d = 0;
        for (let i = line.length - 1; i >= 0; i--) {
            // Simple reverse scan
            const ch = line[i];
            if (ch === ')') d++;
            if (ch === '(') {
                if (d > 0) d--;
                else return i;
            }
        }
    }
    return -1;
}

function findCallEnd(lines, startLine, startCol) {
    let depth = 0;
    let inStr = false, inChar = false, inVerbatim = false, inInterp = false;
    let interpBrace = 0;
    
    for (let i = startLine; i < Math.min(lines.length, startLine + 50); i++) {
        const line = lines[i];
        const start = (i === startLine) ? startCol : 0;
        
        for (let j = start; j < line.length; j++) {
            const ch = line[j];
            
            if (inVerbatim) {
                if (ch === '"' && j + 1 < line.length && line[j + 1] === '"') { j++; }
                else if (ch === '"') { inVerbatim = false; }
                continue;
            }
            if (inInterp) {
                if (interpBrace > 0) {
                    if (ch === '{') interpBrace++;
                    else if (ch === '}') interpBrace--;
                    else if (ch === '"') {
                        for (let k = j + 1; k < line.length; k++) {
                            if (line[k] === '\\') { k++; continue; }
                            if (line[k] === '"') { j = k; break; }
                        }
                    }
                } else {
                    if (ch === '\\') { j++; }
                    else if (ch === '{' && j + 1 < line.length && line[j + 1] === '{') { j++; }
                    else if (ch === '{') { interpBrace = 1; }
                    else if (ch === '"') { inInterp = false; }
                }
                continue;
            }
            if (inStr) {
                if (ch === '\\') { j++; }
                else if (ch === '"') { inStr = false; }
                continue;
            }
            if (inChar) {
                if (ch === '\\') { j++; }
                else if (ch === '\'') { inChar = false; }
                continue;
            }
            
            if (ch === '$' && j + 1 < line.length && line[j + 1] === '"') { inInterp = true; interpBrace = 0; j++; continue; }
            if (ch === '$' && j + 1 < line.length && line[j + 1] === '@' && j + 2 < line.length && line[j + 2] === '"') { inVerbatim = true; j += 2; continue; }
            if (ch === '@' && j + 1 < line.length && line[j + 1] === '$' && j + 2 < line.length && line[j + 2] === '"') { inVerbatim = true; j += 2; continue; }
            if (ch === '@' && j + 1 < line.length && line[j + 1] === '"') { inVerbatim = true; j++; continue; }
            if (ch === '"') { inStr = true; continue; }
            if (ch === '\'') { inChar = true; continue; }
            
            if (ch === '(') depth++;
            if (ch === ')') {
                depth--;
                if (depth === 0) return { line: i, col: j };
            }
        }
    }
    return null;
}

function extractInner(lines, startLine, startCol, endLine, endCol) {
    let text = '';
    for (let i = startLine; i <= endLine; i++) {
        const line = lines[i];
        let s = (i === startLine) ? startCol + 1 : 0;
        let e = (i === endLine) ? endCol : line.length;
        let segment = line.substring(s, e);
        // Strip trailing // comments (but not inside strings)
        segment = stripTrailingComment(segment);
        text += (text ? ' ' : '') + segment.trim();
    }
    return text.trim();
}

function stripTrailingComment(text) {
    let inStr = false, inChar = false, inVerbatim = false, inInterp = false;
    let interpBrace = 0;
    
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        
        if (inVerbatim) {
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') { i++; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inInterp) {
            if (interpBrace > 0) {
                if (ch === '{') interpBrace++;
                else if (ch === '}') interpBrace--;
            } else {
                if (ch === '\\') { i++; }
                else if (ch === '{' && i + 1 < text.length && text[i + 1] === '{') { i++; }
                else if (ch === '{') { interpBrace = 1; }
                else if (ch === '"') { inInterp = false; }
            }
            continue;
        }
        if (inStr) {
            if (ch === '\\') { i++; }
            else if (ch === '"') { inStr = false; }
            continue;
        }
        if (inChar) {
            if (ch === '\\') { i++; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') { inInterp = true; interpBrace = 0; i++; continue; }
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') { inVerbatim = true; i++; continue; }
        if (ch === '"') { inStr = true; continue; }
        if (ch === '\'') { inChar = true; continue; }
        
        // Found a // comment outside strings
        if (ch === '/' && i + 1 < text.length && text[i + 1] === '/') {
            return text.substring(0, i);
        }
    }
    return text;
}

function splitParams(text) {
    const params = [];
    let current = '';
    let depth = 0;
    let inStr = false, inChar = false, inVerbatim = false, inInterp = false;
    let interpBrace = 0;

    for (let i = 0; i < text.length; i++) {
        const ch = text[i];

        if (inVerbatim) {
            current += ch;
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') { current += text[++i]; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }

        if (inInterp) {
            current += ch;
            if (interpBrace > 0) {
                if (ch === '{') { interpBrace++; }
                else if (ch === '}') { interpBrace--; }
                else if (ch === '"') {
                    for (let j = i + 1; j < text.length; j++) {
                        current += text[j];
                        if (text[j] === '\\' && j + 1 < text.length) { current += text[++j]; continue; }
                        if (text[j] === '"') { i = j; break; }
                    }
                }
            } else {
                if (ch === '\\') { if (i + 1 < text.length) current += text[++i]; }
                else if (ch === '{' && i + 1 < text.length && text[i + 1] === '{') { current += text[++i]; }
                else if (ch === '{') { interpBrace = 1; }
                else if (ch === '"') { inInterp = false; }
            }
            continue;
        }

        if (inStr) {
            current += ch;
            if (ch === '\\' && i + 1 < text.length) { current += text[++i]; }
            else if (ch === '"') { inStr = false; }
            continue;
        }

        if (inChar) {
            current += ch;
            if (ch === '\\' && i + 1 < text.length) { current += text[++i]; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }

        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') {
            inInterp = true; interpBrace = 0;
            current += ch; current += text[++i];
            continue;
        }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '@' && i + 2 < text.length && text[i + 2] === '"') {
            inVerbatim = true;
            current += ch; current += text[++i]; current += text[++i];
            continue;
        }
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '$' && i + 2 < text.length && text[i + 2] === '"') {
            inVerbatim = true;
            current += ch; current += text[++i]; current += text[++i];
            continue;
        }
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') {
            inVerbatim = true;
            current += ch; current += text[++i];
            continue;
        }
        if (ch === '"') { inStr = true; current += ch; continue; }
        if (ch === '\'') { inChar = true; current += ch; continue; }

        if (ch === '(' || ch === '[' || ch === '{') { depth++; current += ch; continue; }
        if (ch === ')' || ch === ']' || ch === '}') { depth--; current += ch; continue; }

        if (ch === ',' && depth === 0) {
            params.push(current.trim());
            current = '';
            continue;
        }

        current += ch;
    }

    if (current.trim()) params.push(current.trim());
    return params;
}

function isAlreadyFormatted(lines, callStart, callEnd, params) {
    const paramLineCount = callEnd.line - callStart.line - 1;
    if (paramLineCount === params.length) return true;
    return false;
}

function stripStringsSimple(line) {
    return line.replace(/"[^"]*"/g, '""').replace(/'[^']*'/g, "''");
}

function countParens(text, target) {
    let count = 0;
    let inStr = false, inChar = false, inVerbatim = false, inInterp = false;
    let interpBrace = 0;
    
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        if (inVerbatim) {
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') { i++; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inInterp) {
            if (interpBrace > 0) {
                if (ch === '{') interpBrace++;
                else if (ch === '}') interpBrace--;
                else if (ch === '"') {
                    for (let j = i + 1; j < text.length; j++) {
                        if (text[j] === '\\') { j++; continue; }
                        if (text[j] === '"') { i = j; break; }
                    }
                }
            } else {
                if (ch === '\\') { i++; }
                else if (ch === '{' && i + 1 < text.length && text[i + 1] === '{') { i++; }
                else if (ch === '{') { interpBrace = 1; }
                else if (ch === '"') { inInterp = false; }
            }
            continue;
        }
        if (inStr) {
            if (ch === '\\') { i++; }
            else if (ch === '"') { inStr = false; }
            continue;
        }
        if (inChar) {
            if (ch === '\\') { i++; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') { inInterp = true; interpBrace = 0; i++; continue; }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '@' && i + 2 < text.length && text[i + 2] === '"') { inVerbatim = true; i += 2; continue; }
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '$' && i + 2 < text.length && text[i + 2] === '"') { inVerbatim = true; i += 2; continue; }
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') { inVerbatim = true; i++; continue; }
        if (ch === '"') { inStr = true; continue; }
        if (ch === '\'') { inChar = true; continue; }
        if (ch === target) count++;
    }
    return count;
}
