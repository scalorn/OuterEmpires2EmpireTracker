// test-api5.js - Check if data exists in export vs typed endpoints
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
      res.on('end', () => { resolve({ status: res.statusCode, body: data }); });
    });
    req.on('error', (e) => { resolve({ status: 'ERROR', body: e.message }); });
    req.end();
  });
}

async function main() {
  // Check export endpoint - assembles from typed storage
  console.log('=== GET /export (assembles from typed storage) ===');
  let r = await makeRequest(`/api/v1/characters/${charUUID}/export`);
  console.log(`Status: ${r.status}`);
  const exportData = JSON.parse(r.body);
  for (const [key, value] of Object.entries(exportData)) {
    const arr = Array.isArray(value) ? value : [];
    if (arr.length > 0) {
      console.log(`  ${key}: ${arr.length} items`);
    }
  }
  console.log('  (All other collections are empty)');

  // Check typed endpoints directly
  console.log('\n=== Typed endpoints (direct) ===');
  const endpoints = ['colonies', 'blueprints', 'surveys', 'profiles', 'delivery-routes', 'ships'];
  for (const ep of endpoints) {
    r = await makeRequest(`/api/v1/characters/${charUUID}/${ep}`);
    const data = JSON.parse(r.body);
    const count = Array.isArray(data) ? data.length : (data.items ? data.items.length : '?');
    console.log(`  ${ep}: ${count} items (status ${r.status})`);
  }

  // Check if there's data on the second character
  const char2UUID = 'f3e11ab0-a5d0-4e57-8f1a-2c8d2a781315';
  console.log(`\n=== Second character (${char2UUID}) ===`);
  r = await makeRequest(`/api/v1/characters/${char2UUID}/export`);
  console.log(`Status: ${r.status}`);
  const export2 = JSON.parse(r.body);
  for (const [key, value] of Object.entries(export2)) {
    const arr = Array.isArray(value) ? value : [];
    if (arr.length > 0) {
      console.log(`  ${key}: ${arr.length} items`);
    }
  }
  console.log('  (All other collections are empty)');
}

main();
