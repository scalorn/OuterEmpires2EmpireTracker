#!/usr/bin/env node
/**
 * fix-catch-return.js — Fix "} catch (ObjectDisposedException) { } return;" pattern
 * Split the return onto its own line.
 */
const fs = require('fs');
const path = require('path');

function findFiles(dir) {
    const results = [];
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory() && !['node_modules','bin','obj','packages','.git','.vs'].includes(entry.name)) {
            results.push(...findFiles(full));
        } else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.includes('.Designer.cs')) {
            results.push(full);
        }
    }
    return results;
}

let fixed = 0;
for (const f of findFiles('OE2EmpireTracker')) {
    let content = fs.readFileSync(f, 'utf8');
    const re = /(}\s*catch\s*\(ObjectDisposedException\)\s*{\s*})\s*return;/g;
    if (re.test(content)) {
        // Reset regex
        re.lastIndex = 0;
        content = content.replace(re, '$1\n                return;');
        fs.writeFileSync(f, content, 'utf8');
        fixed++;
        console.log(`Fixed: ${path.basename(f)}`);
    }
}
console.log(`\nTotal files fixed: ${fixed}`);
