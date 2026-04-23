// Fix SA1117: Parameters should all be on same line or each on its own line
// Strategy: For each warning, find the method call, and put each parameter on its own line
const fs = require('fs');
const path = require('path');

const buildLog = fs.readFileSync('_build.log', 'utf8').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
const warnings = [];
for (const logLine of buildLog.split('\n')) {
  const m = logLine.match(/([A-Z]:\\[^(]+\.cs)\((\d+),(\d+)\): warning SA1117/);
  if (m) {
    warnings.push({ file: m[1], line: parseInt(m[2]), col: parseInt(m[3]) });
  }
}

// Group by file
const byFile = {};
for (const w of warnings) {
  if (!byFile[w.file]) byFile[w.file] = [];
  byFile[w.file].push(w);
}

let totalFixed = 0;

for (const [filePath, fileWarnings] of Object.entries(byFile)) {
  let content = fs.readFileSync(filePath, 'utf8');
  let lines = content.split('\n');
  let modified = false;
  
  // Process warnings from bottom to top to preserve line numbers
  const sorted = [...fileWarnings].sort((a, b) => b.line - a.line);
  
  // Deduplicate by line number (multiple warnings on same line)
  const seen = new Set();
  const unique = sorted.filter(w => {
    if (seen.has(w.line)) return false;
    seen.add(w.line);
    return true;
  });
  
  for (const w of unique) {
    const idx = w.line - 1;
    if (idx < 0 || idx >= lines.length) continue;
    
    // Find the start of the call - scan backwards to find the opening paren
    let startLine = idx;
    let endLine = idx;
    
    // The warning is on a line where params are mixed (some on same line, some on different)
    // We need to find the full call expression
    
    // Scan backwards to find the line with the opening paren
    let parenDepth = 0;
    let foundOpen = false;
    for (let i = idx; i >= Math.max(0, idx - 20); i--) {
      const line = lines[i];
      for (let j = line.length - 1; j >= 0; j--) {
        if (line[j] === ')') parenDepth++;
        if (line[j] === '(') {
          parenDepth--;
          if (parenDepth < 0) {
            startLine = i;
            foundOpen = true;
            break;
          }
        }
      }
      if (foundOpen) break;
    }
    
    if (!foundOpen) continue;
    
    // Scan forward to find the closing paren
    parenDepth = 0;
    let foundClose = false;
    for (let i = startLine; i < Math.min(lines.length, startLine + 30); i++) {
      const line = lines[i];
      for (let j = 0; j < line.length; j++) {
        if (line[j] === '(') parenDepth++;
        if (line[j] === ')') {
          parenDepth--;
          if (parenDepth === 0) {
            endLine = i;
            foundClose = true;
            break;
          }
        }
      }
      if (foundClose) break;
    }
    
    if (!foundClose) continue;
    
    // Extract the full call text
    const callLines = lines.slice(startLine, endLine + 1);
    const fullText = callLines.join('\n');
    
    // Find the opening paren position in the first line
    let openParenIdx = -1;
    parenDepth = 0;
    const firstLine = callLines[0];
    for (let j = firstLine.length - 1; j >= 0; j--) {
      // Scan from end to find the relevant opening paren
    }
    // Actually, let's find the first unmatched opening paren
    for (let j = 0; j < firstLine.length; j++) {
      if (firstLine[j] === '(') {
        // Check if this is the one
        let depth = 0;
        let isOurs = true;
        for (let k = j; k < firstLine.length; k++) {
          if (firstLine[k] === '(') depth++;
          if (firstLine[k] === ')') depth--;
          if (depth === 0 && k < firstLine.length - 1) {
            isOurs = false;
            break;
          }
        }
        if (isOurs || depth > 0) {
          openParenIdx = j;
          break;
        }
      }
    }
    
    if (openParenIdx < 0) {
      // Try simpler approach - find last ( on the start line
      openParenIdx = firstLine.lastIndexOf('(');
      if (openParenIdx < 0) continue;
    }
    
    // Extract the part before the opening paren (method name + indentation)
    const prefix = firstLine.substring(0, openParenIdx + 1);
    const baseIndent = firstLine.match(/^(\s*)/)[1];
    const paramIndent = baseIndent + '    '; // 4 more spaces for params
    
    // Extract all content between parens
    let innerContent = '';
    if (callLines.length === 1) {
      // Single line - extract between first ( and last )
      const lastParen = firstLine.lastIndexOf(')');
      innerContent = firstLine.substring(openParenIdx + 1, lastParen);
    } else {
      // Multi-line - join everything between open and close parens
      innerContent = firstLine.substring(openParenIdx + 1);
      for (let i = 1; i < callLines.length - 1; i++) {
        innerContent += '\n' + callLines[i];
      }
      const lastLine = callLines[callLines.length - 1];
      const lastParen = lastLine.lastIndexOf(')');
      innerContent += '\n' + lastLine.substring(0, lastParen);
    }
    
    // Parse parameters (respecting nested parens, strings, etc.)
    const params = [];
    let current = '';
    let depth = 0;
    let inString = false;
    let stringChar = '';
    
    for (let i = 0; i < innerContent.length; i++) {
      const ch = innerContent[i];
      
      if (inString) {
        current += ch;
        if (ch === stringChar && innerContent[i-1] !== '\\') {
          inString = false;
        }
        continue;
      }
      
      if (ch === '"' || ch === '\'') {
        inString = true;
        stringChar = ch;
        current += ch;
        continue;
      }
      
      if (ch === '(' || ch === '[' || ch === '{') {
        depth++;
        current += ch;
        continue;
      }
      
      if (ch === ')' || ch === ']' || ch === '}') {
        depth--;
        current += ch;
        continue;
      }
      
      if (ch === ',' && depth === 0) {
        params.push(current.trim());
        current = '';
        continue;
      }
      
      current += ch;
    }
    if (current.trim()) params.push(current.trim());
    
    if (params.length <= 1) continue; // Nothing to fix
    
    // Check if all params are already on separate lines
    // If the warning fired, they're not, so we need to fix
    
    // Check if params fit on one line (< 120 chars)
    const oneLine = prefix + params.join(', ') + ')';
    // Get the suffix after the closing paren on the last line
    const lastCallLine = callLines[callLines.length - 1];
    const lastParenPos = lastCallLine.lastIndexOf(')');
    const suffix = lastCallLine.substring(lastParenPos + 1);
    
    let newLines;
    if (oneLine.length + suffix.length <= 120) {
      // Put all on one line
      newLines = [oneLine + suffix];
    } else {
      // Put each param on its own line
      newLines = [prefix];
      for (let i = 0; i < params.length; i++) {
        const comma = i < params.length - 1 ? ',' : ')' + suffix;
        newLines.push(paramIndent + params[i] + comma);
      }
    }
    
    // Replace the lines
    lines.splice(startLine, endLine - startLine + 1, ...newLines);
    modified = true;
    totalFixed++;
  }
  
  if (modified) {
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log(`Fixed ${fileWarnings.length} SA1117 in: ${path.basename(filePath)}`);
  }
}

console.log(`Total SA1117 fixed: ${totalFixed}`);
