#!/usr/bin/env node
/**
 * repair-blueprint-itemtype.js — Fixes corrupted Blueprint entries in JSON data files.
 *
 * Blueprints are Items and should have BOTH ItemType="Blueprint" AND a valid BluePrintType.
 * This tool fixes two corruption patterns:
 *
 * 1. ItemType changed from "Blueprint" to something else (e.g. "Survey")
 *    → Sets ItemType back to "Blueprint"
 *
 * 2. BluePrintType is wrong (e.g. a "Mining Rig Flatpack" with type
 *    "Flatpacks/CommodityFactory/MiningIndustryCentre" instead of "Flatpacks/MiningRig")
 *    → Looks up the correct type from the BlueprintType definitions by matching the Name
 *
 * Usage:
 *   node .kiro/tools/repair-blueprint-itemtype.js                    # dry run
 *   node .kiro/tools/repair-blueprint-itemtype.js --fix              # apply fixes
 */

const fs = require('fs');

const args = process.argv.slice(2);
const fix = args.includes('--fix');

const FILES = [
    'OE2EmpireTracker/BaselineData.json',
    'OE2EmpireTracker.Tests/TestData/BaselineData.json',
    'OE2EmpireTracker/Alpha3.json',
];

let totalFixed = 0;

for (const file of FILES) {
    if (!fs.existsSync(file)) continue;

    const raw = fs.readFileSync(file, 'utf8');
    let data;
    try {
        data = JSON.parse(raw);
    } catch (e) {
        console.log(`PARSE ERROR: ${file}: ${e.message}`);
        continue;
    }

    // Build name→type lookup from BlueprintType definitions
    const nameToType = {};
    for (const bt of (data.BlueprintType || [])) {
        if (bt.Name && bt.Id) {
            nameToType[bt.Name] = bt.Id;
        }
    }

    const blueprints = data.Blueprint || [];
    let fileFixed = 0;

    for (const bp of blueprints) {
        const fixes = [];

        // Fix 1: ItemType should be "Blueprint"
        if (bp.ItemType !== undefined && bp.ItemType !== 'Blueprint') {
            fixes.push(`ItemType "${bp.ItemType}" → "Blueprint"`);
            if (fix) bp.ItemType = 'Blueprint';
        }

        // Fix 2: BluePrintType should match the name in the type definitions
        if (bp.Name && nameToType[bp.Name] && bp.BluePrintType !== nameToType[bp.Name]) {
            const correctType = nameToType[bp.Name];
            fixes.push(`BluePrintType "${bp.BluePrintType || '(missing)'}" → "${correctType}"`);
            if (fix) bp.BluePrintType = correctType;
        }

        if (fixes.length > 0) {
            console.log(`  ${bp.Name} (${bp.UUID}): ${fixes.join(', ')}`);
            fileFixed++;
        }
    }

    if (fileFixed > 0) {
        console.log(`${file}: ${fileFixed} blueprint(s) to fix\n`);
        totalFixed += fileFixed;

        if (fix) {
            const output = JSON.stringify(data, null, 2);
            fs.writeFileSync(file, output, 'utf8');
            console.log(`  Written: ${file}\n`);
        }
    } else {
        console.log(`${file}: clean`);
    }
}

console.log(`\nTotal: ${totalFixed} blueprints ${fix ? 'fixed' : 'need fixing'}`);
if (!fix && totalFixed > 0) {
    console.log('Run with --fix to apply repairs.');
}

process.exit(totalFixed > 0 && !fix ? 1 : 0);
