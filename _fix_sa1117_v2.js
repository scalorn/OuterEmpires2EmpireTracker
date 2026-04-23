// Fix SA1117 v2: More robust parameter splitting
// Strategy: For each warning line, find the enclosing call/declaration,
// extract params, put each on its own line
const fs = require('fs');
const path = require('path');

const buildLog = fs.readFileSync('_build.log', 'utf8').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
const warnings = [];
for (const logLine of buildLog.split('\n')) {
  const m = logLine.match(/([A-Z]:\\[^(]+\.cs)\((\d+),(\d+)\): warning SA1117/);
  if (m) warnings.push({ file: m[1], line: parseInt(m[2]), col: parseInt(m[3]) });
}

const byFile = {};
for (const w of warnings) {
  if (!byFile[w.file]) byFile[w.file] = [];
  byFile[w.file].push(w);
}

let totalFixed = 0;

for (const [filePath, fileWarnings] of Object.entries(byFile)) {
  let lines = fs.readFileSync(filePath, 'utf8').split('\n');
  let modified = false;
  
  // Deduplicate by line, process bottom-up
  const seen = new Set();
  const unique = [...fileWarnings]
    .sort((a, b) => b.line - a.line)
    .filter(w => { if (seen.has(w.line)) return false; seen.add(w.line); return true; });
  
  for (const w of unique) {
    const warnIdx = w.line - 1;
    if (warnIdx < 0 || warnIdx >= lines.length) continue;
    
    // Find the opening paren by scanning backwards from warning line
    let openLine = -1, openCol = -1;
    let depth = 0;
    
    for (let i = warnIdx; i >= Math.max(0, warnIdx - 30); i--) {
      const ln = lines[i];
      const end = (i === warnIdx) ? w.col - 1 : ln.length - 1;
      for (let j = end; j >= 0; j--) {
        const ch = ln[j];
        if (ch === ')') depth++;
        if (ch === '(') {
          if (depth === 0) { openLine = i; openCol = j; break; }
          depth--;
        }
      }
      if (openLine >= 0) break;
    }
    
    if (openLine < 0) continue;
    
    // Find the closing paren
    let closeLine = -1, closeCol = -1;
    depth = 0;
    let inStr = false, strCh = '';
    
    for (let i = openLine; i < Math.min(lines.length, openLine + 40); i++) {
      const ln = lines[i];
      const start = (i === openLine) ? openCol : 0;
      for (let j = start; j < ln.length; j++) {
        const ch = ln[j];
        if (inStr) {
          if (ch === strCh && ln[j-1] !== '\\') inStr = false;
          continue;
        }
        if (ch === '"' || ch === '\'') { inStr = true; strCh = ch; continue; }
        if (ch === '$' && j + 1 < ln.length && ln[j+1] === '"') { inStr = true; strCh = '"'; j++; continue; }
        if (ch === '(') depth++;
        if (ch === ')') {
          depth--;
          if (depth === 0) { closeLine = i; closeCol = j; break; }
        }
      }
      if (closeLine >= 0) break;
    }
    
    if (closeLine < 0) continue;
    
    // Extract the full text from openLine to closeLine
    const callLines = lines.slice(openLine, closeLine + 1);
    const firstLine = callLines[0];
    const lastLine = callLines[callLines.length - 1];
    
    // Get prefix (everything before and including the opening paren)
    const prefix = firstLine.substring(0, openCol + 1);
    const baseIndent = firstLine.match(/^(\s*)/)[1];
    const paramIndent = baseIndent + '    ';
    
    // Get suffix (everything after the closing paren on the last line)
    const suffix = lastLine.substring(closeCol + 1);
    
    // Extract inner content between parens
    let inner = '';
    if (openLine === closeLine) {
      inner = firstLine.substring(openCol + 1, closeCol);
    } else {
      inner = firstLine.substring(openCol + 1);
      for (let i = 1; i < callLines.length - 1; i++) {
        inner += '\n' + callLines[i];
      }
      inner += '\n' + lastLine.substring(0, closeCol);
    }
    
    // Parse parameters respecting nesting, strings, interpolated strings
    const params = [];
    let current = '';
    depth = 0;
    inStr = false;
    strCh = '';
    let inInterp = false;
    let interpDepth = 0;
    
    for (let i = 0; i < inner.length; i++) {
      const ch = inner[i];
      
      if (inStr) {
        current += ch;
        if (ch === strCh && inner[i-1] !== '\\') {
          inStr = false;
        }
        // Handle interpolated string braces
        if (inInterp && ch === '{' && inner[i-1] !== '{') interpDepth++;
        if (inInterp && ch === '}' && inner[i+1] !== '}') {
          interpDepth--;
          if (interpDepth === 0) { /* still in string */ }
        }
        continue;
      }
      
      if (ch === '"') {
        inStr = true;
        strCh = '"';
        if (i > 0 && inner[i-1] === '$') inInterp = true;
        current += ch;
        continue;
      }
      if (ch === '\'') { inStr = true; strCh = '\''; current += ch; continue; }
      
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
    
    if (params.length <= 1) continue;
    
    // Filter out empty params (from trailing commas or whitespace)
    const cleanParams = params.filter(p => p.length > 0);
    if (cleanParams.length <= 1) continue;
    
    // Check if all params fit on one line
    const oneLine = prefix + cleanParams.join(', ') + ')' + suffix;
    
    let newLines;
    if (oneLine.length <= 120 && !oneLine.includes('\n')) {
      newLines = [oneLine];
    } else {
      // Each param on its own line
      newLines = [prefix];
      for (let i = 0; i < cleanParams.length; i++) {
        const isLast = i === cleanParams.length - 1;
        const paramText = cleanParams[i];
        // Check if param contains newlines (multi-line expression)
        if (paramText.includes('\n')) {
          // Multi-line param - indent each line
          const paramLines = paramText.split('\n');
          for (let j = 0; j < paramLines.length; j++) {
            const pl = paramLines[j].trim();
            if (j === 0) {
              newLines.push(paramIndent + pl + (j === paramLines.length - 1 && !isLast ? ',' : ''));
            } else if (j === paramLines.length - 1) {
              newLines.push(paramIndent + '    ' + pl + (isLast ? ')' + suffix : ','));
            } else {
              newLines.push(paramIndent + '    ' + pl);
            }
          }
        } else {
          newLines.push(paramIndent + paramText + (isLast ? ')' + suffix : ','));
        }
      }
    }
    
    lines.splice(openLine, closeLine - openLine + 1, ...newLines);
    modified = true;
    totalFixed++;
  }
  
  if (modified) {
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log(`Fixed in: ${path.basename(filePath)}`);
  }
}

console.log(`Total SA1117 fixed: ${totalFixed}`);
