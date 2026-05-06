
const fs = require('fs');
const content = fs.readFileSync('_design_content.txt', 'utf8');
fs.writeFileSync('.kiro/specs/grid-context-menus/design.md', content);
console.log('Wrote ' + content.length + ' chars');
