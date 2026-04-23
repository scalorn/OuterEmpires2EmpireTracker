const fs = require('fs');
const path = require('path');

// Rename map - only safe renames that won't break JSON serialization
// For JSON-serialized properties, we already added [JsonProperty] attributes
const renameMap = {
    // ColonyStructure properties (already have [JsonProperty] added)
    'displaySequence': 'DisplaySequence',
    'buildingID': 'BuildingID',
    'buildQueueSequence': 'BuildQueueSequence',
    // DataEntryGridView private field
    'previousControl': 'PreviousControl',
    // DataEntryGridView private methods
    'handleBackwards': 'HandleBackwards',
    'handleForward': 'HandleForward',
    'handleEditCell': 'HandleEditCell',
    // FormPlayerProfile private method
    'configureSkillBlockOnce': 'ConfigureSkillBlockOnce',
    // PlayerContext private methods
    'initPlayerProfiles': 'InitPlayerProfiles',
    'initColonies': 'InitColonies',
    // Static factory methods in enum-like classes
    'getCommodityIndustries': 'GetCommodityIndustries',
    'getClasses': 'GetClasses',
    'getPurities': 'GetPurities',
    'getWorkerDetails': 'GetWorkerDetails',
    'getItemTypes': 'GetItemTypes',
    'getGroups': 'GetGroups',
    'getCommodityGroups': 'GetCommodityGroups',
    'getCommodities': 'GetCommodities',
    // PropertyBag methods
    'getDecimal': 'GetDecimal',
    'getLong': 'GetLong',
    'getBoolean': 'GetBoolean',
    'getString': 'GetString',
    'setProperty': 'SetProperty',
    // BlueprintScanner local
    'children': 'Children',
    // ProgrammaticUpdateGuard
    'release': 'Release',
    // CountDownTimeReference properties (not serialized)
    // These need special handling since 'source' is too common
};

// Restricted renames - only apply in specific files
const restrictedRenames = {
    'children': ['BlueprintScanner.cs'],
    'release': ['ProgrammaticUpdateGuard.cs'],
};

// Build regex
const sorted = Object.keys(renameMap).sort((a, b) => b.length - a.length);
const pattern = new RegExp('\\b(' + sorted.map(s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|') + ')\\b', 'g');

function findCsFiles(dir) {
    const results = [];
    try {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
            const full = path.join(dir, entry.name);
            if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules' && entry.name !== 'bin' && entry.name !== 'obj' && entry.name !== 'packages') {
                results.push(...findCsFiles(full));
            } else if (entry.isFile() && entry.name.endsWith('.cs') && !entry.name.endsWith('.Designer.cs')) {
                results.push(full);
            }
        }
    } catch (e) {}
    return results;
}

const csFiles = [...findCsFiles('OE2EmpireTracker'), ...findCsFiles('OE2EmpireTracker.Tests')];
console.log(`Scanning ${csFiles.length} .cs files...`);

let filesChanged = 0;
let totalReplacements = 0;

for (const file of csFiles) {
    const basename = path.basename(file);
    const content = fs.readFileSync(file, 'utf8');
    let count = 0;
    
    const newContent = content.replace(pattern, (match, p1, offset) => {
        // Check restricted renames
        if (restrictedRenames[match]) {
            if (!restrictedRenames[match].some(f => basename === f)) {
                return match;
            }
        }
        
        // Don't rename inside string literals
        // Simple heuristic: check if preceded by \" (escaped quote in string)
        if (offset >= 2) {
            const twoBack = content.substring(offset - 2, offset);
            if (twoBack === '\\"' || twoBack === "\\\"") {
                return match;
            }
        }
        // Check if preceded by a regular quote (inside a string)
        if (offset >= 1 && content[offset - 1] === '"') {
            // Look back to see if this is inside a string
            const lineStart = content.lastIndexOf('\n', offset) + 1;
            const beforeOnLine = content.substring(lineStart, offset);
            // Count unescaped quotes - if odd, we're inside a string
            const quotes = (beforeOnLine.match(/(?<!\\)"/g) || []).length;
            if (quotes % 2 === 1) {
                return match; // Inside a string literal
            }
        }
        
        count++;
        return renameMap[match];
    });
    
    if (count > 0) {
        fs.writeFileSync(file, newContent, 'utf8');
        filesChanged++;
        totalReplacements += count;
        console.log(`  ${basename}: ${count} replacements`);
    }
}

console.log(`\nDone: ${filesChanged} files changed, ${totalReplacements} total replacements`);
