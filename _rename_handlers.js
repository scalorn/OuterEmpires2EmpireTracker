const fs = require('fs');
const path = require('path');

// Read handler names
const handlers = fs.readFileSync('_handlers.tmp', 'utf8')
    .split('\n')
    .map(h => h.trim())
    .filter(h => h.length > 0);

console.log(`Loaded ${handlers.length} handler names to rename`);

// Build rename map
const renameMap = {};
for (const h of handlers) {
    renameMap[h] = h[0].toUpperCase() + h.slice(1);
}

// Build a single regex that matches any handler name at word boundary
// Sort by length descending to match longer names first
const sorted = Object.keys(renameMap).sort((a, b) => b.length - a.length);
const pattern = new RegExp('\\b(' + sorted.map(s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|') + ')\\b', 'g');

// Find all .cs files recursively
function findCsFiles(dir) {
    const results = [];
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules' && entry.name !== 'bin' && entry.name !== 'obj' && entry.name !== 'packages') {
            results.push(...findCsFiles(full));
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            results.push(full);
        }
    }
    return results;
}

const csFiles = [
    ...findCsFiles('OE2EmpireTracker'),
    ...findCsFiles('OE2EmpireTracker.Tests')
];

console.log(`Scanning ${csFiles.length} .cs files...`);

let filesChanged = 0;
let totalReplacements = 0;

for (const file of csFiles) {
    const content = fs.readFileSync(file, 'utf8');
    let changed = false;
    let count = 0;
    
    const newContent = content.replace(pattern, (match) => {
        if (renameMap[match]) {
            count++;
            changed = true;
            return renameMap[match];
        }
        return match;
    });
    
    if (changed) {
        fs.writeFileSync(file, newContent, 'utf8');
        filesChanged++;
        totalReplacements += count;
        if (count > 5) console.log(`  ${path.basename(file)}: ${count} replacements`);
    }
}

console.log(`\nDone: ${filesChanged} files changed, ${totalReplacements} total replacements`);
