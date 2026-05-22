// test-api6.js - Test bulk import with a sample colony
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;
const charUUID = 'bd61f05d-2fd2-4d1c-8c52-5bd132b21e8a';

const agent = new https.Agent({ rejectUnauthorized: false });

function makeRequest(method, path, body) {
  return new Promise((resolve) => {
    const options = {
      hostname: host, port: port, path: path, method: method,
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      agent: agent, timeout: 10000
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
  // Test 1: Import a simple colony
  const payload = {
    DataVersion: 1,
    CurrentPlayerUUID: charUUID,
    Colony: [
      {
        UUID: "test-colony-001",
        OwnerUUID: charUUID,
        PlanetName: "Test Planet",
        ColonyName: "Test Colony"
      }
    ]
  };

  console.log('=== PUT /import with test colony ===');
  let r = await makeRequest('PUT', `/api/v1/characters/${charUUID}/import`, JSON.stringify(payload));
  console.log(`Status: ${r.status}`);
  console.log(`Body: ${r.body}`);

  // Test 2: Check if it was persisted
  console.log('\n=== GET /colonies after import ===');
  r = await makeRequest('GET', `/api/v1/characters/${charUUID}/colonies`);
  console.log(`Status: ${r.status}`);
  console.log(`Body: ${r.body}`);

  // Test 3: Try import with Newtonsoft-style serialization (what desktop sends)
  // Newtonsoft with DefaultValueHandling.Ignore omits 0/false/null/empty
  const newtonsoftPayload = {
    DataVersion: 1,
    CurrentPlayerUUID: charUUID,
    Colony: [
      {
        UUID: "test-colony-002",
        OwnerUUID: charUUID,
        PlanetName: "Newtonsoft Planet",
        ColonyName: "Newtonsoft Colony"
      }
    ],
    Blueprint: [
      {
        UUID: "test-bp-001",
        OwnerUUID: charUUID,
        Name: "Test Blueprint",
        BluePrintType: "Weapon"
      }
    ]
  };

  console.log('\n=== PUT /import with Newtonsoft-style payload ===');
  r = await makeRequest('PUT', `/api/v1/characters/${charUUID}/import`, JSON.stringify(newtonsoftPayload));
  console.log(`Status: ${r.status}`);
  console.log(`Body: ${r.body}`);

  // Test 4: Verify persistence
  console.log('\n=== GET /colonies after second import ===');
  r = await makeRequest('GET', `/api/v1/characters/${charUUID}/colonies`);
  console.log(`Status: ${r.status}`);
  console.log(`Body: ${r.body}`);

  console.log('\n=== GET /blueprints after import ===');
  r = await makeRequest('GET', `/api/v1/characters/${charUUID}/blueprints`);
  console.log(`Status: ${r.status}`);
  console.log(`Body: ${r.body}`);
}

main();
