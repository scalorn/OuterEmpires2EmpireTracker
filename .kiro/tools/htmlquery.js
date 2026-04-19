#!/usr/bin/env node
/**
 * htmlquery.js — Query HTML files with CSS selectors for parser development.
 *
 * Usage:
 *   node .kiro/tools/htmlquery.js <htmlFile> <cssSelector> [--attr name] [--text] [--count] [--limit N]
 *
 * Loads an HTML file, runs a CSS selector query, and outputs matching elements.
 * Useful for testing selectors against game HTML before writing C# XPath queries.
 *
 * Options:
 *   --text     Output only the text content of matched elements (one per line)
 *   --attr X   Output only the value of attribute X on matched elements
 *   --count    Output only the count of matched elements
 *   --limit N  Show at most N matches (default: 20)
 *   --outer    Show outer HTML of each match (default: inner text summary)
 *   --classes  List all unique CSS class names found in the document
 *   --ids      List all unique id values found in the document
 *   --struct   Show document structure (tag names + classes, indented tree)
 *
 * CSS to XPath note:
 *   CSS: div.ScanDetailOutputResourceName
 *   XPath: //div[contains(@class,'ScanDetailOutputResourceName')]
 *
 * Examples:
 *   node .kiro/tools/htmlquery.js TestData/colony.html "div.ui_text_white" --text
 *   node .kiro/tools/htmlquery.js TestData/colony.html "[data-ui-tooltip]" --attr data-ui-tooltip
 *   node .kiro/tools/htmlquery.js TestData/colony.html "div.Profile_Skill_Group" --count
 *   node .kiro/tools/htmlquery.js TestData/colony.html --classes
 *   node .kiro/tools/htmlquery.js TestData/colony.html --struct --limit 50
 */

// Using a simple regex-based approach since we don't have cheerio/jsdom installed.
// This handles the common cases for game HTML inspection.

const fs = require('fs');

const args = process.argv.slice(2);
if (args.length < 1) {
    console.error('Usage: htmlquery.js <htmlFile> <selector> [options]');
    console.error('       htmlquery.js <htmlFile> --classes|--ids|--struct');
    process.exit(1);
}

const htmlFile = args[0];
if (!fs.existsSync(htmlFile)) {
    console.error(`File not found: ${htmlFile}`);
    process.exit(1);
}

const html = fs.readFileSync(htmlFile, 'utf8');

// Parse options
let selector = '';
let mode = 'summary'; // summary, text, attr, count, outer
let attrName = '';
let limit = 20;
let specialMode = ''; // classes, ids, struct

for (let i = 1; i < args.length; i++) {
    if (args[i] === '--text') mode = 'text';
    else if (args[i] === '--attr') { mode = 'attr'; attrName = args[++i]; }
    else if (args[i] === '--count') mode = 'count';
    else if (args[i] === '--outer') mode = 'outer';
    else if (args[i] === '--limit') limit = parseInt(args[++i]) || 20;
    else if (args[i] === '--classes') specialMode = 'classes';
    else if (args[i] === '--ids') specialMode = 'ids';
    else if (args[i] === '--struct') specialMode = 'struct';
    else if (!selector) selector = args[i];
}

// Special modes that don't need a selector
if (specialMode === 'classes') {
    const classRe = /class="([^"]*)"/gi;
    const classes = new Set();
    let m;
    while ((m = classRe.exec(html)) !== null) {
        m[1].split(/\s+/).filter(c => c).forEach(c => classes.add(c));
    }
    const sorted = [...classes].sort();
    console.log(`${sorted.length} unique classes:`);
    sorted.forEach(c => console.log(`  ${c}`));
    process.exit(0);
}

if (specialMode === 'ids') {
    const idRe = /id="([^"]*)"/gi;
    const ids = new Set();
    let m;
    while ((m = idRe.exec(html)) !== null) {
        ids.add(m[1]);
    }
    const sorted = [...ids].sort();
    console.log(`${sorted.length} unique ids:`);
    sorted.forEach(id => console.log(`  ${id}`));
    process.exit(0);
}

