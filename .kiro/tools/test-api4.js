// test-api4.js - Test if the issue is relative URL resolution
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;
const charUUID = 'bd61f05d-2fd2-4d1c-8c52-5bd132b21e8a';

const agent = new https.Agent({ rejectUnauthorized: false });

function makeRequest(path) {
  return new Promise((resolve) => {
    const req = https.request({
      hostname: host, port: port, path: path, method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
      agent: agent, timeout: 10000
    }, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => { resolve({ status: res.statusCode, body: data.substring(0, 200) }); });
    });
    req.on('error', (e) => { resolve({ status: 'ERROR', body: e.message }); });
    req.end();
  });
}

async function main() {
  // Test what happens with relative paths from different "page" locations
  // In browser, if user is at /app/profile, relative 'api/v1/...' becomes /app/api/v1/...
  
  console.log('=== /api/v1/characters (absolute) ===');
  let r = await makeRequest('/api/v1/characters');
  console.log(`Status: ${r.status} | ${r.body}`);

  console.log('\n=== /app/api/v1/characters (if resolved from /app/profile) ===');
  r = await makeRequest('/app/api/v1/characters');
  console.log(`Status: ${r.status} | ${r.body}`);

  // Check what ky does with prefix=undefined and input='api/v1/characters/...'
  // ky resolves relative to window.location when prefix is undefined
  // If page is at https://host:5443/app/profile, 'api/v1/...' -> https://host:5443/app/api/v1/...
  // If page is at https://host:5443/, 'api/v1/...' -> https://host:5443/api/v1/...
  
  console.log('\n=== Check: does the SPA use client-side routing with /app/ prefix? ===');
  r = await makeRequest('/app/colonies');
  console.log(`Status: ${r.status} | ${r.body.substring(0, 100)}`);
  
  r = await makeRequest('/login');
  console.log(`/login Status: ${r.status} | ${r.body.substring(0, 100)}`);
}

main();
