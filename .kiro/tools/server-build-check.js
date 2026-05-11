#!/usr/bin/env node
/**
 * server-build-check.js — Verify OE2EmpireTracker.Server builds with zero warnings.
 *
 * Usage:
 *   node .kiro/tools/server-build-check.js
 *
 * Runs `dotnet build` on the server project and checks for any warnings.
 * This catches StyleCop violations, compiler warnings, and other issues.
 *
 * Exit code 0 = clean, 1 = findings.
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const SERVER_CSPROJ = path.join('OE2EmpireTracker.Server', 'OE2EmpireTracker.Server.csproj');

// Only run if the server project exists (branch may not have it)
if (!fs.existsSync(SERVER_CSPROJ)) {
    process.exit(0);
}

let output;
try {
    output = execSync(`dotnet build "${SERVER_CSPROJ}" /v:minimal 2>&1`, {
        encoding: 'utf8',
        timeout: 60000,
    });
} catch (err) {
    // Build failed entirely
    console.error('Server build FAILED:');
    console.error(err.stdout || err.message);
    process.exit(1);
}

// Check for warnings in the output
const warningLines = output.split('\n').filter(line =>
    line.includes(': warning ') && !line.includes('NETSDK1')
);

if (warningLines.length > 0) {
    console.error(`Server build has ${warningLines.length} warning(s):`);
    warningLines.forEach(line => console.error('  ' + line.trim()));
    process.exit(1);
}

process.exit(0);
