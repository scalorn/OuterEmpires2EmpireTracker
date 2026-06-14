#!/usr/bin/env node
// metrics-stats.js — Processes _typed-asset-metrics.csv and outputs per-API and overall statistics.
// Usage: node .kiro/tools/metrics-stats.js [path-to-csv]
// Default path: spec/game-api-data/_typed-asset-metrics.csv

'use strict';

const fs = require('fs');
const path = require('path');

const csvPath = process.argv[2] || path.join(__dirname, '..', '..', 'spec', 'game-api-data', '_typed-asset-metrics.csv');

if (!fs.existsSync(csvPath)) {
    console.error(`File not found: ${csvPath}`);
    process.exit(1);
}

const lines = fs.readFileSync(csvPath, 'utf8').split('\n').filter(l => l.trim());
const header = lines[0].split(',');
const rows = lines.slice(1).map(line => {
    const cols = line.split(',');
    return {
        timestamp: cols[0],
        label: cols[1],
        status: cols[2],
        durationMs: parseInt(cols[3], 10),
        attempt: parseInt(cols[4], 10),
        inflight: parseInt(cols[5], 10),
        effectiveTps: parseFloat(cols[6]),
    };
});

// Extract API category from label (e.g. "assets/locations" -> "assets/locations",
// "assets/crate-123" -> "assets/crate", "colonies/585/buildings" -> "colonies/buildings")
function normalizeLabel(label) {
    // assets/blueprint-NNNNN -> assets/blueprint
    // assets/survey-NNNNN -> assets/survey
    // assets/crate-NNNNN -> assets/crate
    // assets/location-NNNNN -> assets/location
    // colonies/NNN/buildings -> colonies/buildings
    // colonies/NNN/summary -> colonies/summary
    // colonies/NNN/warehouse -> colonies/warehouse
    // colonies/NNN/workers -> colonies/workers
    // banking/transactions-page-N -> banking/transactions
    let norm = label.replace(/-\d+$/, '');
    norm = norm.replace(/\/\d+\//, '/');
    norm = norm.replace(/-page$/, '');
    return norm;
}

function percentile(sorted, p) {
    if (sorted.length === 0) return 0;
    const idx = Math.min(Math.floor(sorted.length * p), sorted.length - 1);
    return sorted[idx];
}

function computeStats(entries) {
    const durations = entries.map(e => e.durationMs).sort((a, b) => a - b);
    const ok = entries.filter(e => e.status === 'OK').length;
    const fail = entries.filter(e => e.status === 'FAIL').length;
    const retry = entries.filter(e => e.status === 'RETRY').length;
    const avg = durations.length > 0 ? Math.round(durations.reduce((a, b) => a + b, 0) / durations.length) : 0;
    return {
        count: entries.length,
        ok,
        fail,
        retry,
        avg,
        p50: percentile(durations, 0.50),
        p90: percentile(durations, 0.90),
        p99: percentile(durations, 0.99),
        p100: durations.length > 0 ? durations[durations.length - 1] : 0,
    };
}

// Group by normalized label
const groups = {};
for (const row of rows) {
    const key = normalizeLabel(row.label);
    if (!groups[key]) groups[key] = [];
    groups[key].push(row);
}

// Compute per-API stats
const apiStats = Object.entries(groups)
    .map(([api, entries]) => ({ api, ...computeStats(entries) }))
    .sort((a, b) => b.count - a.count);

// Compute overall stats
const overall = computeStats(rows);

// Compute run duration
const timestamps = rows.map(r => new Date(r.timestamp).getTime()).filter(t => !isNaN(t));
const runDurationSec = timestamps.length > 1
    ? ((Math.max(...timestamps) - Math.min(...timestamps)) / 1000).toFixed(1)
    : '0';

// Output
console.log('=== API Metrics Summary ===');
console.log(`File: ${csvPath}`);
console.log(`Run duration: ${runDurationSec}s`);
console.log(`Total requests: ${overall.count} (OK: ${overall.ok}, FAIL: ${overall.fail}, RETRY: ${overall.retry})`);
console.log(`Overall latency: avg=${overall.avg}ms, P50=${overall.p50}ms, P90=${overall.p90}ms, P99=${overall.p99}ms, P100=${overall.p100}ms`);
console.log('');

const colW = { api: 30, count: 7, ok: 5, fail: 5, retry: 6, avg: 6, p50: 6, p90: 6, p99: 6, p100: 7 };
const hdr = [
    'API'.padEnd(colW.api),
    'Count'.padStart(colW.count),
    'OK'.padStart(colW.ok),
    'Fail'.padStart(colW.fail),
    'Retry'.padStart(colW.retry),
    'Avg'.padStart(colW.avg),
    'P50'.padStart(colW.p50),
    'P90'.padStart(colW.p90),
    'P99'.padStart(colW.p99),
    'P100'.padStart(colW.p100),
].join('  ');
console.log(hdr);
console.log('-'.repeat(hdr.length));

for (const s of apiStats) {
    console.log([
        s.api.padEnd(colW.api),
        String(s.count).padStart(colW.count),
        String(s.ok).padStart(colW.ok),
        String(s.fail).padStart(colW.fail),
        String(s.retry).padStart(colW.retry),
        String(s.avg).padStart(colW.avg),
        String(s.p50).padStart(colW.p50),
        String(s.p90).padStart(colW.p90),
        String(s.p99).padStart(colW.p99),
        String(s.p100).padStart(colW.p100),
    ].join('  '));
}

console.log('-'.repeat(hdr.length));
console.log([
    'TOTAL'.padEnd(colW.api),
    String(overall.count).padStart(colW.count),
    String(overall.ok).padStart(colW.ok),
    String(overall.fail).padStart(colW.fail),
    String(overall.retry).padStart(colW.retry),
    String(overall.avg).padStart(colW.avg),
    String(overall.p50).padStart(colW.p50),
    String(overall.p90).padStart(colW.p90),
    String(overall.p99).padStart(colW.p99),
    String(overall.p100).padStart(colW.p100),
].join('  '));
