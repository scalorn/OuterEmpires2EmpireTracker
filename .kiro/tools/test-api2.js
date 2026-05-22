// test-api2.js - Test profile and character-specific endpoints
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;
const charUUID = 'bd61f05d-2fd2-4d1c-8c52-5bd132b21e8a';

const agent = new https.Agent({ rejectUnauthorized: false });

function makeRequest(path) {
  return new Promise((resolve) => {
    const options = {
      hostname: host,
      port: port,
      path: path,
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
      agent: agent,
      timeout: 10000
    };

    const req = https.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        resolve({ status: res.statusCode, body: data.substring(0, 800) });
      });
    });

    req.on('error', (e) => {
      resolve({ status: 'ERROR', body: e.message });
    });

    req.on('timeout', () => {
      req.destroy();
      resolve({ status: 'TIMEOUT', body: 'Request timed out' });
    });

    req.end();
  });
}

async function main() {
  console.log('=== GET /api/v1/characters/{uuid}/profiles ===');
  let r = await makeRequest(`/api/v1/characters/${charUUID}/profiles`);
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  console.log('\n=== GET /api/v1/characters/{uuid}/colonies ===');
  r = await makeRequest(`/api/v1/characters/${charUUID}/colonies`);
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  console.log('\n=== GET /api/v1/characters/{uuid}/export ===');
  r = await makeRequest(`/api/v1/characters/${charUUID}/export`);
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  // Check what the web UI login flow would see
  console.log('\n=== Simulating web UI login flow ===');
  console.log('1. charactersApi.getAll() returns 2 characters');
  console.log('2. First character UUID: ' + charUUID);
  console.log('3. Auth store sets characterUUID = ' + charUUID);
  console.log('4. ProfileEditor calls profilesApi.getAll(' + charUUID + ')');
}

main();
