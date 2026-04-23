#!/usr/bin/env node
/**
 * fix-sa1501.js — Fix SA1501 (statement should not be on single line)
 * 
 * Expands single-line blocks like:
 *   if (x) { return; }
 * to:
 *   if (x)
 *   {
 *       return;
 *   }
 * 
 * Also handles: lock, using, try, catch, finally, foreach, while, else, etc.
 * 
 * Reads _build.log for SA1501 warnings and fixes each flagged line.
 */
const fs = require('fs');
const path = require('path');

// Extract SA1501 warnings from build log
const log = fs.readFileSync('_build.log', 'utf8');
const fileLines = {};

for (const line of log.split('\n')) {
    if (!line.includes('warning SA1501')) continue;
    const match = line.match(/^(.+?\.cs)\((\d+),/);
    if (match) {
        const fp = match[1].trim();
        const ln = parseInt(match[2], 10);
        if (fp.includes('.Designer.cs')) continue;
        if (!fileLines[fp]) fileLines[fp] = new Set();
        fileLines[fp].add(ln);
    }
}

let totalFixed = 0;

for (const [filePath, lineNums] of Object.entries(fileLines)) {
    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');
    let changed = false;
    
    // Process in reverse order
    const sorted = [...lineNums].sort((a, b) => b - a);
    
    for (const lineNum of sorted) {
        const idx = lineNum - 1;
        if (idx < 0 || idx >= lines.length) continue;
        
        const line = lines[idx];
        const expanded = expandSingleLineBlock(line);
        
        if (expanded) {
            lines.splice(idx, 1, ...expanded);
            changed = true;
            totalFixed++;
        }
    }
    
    if (changed) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed ${sorted.length} in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}`);

function expandSingleLineBlock(line) {
    const trimmed = line.trim();
    const indent = line.match(/^(\s*)/)[1];
    
    // Match patterns like: keyword (...) { body }
    // or: } else { body }
    // or: } catch (Exception) { body }
    // or: } finally { body }
    
    // Find the opening brace
    const braceIdx = findOpeningBrace(trimmed);
    if (braceIdx < 0) return null;
    
    // Find matching closing brace
    const closeIdx = findMatchingClose(trimmed, braceIdx);
    if (closeIdx < 0) return null;
    
    // The body is between braces
    const prefix = trimmed.substring(0, braceIdx).trim();
    const body = trimmed.substring(braceIdx + 1, closeIdx).trim();
    const suffix = trimmed.substring(closeIdx + 1).trim();
    
    // Don't expand empty blocks: { } — ACTUALLY we DO need to expand these for SA1501
    // StyleCop wants even empty blocks on multiple lines
    if (!body) {
        const result = [];
        result.push(indent + prefix);
        result.push(indent + '{');
        result.push(indent + '}');
        return result;
    }
    
    // Don't expand object/collection initializers
    if (/=\s*new\s+/.test(prefix) && /=/.test(body)) return null;
    
    const result = [];
    result.push(indent + prefix);
    result.push(indent + '{');
    
    // Body might have multiple statements
    const stmts = splitStatements(body);
    for (const stmt of stmts) {
        result.push(indent + '    ' + stmt);
    }
    
    result.push(indent + '}');
    
    // Handle suffix (e.g., "else" after closing brace)
    if (suffix) {
        // If suffix starts with else/catch/finally, it should be on the closing brace line
        if (/^(else|catch|finally)/.test(suffix)) {
            // Actually for StyleCop, else goes on same line as }
            // But that might cause other issues. Let's keep it separate for now.
            result[result.length - 1] = indent + '}'; // just the brace
            // Check if the suffix itself has a brace block
            const suffixExpanded = expandSingleLineBlock(indent + suffix);
            if (suffixExpanded) {
                result.push(...suffixExpanded);
            } else {
                result.push(indent + suffix);
            }
        } else {
            result.push(indent + suffix);
        }
    }
    
    return result;
}

function findOpeningBrace(text) {
    // Find the first { that's not inside a string or parenthesized expression
    let inString = false;
    let inChar = false;
    let inVerbatim = false;
    let parenDepth = 0;
    
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        
        if (inVerbatim) {
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') { i++; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inString) {
            if (ch === '\\') { i++; }
            else if (ch === '"') { inString = false; }
            continue;
        }
        if (inChar) {
            if (ch === '\\') { i++; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') { inVerbatim = true; i++; continue; }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') { inString = true; i++; continue; }
        if (ch === '"') { inString = true; continue; }
        if (ch === '\'') { inChar = true; continue; }
        
        if (ch === '(') parenDepth++;
        if (ch === ')') parenDepth--;
        
        if (ch === '{' && parenDepth === 0) return i;
    }
    return -1;
}

function findMatchingClose(text, openIdx) {
    let depth = 0;
    let inString = false;
    let inChar = false;
    let inVerbatim = false;
    
    for (let i = openIdx; i < text.length; i++) {
        const ch = text[i];
        
        if (inVerbatim) {
            if (ch === '"' && i + 1 < text.length && text[i + 1] === '"') { i++; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inString) {
            if (ch === '\\') { i++; }
            else if (ch === '"') { inString = false; }
            continue;
        }
        if (inChar) {
            if (ch === '\\') { i++; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        
        if (ch === '@' && i + 1 < text.length && text[i + 1] === '"') { inVerbatim = true; i++; continue; }
        if (ch === '$' && i + 1 < text.length && text[i + 1] === '"') { inString = true; i++; continue; }
        if (ch === '"') { inString = true; continue; }
        if (ch === '\'') { inChar = true; continue; }
        
        if (ch === '{') depth++;
        if (ch === '}') {
            depth--;
            if (depth === 0) return i;
        }
    }
    return -1;
}

function splitStatements(body) {
    const stmts = [];
    let current = '';
    let inString = false;
    let inChar = false;
    let inVerbatim = false;
    let parenDepth = 0;
    let braceDepth = 0;
    
    for (let i = 0; i < body.length; i++) {
        const ch = body[i];
        
        if (inVerbatim) {
            current += ch;
            if (ch === '"' && i + 1 < body.length && body[i + 1] === '"') { current += body[++i]; }
            else if (ch === '"') { inVerbatim = false; }
            continue;
        }
        if (inString) {
            current += ch;
            if (ch === '\\') { current += body[++i]; }
            else if (ch === '"') { inString = false; }
            continue;
        }
        if (inChar) {
            current += ch;
            if (ch === '\\') { current += body[++i]; }
            else if (ch === '\'') { inChar = false; }
            continue;
        }
        
        if (ch === '@' && i + 1 < body.length && body[i + 1] === '"') { inVerbatim = true; current += ch; continue; }
        if (ch === '$' && i + 1 < body.length && body[i + 1] === '"') { inString = true; current += ch; continue; }
        if (ch === '"') { inString = true; current += ch; continue; }
        if (ch === '\'') { inChar = true; current += ch; continue; }
        
        if (ch === '(' || ch === '[') parenDepth++;
        if (ch === ')' || ch === ']') parenDepth--;
        if (ch === '{') braceDepth++;
        if (ch === '}') braceDepth--;
        
        current += ch;
        
        if (ch === ';' && parenDepth === 0 && braceDepth === 0) {
            stmts.push(current.trim());
            current = '';
        }
    }
    
    if (current.trim()) stmts.push(current.trim());
    return stmts;
}
