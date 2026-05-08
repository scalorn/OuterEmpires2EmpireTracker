#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const SRC_DIRS = ['OE2EmpireTracker', 'OE2EmpireTracker.Tests'];
const SKIP_DIRS = new Set(['bin', 'obj', '.vs']);

function findCsFiles(dir, results) {
    results = results || [];
    let entries;
    try { entries = fs.readdirSync(dir, { withFileTypes: true }); } catch (e) { return results; }
    for (const entry of entries) {
        if (SKIP_DIRS.has(entry.name)) continue;
        const fullPath = path.join(dir, entry.name);
        if (entry.isDirectory()) { findCsFiles(fullPath, results); }
        else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) { results.push(fullPath); }
    }
    return results;
}

const TYPE_DECL_REGEX = /(public|internal|private|protected)\s+(static\s+)?(abstract\s+)?(partial\s+)?(class|interface|enum|struct)\s+(\w+)/g;

function extractTypes(content) {
    const types = [];
    let match;
    TYPE_DECL_REGEX.lastIndex = 0;
    while ((match = TYPE_DECL_REGEX.exec(content)) !== null) { types.push(match[6]); }
    return types;
}

function buildGraph() {
    const allFiles = [];
    for (const dir of SRC_DIRS) { if (fs.existsSync(dir)) { findCsFiles(dir, allFiles); } }
    const fileContents = new Map();
    for (const file of allFiles) { fileContents.set(file, fs.readFileSync(file, 'utf8')); }
    const typeDeclarations = new Map();
    const fileTypes = new Map();
    for (const [file, content] of fileContents) {
        const types = extractTypes(content);
        fileTypes.set(file, types);
        for (const typeName of types) {
            if (!typeDeclarations.has(typeName)) { typeDeclarations.set(typeName, []); }
            typeDeclarations.get(typeName).push(file);
        }
    }
    const typeDependents = new Map();
    for (const [typeName, declFiles] of typeDeclarations) {
        const declSet = new Set(declFiles);
        const regex = new RegExp('\\b' + typeName + '\\b');
        const refs = [];
        for (const [file, content] of fileContents) {
            if (declSet.has(file)) continue;
            if (regex.test(content)) { refs.push(file); }
        }
        typeDependents.set(typeName, refs);
    }
    return { typeDeclarations, typeDependents, fileTypes, fileContents };
}

function relPath(filePath) { return path.relative('.', filePath).replace(/\\/g, '/'); }
function isTestFile(filePath) { return filePath.startsWith('OE2EmpireTracker.Tests') || filePath.includes('OE2EmpireTracker.Tests'); }

function cmdDependents(graph, className) {
    const { typeDeclarations, typeDependents } = graph;
    if (!typeDeclarations.has(className)) { console.error('Class not found: ' + className); process.exit(1); }
    const declFiles = typeDeclarations.get(className);
    const refs = typeDependents.get(className) || [];
    const declDisplay = declFiles.map(relPath).join(', ');
    console.log('Dependents of ' + className + ' (declared in ' + declDisplay + '):');
    const prodFiles = refs.filter(f => !isTestFile(f)).map(relPath).sort();
    const testFiles = refs.filter(f => isTestFile(f)).map(relPath).sort();
    if (prodFiles.length > 0) {
        console.log('');
        console.log('  Production:');
        for (const f of prodFiles) { console.log('    ' + f); }
    }
    if (testFiles.length > 0) {
        console.log('');
        console.log('  Tests:');
        for (const f of testFiles) { console.log('    ' + f); }
    }
    const total = prodFiles.length + testFiles.length;
    if (total === 0) { console.log('  (none)'); }
    else { console.log(''); console.log('  ... (' + total + ' total)'); }
}

function cmdDependencies(graph, className) {
    const { typeDeclarations, fileTypes, fileContents } = graph;
    if (!typeDeclarations.has(className)) { console.error('Class not found: ' + className); process.exit(1); }
    const declFiles = typeDeclarations.get(className);
    const declDisplay = declFiles.map(relPath).join(', ');
    console.log('Dependencies of ' + className + ' (declared in ' + declDisplay + '):');
    const deps = [];
    const selfTypes = new Set();
    for (const file of declFiles) {
        const types = fileTypes.get(file) || [];
        for (const t of types) { selfTypes.add(t); }
    }
    for (const [typeName, typeDeclFiles] of typeDeclarations) {
        if (selfTypes.has(typeName)) continue;
        const regex = new RegExp('\\b' + typeName + '\\b');
        for (const file of declFiles) {
            const content = fileContents.get(file);
            if (regex.test(content)) { deps.push({ name: typeName, file: typeDeclFiles[0] }); break; }
        }
    }
    if (deps.length === 0) { console.log('  (none)'); }
    else {
        deps.sort((a, b) => a.name.localeCompare(b.name));
        for (const dep of deps) { console.log('  ' + dep.name + ' (' + relPath(dep.file) + ')'); }
        console.log(''); console.log('  ... (' + deps.length + ' total)');
    }
}
function cmdBlastRadius(graph, className) {
    const { typeDeclarations } = graph;
    if (!typeDeclarations.has(className)) { console.error('Class not found: ' + className); process.exit(1); }
    console.log('=== Blast Radius: ' + className + ' ===');
    console.log('');
    cmdDependents(graph, className);
    console.log('');
    cmdDependencies(graph, className);
}

function cmdOrphans(graph) {
    const { typeDeclarations, typeDependents } = graph;
    const excludePatterns = [/^Program$/, /Tests?$/, /TestFixture$/, /^I[A-Z]/];
    const orphans = [];
    for (const [typeName, refs] of typeDependents) {
        if (refs.length > 0) continue;
        if (excludePatterns.some(p => p.test(typeName))) continue;
        const declFiles = typeDeclarations.get(typeName) || [];
        if (declFiles.every(f => isTestFile(f))) continue;
        orphans.push({ name: typeName, file: declFiles[0] });
    }
    if (orphans.length === 0) { console.log('No orphaned types found.'); }
    else {
        console.log('Orphaned types (' + orphans.length + ' -- never referenced by other files):');
        console.log('');
        orphans.sort((a, b) => a.name.localeCompare(b.name));
        for (const o of orphans) { console.log('  ' + o.name + ' (' + relPath(o.file) + ')'); }
    }
}
function printUsage() {
    console.log('Usage:');
    console.log('  node .kiro/tools/depgraph.js dependents <ClassName>   -- who depends on this class');
    console.log('  node .kiro/tools/depgraph.js dependencies <ClassName> -- what this class depends on');
    console.log('  node .kiro/tools/depgraph.js blast-radius <ClassName> -- both dependents and dependencies');
    console.log('  node .kiro/tools/depgraph.js orphans                  -- types never referenced elsewhere');
}
const args = process.argv.slice(2);
if (args.length === 0) { printUsage(); process.exit(1); }
const command = args[0];
const className = args[1];
const graph = buildGraph();
switch (command) {
    case 'dependents':
        if (!className) { console.error('Missing class name'); printUsage(); process.exit(1); }
        cmdDependents(graph, className);
        break;
    case 'dependencies':
        if (!className) { console.error('Missing class name'); printUsage(); process.exit(1); }
        cmdDependencies(graph, className);
        break;
    case 'blast-radius':
        if (!className) { console.error('Missing class name'); printUsage(); process.exit(1); }
        cmdBlastRadius(graph, className);
        break;
    case 'orphans':
        cmdOrphans(graph);
        break;
    default:
        console.error('Unknown command: ' + command);
        printUsage();
        process.exit(1);
}
