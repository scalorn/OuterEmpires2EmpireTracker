#!/usr/bin/env node
/**
 * Discovery Metrics Analysis Tool
 *
 * Reads the CSV metrics file produced by GameApiRequestQueue and outputs
 * a comprehensive analysis including latency percentiles, inflight distribution,
 * throughput over time, and retry/failure details.
 *
 * Usage: node .kiro/tools/discovery-metrics.js [path-to-csv]
 * Default path: spec/game-api-data/_metrics.csv
 */

const fs = require('fs');
const path = require('path');

const csvPath = process.argv[2] || path.join(__dirname, '..', '..', 'spec', 'game-api-data', '_metrics.csv');

if (!fs.existsSync(csvPath)) {
    console.error(`Metrics CSV not found: ${csvPath}`);
    process.exit(1);
}

const lines = fs.readFileSync(csvPath, 'utf8').trim().split(/\r?\n/);
if (lines.length < 2) {
    console.error('CSV file has no data rows.');
    process.exit(1);
}

// Parse header and rows
const header = lines[0].split(',');
const rows = [];
for (let i = 1; i < lines.length; i++) {
    const cols = lines[i].split(',');
    if (cols.length < 7) continue;
    rows.push({
        timestamp: new Date(cols[0]),
        label: cols[1],
        status: cols[2],
        durationMs: parseInt(cols[3], 10),
        attempt: parseInt(cols[4], 10),
        inflight: parseInt(cols[5], 10),
        effectiveTps: parseFloat(cols[6])
    });
}

if (rows.length === 0) {
    console.error('No valid data rows found.');
    process.exit(1);
}

// === Summary ===
const firstTs = rows[0].timestamp;
const lastTs = rows[rows.length - 1].timestamp;
const durationSec = (lastTs - firstTs) / 1000;
const durationMin = Math.floor(durationSec / 60);
const durationRemSec = Math.round(durationSec % 60);

const okRows = rows.filter(r => r.status === 'OK');
const retryRows = rows.filter(r => r.status === 'RETRY');
const failRows = rows.filter(r => r.status === 'FAIL');

const totalRequests = rows.length;
const effectiveTpsActual = durationSec > 0 ? (okRows.length / durationSec) : 0;
const retryRate = totalRequests > 0 ? (retryRows.length / totalRequests * 100) : 0;

console.log('=== Discovery Metrics Analysis ===');
console.log('');
console.log(`Run Duration: ${durationMin}m ${durationRemSec}s`);
console.log(`Total Requests: ${totalRequests} (OK: ${okRows.length}, RETRY: ${retryRows.length}, FAIL: ${failRows.length})`);
console.log(`Effective TPS: ${effectiveTpsActual.toFixed(1)} (total OK / duration seconds)`);
console.log(`Retry Rate: ${retryRate.toFixed(1)}% (retries / total attempts)`);

// === Latency by Endpoint Type ===
function classifyEndpoint(label) {
    // "colonies/585/summary" -> "colonies-summary"
    // "assets/survey-123" -> "assets-survey"
    // "mail/24500" -> "mail-detail"
    // "character/profile" -> "character-profile"
    // "banking/transactions-page-0" -> "banking-transactions"
    const parts = label.split('/');
    if (parts.length === 1) return label;

    const category = parts[0];
    const rest = parts.slice(1).join('/');

    // Strip numeric IDs and UUIDs
    const cleaned = rest
        .replace(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, '')
        .replace(/\d+/g, '')
        .replace(/^[-/]+|[-/]+$/g, '')
        .replace(/[-/]{2,}/g, '-');

    if (!cleaned || cleaned === '-') return `${category}-detail`;
    return `${category}-${cleaned}`;
}

function percentile(sorted, p) {
    if (sorted.length === 0) return 0;
    const idx = Math.min(Math.floor(sorted.length * p), sorted.length - 1);
    return sorted[idx];
}

// Group OK rows by endpoint type for latency analysis
const byEndpoint = {};
for (const row of okRows) {
    const ep = classifyEndpoint(row.label);
    if (!byEndpoint[ep]) byEndpoint[ep] = [];
    byEndpoint[ep].push(row.durationMs);
}

