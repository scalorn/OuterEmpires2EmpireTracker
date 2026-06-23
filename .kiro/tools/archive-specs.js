#!/usr/bin/env node
/**
 * archive-specs.js — Moves completed specs to .kiro/specs/_completed/
 * 
 * A spec is "completed" if its tasks.md exists and contains NO unchecked
 * task lines (lines matching /^- \[ \]/ or /^\s+- \[ \]/).
 * 
 * Usage: node .kiro/tools/archive-specs.js [--dry-run]
 */
const fs = require('fs');
const path = require('path');

const SPECS_DIR = path.join(__dirname, '..', 'specs');
const COMPLETED_DIR = path.join(SPECS_DIR, '_completed');
const dryRun = process.argv.includes('--dry-run');

if (!fs.existsSync(COMPLETED_DIR)) {
    fs.mkdirSync(COMPLETED_DIR, { recursive: true });
}

const entries = fs.readdirSync(SPECS_DIR, { withFileTypes: true });
let moved = 0;
let skipped = 0;

for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (entry.name === '_completed') continue;

    const specDir = path.join(SPECS_DIR, entry.name);
    const tasksFile = path.join(specDir, 'tasks.md');

    if (!fs.existsSync(tasksFile)) {
        // No tasks.md — skip (incomplete spec)
        skipped++;
        continue;
    }

    const content = fs.readFileSync(tasksFile, 'utf8');
    const lines = content.split('\n');

    // Check for any unchecked task boxes
    const hasUnchecked = lines.some(line => /^\s*- \[ \]/.test(line));

    if (hasUnchecked) {
        skipped++;
        if (dryRun) console.log(`  ACTIVE: ${entry.name}`);
        continue;
    }

    // All tasks checked — move to _completed
    const dest = path.join(COMPLETED_DIR, entry.name);
    if (dryRun) {
        console.log(`  MOVE: ${entry.name}`);
    } else {
        fs.renameSync(specDir, dest);
    }
    moved++;
}

console.log(`\n${dryRun ? '[DRY RUN] ' : ''}Completed: ${moved} moved, ${skipped} kept active.`);
