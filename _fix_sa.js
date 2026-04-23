// SA warning fixer - reads build log and fixes common StyleCop warnings
const fs = require('fs');
const path = require('path');

const buildLog = fs.readFileSync('_build.log', 'utf8').replace(/\r\n/g, '\n').replace(/\r/g, '\n');
const warnings = [];
// Match each line individually
for (const logLine of buildLog.split('\n')) {
  const m = logLine.match(/([A-Z]:\\[^(]+\.cs)\((\d+),(\d+)\): warning (SA\d+)/);
  if (m) {
    warnings.push({ file: m[1], line: parseInt(m[2]), col: parseInt(m[3]), rule: m[4] });
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
  let lines = fs.readFileSync(filePath, 'utf8').split('\n');
  let modified = false;
  
  // Sort warnings by line descending so we can modify from bottom up
  const sorted = [...fileWarnings].sort((a, b) => b.line - a.line);
  
  for (const w of sorted) {
    const idx = w.line - 1;
    if (idx < 0 || idx >= lines.length) continue;
    const line = lines[idx];
    
    switch (w.rule) {
      case 'SA1119': {
        // Remove unnecessary parentheses
        // Find the column position and check for double parens
        // This is tricky - skip for now, handle manually
        break;
      }
      
      case 'SA1513': {
        // Closing brace should be followed by blank line
        // Check if next line is not blank and not another closing brace
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
      
      case 'SA1505': {
        // Opening brace should not be followed by blank line
        if (idx + 1 < lines.length && lines[idx + 1].trim() === '') {
          lines.splice(idx + 1, 1);
          modified = true;
          totalFixed++;
        }
        break;
      }
      
      case 'SA1001': {
        // Commas should not be followed by whitespace (extra spaces)
        // Find double spaces after comma
        if (line.includes(',  ')) {
          lines[idx] = line.replace(/,  +/g, ', ');
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
    console.log(`Fixed: ${filePath}`);
  }
}

console.log(`Total auto-fixed: ${totalFixed}`);
