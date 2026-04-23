#!/usr/bin/env node
// member-order.js - Reorders C# class members per StyleCop SA1201-SA1214
// Usage: node .kiro/tools/member-order.js <file.cs> [--dry-run] [--verbose]
const fs = require('fs');

const TYPE_ORDER = ['const','static_readonly_field','readonly_field','field','constructor','destructor','delegate','event','enum','interface','property','indexer','operator','method','struct','class'];
const ACCESS_ORDER = ['public','internal','protected_internal','protected','private_protected','private'];

function sortKey(m) {
  const t = TYPE_ORDER.indexOf(m.memberType);
  const a = ACCESS_ORDER.indexOf(m.access);
  const s = m.isStatic ? 0 : 1;
  return (t >= 0 ? t : 13) * 10000 + (a >= 0 ? a : 5) * 100 + s * 10;
}

// Skip through string/comment/char literals safely
function skipLiterals(text, i) {
  const c = text[i], n = text[i + 1] || '';
  if (c === '/' && n === '/') { const eol = text.indexOf('\n', i); return { end: eol < 0 ? text.length : eol, type: 'lineComment' }; }
  if (c === '/' && n === '*') { const ec = text.indexOf('*/', i + 2); return { end: ec < 0 ? text.length : ec + 2, type: 'blockComment' }; }
  if (c === '@' && n === '"') {
    for (let j = i + 2; j < text.length; j++) {
      if (text[j] === '"' && text[j + 1] === '"') { j++; continue; }
      if (text[j] === '"') return { end: j + 1, type: 'verbatim' };
    }
    return { end: text.length, type: 'verbatim' };
  }
  if ((c === '$' && n === '"') || c === '"') {
    const start = c === '$' ? i + 2 : i + 1;
    for (let j = start; j < text.length; j++) {
      if (text[j] === '\\') { j++; continue; }
      if (text[j] === '"') return { end: j + 1, type: 'string' };
    }
    return { end: text.length, type: 'string' };
  }
  if (c === '\'') {
    for (let j = i + 1; j < text.length; j++) {
      if (text[j] === '\\') { j++; continue; }
      if (text[j] === '\'') return { end: j + 1, type: 'char' };
    }
    return { end: text.length, type: 'char' };
  }
  return null;
}

// Find matching closing brace from an opening brace
function findMatchingBrace(text, startIdx) {
  let depth = 0;
  for (let i = startIdx; i < text.length; i++) {
    const lit = skipLiterals(text, i);
    if (lit) { i = lit.end - 1; continue; }
    if (text[i] === '{') depth++;
    if (text[i] === '}') { depth--; if (depth === 0) return i; }
  }
  return -1;
}