console.log('');
console.log('=== Latency by Endpoint Type (ms) ===');
const epHeader = 'Endpoint'.padEnd(25) + 'Count'.padStart(8) + 'P50'.padStart(8) + 'P90'.padStart(8) + 'P99'.padStart(8) + 'P100'.padStart(8);
console.log(epHeader);

const sortedEndpoints = Object.keys(byEndpoint).sort();
for (const ep of sortedEndpoints) {
    const durations = byEndpoint[ep].slice().sort((a, b) => a - b);
    const count = durations.length;
    const p50 = percentile(durations, 0.50);
    const p90 = percentile(durations, 0.90);
    const p99 = percentile(durations, 0.99);
    const p100 = durations[durations.length - 1];
    console.log(
        ep.padEnd(25) +
        String(count).padStart(8) +
        String(p50).padStart(8) +
        String(p90).padStart(8) +
        String(p99).padStart(8) +
        String(p100).padStart(8)
    );
}

// === Inflight Distribution ===
console.log('');
console.log('=== Inflight Distribution ===');
const inflightBuckets = [
    { label: '1-5', min: 1, max: 5 },
    { label: '6-10', min: 6, max: 10 },
    { label: '11-20', min: 11, max: 20 },
    { label: '21-30', min: 21, max: 30 },
    { label: '31+', min: 31, max: Infinity }
];

const inflightHeader = 'Slots Used'.padEnd(12) + 'Count'.padStart(8) + 'Pct'.padStart(8);
console.log(inflightHeader);

for (const bucket of inflightBuckets) {
    const count = rows.filter(r => r.inflight >= bucket.min && r.inflight <= bucket.max).length;
    const pct = totalRequests > 0 ? (count / totalRequests * 100) : 0;
    if (count > 0) {
        console.log(
            bucket.label.padEnd(12) +
            String(count).padStart(8) +
            `${pct.toFixed(0)}%`.padStart(8)
        );
    }
}

// === Throughput Over Time (10s windows) ===
console.log('');
console.log('=== Throughput Over Time (10s windows) ===');
const windowSize = 10; // seconds
const windowHeader = 'Window'.padEnd(12) + 'Dispatched'.padStart(12) + 'Completed'.padStart(12) + 'Inflight(avg)'.padStart(15);
console.log(windowHeader);

if (durationSec > 0) {
    const windowCount = Math.ceil(durationSec / windowSize);
    for (let w = 0; w < windowCount && w < 50; w++) { // cap at 50 windows
        const windowStart = new Date(firstTs.getTime() + w * windowSize * 1000);
        const windowEnd = new Date(firstTs.getTime() + (w + 1) * windowSize * 1000);

        const windowRows = rows.filter(r => r.timestamp >= windowStart && r.timestamp < windowEnd);
        const dispatched = windowRows.length;
        const completed = windowRows.filter(r => r.status === 'OK' || r.status === 'FAIL').length;
        const avgInflight = windowRows.length > 0
            ? (windowRows.reduce((sum, r) => sum + r.inflight, 0) / windowRows.length)
            : 0;

        const startSec = w * windowSize;
        const endSec = (w + 1) * windowSize;
        const windowLabel = `${startSec}-${endSec}s`;

        console.log(
            windowLabel.padEnd(12) +
            String(dispatched).padStart(12) +
            String(completed).padStart(12) +
            avgInflight.toFixed(1).padStart(15)
        );
    }
}

// === Retries and Failures ===
console.log('');
console.log('=== Retries and Failures ===');

// Group by label to find items that had retries
const byLabel = {};
for (const row of rows) {
    if (!byLabel[row.label]) byLabel[row.label] = [];
    byLabel[row.label].push(row);
}

const retried = Object.entries(byLabel)
    .filter(([, items]) => items.length > 1 || items.some(i => i.status === 'FAIL'))
    .sort(([a], [b]) => a.localeCompare(b));

if (retried.length === 0) {
    console.log('(none)');
} else {
    const retryHeader = 'Label'.padEnd(35) + 'Attempts'.padStart(10) + 'Final Status'.padStart(14);
    console.log(retryHeader);

    for (const [label, items] of retried) {
        const attempts = items.length;
        const finalStatus = items[items.length - 1].status;
        console.log(
            label.substring(0, 35).padEnd(35) +
            String(attempts).padStart(10) +
            finalStatus.padStart(14)
        );
    }
}
