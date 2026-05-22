// test-api7.js - Check what the server has after the desktop push
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;

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
  // Get all characters
  console.log('=== All characters on server ===');
  let r = await makeRequest('/api/v1/characters');
  const chars = JSON.parse(r.body);
  for (const c of chars) {
    console.log(`  ${c.uuid} = ${c.name}`);
  }

  // Check each character for data
  for (const c of chars) {
    console.log(`\n=== ${c.name} (${c.uuid}) ===`);
    r = await makeRequest(`/api/v1/characters/${c.uuid}/colonies`);
    const colonies = JSON.parse(r.body);
    const colCount = Array.isArray(colonies) ? colonies.length : 0;
    
    r = await makeRequest(`/api/v1/characters/${c.uuid}/blueprints`);
    const bps = JSON.parse(r.body);
    const bpCount = Array.isArray(bps) ? bps.length : 0;
    
    r = await makeRequest(`/api/v1/characters/${c.uuid}/surveys`);
    const surveys = JSON.parse(r.body);
    const surveyCount = Array.isArray(surveys) ? surveys.length : 0;

    r = await makeRequest(`/api/v1/characters/${c.uuid}/profiles`);
    const profiles = JSON.parse(r.body);
    const profileCount = Array.isArray(profiles) ? profiles.length : 0;

    console.log(`  Colonies: ${colCount}, Blueprints: ${bpCount}, Surveys: ${surveyCount}, Profiles: ${profileCount}`);
  }

  // Also check all tokens to see what characterUUID they map to
  console.log('\n=== All tokens ===');
  r = await makeRequest('/api/v1/tokens');
  if (r.status === 200) {
    const tokens = JSON.parse(r.body);
    for (const t of tokens) {
      console.log(`  ${t.id}: role=${t.role}, charUUID=${t.characterUUID || '(none)'}, name=${t.characterName || '(none)'}`);
    }
  } else {
    console.log(`  Status: ${r.status}`);
  }
}

main();
