#!/usr/bin/env node
/**
 * fix-sa1500.js — Fix SA1500 (braces for multi-line statements should not share line)
 * 
 * Handles:
 * - "try { statement;" -> expand try block
 * - "} catch (...)" -> put catch on own line
 * - "} else" -> put else on own line  
 * - "} while (...)" -> put while on own line
 * - Property/method declarations with { on same line
 */
const fs = require('fs');
const path = require('path');

const log = fs.readFileSync('_build.log', 'utf8');
const fileWarnings = {};

for (const line of log.split('\n')) {
    if (!line.includes('warning SA1500')) continue;
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
    
    const sorted = [...warnLines].sort((a, b) => b - a);
    
    for (const warnLine of sorted) {
        const idx = warnLine - 1;
        if (idx < 0 || idx >= lines.length) continue;
        
        const line = lines[idx];
        const trimmed = line.trim();
        const indent = line.match(/^(\s*)/)[1];
        
        // Pattern: "try { statement;" -> expand
        if (/^\s*try\s*\{/.test(line) && !trimmed.endsWith('{')) {
            const braceIdx = trimmed.indexOf('{');
            const body = trimmed.substring(braceIdx + 1).trim();
            lines[idx] = indent + 'try';
            lines.splice(idx + 1, 0, indent + '{');
            lines.splice(idx + 2, 0, indent + '    ' + body);
            changed = true;
            totalFixed++;
            continue;
        }
        
        // Pattern: "} catch (Exception)" -> split } and catch
        if (/^\s*\}\s*catch/.test(line)) {
            const catchPart = trimmed.substring(trimmed.indexOf('catch'));
            lines[idx] = indent + '}';
            lines.splice(idx + 1, 0, indent + catchPart);
            changed = true;
            totalFixed++;
            continue;
        }
        
        // Pattern: "} else" -> split } and else
        if (/^\s*\}\s*else/.test(line)) {
            const elsePart = trimmed.substring(trimmed.indexOf('else'));
            lines[idx] = indent + '}';
            lines.splice(idx + 1, 0, indent + elsePart);
            changed = true;
            totalFixed++;
            continue;
        }
        
        // Pattern: "} while (...)" -> split } and while
        if (/^\s*\}\s*while/.test(line)) {
            const whilePart = trimmed.substring(trimmed.indexOf('while'));
            lines[idx] = indent + '}';
            lines.splice(idx + 1, 0, indent + whilePart);
            changed = true;
            totalFixed++;
            continue;
        }
        
        // Pattern: "get {" or "set {" on same line as body
        if (/^\s*(get|set)\s*\{/.test(line) && !trimmed.endsWith('{')) {
            const keyword = trimmed.match(/^(get|set)/)[1];
            const body = trimmed.substring(trimmed.indexOf('{') + 1).trim();
            if (body.endsWith('}')) {
                // Single-line accessor: get { return x; }
                const innerBody = body.substring(0, body.length - 1).trim();
                lines[idx] = indent + keyword;
                lines.splice(idx + 1, 0, indent + '{');
                lines.splice(idx + 2, 0, indent + '    ' + innerBody);
                lines.splice(idx + 3, 0, indent + '}');
                changed = true;
                totalFixed++;
            }
            continue;
        }
        
        // Pattern: property/method with { on same line
        // e.g., "public string Foo {" where the next line has get/set
        // or "public BlueprintType() {" 
        if (trimmed.endsWith('{') && !trimmed.startsWith('{') && !trimmed.startsWith('//')) {
            const prefix = trimmed.substring(0, trimmed.length - 1).trim();
            lines[idx] = indent + prefix;
            lines.splice(idx + 1, 0, indent + '{');
            changed = true;
            totalFixed++;
            continue;
        }
        
        // Pattern: "lock (_x) { statement;" on same line
        if (/^\s*lock\s*\(/.test(line) && trimmed.includes('{') && !trimmed.endsWith('{')) {
            const braceIdx = trimmed.indexOf('{');
            const prefix = trimmed.substring(0, braceIdx).trim();
            const body = trimmed.substring(braceIdx + 1).trim();
            lines[idx] = indent + prefix;
            lines.splice(idx + 1, 0, indent + '{');
            if (body) {
                lines.splice(idx + 2, 0, indent + '    ' + body);
            }
            changed = true;
            totalFixed++;
            continue;
        }
    }
    
    if (changed) {
        fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
        console.log(`Fixed in ${path.basename(filePath)}`);
    }
}

console.log(`\nTotal fixed: ${totalFixed}`);