// Classify a declaration line into member type, access, static
function classifyDecl(line) {
  let s = line.trim();
  // Strip attributes
  while (s.startsWith('[')) { const e = s.indexOf(']'); if (e < 0) break; s = s.substring(e + 1).trim(); }
  
  let access = 'private';
  if (/\bpublic\b/.test(s)) access = 'public';
  else if (/\bprotected\s+internal\b/.test(s) || /\binternal\s+protected\b/.test(s)) access = 'protected_internal';
  else if (/\bprivate\s+protected\b/.test(s)) access = 'private_protected';
  else if (/\bprotected\b/.test(s)) access = 'protected';
  else if (/\binternal\b/.test(s)) access = 'internal';
  
  const isStatic = /\bstatic\b/.test(s);
  const isReadonly = /\breadonly\b/.test(s);
  const isConst = /\bconst\b/.test(s);
  
  // Strip all modifiers
  const stripped = s.replace(/\b(public|private|protected|internal|static|readonly|const|virtual|override|abstract|sealed|extern|unsafe|volatile|new|partial|async)\b/g, '').trim();
  
  let mt;
  if (isConst) mt = 'const';
  else if (/\benum\s+\w+/.test(stripped)) mt = 'enum';
  else if (/\binterface\s+\w+/.test(stripped)) mt = 'interface';
  else if (/\bstruct\s+\w+/.test(stripped) && !/;\s*$/.test(stripped)) mt = 'struct';
  else if (/\bclass\s+\w+/.test(stripped)) mt = 'class';
  else if (/\bdelegate\b/.test(s)) mt = 'delegate';
  else if (/\bevent\b/.test(s)) mt = 'event';
  else if (/\boperator\b/.test(stripped)) mt = 'operator';
  else if (/~\w+\s*\(/.test(stripped)) mt = 'destructor';
  else if (/\bget\b/.test(s) || /\bset\b/.test(s)) mt = 'property';
  else {
    // Check if it's a field with initializer (has = before any ( or {)
    // But NOT => which is expression-bodied member
    const eqIdx = stripped.indexOf('=');
    const arrowIdx = stripped.indexOf('=>');
    const parenIdx = stripped.indexOf('(');
    const braceIdx = stripped.indexOf('{');
    const isFieldInit = eqIdx >= 0 && arrowIdx < 0 && (parenIdx < 0 || eqIdx < parenIdx) && (braceIdx < 0 || eqIdx < braceIdx);
    // Also check: if line ends with ; and has no { } it's likely a field
    const endsWithSemicolon = /;\s*$/.test(s);
    
    if (isFieldInit && endsWithSemicolon) {
      mt = isStatic && isReadonly ? 'static_readonly_field' : isReadonly ? 'readonly_field' : 'field';
    } else if (arrowIdx >= 0) {
      // Expression-bodied member: has =>
      // Check if it has parens (method) or not (property)
      const beforeArrow = stripped.substring(0, arrowIdx).trim();
      let hasRealParen = false;
      let ad = 0;
      for (let ci = 0; ci < beforeArrow.length; ci++) {
        if (beforeArrow[ci] === '<') ad++;
        else if (beforeArrow[ci] === '>') ad--;
        else if (beforeArrow[ci] === '(' && ad === 0) { hasRealParen = true; break; }
      }
      if (hasRealParen) {
        const bparen = beforeArrow.substring(0, beforeArrow.indexOf('(')).trim();
        const words = bparen.split(/\s+/).filter(w => w.length > 0);
        let wordCount = 0, inA = 0;
        for (const w of words) { if (inA === 0) wordCount++; for (const ch of w) { if (ch === '<') inA++; if (ch === '>') inA--; } }
        mt = wordCount <= 1 ? 'constructor' : 'method';
      } else {
        mt = 'property';
      }
    } else if (endsWithSemicolon && !stripped.includes('(') && arrowIdx < 0) {
      // Simple field declaration with no parens at all
      mt = isStatic && isReadonly ? 'static_readonly_field' : isReadonly ? 'readonly_field' : 'field';
    } else {
      const bp = stripped.split('{')[0].split('=>')[0].trim();
      
      // Find the first ( that's NOT inside <...> angle brackets
      let firstRealParen = -1;
      let angleDepth = 0;
      for (let ci = 0; ci < bp.length; ci++) {
        if (bp[ci] === '<') angleDepth++;
        else if (bp[ci] === '>') angleDepth--;
        else if (bp[ci] === '(' && angleDepth === 0) { firstRealParen = ci; break; }
      }
      const hasParen = firstRealParen >= 0;
      
      if (hasParen) {
        const bparen = bp.substring(0, firstRealParen).trim();
        const words = bparen.split(/\s+/).filter(w => w.length > 0);
        // A constructor has just the class name (1 word), a method has return type + name (2+ words)
        // But we need to handle generic return types like List<Item> as one "word"
        // Count non-generic-bracket words
        let wordCount = 0;
        let inAngle = 0;
        for (const w of words) {
          if (inAngle === 0) wordCount++;
          for (const ch of w) { if (ch === '<') inAngle++; if (ch === '>') inAngle--; }
        }
        mt = wordCount <= 1 ? 'constructor' : 'method';
      } else if (endsWithSemicolon) {
        // Field: ends with ; and no real parens
        mt = isStatic && isReadonly ? 'static_readonly_field' : isReadonly ? 'readonly_field' : 'field';
      } else if (/\bget\b|\bset\b/.test(s) || (/\{/.test(s) && !hasParen)) {
        mt = 'property';
      } else if (/=>\s*/.test(s) && !/\(/.test(stripped.split('=>')[0])) {
        mt = 'property';
      } else {
        mt = isStatic && isReadonly ? 'static_readonly_field' : isReadonly ? 'readonly_field' : 'field';
      }
    }
  }
  if (mt === 'field') {
    if (isStatic && isReadonly) mt = 'static_readonly_field';
    else if (isReadonly) mt = 'readonly_field';
  }
  return { memberType: mt, access, isStatic };
}

// Parse members within a class body (between { and })
function parseMembers(text, bodyStart, bodyEnd) {
  const members = [];
  let i = bodyStart + 1;
  
  while (i < bodyEnd) {
    // Skip only newlines/carriage returns, but capture indentation with the member
    while (i < bodyEnd && (text[i] === '\n' || text[i] === '\r')) i++;
    if (i >= bodyEnd || text[i] === '}') break;
    
    // Skip preprocessor directives
    if (text[i] === '#' || (text[i] === ' ' && text.substring(i).trimStart().startsWith('#'))) {
      const lineStart = i;
      // Find the actual # after whitespace
      let hi = i;
      while (hi < bodyEnd && text[hi] === ' ') hi++;
      if (text[hi] === '#') {
        const eol = text.indexOf('\n', hi);
        i = eol < 0 ? bodyEnd : eol + 1;
        continue;
      }
    }
    
    const memberStart = i; // includes indentation
    
    // Skip leading trivia (comments, attributes, whitespace) to find declaration
    let declStart = i;
    while (declStart < bodyEnd) {
      const c = text[declStart];
      if (c === '/' && text[declStart + 1] === '/') {
        const eol = text.indexOf('\n', declStart);
        declStart = eol < 0 ? bodyEnd : eol + 1;
      } else if (c === '/' && text[declStart + 1] === '*') {
        const ec = text.indexOf('*/', declStart + 2);
        declStart = ec < 0 ? bodyEnd : ec + 2;
      } else if (c === '[') {
        let depth = 0, j = declStart;
        while (j < bodyEnd) {
          if (text[j] === '[') depth++;
          else if (text[j] === ']') { depth--; if (depth === 0) { j++; break; } }
          j++;
        }
        declStart = j;
      } else if (/\s/.test(c)) {
        declStart++;
      } else {
        break;
      }
    }
    
    // Find end of member (matching brace or semicolon at depth 0)
    let memberEnd;
    let braceDepth = 0;
    for (let j = declStart; j < bodyEnd; j++) {
      const lit = skipLiterals(text, j);
      if (lit) { j = lit.end - 1; continue; }
      const c = text[j];
      if (c === '{') braceDepth++;
      else if (c === '}') {
        braceDepth--;
        if (braceDepth === 0) {
          // Check if there's a property initializer after the closing brace: } = value;
          let k = j + 1;
          while (k < bodyEnd && /\s/.test(text[k])) k++;
          if (k < bodyEnd && text[k] === '=') {
            // Find the semicolon that ends the initializer
            for (let m = k + 1; m < bodyEnd; m++) {
              const lit2 = skipLiterals(text, m);
              if (lit2) { m = lit2.end - 1; continue; }
              if (text[m] === ';') { memberEnd = m + 1; break; }
            }
            if (memberEnd === undefined) memberEnd = j + 1;
          } else if (k < bodyEnd && text[k] === ';') {
            // Field/variable declaration ending with }; (array/object initializer)
            memberEnd = k + 1;
          } else {
            memberEnd = j + 1;
          }
          break;
        }
      }
      else if (c === ';' && braceDepth === 0) { memberEnd = j + 1; break; }
    }
    
    if (memberEnd === undefined) break;
    
    // Include trailing newline in member text
    let actualEnd = memberEnd;
    while (actualEnd < bodyEnd && (text[actualEnd] === '\r' || text[actualEnd] === '\n')) actualEnd++;
    
    const declText = text.substring(declStart, Math.min(declStart + 500, memberEnd));
    const firstLine = declText.split('\n')[0].trim();
    const cls = classifyDecl(firstLine);
    
    members.push({
      text: text.substring(memberStart, actualEnd),
      startChar: memberStart,
      endChar: actualEnd,
      declLine: firstLine,
      ...cls,
      key: sortKey(cls),
    });
    
    i = actualEnd;
  }
  return members;
}

// Find all class/struct bodies in the file
function findClassBodies(text) {
  const results = [];
  const pattern = /\b(class|struct)\s+\w+/g;
  let match;
  while ((match = pattern.exec(text)) !== null) {
    // Make sure this isn't inside a string or comment
    let i = match.index + match[0].length;
    while (i < text.length) {
      const lit = skipLiterals(text, i);
      if (lit) { i = lit.end; continue; }
      if (text[i] === '{') {
        const closeIdx = findMatchingBrace(text, i);
        if (closeIdx > 0) results.push({ start: i, end: closeIdx, keyword: match[0] });
        break;
      }
      if (text[i] === ';') break;
      i++;
    }
  }
  // Process from last to first to avoid offset issues
  results.sort((a, b) => b.start - a.start);
  return results;
}

function processFile(filePath, dryRun, verbose) {
  const text = fs.readFileSync(filePath, 'utf8');
  const bodies = findClassBodies(text);
  if (bodies.length === 0) { if (verbose) console.log('No classes in ' + filePath); return false; }
  
  let result = text;
  let changed = false;
  
  for (const body of bodies) {
    // Recalculate body position in current result
    const members = parseMembers(result, body.start, body.end);
    if (members.length <= 1) continue;
    
    const sorted = [...members].sort((a, b) => a.key - b.key || a.startChar - b.startChar);
    
    // Check if order changed
    let orderChanged = false;
    for (let i = 0; i < members.length; i++) {
      if (members[i].startChar !== sorted[i].startChar) { orderChanged = true; break; }
    }
    if (!orderChanged) continue;
    
    if (verbose || dryRun) {
      console.log((dryRun ? 'Would reorder ' : 'Reordering ') + members.length + ' members in ' + body.keyword);
      for (const m of sorted) {
        console.log('  ' + m.memberType + ' (' + m.access + (m.isStatic ? ' static' : '') + '): ' + m.declLine.substring(0, 80));
      }
    }
    
    if (dryRun) continue;
    changed = true;
    
    // Reconstruct: keep text before first member and after last member
    const firstStart = members[0].startChar;
    const lastEnd = members[members.length - 1].endChar;
    const before = result.substring(body.start + 1, firstStart);
    const after = result.substring(lastEnd, body.end);
    
    let newBody = '{' + before;
    
    for (let i = 0; i < sorted.length; i++) {
      let mt = sorted[i].text;
      // Normalize trailing whitespace: ensure exactly one blank line after each member
      // (two newlines: one to end the member line, one blank line)
      mt = mt.replace(/[\r\n]+$/, '\n\n');
      
      newBody += mt;
    }
    // Remove the extra blank line before the closing brace
    newBody = newBody.replace(/\n\n$/, '\n');
    // Ensure proper ending: newline before closing brace
    if (!newBody.endsWith('\n')) newBody += '\n';
    newBody += after;
    // Add back the closing brace of the class body
    newBody += '}';
    
    result = result.substring(0, body.start) + newBody + result.substring(body.end + 1);
  }
  
  if (changed) {
    fs.writeFileSync(filePath, result, 'utf8');
    console.log('Reordered: ' + filePath);
  }
  return changed;
}

// Main
const args = process.argv.slice(2);
const dryRun = args.includes('--dry-run');
const verbose = args.includes('--verbose');
let files = args.filter(a => !a.startsWith('--'));

// Support --filelist to read files from a text file
const filelistIdx = args.indexOf('--filelist');
if (filelistIdx >= 0 && args[filelistIdx + 1]) {
  const listContent = fs.readFileSync(args[filelistIdx + 1], 'utf8');
  files = listContent.split('\n').map(l => l.trim()).filter(l => l && !l.startsWith('#'));
}

if (files.length === 0) { console.log('Usage: node member-order.js <file.cs> [--dry-run] [--verbose] [--filelist list.txt]'); process.exit(1); }
let total = 0;
for (const f of files) {
  try { if (processFile(f, dryRun, verbose)) total++; }
  catch (e) { console.error('Error: ' + f + ': ' + e.message); }
}
console.log('Processed ' + total + ' files');
