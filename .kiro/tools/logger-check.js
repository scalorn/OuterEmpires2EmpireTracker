#!/usr/bin/env node
/**
 * logger-check.js — Verify all ViewModel and Service classes have NLog Logger.
 *
 * Scans OE2EmpireTracker/ViewModels/*.cs and OE2EmpireTracker/Services/*.cs
 * for classes that are missing `LogManager.GetCurrentClassLogger()`.
 *
 * Excludes:
 * - Interfaces (files starting with I and containing only interface declarations)
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const DIRS = [
    path.join('OE2EmpireTracker', 'ViewModels'),
    path.join('OE2EmpireTracker', 'Services'),
];

const findings = [];

for (const dir of DIRS) {
    if (!fs.existsSync(dir)) continue;
    const files = fs.readdirSync(dir).filter(f => f.endsWith('.cs') && !f.endsWith('.Designer.cs'));

    for (const file of files) {
        const fullPath = path.join(dir, file);
        const content = fs.readFileSync(fullPath, 'utf8');
        const relPath = fullPath.replace(/\\/g, '/');

        // Skip files that only contain interfaces
        if (/^\s*public\s+interface\s+/m.test(content) && !/^\s*public\s+class\s+/m.test(content)) continue;

        // Skip files with no class declaration
        if (!/^\s*public\s+(static\s+)?class\s+/m.test(content)) continue;

        // Extract class name
        const classMatch = content.match(/public\s+(?:static\s+)?class\s+(\w+)/);
        if (!classMatch) continue;
        const className = classMatch[1];

        // Skip pure data containers (DTOs, POCOs with only properties)
        const DATA_CONTAINERS = new Set([
            'LocalRankData', 'LocalSkillData', 'SkillUpdateData',
            'PlayerProfileUpdateRequest', 'PlayerProfileCreateRequest',
            'BlueprintUpdateRequest', 'BlueprintCreateRequest',
        ]);
        if (DATA_CONTAINERS.has(className)) continue;

        if (!content.includes('LogManager.GetCurrentClassLogger()')) {
            findings.push(`MISSING: ${className} has no NLog Logger (${relPath})`);
        }
    }
}

if (findings.length === 0) {
    console.log('Clean — all ViewModel and Service classes have NLog Logger');
    process.exit(0);
} else {
    console.log('=== Logger Check ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