if (specialMode === 'struct') {
    // Show tag + class structure
    const tagRe = /<(\w+)([^>]*)>/g;
    let m;
    let count = 0;
    while ((m = tagRe.exec(html)) !== null && count < limit) {
        const tag = m[1].toLowerCase();
        if (['script', 'style', 'meta', 'link', 'br', 'hr', 'img', 'input'].includes(tag)) continue;
        const attrs = m[2];
        const classMatch = attrs.match(/class="([^"]*)"/);
        const idMatch = attrs.match(/id="([^"]*)"/);
        let line = `<${tag}`;
        if (idMatch) line += ` id="${idMatch[1]}"`;
        if (classMatch) line += ` class="${classMatch[1]}"`;
        line += '>';
        console.log(line);
        count++;
    }
    if (count >= limit) console.log(`... (limited to ${limit}, use --limit N for more)`);
    process.exit(0);
}

if (!selector) {
    console.error('No selector provided. Use --classes, --ids, --struct, or provide a CSS selector.');
    process.exit(1);
}

// Simple CSS selector to regex conversion for common patterns:
// div.className → <div[^>]*class="[^"]*className[^"]*"[^>]*>
// .className → <\w+[^>]*class="[^"]*className[^"]*"[^>]*>
// #idName → <\w+[^>]*id="idName"[^>]*>
// [attr] → <\w+[^>]*attr="[^"]*"[^>]*>
// [attr=value] → <\w+[^>]*attr="value"[^>]*>
// tag → <tag[^>]*>

function selectorToRegex(sel) {
    sel = sel.trim();

    // [attr=value]
    let attrMatch = sel.match(/^\[([a-z-]+)="([^"]*)"\]$/i);
    if (attrMatch) return new RegExp(`<\\w+[^>]*${attrMatch[1]}="${attrMatch[2]}"[^>]*>`, 'gi');

    // [attr]
    attrMatch = sel.match(/^\[([a-z-]+)\]$/i);
    if (attrMatch) return new RegExp(`<\\w+[^>]*${attrMatch[1]}="[^"]*"[^>]*>`, 'gi');

    // tag.class
    let tcMatch = sel.match(/^(\w+)\.([a-zA-Z0-9_-]+)$/);
    if (tcMatch) return new RegExp(`<${tcMatch[1]}[^>]*class="[^"]*${tcMatch[2]}[^"]*"[^>]*>`, 'gi');

    // .class
    let cMatch = sel.match(/^\.([a-zA-Z0-9_-]+)$/);
    if (cMatch) return new RegExp(`<\\w+[^>]*class="[^"]*${cMatch[1]}[^"]*"[^>]*>`, 'gi');

    // #id
    let iMatch = sel.match(/^#([a-zA-Z0-9_-]+)$/);
    if (iMatch) return new RegExp(`<\\w+[^>]*id="${iMatch[1]}"[^>]*>`, 'gi');

    // plain tag
    if (/^\w+$/.test(sel)) return new RegExp(`<${sel}[^>]*>`, 'gi');

    console.error(`Unsupported selector: ${sel}`);
    console.error('Supported: tag, .class, tag.class, #id, [attr], [attr=value]');
    process.exit(1);
}

const re = selectorToRegex(selector);
const matches = [];
let m;
while ((m = re.exec(html)) !== null) {
    const startIdx = m.index;
    // Extract a chunk after the opening tag for context
    const chunk = html.substring(startIdx, startIdx + 500);

    // Try to find the text content (between > and <)
    const textMatch = chunk.match(/^<[^>]*>([\s\S]*?)(?:<\/|<[a-z])/i);
    const text = textMatch ? textMatch[1].replace(/\s+/g, ' ').trim() : '';

    matches.push({
        tag: m[0],
        text: text.substring(0, 200),
        chunk: chunk.substring(0, 300)
    });
}

if (mode === 'count') {
    console.log(matches.length);
    process.exit(0);
}

console.log(`${matches.length} matches for "${selector}"${matches.length > limit ? ` (showing first ${limit})` : ''}:`);

for (let i = 0; i < Math.min(matches.length, limit); i++) {
    const match = matches[i];
    if (mode === 'text') {
        console.log(match.text || '(empty)');
    } else if (mode === 'attr') {
        const attrRe = new RegExp(`${attrName}="([^"]*)"`, 'i');
        const am = match.tag.match(attrRe);
        console.log(am ? am[1] : '(not found)');
    } else if (mode === 'outer') {
        console.log(match.chunk);
        console.log('---');
    } else {
        // summary: tag + first bit of text
        const shortTag = match.tag.length > 100 ? match.tag.substring(0, 100) + '...' : match.tag;
        console.log(`  ${shortTag}`);
        if (match.text) console.log(`    text: ${match.text}`);
    }
}
