#!/usr/bin/env node
/**
 * logger-check.js — Verify all ViewModel and Service classes have NLog Logger.
 *
 * Scans OE2EmpireTracker/ViewModels/*.cs and OE2EmpireTracker/Services/*.cs
 * for classes that are missing `LogManager.GetCurrentClassLogger()`.
 *
 * Excludes:
 * - Interfaces (files starting with I and containing only interface declarations)
 * - Static helper classes with no state (pure functions)
 * - Model-like classes in Services/ that are just data containers
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const DIRS = [
    path.join('OE2EmpireTracker', 'ViewModels'),
    path.join('OE2EmpireTracker', 'Services'),
];

// Files that are intentionally exempt:
// - Pure data containers / filter criteria with no behavior worth logging
// - Static utility classes with pure functions (no state, no side effects)
// - Tiny configuration classes
// - Reference counters (pure query classes, no mutations to log)
const EXEMPT = new Set([
    'IdealColonyStructureWorkers',      // inside IColonyStructureWorkers.cs
    'BlueprintFilterCriteria',          // pure data container for filter state
    'JsonSettings',                     // static config, 3 lines
    'SortedDictionaryContractResolver', // JSON serialization plumbing
    'SystemClock',                      // static DateTime.UtcNow wrapper for testing
    'SurveyDateTimeParser',             // static pure parse/format functions
    'SerializationSorter',              // static pure sort functions
    'HelpTopicRegistry',                // static dictionary of help topic mappings
    'TabWarningService',                // static pure threshold evaluation functions
    'BuildPlanReferenceCounter',        // static pure query — counts references
    'DeliveryPlanReferenceCounter',     // static pure query — counts references
    'DeliveryRouteReferenceCounter',    // static pure query — counts references
    'StockPlanReferenceCounter',        // static pure query — counts references
    'CollectionSortHelper',             // static pure sort functions — no state
]);

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

        if (EXEMPT.has(className)) continue;

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
