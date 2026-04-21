#!/usr/bin/env node
/**
 * control-wiring.js — Check that Form controls follow required patterns.
 *
 * Usage:
 *   node .kiro/tools/control-wiring.js
 *
 * For each Form*.cs, checks:
 *
 * Universal checks (all forms including dialogs):
 * - DataError handler on grids with combo columns (REQ-ARCH-114)
 * - No UUID shown as display fallback (REQ-ARCH-111)
 *
 * Full form checks (excludes DIALOG_FORMS):
 * - IProgrammaticUpdateSource implementation
 * - NLog Logger declaration
 * - CurrentPlayerChanged subscription
 * - OnFormClosed event unsubscription
 * - _isProgrammaticUpdate field
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const FORMS_DIR = path.join('OE2EmpireTracker', 'Forms');

// Dialog forms: modal popups that don't need IProgrammaticUpdateSource,
// CurrentPlayerChanged, or OnFormClosed. Still checked for DataError
// handlers and UUID display.
const DIALOG_FORMS = [
    'FormAbout', 'FormHelp', 'FormAutoFill', 'FormPreferences',
    'FormListingEdit', 'FormRecordSale', 'FormStructureAllocation'
];

function findFormFiles(dir, results) {
    results = results || [];
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findFormFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.startsWith('Form') && entry.name.endsWith('.cs')) {
            if (/\.Designer\.cs$/.test(entry.name)) continue;
            const formName = entry.name.replace('.cs', '');
            results.push({ path: fullPath, name: formName, isDialog: DIALOG_FORMS.includes(formName) });
        }
    }
    return results;
}

const formFiles = findFormFiles(FORMS_DIR);
const findings = [];

for (const form of formFiles) {
    const content = fs.readFileSync(form.path, 'utf8');
    const relPath = path.relative('.', form.path).replace(/\\/g, '/');

    // === Universal checks (all forms) ===

    // Check DataError handler on grids with combo columns
    const designerPath = form.path.replace('.cs', '.Designer.cs');
    if (fs.existsSync(designerPath)) {
        const designerContent = fs.readFileSync(designerPath, 'utf8');
        if (designerContent.includes('DataGridViewComboBoxColumn') || content.includes('DataGridViewComboBoxCell')) {
            if (!content.includes('DataError')) {
                findings.push('MISSING: ' + form.name + ' has combo columns but no DataError handler (' + relPath + ')');
            }
        }
    }

    // Check for UUID shown as display fallback
    const uuidFallbackRe = /(?:ExtendedName|Name|Display)\s*\?\?\s*\w+\.(?:UUID|BlueprintUUID|TemplateUUID)\b/g;
    const uuidMatches = content.match(uuidFallbackRe);
    if (uuidMatches) {
        for (const m of uuidMatches) {
            findings.push('UUID_DISPLAY: ' + form.name + ' shows UUID as fallback display: ' + m.trim() + ' (' + relPath + ')');
        }
    }

    // === Full form checks (skip for dialog forms) ===
    if (form.isDialog) continue;

    if (!content.includes('IProgrammaticUpdateSource')) {
        findings.push('MISSING: ' + form.name + ' does not implement IProgrammaticUpdateSource (' + relPath + ')');
    }
    if (!content.includes('LogManager.GetCurrentClassLogger()')) {
        findings.push('MISSING: ' + form.name + ' has no NLog Logger (' + relPath + ')');
    }
    if (!content.includes('CurrentPlayerChanged')) {
        findings.push('MISSING: ' + form.name + ' does not subscribe to CurrentPlayerChanged (' + relPath + ')');
    }
    if (!content.includes('OnFormClosed')) {
        findings.push('MISSING: ' + form.name + ' does not override OnFormClosed (' + relPath + ')');
    }
    if (!content.includes('_isProgrammaticUpdate')) {
        findings.push('MISSING: ' + form.name + ' has no _isProgrammaticUpdate field (' + relPath + ')');
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Control Wiring Audit ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
