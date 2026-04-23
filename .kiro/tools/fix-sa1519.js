#!/usr/bin/env node
// fix-sa1519.js - Add braces to multi-line child statements (SA1519)
// The warning line points to the CHILD statement; we need to add braces to the PARENT
const fs = require('fs');

const logFile = process.argv[2] || '_build.log';
const debug = process.argv.includes('--debug');
const log = fs.readFileSync(logFile, 'utf8');

const re = /([^\s]+\.cs)\((\d+),\d+\): warning SA1519/g;
const fileWarnings = {};
let m;
while (m = re.exec(log)) {
  const file = m[1];
  if (!fileWarnings[file]) fileWarnings[file] = [];
  fileWarnings[file].push(parseInt(m[2]));
}

let totalFixed = 0;

for (const [filePath, warnLines] of Object.entries(fileWarnings)) {
  const shortName = filePath.replace(/.*OE2EmpireTracker[\\/]/, '');
  let lines;
  try { lines = fs.readFileSync(filePath, 'utf8').split('\n'); }
  catch (e) { console.error('Cannot read: ' + shortName); continue; }
  
  const sorted = [...new Set(warnLines)].sort((a, b) => b - a);
  let fileFixed = 0;
  
  for (const warnLine of sorted) {
    const childIdx = warnLine - 1;
    if (childIdx < 1 || childIdx >= lines.length) continue;
    
    // Walk backwards to find the parent control flow statement
    let parentIdx = -1;
    for (let i = childIdx - 1; i >= 0; i--) {
      const t = lines[i].trim();
      // Skip blank lines
      if (t === '') continue;
      // Check if this is a control flow keyword ending with ) or 'else'
      if (/^(foreach|if|else\s*if|while|for|using)\s*\(/.test(t) || /^else\s*$/.test(t)) {
        // Verify the condition ends before the child line
        // Find the closing ) of the condition
        let depth = 0, foundOpen = false, condEnd = i;
        for (let ci = i; ci <= childIdx; ci++) {
          for (let cj = 0; cj < lines[ci].length; cj++) {
            if (lines[ci][cj] === '(') { depth++; foundOpen = true; }
            if (lines[ci][cj] === ')') { depth--; if (depth === 0 && foundOpen) { condEnd = ci; break; } }
          }
          if (depth === 0 && foundOpen) break;
        }
        // The body should start right after the condition
        if (condEnd + 1 <= childIdx) {
          parentIdx = i;
          break;
        }
      }
      // If we hit a line that's not blank and not a keyword, stop
      break;
    }
    
    if (parentIdx < 0) {
      if (debug) console.log('  No parent found for L' + warnLine + ': ' + lines[childIdx].trim().substring(0, 60));
      continue;
    }
    
    const parentLine = lines[parentIdx];
    const indent = parentLine.match(/^(\s*)/)[1];
    
    if (debug) console.log('  Parent L' + (parentIdx+1) + ': ' + parentLine.trim().substring(0, 60));
    
    // Find end of parent's condition
    let condEndIdx = parentIdx;
    if (/^else\s*$/.test(lines[parentIdx].trim())) {
      condEndIdx = parentIdx;
    } else {
      let depth = 0, foundOpen = false;
      outer:
      for (let i = parentIdx; i < lines.length; i++) {
        for (let j = 0; j < lines[i].length; j++) {
          if (lines[i][j] === '(') { depth++; foundOpen = true; }
          if (lines[i][j] === ')') { depth--; if (depth === 0 && foundOpen) { condEndIdx = i; break outer; } }
        }
      }
    }
    
    const bodyStartIdx = condEndIdx + 1;
    if (bodyStartIdx >= lines.length) continue;
    if (lines[bodyStartIdx].trim().startsWith('{')) continue; // already has braces
    
    // Find end of body: the body is a single statement (possibly multi-line)
    let bodyEndIdx = bodyStartIdx;
    let bd = 0;
    for (let i = bodyStartIdx; i < lines.length; i++) {
      const t = lines[i];
      for (let j = 0; j < t.length; j++) {
        if (t[j] === '{') bd++;
        if (t[j] === '}') bd--;
      }
      if (bd <= 0 && (t.trim().endsWith(';') || t.trim().endsWith('}'))) {
        bodyEndIdx = i;
        break;
      }
    }
    
    // Insert braces
    lines.splice(bodyEndIdx + 1, 0, indent + '}');
    lines.splice(bodyStartIdx, 0, indent + '{');
    
    fileFixed++;
    totalFixed++;
  }
  
  if (fileFixed > 0) {
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log('Fixed ' + fileFixed + ' in ' + shortName);
  }
}

console.log('Total fixed: ' + totalFixed);
