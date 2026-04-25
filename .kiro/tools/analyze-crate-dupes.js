#!/usr/bin/env node
/**
 * Analyze crate JSON files for dedup key collisions.
 * Shows how many blueprints share the same Name+Evo+Type+Class+TechLevel
 * but have different stats/resources.
 */
const fs = require('fs');
const path = require('path');

const dir = 'OE2EmpireTracker.Tests/TestData/Crates';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));

const allBPs = [];
for (const file of files) {
    const data = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8'));
    for (const bp of data) {
        bp._file = file;
        allBPs.push(bp);
    }
}

console.log(`Total blueprints across ${files.length} files: ${allBPs.length}`);

// Group by current dedup key: Name + Evolution + BluePrintType + Class + TechLevel
// We don't have BluePrintType in the JSON (it's resolved at import time), so use name-based grouping
const groups = {};
for (const bp of allBPs) {
    const key = `${bp.name}|${bp.evolution}|${bp.techLevel || ''}`;
    if (!groups[key]) groups[key] = [];
    groups[key].push(bp);
}

// Find collisions (same key, multiple entries)
const collisions = Object.entries(groups).filter(([k, v]) => v.length > 1);
console.log(`\nUnique dedup keys: ${Object.keys(groups).length}`);
console.log(`Keys with multiple entries (collisions): ${collisions.length}`);
console.log(`Total blueprints in collision groups: ${collisions.reduce((s, [k, v]) => s + v.length, 0)}`);

// Show details of collisions
console.log('\n=== COLLISIONS ===');
for (const [key, bps] of collisions.sort((a, b) => b[1].length - a[1].length)) {
    console.log(`\n${key} (${bps.length} entries):`);
    for (const bp of bps) {
        const propSummary = Object.entries(bp.properties || {}).map(([k, v]) => `${k}=${v}`).join(', ');
        const resSummary = Object.entries(bp.resources || {}).map(([k, v]) => `${k}:${v}`).join(', ');
        console.log(`  [${bp._file.substring(11, 30)}] props: {${propSummary.substring(0, 100)}}`);
        console.log(`    resources: {${resSummary.substring(0, 100)}}`);
    }
}

// Show a specific example
console.log('\n=== EXAMPLE: 10cm Shardstorm Fragmentation Munitions Evo 2 ===');
const example = allBPs.filter(bp => bp.name.includes('Shardstorm') && bp.name.includes('10cm') && bp.evolution === 2);
for (const bp of example) {
    console.log(`  File: ${bp._file}`);
    console.log(`  Props:`, bp.properties);
    console.log(`  Resources:`, bp.resources);
    console.log('');
}
