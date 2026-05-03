const fs = require('fs');
const path = process.argv[2];
const lines = [];
process.stdin.setEncoding('utf8');
process.stdin.on('data', d => lines.push(d));
process.stdin.on('end', () => { fs.writeFileSync(path, lines.join('')); console.log('Wrote ' + lines.join('').length + ' chars to ' + path); });
