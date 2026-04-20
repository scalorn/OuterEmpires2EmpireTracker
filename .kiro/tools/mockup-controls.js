#!/usr/bin/env node
/**
 * mockup-controls.js — Cross-reference mockup control names against Designer.cs files.
 *
 * Usage:
 *   node .kiro/tools/mockup-controls.js
 *
 * For each mockup in spec/mockups/, extracts control names (txt*, cmb*, cmd*, dgv*, chk*, lvw*, etc.)
 * and checks they exist in the corresponding Form*.Designer.cs file.
 * Also checks Designer.cs for controls not mentioned in the mockup.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const MOCKUPS_DIR = path.join('spec', 'mockups');
const FORMS_DIR = path.join('OE2EmpireTracker', 'Forms');

// Map mockup files to their form directories
const MOCKUP_TO_FORM = {
    'build-planner.md': ['BuildPlanner/FormBuildPlanner'],
    'ships.md': ['ShipTemplate/FormShipTemplate', 'ShipInstance/FormShipInstance'],
    'stations.md': ['Station/FormStation'],
    'market.md': ['Market/FormMarket'],
    'stock-targets.md': ['StockTargets/FormStockTargets'],
    'contacts.md': ['Contacts/FormContacts'],
    'asteroids.md': ['Asteroid/FormAsteroid'],
    'supply-chains.md': ['SupplyChain/FormSupplyChain'],
    'colony-overflow.md': ['ColonyV2/FormColonyV2'],
    'blueprints.md': ['BlueprintV2/FormBlueprintV2'],
    'surveys.md': ['Survey/FormSurvey'],
    'delivery-routes.md': ['DeliveryRoute/FormDeliveryRoute'],
    'delivery-execution.md': ['DeliveryExecution/FormDeliveryExecution'],
    'player-profile.md': ['PlayerProfile/FormPlayerProfile'],
    'pricing-plans.md': ['PricingPlan/FormPricingPlan'],
    'colony-activity.md': ['ColonyActivity/FormColonyActivity'],
    'colony-daily-build.md': ['ColonyDailyBuild/FormColonyDailyBuild'],
    'preferences.md': ['FormPreferences/FormPreferences'],
};

// Control name prefixes to look for
const CONTROL_PREFIXES = ['txt', 'cmb', 'cmd', 'dgv', 'chk', 'lvw', 'btn', 'lbl', 'flp', 'tab', 'rtb', 'dtp', 'pnl', 'split'];

function extractControlNames(content) {
    const names = new Set();
    // Match backtick-wrapped control names like `txtFilter` or `cmbResource`
    const backtickRegex = /`(\w+)`/g;
    let match;
    while ((match = backtickRegex.exec(content)) !== null) {
        const name = match[1];
        for (const prefix of CONTROL_PREFIXES) {
            if (name.startsWith(prefix)) {
                names.add(name);
                break;
            }
        }
    }
    return names;
}

function extractDesignerControls(content) {
    const names = new Set();
    // Match field declarations like: private System.Windows.Forms.Button cmdSave;
    const fieldRegex = /private\s+[\w.]+\s+(\w+)\s*;/g;
    let match;
    while ((match = fieldRegex.exec(content)) !== null) {
        const name = match[1];
        for (const prefix of CONTROL_PREFIXES) {
            if (name.startsWith(prefix)) {
                names.add(name);
                break;
            }
        }
    }
    return names;
}

const findings = [];

for (const [mockupFile, formPaths] of Object.entries(MOCKUP_TO_FORM)) {
    const mockupPath = path.join(MOCKUPS_DIR, mockupFile);
    if (!fs.existsSync(mockupPath)) continue;

    const mockupContent = fs.readFileSync(mockupPath, 'utf8');
    const mockupControls = extractControlNames(mockupContent);

    for (const formPath of formPaths) {
        const designerPath = path.join(FORMS_DIR, formPath + '.Designer.cs');
        if (!fs.existsSync(designerPath)) {
            findings.push('MISSING DESIGNER: ' + designerPath);
            continue;
        }

        const designerContent = fs.readFileSync(designerPath, 'utf8');
        const designerControls = extractDesignerControls(designerContent);

        // Controls in mockup but not in Designer
        for (const ctrl of mockupControls) {
            if (!designerControls.has(ctrl)) {
                findings.push('IN MOCKUP NOT CODE: ' + ctrl + ' (' + mockupFile + ' -> ' + formPath + ')');
            }
        }

        // Controls in Designer but not in mockup (only report significant ones, skip layout panels)
        for (const ctrl of designerControls) {
            if (!mockupControls.has(ctrl) && !ctrl.startsWith('flp') && !ctrl.startsWith('split') && !ctrl.startsWith('pnl')) {
                // Only report interactive controls, not layout containers
                if (ctrl.startsWith('txt') || ctrl.startsWith('cmb') || ctrl.startsWith('cmd') ||
                    ctrl.startsWith('dgv') || ctrl.startsWith('chk') || ctrl.startsWith('btn') ||
                    ctrl.startsWith('lvw')) {
                    findings.push('IN CODE NOT MOCKUP: ' + ctrl + ' (' + formPath + ' -> ' + mockupFile + ')');
                }
            }
        }
    }
}

if (findings.length === 0) {
    console.log('Clean — no findings');
    process.exit(0);
} else {
    console.log('=== Mockup-Controls Cross-Reference ===\n');
    findings.forEach(f => console.log(f));
    console.log('\n' + findings.length + ' findings');
    process.exit(1);
}
