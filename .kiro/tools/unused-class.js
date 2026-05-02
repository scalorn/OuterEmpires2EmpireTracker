#!/usr/bin/env node
/**
 * unused-class.js — Detect public/internal classes that are never referenced outside their declaring file.
 *
 * Usage:
 *   node .kiro/tools/unused-class.js
 *
 * Scans all .cs files in OE2EmpireTracker/ (excluding Designer.cs, obj/, bin/) for class declarations.
 * For each class, checks if its name appears in any OTHER .cs file (including Designer.cs and test files).
 * If the class name only appears in its declaring file, it's flagged — unless it appears at least twice
 * in that file (declaration + usage as a return type, parameter, local variable, etc.).
 *
 * Exempt categories:
 * - Extension method classes (static classes whose methods use 'this' parameter syntax)
 * - Classes in the KNOWN_INTERNAL set (used via reflection, attributes, or framework conventions)
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SRC_DIR = path.join('OE2EmpireTracker');
const TEST_DIR = path.join('OE2EmpireTracker.Tests');
const SKIP_DIRS = ['obj', 'bin', '.vs', 'specs'];

// Classes that are intentionally only referenced within their declaring file or via
// framework mechanisms that text search can't detect (reflection, attributes, etc.)
const KNOWN_INTERNAL = new Set([
    // Extension method holder classes — called via extension syntax, not class name
    'BlueprintTypePrefixes',
    'BlueprintTypeExtensions',
    'SkillNameExtensions',
    // Entry point — called by the runtime
    'Program',
    // BL-108 DTOs — consumed by BlueprintViewModel and BlueprintService (in-progress refactoring)
    'BlueprintUpdateRequest',
    'BlueprintCreateRequest',
]);

function findCsFiles(dir, results, includeDesigner) {
    results = results || [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.includes(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findCsFiles(fullPath, results, includeDesigner);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            if (!includeDesigner && entry.name.endsWith('.Designer.cs')) continue;
            results.push(fullPath);
        }
    }
    return results;
}

// Extract class declarations from non-Designer files
function extractClasses(filePath, content) {
    const classes = [];
    const regex = /^\s*(?:public|internal)\s+(?:(?:static|abstract|sealed|partial)\s+)*class\s+(\w+)/gm;
    let match;
    while ((match = regex.exec(content)) !== null) {
        const name = match[1];
        if (KNOWN_INTERNAL.has(name)) continue;
        const line = content.substring(0, match.index).split('\n').length;
        classes.push({ name, file: filePath, line });
    }
    return classes;
}

// Main
const declFiles = findCsFiles(SRC_DIR, [], false); // non-Designer for declarations
const allSrcFiles = findCsFiles(SRC_DIR, [], true); // all files including Designer for references
const testFiles = findCsFiles(TEST_DIR, [], true);  // test files as reference sources
const refFiles = [...allSrcFiles, ...testFiles];

// Step 1: Extract all class declarations
const allClasses = [];
for (const file of declFiles) {
    const content = fs.readFileSync(file, 'utf8');
    allClasses.push(...extractClasses(file, content));
}

// Step 2: Load all file contents for reference searching
const fileContents = new Map();
for (const file of refFiles) {
    fileContents.set(file, fs.readFileSync(file, 'utf8'));
}

// Step 3: For each class, check references
const findings = [];

for (const cls of allClasses) {
    let foundInOtherFile = false;
    let selfFileOccurrences = 0;

    for (const [file, content] of fileContents) {
        if (file === cls.file) {
            // Count occurrences in declaring file
            // Use word boundary to avoid partial matches (e.g. "Item" matching "ItemBag")
            const re = new RegExp('\\b' + cls.name + '\\b', 'g');
            const matches = content.match(re);
            selfFileOccurrences = matches ? matches.length : 0;
        } else {
            // Check other files with word boundary
            const re = new RegExp('\\b' + cls.name + '\\b');
            if (re.test(content)) {
                foundInOtherFile = true;
                break;
            }
        }
    }

    if (!foundInOtherFile && selfFileOccurrences < 2) {
        // Only appears once (the declaration) — truly unused
        const relFile = path.relative('.', cls.file).replace(/\\/g, '/');
        findings.push(`UNUSED CLASS: ${cls.name} (${relFile}:${cls.line})`);
    }
}

if (findings.length === 0) {
    console.log('Clean — no unused classes found');
    process.exit(0);
} else {
    console.log('=== Unused Class Check ===\n');
    findings.sort().forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
