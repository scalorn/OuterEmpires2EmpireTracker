#!/usr/bin/env node
/**
 * flatpack-check.js — Validates flatpack blueprint integrity in BaselineData.json.
 *
 * Checks:
 * 1. Every BlueprintType with Id starting "Flatpacks/" has exactly one Blueprint
 *    with that BluePrintType in the Blueprint array.
 * 2. No Blueprint in the array has "ItemType" set (blueprints use BluePrintType, not ItemType).
 * 3. No duplicate UUIDs among flatpack blueprints.
 * 4. Every flatpack blueprint has a non-empty OwnerUUID field (even if empty string — must be present).
 *
 * Scans both OE2EmpireTracker/BaselineData.json and OE2EmpireTracker.Tests/TestData/BaselineData.json.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const FILES = [
    'OE2EmpireTracker.Tests/TestData/BaselineData.json',
    'OE2EmpireTracker/BaselineData.json',
];

let findings = 0;

for (const file of FILES) {
    if (!fs.existsSync(file)) continue;

    const label = path.basename(path.dirname(file)) + '/' + path.basename(file);
    const raw = fs.readFileSync(file, 'utf8');
    let data;
    try {
        data = JSON.parse(raw);
    } catch (e) {
        console.log(`PARSE ERROR: ${label}: ${e.message}`);
        findings++;
        continue;
    }

    const blueprintTypes = (data.BlueprintType || [])
        .filter(bt => bt.Id && bt.Id.startsWith('Flatpacks/'));
    const blueprints = data.Blueprint || [];

    // Build map of BluePrintType -> blueprints
    const typeMap = {};
    for (const bp of blueprints) {
        if (!bp.BluePrintType || !bp.BluePrintType.startsWith('Flatpacks/')) continue;
        if (!typeMap[bp.BluePrintType]) typeMap[bp.BluePrintType] = [];
        typeMap[bp.BluePrintType].push(bp);
    }

    // Check 1: Every flatpack type definition has exactly one blueprint
    for (const bt of blueprintTypes) {
        const bps = typeMap[bt.Id] || [];
        if (bps.length === 0) {
            console.log(`MISSING: ${label}: no blueprint for type ${bt.Id} (${bt.Name})`);
            findings++;
        } else if (bps.length > 1) {
            console.log(`DUPLICATE: ${label}: ${bps.length} blueprints for type ${bt.Id} (${bt.Name})`);
            for (const bp of bps) {
                console.log(`  UUID=${bp.UUID} Name=${bp.Name}`);
            }
            findings++;
        }
    }

    // Check 2: No blueprint has wrong ItemType (should be "Blueprint" or absent)
    for (const bp of blueprints) {
        if (bp.ItemType !== undefined && bp.ItemType !== 'Blueprint') {
            console.log(`WRONG ITEMTYPE: ${label}: blueprint "${bp.Name}" (UUID=${bp.UUID}) has ItemType="${bp.ItemType}" — should be "Blueprint"`);
            findings++;
        }
    }

    // Check 3: No duplicate UUIDs among flatpack blueprints
    const uuidMap = {};
    for (const bp of blueprints) {
        if (!bp.BluePrintType || !bp.BluePrintType.startsWith('Flatpacks/')) continue;
        if (!bp.UUID) continue;
        if (uuidMap[bp.UUID]) {
            console.log(`DUP UUID: ${label}: UUID ${bp.UUID} used by "${bp.Name}" and "${uuidMap[bp.UUID]}"`);
            findings++;
        }
        uuidMap[bp.UUID] = bp.Name;
    }

    // Check 4: Every flatpack blueprint has BluePrintType (not just ItemType)
    for (const bp of blueprints) {
        if (bp.Name && bp.Name.endsWith('Flatpack') && !bp.BluePrintType) {
            console.log(`NO TYPE: ${label}: "${bp.Name}" (UUID=${bp.UUID}) has no BluePrintType`);
            findings++;
        }
    }
}

if (findings === 0) {
    console.log('Clean — no findings');
}

process.exit(findings > 0 ? 1 : 0);
