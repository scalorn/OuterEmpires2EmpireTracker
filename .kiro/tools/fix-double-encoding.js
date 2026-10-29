#!/usr/bin/env node
/*
 * Repair double-encoded UTF-8.
 *
 * Cause: a tool reads UTF-8 bytes as Windows-1252 and writes them back as UTF-8.
 * Every original non-ASCII byte B becomes the UTF-8 encoding of whatever codepoint
 * CP1252 maps B to, so '§' (C2 A7) becomes 'Â§' (C3 82 C2 A7).
 *
 * Reversal: decode the file as UTF-8, then map each character back to the single
 * CP1252 byte it came from. That is not a byte-pattern substitution -- an earlier
 * attempt at one was wrong, because C2 is a lead byte and never appears in the
 * continuation range the pattern was matching.
 *
 * Safety: the result is validated as UTF-8 before anything is written, and a file is
 * skipped if the reversal would not produce valid UTF-8 (which means the file is
 * only partly corrupted and needs a targeted fix instead of a whole-file one).
 *
 * Usage: node .kiro/tools/fix-double-encoding.js <file> [...]
 *        node .kiro/tools/fix-double-encoding.js --check <file> [...]
 */

const fs = require('fs');

// CP1252 differs from Latin-1 only in 0x80-0x9F, which it maps to printable
// characters. Those are exactly the ones that show up in mojibake, so the table
// is required rather than optional.
const CP1252_HIGH = [
    0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
    0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
    0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
    0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178,
];

const REVERSE = new Map();
for (let b = 0; b < 0x80; b++) {
    REVERSE.set(b, b);
}

for (let i = 0; i < CP1252_HIGH.length; i++) {
    REVERSE.set(CP1252_HIGH[i], 0x80 + i);
}

for (let b = 0xA0; b <= 0xFF; b++) {
    REVERSE.set(b, b);
}

function isValidUtf8(buf) {
    // Node replaces invalid sequences with U+FFFD rather than throwing, so a
    // round-trip comparison is the reliable test.
    return Buffer.compare(Buffer.from(buf.toString('utf8'), 'utf8'), buf) === 0;
}

function reverse(buf) {
    const text = buf.toString('utf8').replace(/^\uFEFF/, '');
    const out = [];

    for (const ch of text) {
        const cp = ch.codePointAt(0);
        const byte = REVERSE.get(cp);
        if (byte === undefined) {
            return null; // not reachable from a CP1252 misread; leave the file alone
        }

        out.push(byte);
    }

    return Buffer.from(out);
}

const args = process.argv.slice(2);
const checkOnly = args.includes('--check');
const files = args.filter((a) => !a.startsWith('--'));

if (files.length === 0) {
    console.error('usage: node .kiro/tools/fix-double-encoding.js [--check] <file> [...]');
    process.exit(2);
}

let repaired = 0;
let skipped = 0;

for (const file of files) {
    const buf = fs.readFileSync(file);

    if (!isValidUtf8(buf)) {
        console.log(`SKIP (not valid UTF-8 to begin with): ${file}`);
        skipped++;
        continue;
    }

    const candidate = reverse(buf);

    if (candidate === null) {
        console.log(`SKIP (contains characters no CP1252 misread could produce): ${file}`);
        skipped++;
        continue;
    }

    if (Buffer.compare(candidate, buf) === 0) {
        console.log(`clean: ${file}`);
        continue;
    }

    if (!isValidUtf8(candidate)) {
        console.log(`SKIP (reversal would not be valid UTF-8 -- partial corruption): ${file}`);
        skipped++;
        continue;
    }

    if (checkOnly) {
        console.log(`WOULD REPAIR: ${file}  (${buf.length} -> ${candidate.length} bytes)`);
    } else {
        fs.writeFileSync(file, candidate);
        console.log(`repaired: ${file}  (${buf.length} -> ${candidate.length} bytes)`);
    }

    repaired++;
}

console.log(`\n${checkOnly ? 'would repair' : 'repaired'}: ${repaired}   skipped: ${skipped}`);
process.exit(skipped > 0 ? 1 : 0);
