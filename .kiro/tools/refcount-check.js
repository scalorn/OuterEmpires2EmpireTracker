#!/usr/bin/env node
/**
 * refcount-check.js — Verify reference counters match the spec's reference graph.
 *
 * Usage:
 *   node .kiro/tools/refcount-check.js
 *
 * Reads spec/design/reference-counting.md, extracts the reference graph table,
 * and checks each reference counter .cs file contains references to all sources
 * listed in the "Referenced By" column.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SPEC_FILE = path.join('spec', 'design', 'reference-counting.md');
const SERVICES_DIRS = [
    path.join('OE2EmpireTracker', 'Services'),
    path.join('OE2EmpireTracker.Common', 'Services')
];

// Parse the reference graph table from the spec
function parseReferenceTable(content) {
    const lines = content.split('\n');
    const entries = [];
    let inTable = false;
    let headerSkipped = false;

    for (const line of lines) {
        // Detect table start by looking for the header row
        if (line.includes('| Entity |') && line.includes('| Referenced By |') && line.includes('| Reference Counter |')) {
            inTable = true;
            continue;
        }

        // Skip separator row
        if (inTable && /^\|[-|]+\|$/.test(line.trim())) {
            headerSkipped = true;
            continue;
        }

        // Parse data rows
        if (inTable && headerSkipped && line.trim().startsWith('|')) {
            const cells = line.split('|').map(c => c.trim()).filter(c => c !== '');
            if (cells.length >= 3) {
                const entity = cells[0];
                const referencedBy = cells[1];
                const counterName = cells[2];

                // Skip entries with no counter (marked as —)
                if (counterName === '\u2014' || counterName === '-' || counterName === '') continue;

                // Extract the counter class name (e.g. "BlueprintReferenceCounter (expand)" -> "BlueprintReferenceCounter")
                const counterMatch = counterName.match(/^(\w+ReferenceCounter)/);
                if (!counterMatch) continue;

                // Parse referenced-by sources — extract entity names from the descriptions
                const sources = parseReferencedBy(referencedBy);

                entries.push({
                    entity,
                    counterClass: counterMatch[1],
                    sources,
                    rawReferencedBy: referencedBy
                });
            }
        }

        // End of table
        if (inTable && headerSkipped && !line.trim().startsWith('|') && line.trim() !== '') {
            break;
        }
    }
    return entries;
}

// Extract entity names from "Referenced By" text
// e.g. "ColonyStructure (Flatpack...), BuildItem.BlueprintUUID, Ship.HullBlueprintUUID"
// -> ["ColonyStructure", "BuildItem", "Ship", ...]
function parseReferencedBy(text) {
    const sources = new Set();
    // Split by comma, then extract the leading entity name from each part
    const parts = text.split(',');
    for (const part of parts) {
        const trimmed = part.trim();
        // Match leading word (entity name) — could be "EntityName.Field" or "EntityName (description)"
        const match = trimmed.match(/^(\w+)/);
        if (match) {
            const name = match[1];
            // Skip noise words
            if (!['when', 'not', 'combo', 'future'].includes(name.toLowerCase())) {
                sources.add(name);
            }
        }
    }
    return [...sources];
}

// Main
if (!fs.existsSync(SPEC_FILE)) {
    console.error('Spec file not found: ' + SPEC_FILE);
    process.exit(1);
}

const specContent = fs.readFileSync(SPEC_FILE, 'utf8');
const entries = parseReferenceTable(specContent);
const findings = [];

// Build common variable pattern alternatives for entity names
function getSearchPatterns(source) {
    const patterns = [source];
    // Add lowercase version (e.g. "Colony" -> "colony")
    if (source.length > 1) {
        patterns.push(source[0].toLowerCase() + source.slice(1));
    }
    // Add common abbreviations for compound names (e.g. "ColonyStructure" -> "structure", "Structure")
    const parts = source.match(/[A-Z][a-z]+/g);
    if (parts && parts.length > 1) {
        // Add last part as-is and lowercase (e.g. "ColonyStructure" -> "Structure", "structure")
        patterns.push(parts[parts.length - 1]);
        patterns.push(parts[parts.length - 1].toLowerCase());
    }
    return patterns;
}

for (const entry of entries) {
    // Search for the counter file in all service directories
    let counterFile = null;
    for (const dir of SERVICES_DIRS) {
        const candidate = path.join(dir, entry.counterClass + '.cs');
        if (fs.existsSync(candidate)) {
            counterFile = candidate;
            break;
        }
    }

    if (!counterFile) {
        findings.push('MISSING FILE: ' + entry.counterClass + '.cs not found in Services/');
        continue;
    }

    const counterContent = fs.readFileSync(counterFile, 'utf8');

    for (const source of entry.sources) {
        // Check if any search pattern for this source appears in the counter code
        const patterns = getSearchPatterns(source);
        const found = patterns.some(p => counterContent.includes(p));
        if (!found) {
            findings.push('MISSING: ' + entry.counterClass + ' does not reference ' + source + ' (entity: ' + entry.entity + ')');
        }
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Reference Counter Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
