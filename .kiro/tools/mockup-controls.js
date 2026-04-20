#!/usr/bin/env node
/**
 * mockup-controls.js — Cross-reference mockup control names against Designer.cs files.
 *
 * Usage:
 *   node .kiro/tools/mockup-controls.js
 *
 * Parses mockup files by section (### or #### headers) and maps each section to its
 * specific form Designer.cs. Supports:
 *   - Section-level mapping (multi-form mockups like ships.md)
 *   - Alias mapping (mockup says txtTemplateFilter, code says txtFilter)
 *   - Partial-form sections (colony-overflow.md covers only the Overflow tab)
 *   - Both private and internal field declarations
 *
 * Exit code 0 = clean, 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const MOCKUPS_DIR = path.join('spec', 'mockups');
const FORMS_DIR = path.join('OE2EmpireTracker', 'Forms');

const CONTROL_PREFIXES = ['txt', 'cmb', 'cmd', 'dgv', 'chk', 'lvw', 'btn', 'lbl', 'flp', 'tab', 'rtb', 'dtp', 'pnl', 'split'];
const INTERACTIVE_PREFIXES = ['txt', 'cmb', 'cmd', 'dgv', 'chk', 'btn', 'lvw'];

// Section-to-form mapping. Each mockup file maps to sections identified by header regex.
// aliases: { mockupName: codeName } for known naming differences between mockup and code.
// partial: true means this section covers only part of the form (e.g. one tab) —
//          code controls not in the mockup are NOT reported.
const SECTION_MAP = {
    'build-planner.md': [
        {
            header: /^###\s+FormBuildPlanner\b/,
            form: 'BuildPlanner/FormBuildPlanner',
            aliases: {
                'chkActive': 'chkIsActive',
                'flpPlanData': 'flpDetail',
                'cmdGenerateBuildPlan': 'cmdGenerateDelivery',
            },
        },
        {
            header: /^#{3,4}\s+(FormStructureAllocation|Structure Allocation Dialog)/,
            form: 'BuildPlanner/FormStructureAllocation',
            aliases: {
                'txtStructureFilter': 'txtFilter',
            },
        },
        {
            header: /^###\s+Colony Administration Tab/,
            form: 'ColonyV2/FormColonyV2',
            partial: true,
        },
    ],
    'ships.md': [
        {
            header: /^###\s+FormShipTemplate\b/,
            form: 'ShipTemplate/FormShipTemplate',
            aliases: {
                'txtTemplateFilter': 'txtFilter',
                'txtTemplateName': 'txtName',
                'dgvComponents': 'dgvSlots',
            },
        },
        {
            header: /^###\s+FormShipInstance\b/,
            form: 'ShipInstance/FormShipInstance',
            aliases: {
                'txtShipFilter': 'txtFilter',
                'txtShipName': 'txtName',
                'cmdCreateFromTemplate': 'cmdFromTemplate',
            },
        },
    ],
    'stations.md': [
        {
            header: /^###\s+FormStation\b/,
            form: 'Station/FormStation',
            aliases: {
                'txtStationFilter': 'txtFilter',
            },
        },
    ],
    'market.md': [
        {
            header: /^###\s+FormMarket\b/,
            form: 'Market/FormMarket',
            aliases: {
                'cmdEditListing': 'cmdListingEdit',
                'cmdDeleteListing': 'cmdListingDelete',
                'cmdAddTransaction': 'cmdTxApply',
                'cmdEditTransaction': 'cmdTxApply',
                'cmdDeleteTransaction': 'cmdTxApply',
            },
        },
    ],
    'stock-targets.md': [
        {
            header: /^###\s+FormStockTargets\b/,
            form: 'StockTargets/FormStockTargets',
            aliases: {
                'txtPlanFilter': 'txtFilter',
                'lvwStockPlans': 'lvwPlans',
                'cmdNewPlan': 'cmdNew',
                'cmdDeletePlan': 'cmdDelete',
                'chkPlanActive': 'chkActive',
                'cmbLocation': 'cmbTargetLocation',
            },
        },
    ],
    'contacts.md': [
        {
            header: /^###\s+FormContacts\b/,
            form: 'Contacts/FormContacts',
            aliases: {
                'dgvFactions': 'lvwFactions',
                'dgvMembers': 'lvwFactions',
                'dgvCharacters': 'lvwCharacters',
                'txtFactionFilterChar': 'txtCharFilter',
                'cmbFaction': 'cmbCharFaction',
            },
        },
    ],
    'asteroids.md': [
        {
            header: /^###\s+FormAsteroid\b/,
            form: 'Asteroid/FormAsteroid',
            aliases: {
                'txtAsteroidFilter': 'txtFilter',
            },
        },
    ],
    'supply-chains.md': [
        {
            header: /^###\s+FormSupplyChain\b/,
            form: 'SupplyChain/FormSupplyChain',
            aliases: {
                'txtChainFilter': 'txtFilter',
                'lvwSupplyChains': 'lvwChains',
                'chkChainActive': 'chkActive',
                'txtChainName': 'txtChainName',
            },
        },
    ],
    'colony-overflow.md': [
        {
            header: /^###\s+Warehouse Overflow/,
            form: 'ColonyV2/FormColonyV2',
            partial: true,
            aliases: {
                'cmdAddRule': 'cmdAddOverflowRule',
                'cmdRemoveRule': 'cmdRemoveOverflowRule',
            },
        },
        {
            header: /^###\s+Stock Profiles/,
            form: 'StockTargets/FormStockTargets',
            partial: true,
        },
    ],
    'blueprints.md': [
        {
            header: /^###\s+FormBlueprintV2\b/,
            form: 'BlueprintV2/FormBlueprintV2',
        },
    ],
    'surveys.md': [
        {
            header: /^###\s+FormSurvey\b/,
            form: 'Survey/FormSurvey',
        },
    ],
    'delivery-routes.md': [
        {
            header: /^###\s+FormDeliveryRoute\b/,
            form: 'DeliveryRoute/FormDeliveryRoute',
        },
    ],
    'delivery-execution.md': [
        {
            header: /^###\s+FormDeliveryExecution\b/,
            form: 'DeliveryExecution/FormDeliveryExecution',
        },
    ],
    'player-profile.md': [
        {
            header: /^###\s+FormPlayerProfile\b/,
            form: 'PlayerProfile/FormPlayerProfile',
        },
    ],
    'pricing-plans.md': [
        {
            header: /^###\s+FormPricingPlan\b/,
            form: 'PricingPlan/FormPricingPlan',
        },
    ],
    'colony-activity.md': [
        {
            header: /^###\s+FormColonyActivity\b/,
            form: 'ColonyActivity/FormColonyActivity',
        },
    ],
    'colony-daily-build.md': [
        {
            header: /^###\s+FormColonyDailyBuild\b/,
            form: 'ColonyDailyBuild/FormColonyDailyBuild',
        },
    ],
    'preferences.md': [
        {
            header: /^###\s+FormPreferences\b/,
            form: 'FormPreferences/FormPreferences',
        },
    ],
};

function isControlName(name) {
    return CONTROL_PREFIXES.some(p => name.startsWith(p));
}

function isInteractive(name) {
    return INTERACTIVE_PREFIXES.some(p => name.startsWith(p));
}

function extractControlNames(content) {
    const names = new Set();
    const backtickRegex = /`(\w+)`/g;
    let match;
    while ((match = backtickRegex.exec(content)) !== null) {
        if (isControlName(match[1])) names.add(match[1]);
    }
    return names;
}

function extractDesignerControls(designerPath) {
    if (!fs.existsSync(designerPath)) return null;
    const content = fs.readFileSync(designerPath, 'utf8');
    const names = new Set();
    // Match both private and internal field declarations
    const fieldRegex = /(?:private|internal)\s+[\w.]+\s+(\w+)\s*;/g;
    let match;
    while ((match = fieldRegex.exec(content)) !== null) {
        if (isControlName(match[1])) names.add(match[1]);
    }
    return names;
}

/**
 * Split a mockup file into sections based on header regexes from sectionDefs.
 * Each section runs from its matching header until the next matching header.
 * Content before the first match is discarded.
 */
