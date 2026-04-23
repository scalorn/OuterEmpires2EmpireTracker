// Fix remaining SA warnings
const fs = require('fs');
const path = require('path');

const buildLog = fs.readFileSync('_build.log', 'utf8').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
const warnings = [];
for (const logLine of buildLog.split('\n')) {
  const m = logLine.match(/([A-Z]:\\[^(]+\.cs)\((\d+),(\d+)\): warning (SA\d+)/);
  if (m) warnings.push({ file: m[1], line: parseInt(m[2]), col: parseInt(m[3]), rule: m[4] });
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
  
  // Sort by line descending
  const sorted = [...fileWarnings].sort((a, b) => b.line - a.line);
  
  for (const w of sorted) {
    const idx = w.line - 1;
    if (idx < 0 || idx >= lines.length) continue;
    
    switch (w.rule) {
      case 'SA1500': {
        // Braces for multi-line statements should not share line
        // Pattern: `if (cond) { ... }` on one line that spans multiple lines
        // or `} else {` patterns
        const line = lines[idx];
        const trimmed = line.trim();
        
        // Pattern: closing brace followed by else/catch/finally with opening brace
        // e.g., "} else {" or "} catch (Exception ex) {"
        if (/^\}\s*(else|catch|finally)/.test(trimmed) && trimmed.endsWith('{')) {
          const indent = line.match(/^(\s*)/)[1];
          const parts = trimmed.match(/^(\})\s*((?:else|catch|finally)[^{]*)\s*(\{)$/);
          if (parts) {
            lines.splice(idx, 1,
              indent + parts[1],
              indent + parts[2],
              indent + parts[3]);
            modified = true;
            totalFixed++;
          }
        }
        // Pattern: single-line if/else with braces: "if (cond) { stmt; }"
        else if (/^(if|else if|else|while|for|foreach|using|lock)\b/.test(trimmed) && trimmed.includes('{') && trimmed.endsWith('}')) {
          // Don't expand truly single-line blocks
        }
        // Pattern: lambda or expression with { on same line as condition
        // e.g., "var x = items.Where(i => { return i > 0; });"
        // These are harder - skip for now
        break;
      }
      
      case 'SA1137': {
        // Elements should have the same indentation
        // Usually means a line has wrong indentation (tabs vs spaces, or wrong level)
        // We can't auto-fix without knowing the expected indent
        break;
      }
      
      case 'SA1513': {
        // Closing brace should be followed by blank line
        if (idx + 1 < lines.length) {
          const nextLine = lines[idx + 1].trim();
          if (nextLine !== '' && nextLine !== '}' && nextLine !== '};') {
            lines.splice(idx + 1, 0, '');
            modified = true;
            totalFixed++;
          }
        }
        break;
      }
      
      case 'SA1509': {
        // Opening brace should not be preceded by blank line
        if (idx > 0 && lines[idx - 1].trim() === '') {
          lines.splice(idx - 1, 1);
          modified = true;
          totalFixed++;
        }
        break;
      }
      
      default:
        break;
    }
  }
  
  if (modified) {
    fs.writeFileSync(filePath, lines.join('\n'), 'utf8');
    console.log(`Fixed: ${path.basename(filePath)}`);
  }
}

console.log(`Total fixed: ${totalFixed}`);
