#!/usr/bin/env node
/**
 * inline-sort-check.js — Detect inline sorting of data model collections.
 *
 * Usage:
 *   node .kiro/tools/inline-sort-check.js
 *
 * Scans all .cs files in OE2EmpireTracker/ and OE2EmpireTracker.Tests/
 * (excluding bin/, obj/, Designer.cs).
 *
 * Skips CollectionSortHelper.cs and SerializationSorter.cs (the only allowed sort locations).
 *
 * Matches lines containing .OrderBy(, .OrderByDescending(, .ThenBy(, .ThenByDescending(, .Sort(
 * and reports findings with file, line number, and matched expression.
 *
 * Smart exemptions:
 * - Sorts on local/transient data (int[] indices, local tuples, UI selection items)
 * - ListView.Sort() calls (UI sort)
 * - Static enum/constant initialization sorts (Models/*.cs static list init)
 * - PropertyBag/dictionary key sorts (infrastructure)
 * - Local variable sorts (not model collection properties)
 *
 * Exit code 0 = clean, exit code 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SRC_DIRS = ['OE2EmpireTracker', 'OE2EmpireTracker.Tests'];
const SKIP_DIRS = new Set(['bin', 'obj', '.vs', 'specs']);

// Files where sorting is allowed (the canonical sort locations)
const ALLOWED_FILES = new Set([
    'CollectionSortHelper.cs',
    'SerializationSorter.cs',
]);

// Files with only local/computation sorts that are fully exempt
const EXEMPT_FILES = new Set([
    'MinerSetupHelper.cs',      // Sorting survey candidates by computed match distance
]);

// Sort patterns to detect
const SORT_PATTERN = /\.(OrderBy|OrderByDescending|ThenBy|ThenByDescending|Sort)\s*\(/;

// Exemption patterns — lines matching these are NOT findings
const EXEMPT_PATTERNS = [
    // Sorting int/index arrays (e.g. indices.OrderBy(x => x))
    /indices\s*\.\s*(OrderBy|OrderByDescending)/i,

    // ListView.Sort() — UI sort call (no arguments)
    /\.Sort\(\s*\)/,

    // Static list initialization sorts: instance.Sort((x, y) => ...)
    /instance\.Sort\s*\(\s*\(/,

    // Local tuple/anonymous type sorts: sorted.Sort((a, b) => ...)
    /sorted\.Sort\s*\(\s*\(/,

    // Sorting dictionary keys (infrastructure): .OrderBy(k => k, StringComparer
    /\.OrderBy\s*\(\s*k\s*=>\s*k\s*,/,

    // Sorting by kvp.Key or kv.Key (dictionary key sort)
    /\.OrderBy\s*\(\s*kv\s*=>\s*kv\.Key/,
    /\.OrderBy\s*\(\s*kvp\s*=>\s*kvp\.Key/,

    // PropertyBag key sorts
    /Properties\.Keys/,

    // internalProps.OrderBy (MarketBlueprintImporter infrastructure)
    /internalProps\.OrderBy/,

    // Sorting local tuples by computed fields (e.g. group.resource, group.purity)
    /\.OrderBy\s*\(\s*g\s*=>\s*g\.\s*(resource|purity)\s*\)/,
    /\.ThenBy\s*\(\s*g\s*=>\s*g\.\s*(resource|purity)\s*\)/,

    // Sorting by runtime-computed values (structureLoad dictionary lookup)
    /structureLoad\[/,

    // Sorting IEnumerable<int> (index manipulation: x => x)
    /\.OrderBy\s*\(\s*x\s*=>\s*x\s*\)/,
    /\.OrderByDescending\s*\(\s*x\s*=>\s*x\s*\)/,

    // ItemBag JSON converter sort (serialization infrastructure in Models)
    /value\.Items/,

    // Shuffling for tests (OrderBy(_ => rng.Next()))
    /OrderBy\s*\(\s*_\s*=>\s*(rng|random)\./i,
    /OrderBy\s*\(\s*_\s*=>\s*\w+\.Next/,

    // Sorting needed.Values (local computation result, not model collection)
    /needed\.Values\.OrderBy/,

    // Colony refining tier sort (local computation)
    /RefiningRecipes\.GetTier/,

    // Gen.Shuffle / test data generation
    /Gen\.Shuffle/,
    /shuffled\.\w+\(\w+\)\.\s*OrderBy/,

    // Sorting entries.Sort (MainWindow window restore)
    /entries\.Sort/,

    // WindowStateHelper / ListViewItemSorter / ListViewItemComparer
    /ListViewItemSorter/,
    /ListViewItemComparer/,

    // SortedDictionaryContractResolver (JSON infrastructure)
    /dict\.Keys\.Cast/,

    // Static enum/constant list sorts (not persisted model collections)
    /Resource\.Resources\.OrderBy/,
    /ResourcePurity\.Purities/,
    /Commodity\.ResourceMapByEnum\.Values\.OrderBy/,
    /Commodity\.Commodities/,
    /WorkerDetail\.WorkerDetails/,
    /commodityNames\.OrderBy/,

    // EmpireContext static data sorts (blueprint types, tech levels, resources, etc.)
    /empireContext\.\w+List\s*\n?\s*\.Where.*\n?\s*\.OrderBy/,
    /empireContext\.BlueprintTypeList/,
    /EmpireContext\.GetInstance\(\)\?\.ResourceList/,
    /EmpireContext\.GetInstance\(\)\?\.CommodityList/,

    // Local DTO/summary sorts (not model collections)
    /summary\.ItemBreakdown\.OrderBy/,
    /rows\.OrderBy\s*\(\s*r\s*=>\s*r\.ColonyName/,
    /\.ThenBy\s*\(\s*r\s*=>\s*r\.StructureName/,

    // ItemBag dictionary value sorts — refactored to CollectionSortHelper.OrderItemBagEntries

    // Local filtered list sorts on static game data (Resource, Commodity, WorkerDetail copies)
    // These are local variables copied from static enum lists, not model collections
    /filteredList\s*[\s\S]*\.OrderBy\s*\(\s*p\s*=>\s*p\.Name\s*\)/,
    /filteredList\s*[\s\S]*\.OrderBy\s*\(\s*p\s*=>\s*p\.ExtendedName\s*\)/,
    /filteredList\s*[\s\S]*\.OrderBy\s*\(\s*c\s*=>\s*c\.ExtendedName\s*\)/,

    // Local filtered list sorts — refactored to CollectionSortHelper

    // Local variable sorts from EmpireContext static data (resources, commodities assigned from EmpireContext/Resource.Resources)
    // These are local variables assigned from static enum lists, not model collections
    /resources\.OrderBy\s*\(\s*r\s*=>\s*r\.Name\s*\)/,
    /resources\.OrderBy\s*\(\s*x\s*=>\s*x\.Name\s*\)/,
    /commodities\.OrderBy\s*\(\s*c\s*=>\s*c\.Name\s*\)/,

    // BlueprintTypeList sorts (static game data, not persisted model collections)
    /flatpackTypes\s*.*\.OrderBy/,
    /\.Where\s*\(\s*bt\s*=>\s*bt\.Id\.IsFlatpack\(\)\s*\)\s*\n?\s*\.OrderBy/,

    // Local grouping sorts (.GroupBy().OrderBy(g => g.Key))
    /\.GroupBy\s*\(.*\)\s*\n?\s*\.OrderBy\s*\(\s*g\s*=>\s*g\.Key\s*\)/,

    // Test assertion sorts — sorting extracted scalars for comparison
    // These sort local projections (.Select(...).OrderBy) for deterministic assertions
    /\.UUID\s*\)\s*\.\s*OrderBy/,
    /\.Select\s*\(\s*\w+\s*=>\s*\w+\.UUID\s*\)/,

    // WindowStateHelper — grid.Sort (UI sort restoration)
    /grid\.Sort\s*\(/,

    // DeliveryGenerationService — resources.OrderBy(r => r.Key) (local dictionary)
    /resources\.OrderBy\s*\(\s*r\s*=>\s*r\.Key\s*\)/,

    // ColonyStructureV2 combo items — sorting local selection items (not model collections)
    /items\.Sort\s*\(\s*\(\s*a\s*,\s*b\s*\)\s*=>\s*string\.Compare\s*\(\s*a\.DisplayName/,
    /unrefinedItems\.Sort/,

    // FormBlueprintV2 graph points — sorting local chart data
    /dataPoints\.Sort/,
    /points\.Sort\s*\(\s*\(\s*a\s*,\s*b\s*\)\s*=>\s*a\.Evolution/,

    // FormSurvey distribution — sorting projected string lists (resource names, purities) for combo population
    /\.Distinct\(\)\s*\n?\s*\.OrderBy\s*\(\s*r\s*=>\s*r\s*\)/,
    /\.Distinct\(\)\s*\n?\s*\.OrderBy\s*\(\s*p\s*=>\s*p\s*\)/,

    // FormColonyActivity — refactored to CollectionSortHelper.OrderActivityRowsByTimeRemaining

    // FormBanking chart/filter sorts — sorting local computed buckets/groups, not model collections
    /buckets\.OrderBy\s*\(\s*b\s*=>\s*b\.Key\s*\)/,
    /\.OrderByDescending\s*\(\s*g\s*=>\s*g\.Total\s*\)/,

    // FormBanking/FormBankingEntry type label sorts — sorting static lookup labels for combo population
    /sortedLabels|BankingTransactionTypes\.TypeLabels\.Values/,
    /\.OrderBy\s*\(\s*label\s*=>\s*label\s*,\s*StringComparer/,
    /\.OrderBy\s*\(\s*kvp\s*=>\s*kvp\.Value\s*,\s*StringComparer/,

    // ColonyAdminReportBuilder refining group sorts — sorting local anonymous-type computation results
    /refiningGroups\.OrderBy\s*\(\s*g\s*=>\s*g\.Resource\s*\)/,
];

function findCsFiles(dir, results) {
    results = results || [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.has(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findCsFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) {
            results.push(fullPath);
        }
    }
    return results;
}

function isExempt(line, prevLine, prevPrevLine) {
    for (const pattern of EXEMPT_PATTERNS) {
        if (pattern.test(line)) return true;
        // Also check previous line + current line together (multi-line expressions)
        if (prevLine && pattern.test(prevLine + ' ' + line.trim())) return true;
        // Check 2 lines back for multi-line chains (e.g. filteredList = filteredList\n.Where(...)\n.OrderBy(...))
        if (prevPrevLine && pattern.test(prevPrevLine + ' ' + prevLine.trim() + ' ' + line.trim())) return true;
    }
    return false;
}

// Main
const allFiles = [];
for (const dir of SRC_DIRS) {
    findCsFiles(dir, allFiles);
}

const findings = [];

for (const filePath of allFiles) {
    const fileName = path.basename(filePath);

    // Skip allowed files
    if (ALLOWED_FILES.has(fileName)) continue;

    // Skip fully exempt files (only local/computation sorts)
    if (EXEMPT_FILES.has(fileName)) continue;

    // Skip test project — test sorts are for assertions/verification, not model consumption
    const relFile = path.relative('.', filePath).replace(/\\/g, '/');
    if (relFile.startsWith('OE2EmpireTracker.Tests/')) continue;

    const content = fs.readFileSync(filePath, 'utf8');
    const lines = content.split('\n');

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        if (!SORT_PATTERN.test(line)) continue;

        // Check if line is in a comment
        const trimmed = line.trim();
        if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('///')) continue;

        const prevLine = i > 0 ? lines[i - 1] : '';
        const prevPrevLine = i > 1 ? lines[i - 2] : '';

        // Check exemptions
        if (isExempt(line, prevLine, prevPrevLine)) continue;

        const lineRelFile = path.relative('.', filePath).replace(/\\/g, '/');
        const lineNum = i + 1;
        const expression = trimmed.length > 120 ? trimmed.substring(0, 120) + '...' : trimmed;
        findings.push(`INLINE SORT: ${lineRelFile}:${lineNum}  ${expression}`);
    }
}

if (findings.length === 0) {
    console.log('Clean \u2014 no findings');
    process.exit(0);
} else {
    console.log('=== Inline Sort Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
