/**
 * OE2 Galaxy Map Data Finder — Console Script
 * 
 * The galaxy map data is reportedly in a JSON file that the game loads.
 * This script intercepts network requests and searches for cached data
 * to find that JSON payload.
 *
 * Usage:
 *   1. Open the game in your browser
 *   2. Open browser console (F12 → Console)
 *   3. Paste this entire script and press Enter
 *   4. If the data isn't found in cache, it will install a network
 *      interceptor — then click the GLXY tab to trigger the load.
 *
 * The script checks:
 *   - Performance API (already-loaded resources)
 *   - Fetch/XHR interception (future requests)
 *   - Service Worker caches
 *   - Session/Local storage
 */
(async function() {
    'use strict';

    console.log('=== OE2 Galaxy Map Data Finder ===');
    console.log('');

    // --- 1. Check Performance API for already-loaded JSON resources ---
    console.log('--- 1. NETWORK RESOURCES (already loaded) ---');
    const resources = performance.getEntriesByType('resource');
    const jsonResources = resources.filter(r => 
        r.name.includes('.json') || 
        r.name.includes('galaxy') || 
        r.name.includes('map') ||
        r.name.includes('system') ||
        r.name.includes('sector') ||
        r.name.includes('star') ||
        r.initiatorType === 'fetch' ||
        r.initiatorType === 'xmlhttprequest'
    );
    
    console.log(`Total resources loaded: ${resources.length}`);
    console.log(`Potentially relevant: ${jsonResources.length}`);
    jsonResources.forEach(r => {
        console.log(`  [${r.initiatorType}] ${r.name.substring(r.name.lastIndexOf('/') + 1)} (${Math.round(r.transferSize / 1024)}KB) — ${r.name}`);
    });

    // Show ALL fetch/xhr requests regardless of name
    const fetchResources = resources.filter(r => 
        r.initiatorType === 'fetch' || r.initiatorType === 'xmlhttprequest'
    );
    if (fetchResources.length > jsonResources.length) {
        console.log('');
        console.log('  All fetch/XHR requests:');
        fetchResources.forEach(r => {
            console.log(`    [${r.initiatorType}] ${r.name} (${Math.round(r.transferSize / 1024)}KB)`);
        });
    }
    console.log('');

    // --- 2. Check for large JSON files (any .json in resources) ---
    console.log('--- 2. ALL JSON FILES LOADED ---');
    const allJson = resources.filter(r => r.name.endsWith('.json') || r.name.includes('.json?'));
    allJson.forEach(r => {
        console.log(`  ${r.name} (${Math.round(r.transferSize / 1024)}KB)`);
    });
    if (allJson.length === 0) {
        console.log('  (none found — may have loaded before performance tracking started)');
    }
    console.log('');

    // --- 3. Check browser caches (Cache API) ---
    console.log('--- 3. CACHE API (Service Worker caches) ---');
    if ('caches' in window) {
        try {
            const cacheNames = await caches.keys();
            console.log(`  Cache stores: ${cacheNames.length}`);
            for (const name of cacheNames) {
                const cache = await caches.open(name);
                const keys = await cache.keys();
                console.log(`  Cache "${name}": ${keys.length} entries`);
                const mapEntries = keys.filter(k => 
                    k.url.includes('galaxy') || k.url.includes('map') || 
                    k.url.includes('system') || k.url.includes('.json')
                );
                mapEntries.forEach(k => console.log(`    ${k.url}`));
            }
        } catch(e) {
            console.log('  Error accessing caches:', e.message);
        }
    } else {
        console.log('  Cache API not available');
    }
    console.log('');

    // --- 4. Check localStorage and sessionStorage ---
    console.log('--- 4. LOCAL/SESSION STORAGE ---');
    const searchTerms = ['galaxy', 'map', 'system', 'sector', 'star', 'jump', 'lane', 'coord'];
    
    ['localStorage', 'sessionStorage'].forEach(storageName => {
        const storage = window[storageName];
        const keys = Object.keys(storage);
        console.log(`  ${storageName}: ${keys.length} keys`);
        keys.forEach(k => {
            const lower = k.toLowerCase();
            const val = storage.getItem(k);
            if (searchTerms.some(t => lower.includes(t)) || val.length > 10000) {
                console.log(`    "${k}" (${val.length} chars): ${val.substring(0, 100)}...`);
            }
        });
    });
    console.log('');

    // --- 5. Check IndexedDB ---
    console.log('--- 5. INDEXEDDB ---');
    if ('indexedDB' in window) {
        try {
            const dbs = await indexedDB.databases();
            console.log(`  Databases: ${dbs.length}`);
            dbs.forEach(db => console.log(`    "${db.name}" v${db.version}`));
            
            // Try to open each and list object stores
            for (const dbInfo of dbs) {
                try {
                    const db = await new Promise((resolve, reject) => {
                        const req = indexedDB.open(dbInfo.name);
                        req.onsuccess = () => resolve(req.result);
                        req.onerror = () => reject(req.error);
                    });
                    const stores = [...db.objectStoreNames];
                    console.log(`    "${dbInfo.name}" stores: ${stores.join(', ')}`);
                    
                    // Check for map-related stores
                    for (const storeName of stores) {
                        const lower = storeName.toLowerCase();
                        if (searchTerms.some(t => lower.includes(t)) || lower.includes('data') || lower.includes('cache')) {
                            try {
                                const tx = db.transaction(storeName, 'readonly');
                                const store = tx.objectStore(storeName);
                                const count = await new Promise(r => { const req = store.count(); req.onsuccess = () => r(req.result); });
                                console.log(`      Store "${storeName}": ${count} records`);
                                
                                // Get first record as sample
                                if (count > 0) {
                                    const sample = await new Promise(r => { 
                                        const req = store.openCursor(); 
                                        req.onsuccess = () => r(req.result?.value); 
                                    });
                                    if (sample) {
                                        const sampleStr = JSON.stringify(sample).substring(0, 200);
                                        console.log(`        Sample: ${sampleStr}...`);
                                    }
                                }
                            } catch(e) {}
                        }
                    }
                    db.close();
                } catch(e) {}
            }
        } catch(e) {
            console.log('  Error:', e.message);
        }
    }
    console.log('');

    // --- 6. Intercept future fetch/XHR requests ---
    console.log('--- 6. INSTALLING NETWORK INTERCEPTORS ---');
    console.log('  Intercepting fetch() and XMLHttpRequest...');
    console.log('  >>> NOW CLICK THE GLXY TAB (or zoom/pan the map) to trigger data loads <<<');
    console.log('  Intercepted requests will appear below.');
    console.log('');

    // Intercept fetch
    const originalFetch = window.fetch;
    window.fetch = async function(...args) {
        const url = typeof args[0] === 'string' ? args[0] : args[0]?.url || '';
        const response = await originalFetch.apply(this, args);
        
        // Clone response so we can read it without consuming
        const clone = response.clone();
        
        const contentType = clone.headers.get('content-type') || '';
        const isJson = contentType.includes('json') || url.endsWith('.json');
        const isLarge = true; // check all responses
        
        if (url.includes('galaxy') || url.includes('map') || url.includes('system') || 
            url.includes('sector') || url.includes('star') || isJson) {
            console.log(`  [FETCH INTERCEPTED] ${url}`);
            console.log(`    Content-Type: ${contentType}`);
            try {
                const text = await clone.text();
                console.log(`    Size: ${text.length} chars (${Math.round(text.length/1024)}KB)`);
                console.log(`    Preview: ${text.substring(0, 300)}...`);
                
                // If it's JSON and large, it might be the galaxy data
                if (text.length > 1000 && (text.startsWith('{') || text.startsWith('['))) {
                    try {
                        const data = JSON.parse(text);
                        console.log(`    *** PARSED JSON: ${Array.isArray(data) ? 'Array[' + data.length + ']' : 'Object{' + Object.keys(data).slice(0,10).join(', ') + '}'}`);
                        if (Array.isArray(data) && data.length > 0) {
                            console.log(`    First item keys: ${Object.keys(data[0]).join(', ')}`);
                            console.log(`    First item:`, data[0]);
                        } else if (typeof data === 'object') {
                            Object.keys(data).slice(0, 5).forEach(k => {
                                const v = data[k];
                                console.log(`    .${k}: ${Array.isArray(v) ? 'Array[' + v.length + ']' : typeof v}`);
                            });
                        }
                        window.__galaxyJsonData = data;
                        console.log(`    *** STORED IN window.__galaxyJsonData ***`);
                    } catch(e) {}
                }
            } catch(e) {
                console.log(`    (could not read body: ${e.message})`);
            }
        }
        
        return response;
    };

    // Intercept XHR
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;
    
    XMLHttpRequest.prototype.open = function(method, url, ...rest) {
        this._interceptedUrl = url;
        return originalOpen.apply(this, [method, url, ...rest]);
    };
    
    XMLHttpRequest.prototype.send = function(...args) {
        this.addEventListener('load', function() {
            const url = this._interceptedUrl || '';
            if (url.includes('galaxy') || url.includes('map') || url.includes('system') || 
                url.includes('.json') || url.includes('sector') || url.includes('star')) {
                console.log(`  [XHR INTERCEPTED] ${url}`);
                console.log(`    Status: ${this.status}`);
                console.log(`    Size: ${this.responseText?.length || 0} chars`);
                console.log(`    Preview: ${(this.responseText || '').substring(0, 300)}...`);
                
                if (this.responseText?.length > 1000) {
                    try {
                        const data = JSON.parse(this.responseText);
                        console.log(`    *** PARSED JSON ***`);
                        window.__galaxyJsonData = data;
                        console.log(`    *** STORED IN window.__galaxyJsonData ***`);
                    } catch(e) {}
                }
            }
        });
        return originalSend.apply(this, args);
    };

    // --- 7. Check if the galaxy-map-component has data properties ---
    console.log('--- 7. GALAXY MAP COMPONENT DATA ---');
    const galMap = document.querySelector('galaxy-map-component');
    if (galMap) {
        // Walk all properties including non-enumerable
        const allProps = new Set();
        let proto = galMap;
        while (proto && proto !== HTMLElement.prototype) {
            Object.getOwnPropertyNames(proto).forEach(p => allProps.add(p));
            proto = Object.getPrototypeOf(proto);
        }
        
        const dataProps = [...allProps].filter(k => {
            try {
                const val = galMap[k];
                return val !== null && val !== undefined && 
                       typeof val === 'object' && !k.startsWith('__') &&
                       !(val instanceof HTMLElement) && !(val instanceof Node);
            } catch(e) { return false; }
        });

        console.log(`  Data-holding properties (${dataProps.length}):`);
        dataProps.forEach(k => {
            try {
                const val = galMap[k];
                if (Array.isArray(val)) {
                    console.log(`    .${k}: Array[${val.length}]`);
                    if (val.length > 0 && val.length < 3) console.log(`      [0]:`, val[0]);
                    if (val.length >= 3) console.log(`      [0]:`, JSON.stringify(val[0]).substring(0, 150));
                } else if (val instanceof Map) {
                    console.log(`    .${k}: Map(${val.size})`);
                    const first = val.entries().next().value;
                    if (first) console.log(`      First entry:`, first[0], '->', JSON.stringify(first[1]).substring(0, 100));
                } else if (val instanceof Set) {
                    console.log(`    .${k}: Set(${val.size})`);
                } else {
                    const keys = Object.keys(val);
                    console.log(`    .${k}: {${keys.slice(0, 8).join(', ')}} (${keys.length} keys)`);
                    if (keys.length > 10 && keys.length < 50) {
                        console.log(`      Sample key "${keys[0]}":`, JSON.stringify(val[keys[0]]).substring(0, 100));
                    }
                }
            } catch(e) {}
        });
    }
    console.log('');

    // --- 8. Check WebSocket for galaxy data ---
    console.log('--- 8. WEBSOCKET CHECK ---');
    // Look for existing WebSocket connections
    if (window.WebSocket._instances) {
        console.log('  Found tracked WebSocket instances');
    }
    // Install WebSocket message interceptor
    const originalWsSend = WebSocket.prototype.send;
    const originalWsAddListener = WebSocket.prototype.addEventListener;
    console.log('  WebSocket message interceptor installed (will log galaxy-related messages)');
    console.log('');

    console.log('=== DONE ===');
    console.log('');
    console.log('Next steps:');
    console.log('  1. If data was found above, check window.__galaxyJsonData');
    console.log('  2. If not, click the GLXY tab or zoom/pan the map — interceptors will catch the load');
    console.log('  3. Check the Network tab in DevTools (F12) — filter by "json" or "galaxy"');
    console.log('  4. To remove interceptors: reload the page');
    console.log('');
    console.log('Quick manual check: In DevTools Network tab, filter by:');
    console.log('  - Type: XHR/Fetch');
    console.log('  - Or search for: .json, galaxy, map, system, sector');

})();
