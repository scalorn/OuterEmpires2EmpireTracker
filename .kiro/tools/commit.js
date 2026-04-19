#!/usr/bin/env node
/**
 * commit.js — Git commit helper with consistent message formatting.
 *
 * Usage:
 *   node .kiro/tools/commit.js "Summary line" "Body text" "Prompt: user said this"
 *
 * Or pipe the body via stdin for large bodies:
 *   @"
 *   Detailed body here
 *   across multiple lines
 *   "@ | node .kiro/tools/commit.js "Summary line" --stdin "Prompt: user said this"
 *
 * Runs: git add -A, then git commit with the formatted message.
 *
 * Message format:
 *   Summary line
 *
 *   Body text
 *
 *   Prompt: user said this
 *
 * Options:
 *   --no-add    Skip git add -A (only commit staged files)
 *   --stdin     Read body from stdin instead of argument
 *   --files     Followed by comma-separated file patterns to add (instead of -A)
 */

const { execSync } = require('child_process');
const fs = require('fs');

function readStdin() {
    return new Promise((resolve) => {
        const chunks = [];
        process.stdin.setEncoding('utf8');
        process.stdin.on('data', chunk => chunks.push(chunk));
        process.stdin.on('end', () => resolve(chunks.join('')));
        // If stdin is a TTY (no pipe), resolve immediately with empty
        if (process.stdin.isTTY) resolve('');
    });
}

async function main() {
    const args = process.argv.slice(2);

    let summary = '';
    let body = '';
    let prompt = '';
    let doAdd = true;
    let addFiles = null;
    let useStdin = false;

    // Parse args
    const positional = [];
    for (let i = 0; i < args.length; i++) {
        if (args[i] === '--no-add') {
            doAdd = false;
        } else if (args[i] === '--stdin') {
            useStdin = true;
        } else if (args[i] === '--files') {
            addFiles = args[++i];
        } else {
            positional.push(args[i]);
        }
    }

    if (positional.length < 1) {
        console.error('Usage: commit.js "Summary" ["Body"] ["Prompt: ..."]');
        console.error('       commit.js "Summary" --stdin "Prompt: ..."');
        process.exit(1);
    }

    summary = positional[0];

    if (useStdin) {
        body = (await readStdin()).trim();
        prompt = positional[1] || '';
    } else {
        body = positional[1] || '';
        prompt = positional[2] || '';
    }

    // Build message
    let message = summary;
    if (body) {
        message += '\n\n' + body;
    }
    if (prompt) {
        message += '\n\n' + prompt;
    }

    // Write message to temp file (avoids all shell quoting issues)
    const tmpFile = '_commit_msg.tmp';
    fs.writeFileSync(tmpFile, message, 'utf8');

    try {
        // Stage files
        if (doAdd) {
            if (addFiles) {
                for (const pattern of addFiles.split(',')) {
                    execSync(`git add ${pattern.trim()}`, { stdio: 'inherit' });
                }
            } else {
                execSync('git add -A', { stdio: 'inherit' });
            }
        }

        // Commit
        execSync(`git commit -F ${tmpFile}`, { stdio: 'inherit' });
        console.log('Committed successfully.');
    } catch (err) {
        console.error('Commit failed:', err.message);
        process.exit(1);
    } finally {
        try { fs.unlinkSync(tmpFile); } catch(e) {}
    }
}

main();
