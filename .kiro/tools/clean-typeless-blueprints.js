#!/usr/bin/env node
/**
 * clean-typeless-blueprints.js
 *
 * Removes blueprint entries that have no BluePrintType (null, undefined, or empty string)
 * from the three JSON data files:
 *   - OE2EmpireTracker/Alpha3.json
 *   - OE2EmpireTracker/PlayerData.json
 *   - OE2EmpireTracker/BaselineData.json
 *
 * Usage:
 *   node .kiro/tools/clean-typeless-blueprints.js           # removes and writes
 *   node .kiro/tools/clean-typeless-blueprints.js --dry-run # report only
 */

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '../..');
const DRY_RUN = process.argv.includes('--dry-run');

const FILES = [
    path.join(ROOT, 'OE2EmpireTracker', 'Alpha3.json'),
    path.join(ROOT, 'OE2EmpireTracker', 'PlayerData.json'),
    path.join(ROOT, 'OE2EmpireTracker', 'BaselineData.json'),
];

function isTypeless(bp) {
    return bp.BluePrintType === null ||
           bp.BluePrintType === undefined ||
           bp.BluePrintType === '';
}

function processFile(filePath) {
    const relPath = path.relative(ROOT, filePath);
    if (!fs.existsSync(filePath)) {
        console.log(`\n[SKIP] ${relPath} — file not found`);
        return;
    }

    const raw = fs.readFileSync(filePath, 'utf8');
    const data = JSON.parse(raw);

    const blueprints = data.Blueprint;
    if (!Array.isArray(blueprints)) {
        console.log(`\n[SKIP] ${relPath} — no Blueprint array found`);
        return;
    }

    const total = blueprints.length;
    const removed = blueprints.filter(isTypeless);
    const remaining = blueprints.filter(bp => !isTypeless(bp));
    const removedCount = removed.length;

    console.log(`\n=== ${relPath} ===`);
    console.log(`  Total blueprints: ${total}`);
    console.log(`  Typeless (removing): ${removedCount}`);
    console.log(`  Remaining: ${remaining.length}`);

    if (removedCount > 0) {
        const showCount = Math.min(removedCount, 20);
        console.log(`  First ${showCount} removed:`);
        removed.slice(0, showCount).forEach(bp => {
            const evo = bp.Evolution !== undefined ? ` evo=${bp.Evolution}` : '';
            console.log(`    - ${bp.Name || '(unnamed)'}${evo} [UUID: ${bp.UUID || '?'}]`);
        });
        if (removedCount > 20) {
            console.log(`    ... and ${removedCount - 20} more`);
        }
    }

    if (!DRY_RUN && removedCount > 0) {
        data.Blueprint = remaining;
        fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
        console.log(`  ✓ Written (${remaining.length} blueprints kept)`);
    } else if (DRY_RUN && removedCount > 0) {
        console.log(`  [DRY RUN] Would write ${remaining.length} blueprints`);
    }
}

console.log(DRY_RUN ? '=== DRY RUN MODE ===' : '=== CLEANING TYPELESS BLUEPRINTS ===');

let totalRemoved = 0;
for (const filePath of FILES) {
    const raw = fs.existsSync(filePath) ? fs.readFileSync(filePath, 'utf8') : null;
    if (raw) {
        const data = JSON.parse(raw);
        if (Array.isArray(data.Blueprint)) {
            totalRemoved += data.Blueprint.filter(isTypeless).length;
        }
    }
    processFile(filePath);
}

console.log(`\n--- Summary ---`);
console.log(`Total typeless blueprints ${DRY_RUN ? 'found' : 'removed'}: ${totalRemoved}`);
if (DRY_RUN && totalRemoved > 0) {
    console.log('Run without --dry-run to remove them.');
}
