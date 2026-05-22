// test-api8.js - Push actual PlayerData.json to the server via bulk import
const https = require('https');
const fs = require('fs');
const path = require('path');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;

const agent = new https.Agent({ rejectUnauthorized: false });

function makeRequest(method, reqPath, body) {
  return new Promise((resolve) => {
    const options = {
      hostname: host, port: port, path: reqPath, method: method,
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      agent: agent, timeout: 30000
    };

    const req = https.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => { resolve({ status: res.statusCode, body: data }); });
    });
    req.on('error', (e) => { resolve({ status: 'ERROR', body: e.message }); });
    if (body) req.write(body);
    req.end();
  });
}

async function main() {
  // Read the actual PlayerData.json
  const playerDataPath = path.join(__dirname, '../../OE2EmpireTracker/PlayerData.json');
  const playerJson = fs.readFileSync(playerDataPath, 'utf8');
  const playerRoot = JSON.parse(playerJson);
  
  console.log(`PlayerData.json loaded:`);
  console.log(`  CurrentPlayerUUID: ${playerRoot.CurrentPlayerUUID}`);
  console.log(`  Colonies: ${playerRoot.Colony ? playerRoot.Colony.length : 0}`);
  console.log(`  Blueprints: ${playerRoot.Blueprint ? playerRoot.Blueprint.length : 0}`);
  console.log(`  Surveys: ${playerRoot.Survey ? playerRoot.Survey.length : 0}`);
  console.log(`  Profiles: ${playerRoot.PlayerProfile ? playerRoot.PlayerProfile.length : 0}`);

  const charUUID = playerRoot.CurrentPlayerUUID;

  // Push it to the server
  console.log(`\n=== PUT /api/v1/characters/${charUUID}/import ===`);
  console.log(`Payload size: ${playerJson.length} bytes`);
  
  const r = await makeRequest('PUT', `/api/v1/characters/${charUUID}/import`, playerJson);
  console.log(`Status: ${r.status}`);
  console.log(`Response: ${r.body.substring(0, 2000)}`);

  if (r.status === 200) {
    // Verify data was persisted
    console.log('\n=== Verification ===');
    const colR = await makeRequest('GET', `/api/v1/characters/${charUUID}/colonies`);
    const cols = JSON.parse(colR.body);
    console.log(`Colonies after import: ${Array.isArray(cols) ? cols.length : '?'}`);
  }
}

main();