function splitSections(fileContent, sectionDefs) {
    const lines = fileContent.split('\n');
    const sections = [];
    let currentDef = null;
    let currentLines = [];

    for (const line of lines) {
        let matched = null;
        for (const def of sectionDefs) {
            if (def.header.test(line)) {
                matched = def;
                break;
            }
        }

        if (matched) {
            if (currentDef) {
                sections.push({ def: currentDef, content: currentLines.join('\n') });
            }
            currentDef = matched;
            currentLines = [line];
        } else if (currentDef) {
            currentLines.push(line);
        }
    }
    if (currentDef) {
        sections.push({ def: currentDef, content: currentLines.join('\n') });
    }
    return sections;
}

// Cache Designer.cs controls per form path
const designerCache = {};
function getDesignerControls(formPath) {
    if (!(formPath in designerCache)) {
        const designerPath = path.join(FORMS_DIR, formPath + '.Designer.cs');
        designerCache[formPath] = extractDesignerControls(designerPath);
    }
    return designerCache[formPath];
}

// Track which code controls have been claimed by at least one mockup section
const claimedControls = {};

const findings = [];

for (const [mockupFile, sectionDefs] of Object.entries(SECTION_MAP)) {
    const mockupPath = path.join(MOCKUPS_DIR, mockupFile);
    if (!fs.existsSync(mockupPath)) continue;

    const fileContent = fs.readFileSync(mockupPath, 'utf8');
    const sections = splitSections(fileContent, sectionDefs);

    // If no sections found, treat whole file as one section using first def
    if (sections.length === 0 && sectionDefs.length > 0) {
        sections.push({ def: sectionDefs[0], content: fileContent });
    }

    for (const section of sections) {
        const { def, content } = section;
        const aliases = def.aliases || {};
        const formPath = def.form;
        const partial = def.partial || false;

        const designerControls = getDesignerControls(formPath);
        if (designerControls === null) {
            findings.push('MISSING DESIGNER: ' + path.join(FORMS_DIR, formPath + '.Designer.cs'));
            continue;
        }

        const mockupControls = extractControlNames(content);
        if (!claimedControls[formPath]) claimedControls[formPath] = new Set();

        // Build reverse alias map for code->mockup lookups
        const reverseAliases = {};
        for (const [mk, cd] of Object.entries(aliases)) {
            reverseAliases[cd] = mk;
        }

        // Check mockup controls against code (only interactive controls)
        for (const ctrl of mockupControls) {
            if (!isInteractive(ctrl)) continue; // Skip layout containers in mockup
            const codeName = aliases[ctrl] || ctrl;
            if (designerControls.has(codeName)) {
                claimedControls[formPath].add(codeName);
            } else if (designerControls.has(ctrl)) {
                // Direct match (alias was wrong but name exists)
                claimedControls[formPath].add(ctrl);
            } else {
                findings.push('IN MOCKUP NOT CODE: ' + ctrl +
                    (codeName !== ctrl ? ' (alias: ' + codeName + ')' : '') +
                    ' (' + mockupFile + ' -> ' + formPath + ')');
            }
        }

        // Check code controls against mockup (only for non-partial sections)
        if (!partial) {
            for (const ctrl of designerControls) {
                if (!isInteractive(ctrl)) continue;
                if (claimedControls[formPath].has(ctrl)) continue;
                const mockupName = reverseAliases[ctrl] || ctrl;
                if (!mockupControls.has(mockupName) && !mockupControls.has(ctrl)) {
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
