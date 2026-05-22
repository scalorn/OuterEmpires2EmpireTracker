// test-api3.js - Simulate exact web UI calls after login
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;

const agent = new https.Agent({ rejectUnauthorized: false });

function makeRequest(path, headers) {
  return new Promise((resolve) => {
    const options = {
      hostname: host,
      port: port,
      path: path,
      method: 'GET',
      headers: headers || { 'Authorization': `Bearer ${token}` },
      agent: agent,
      timeout: 10000
    };

    const req = https.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        resolve({ status: res.statusCode, headers: res.headers, body: data });
      });
    });

    req.on('error', (e) => {
      resolve({ status: 'ERROR', headers: {}, body: e.message });
    });

    req.end();
  });
}

async function main() {
  // Step 1: Login flow - get characters
  console.log('=== Step 1: Login - GET /api/v1/characters ===');
  let r = await makeRequest('/api/v1/characters');
  console.log(`Status: ${r.status}`);
  console.log(`Content-Type: ${r.headers['content-type']}`);
  
  let characters;
  try {
    characters = JSON.parse(r.body);
    console.log(`Parsed: ${characters.length} characters`);
    console.log(`First: ${JSON.stringify(characters[0])}`);
  } catch(e) {
    console.log(`PARSE ERROR: ${e.message}`);
    console.log(`Raw body: ${r.body.substring(0, 200)}`);
    return;
  }

  const charUUID = characters[0].uuid;
  console.log(`\nUsing characterUUID: ${charUUID}`);

  // Step 2: Profile page - get profiles
  console.log('\n=== Step 2: Profile - GET /api/v1/characters/{uuid}/profiles ===');
  r = await makeRequest(`/api/v1/characters/${charUUID}/profiles`);
  console.log(`Status: ${r.status}`);
  console.log(`Content-Type: ${r.headers['content-type']}`);
  console.log(`Raw body: ${r.body}`);
  
  try {
    const profiles = JSON.parse(r.body);
    console.log(`Parsed type: ${typeof profiles}, isArray: ${Array.isArray(profiles)}`);
    if (Array.isArray(profiles)) {
      console.log(`Length: ${profiles.length}`);
    } else {
      console.log(`Keys: ${Object.keys(profiles)}`);
    }
  } catch(e) {
    console.log(`PARSE ERROR: ${e.message}`);
  }

  // Step 3: Check if the response is wrapped in pagination
  console.log('\n=== Step 3: Colonies (for comparison) ===');
  r = await makeRequest(`/api/v1/characters/${charUUID}/colonies`);
  console.log(`Status: ${r.status}`);
  console.log(`Content-Type: ${r.headers['content-type']}`);
  console.log(`Raw body: ${r.body}`);
}

main();
