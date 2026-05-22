// test-api.js - Node.js script to test the server
const https = require('https');

const token = 'X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY';
const host = '192.168.4.36';
const port = 5443;

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
  console.log('=== GET / ===');
  let r = await makeRequest('/');
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  console.log('\n=== GET /api/v1/characters ===');
  r = await makeRequest('/api/v1/characters');
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  console.log('\n=== GET /api/v1/sync ===');
  r = await makeRequest('/api/v1/sync');
  console.log(`Status: ${r.status}`);
  console.log(r.body);

  console.log('\n=== GET /config.json ===');
  r = await makeRequest('/config.json');
  console.log(`Status: ${r.status}`);
  console.log(r.body);
}

main();
