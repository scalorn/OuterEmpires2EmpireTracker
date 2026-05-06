const fs = require('fs');
const path = require('path');
const content = fs.readFileSync(path.join(__dirname, '_content.tmp'), 'utf8');
const target = process.argv[2];
const dir = path.dirname(target);
if (!fs.existsSync(dir)) fs.mkdirSync(dir, {recursive:true});
fs.writeFileSync(target, content);
console.log('Wrote ' + content.length + ' chars to ' + target);