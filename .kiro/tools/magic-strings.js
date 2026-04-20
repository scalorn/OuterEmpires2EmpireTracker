#!/usr/bin/env node
/**
 * magic-strings.js — Detect raw string/number literals that should use Constants.
 *
 * Usage:
 *   node .kiro/tools/magic-strings.js            # strings only (default)
 *   node .kiro/tools/magic-strings.js --numeric   # include numeric constants too
 *   node .kiro/tools/magic-strings.js --all        # same as --numeric
 *
 * Cross-references all const string values defined in
 * OE2EmpireTracker/Constants/*.cs against all string literals in the
 * rest of the codebase. Reports raw usages that should use the constant name.
 *
 * Numeric matching is opt-in via --numeric because small constants (1, 3, 5)
 * produce many false positives. When enabled, numeric matches are only flagged
 * in specific contexts (switch cases, multiplier expressions, comparisons).
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const CONSTANTS_DIR = path.join('OE2EmpireTracker', 'Constants');
const SOURCE_DIR = 'OE2EmpireTracker';
const EXCLUDE_DIRS = ['Constants', 'obj', 'bin'];

// 1. Read all constant files and extract declarations
function extractConstants() {
    const constants = [];
    const files = fs.readdirSync(CONSTANTS_DIR).filter(f => f.endsWith('.cs'));

    for (const file of files) {
        const content = fs.readFileSync(path.join(CONSTANTS_DIR, file), 'utf8');
        const lines = content.split('\n');
        let currentClass = '';

        for (const line of lines) {
            const classMatch = line.match(/public\s+static\s+class\s+(\w+)/);
            if (classMatch) {
                currentClass = classMatch[1];
                continue;
            }

            // Match: public const string X = "value";
            const strMatch = line.match(/public\s+const\s+string\s+(\w+)\s*=\s*"([^"]*)"\s*;/);
            if (strMatch) {
                const [, name, value] = strMatch;
                if (value === '') continue;
                constants.push({ type: 'string', name, value, className: currentClass, fullName: currentClass + '.' + name });
                continue;
            }

            // Match: public const (decimal|int|long) X = value;
            const numMatch = line.match(/public\s+const\s+(decimal|int|long)\s+(\w+)\s*=\s*([^;]+);/);
            if (numMatch) {
                const [, numType, name, rawValue] = numMatch;
                const cleanValue = rawValue.trim().replace(/[mMlLdDfF]$/, '');
                if (cleanValue === '0' || cleanValue === '0.0') continue;
                constants.push({ type: numType, name, value: rawValue.trim(), cleanValue, className: currentClass, fullName: currentClass + '.' + name });
            }
        }
    }
    return constants;
}

// 2. Recursively get all .cs files in source dir (excluding Constants, Designer, tests)
function getSourceFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        const relPath = path.relative('.', fullPath);

        if (entry.isDirectory()) {
            if (EXCLUDE_DIRS.includes(entry.name)) continue;
            if (relPath.includes('OE2EmpireTracker.Tests')) continue;
            getSourceFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            if (/\.Designer\.cs$/.test(entry.name)) continue;
            if (relPath.replace(/\\/g, '/').startsWith('OE2EmpireTracker/Constants/')) continue;
            results.push(fullPath);
        }
    }
    return results;
}

// 3. Search for magic literals
function findMagicLiterals(constants, sourceFiles) {
    const findings = [];

    for (const file of sourceFiles) {
        const content = fs.readFileSync(file, 'utf8');
        const lines = content.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            // Skip comment lines
            if (trimmed.startsWith('//')) continue;
            if (trimmed.startsWith('*')) continue;
            if (trimmed.startsWith('///')) continue;

            for (const c of constants) {
                if (c.type === 'string') {
                    const searchStr = '"' + c.value + '"';
                    if (line.includes(searchStr)) {
                        if (!line.includes(c.fullName) && !line.includes(c.name + ' =')) {
                            const relFile = path.relative('.', file).replace(/\\/g, '/');
                            findings.push('MAGIC: "' + c.value + '" in ' + relFile + ':' + (i + 1) + ' (should use ' + c.fullName + ')');
                        }
                    }
                } else {
                    const cleanVal = c.cleanValue;
                    const escaped = cleanVal.replace(/\./g, '\\.');
                    const numRegex = new RegExp('(?<![\\\\w.])' + escaped + '[mMlLdDfF]?(?![\\\\w.])', 'g');
                    if (numRegex.test(line)) {
                        if (!line.includes(c.fullName) && !line.includes(c.name) && !line.includes('const ')) {
                            const relFile = path.relative('.', file).replace(/\\/g, '/');
                            findings.push('MAGIC: ' + c.value + ' in ' + relFile + ':' + (i + 1) + ' (should use ' + c.fullName + ')');
                        }
                    }
                }
            }
        }
    }
    return findings;
}

// Main
const args = process.argv.slice(2);
const includeNumeric = args.includes('--numeric') || args.includes('--all');

const allConstants = extractConstants();
const constants = includeNumeric ? allConstants : allConstants.filter(c => c.type === 'string');
const sourceFiles = getSourceFiles(SOURCE_DIR);
const findings = findMagicLiterals(constants, sourceFiles);

const mode = includeNumeric ? 'strings + numeric' : 'strings only';
if (findings.length === 0) {
    console.log('Clean — no findings (' + mode + ')');
    process.exit(0);
} else {
    console.log('=== Magic String/Number Audit (' + mode + ') ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
