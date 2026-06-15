#!/usr/bin/env node
/**
 * mutation-audit.js — Detect direct entity mutation outside service classes.
 *
 * Usage:
 *   node .kiro/tools/mutation-audit.js
 *
 * Checks:
 * 1. WriteContext() calls outside allowed service/context files
 * 2. Direct entity property sets in Form*.cs files
 * 3. Direct entity property sets in *ViewModel.cs files
 *
 * Exit code 0 = clean, exit code 1 = findings.
 */

const fs = require('fs');
const path = require('path');

const SRC_DIR = 'OE2EmpireTracker';
const SKIP_DIRS = new Set(['bin', 'obj', '.vs', 'specs']);

// --- Check 1: WriteContext() allowed callers ---

const ALLOWED_WRITE_CONTEXT = new Set([
    'PlayerContext.cs',
    'EmpireContext.cs',
    'BlueprintService.cs',
    'ColonyService.cs',
    'SurveyService.cs',
    'PlayerProfileService.cs',
    'DeliveryRouteService.cs',
    'DeliveryPlanService.cs',
    'PricingPlanService.cs',
    'ShipTemplateService.cs',
    'ShipBuildService.cs',
    'ShipService.cs',
    'StationService.cs',
    'BuildPlanMutationService.cs',
    'StockTargetMutationService.cs',
    'SupplyChainMutationService.cs',
    'ContactsService.cs',
    'AsteroidService.cs',
    'MarketListingService.cs',
    'MarketService.cs',
    'PreferencesStore.cs',
    'BackgroundProcessor.cs',
    'DeliveryFulfillment.cs',
    'BlueprintImportHandler.cs',
    'CrateImporter.cs',
    'MarketBlueprintImporter.cs',
    // Accepted baseline (pending migration)
    'MainWindow.cs',
    'FormBuildPlanner.cs',
    'FormColonyDailyBuild.cs',
    'FormColonyV2.cs',
    'FormShipTemplate.cs',
    'FormStockTargets.cs',
    'FormMarket.cs',
    'BlueprintViewModel.cs',
]);
// --- Check 2 & 3: Entity mutation patterns ---

const ENTITY_MUTATION_PATTERNS = [
    /\._(?:blueprint|colony|survey|profile|route|plan|template|ship|station|buildPlan|stockTarget|supplyChain|contacts|asteroid|listing)\.\w+\s*=[^=]/,
    /\.Data\.\w+\s*=[^=]/,
];

// --- Helpers ---

function findCsFiles(dir, results) {
    results = results || [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
        if (SKIP_DIRS.has(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            findCsFiles(fullPath, results);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            results.push(fullPath);
        }
    }
    return results;
}

function isComment(line) {
    const trimmed = line.trim();
    return trimmed.startsWith('//') || trimmed.startsWith('///') || trimmed.startsWith('*');
}

function isLocalVarDeclaration(line) {
    const trimmed = line.trim();
    return trimmed.startsWith('var ') ||
           trimmed.startsWith('string ') ||
           trimmed.startsWith('int ') ||
           trimmed.startsWith('bool ') ||
           trimmed.startsWith('double ') ||
           trimmed.startsWith('decimal ') ||
           trimmed.startsWith('float ') ||
           trimmed.startsWith('Guid ');
}
// --- Check 1: WriteContext outside allowed files ---

function checkWriteContext(allFiles) {
    const findings = [];
    const writeContextPattern = /WriteContext\s*\(/;

    for (const filePath of allFiles) {
        const fileName = path.basename(filePath);

        // Skip allowed files
        if (ALLOWED_WRITE_CONTEXT.has(fileName)) continue;

        // Skip Designer.cs files
        if (fileName.endsWith('.Designer.cs')) continue;

        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            // Skip comments
            if (isComment(line)) continue;

            // Skip string literals containing WriteContext
            if (trimmed.includes('"') && trimmed.includes('WriteContext')) continue;

            if (writeContextPattern.test(trimmed)) {
                const relPath = path.relative('.', filePath).replace(/\\/g, '/');
                findings.push('WRITE_CONTEXT: ' + relPath + ':' + (i + 1) + '  ' + (trimmed.length > 120 ? trimmed.substring(0, 120) + '...' : trimmed));
            }
        }
    }

    return findings;
}
// --- Check 2: Form direct entity mutation ---

function checkFormMutation(allFiles) {
    const findings = [];

    const formFiles = allFiles.filter(f => {
        const name = path.basename(f);
        return name.startsWith('Form') && name.endsWith('.cs') && !name.endsWith('.Designer.cs');
    });

    for (const filePath of formFiles) {
        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            // Skip comments
            if (isComment(line)) continue;

            // Skip ViewModel field sets (local edit buffer writes)
            if (trimmed.includes('_viewModel.') || trimmed.includes('viewModel.') || trimmed.includes('planViewModel.')) continue;

            // Skip property declarations
            if (trimmed.includes('{ get;')) continue;

            // Skip local variable declarations
            if (isLocalVarDeclaration(trimmed)) continue;

            for (const pattern of ENTITY_MUTATION_PATTERNS) {
                if (pattern.test(trimmed)) {
                    const relPath = path.relative('.', filePath).replace(/\\/g, '/');
                    findings.push('FORM_MUTATION: ' + relPath + ':' + (i + 1) + '  ' + (trimmed.length > 120 ? trimmed.substring(0, 120) + '...' : trimmed));
                    break; // Only report once per line
                }
            }
        }
    }

    return findings;
}
// --- Check 3: ViewModel direct entity mutation ---

function checkViewModelMutation(allFiles) {
    const findings = [];
    const vmDir = path.join(SRC_DIR, 'ViewModels');

    if (!fs.existsSync(vmDir)) return findings;

    const vmFiles = allFiles.filter(f => {
        const relToSrc = path.relative(SRC_DIR, f).replace(/\\/g, '/');
        const name = path.basename(f);
        return relToSrc.startsWith('ViewModels/') && name.endsWith('ViewModel.cs');
    });

    for (const filePath of vmFiles) {
        const content = fs.readFileSync(filePath, 'utf8');
        const lines = content.split('\n');

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();

            // Skip comments
            if (isComment(line)) continue;

            // Skip property declarations
            if (trimmed.includes('{ get;')) continue;

            // Skip ViewModel's own local field assignments (lines starting with _fieldName =)
            if (/^_\w+\s*=/.test(trimmed) || /^this\._\w+\s*=/.test(trimmed)) continue;

            for (const pattern of ENTITY_MUTATION_PATTERNS) {
                if (pattern.test(trimmed)) {
                    const relPath = path.relative('.', filePath).replace(/\\/g, '/');
                    findings.push('VM_MUTATION: ' + relPath + ':' + (i + 1) + '  ' + (trimmed.length > 120 ? trimmed.substring(0, 120) + '...' : trimmed));
                    break; // Only report once per line
                }
            }
        }
    }

    return findings;
}
// --- Main ---

const allFiles = findCsFiles(SRC_DIR);

const writeContextFindings = checkWriteContext(allFiles);
const formMutationFindings = checkFormMutation(allFiles);
const vmMutationFindings = checkViewModelMutation(allFiles);

const allFindings = [...writeContextFindings, ...formMutationFindings, ...vmMutationFindings];

if (allFindings.length === 0) {
    console.log('Clean \u2014 no mutation violations found');
    process.exit(0);
} else {
    console.log('=== Mutation Audit ===\n');
    allFindings.forEach(f => console.log(f));
    console.log('\n' + allFindings.length + ' findings');
    process.exit(1);
}