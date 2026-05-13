/**
 * OE2 Galaxy Map Explorer — Console Script
 * 
 * Purpose: Discover how the galaxy map data is structured in the game DOM.
 * Run this with the GLXY tab open to find system coordinates, connections, and map data.
 *
 * Usage:
 *   1. Open the game in your browser
 *   2. Click the GLXY hex tab (last tab on the left sidebar)
 *   3. Open browser console (F12 → Console)
 *   4. Paste this entire script and press Enter
 *   5. Review the output — it will dump everything it finds
 *
 * This is an EXPLORATION script, not a production scraper. It looks for:
 *   - Canvas elements (the map is likely rendered here)
 *   - SVG elements (alternative rendering)
 *   - Any DOM elements with system names, coordinates, or map data
 *   - JavaScript objects/variables that might hold galaxy data
 *   - WebSocket messages or network data cached in memory
 */
(function() {
    'use strict';

    const results = {
        timestamp: new Date().toISOString(),
        currentSystem: null,
        coordinates: null,
        canvasElements: [],
        svgElements: [],
        galaxyMapElements: [],
        jumpLanes: [],
        systemExplorerData: [],
        jsGlobals: [],
        webComponents: [],
        interestingElements: []
    };

    console.log('=== OE2 Galaxy Map Explorer ===');
    console.log('');

    // --- 1. Current system info from the top bar ---
    console.log('--- 1. CURRENT SYSTEM INFO ---');
    const locationName = document.querySelector('.location_name');
    if (locationName) {
        results.currentSystem = locationName.textContent.trim();
        console.log('System:', results.currentSystem);
    }
    const locationCoords = document.querySelector('[data-ui-tooltip="GENERAL_UI.SYSTEM_LOCATION"]');
    if (locationCoords) {
        results.coordinates = locationCoords.textContent.trim();
        console.log('Coordinates:', results.coordinates);
    }
    console.log('');

    // --- 2. Galaxy hex tab state ---
    console.log('--- 2. GALAXY HEX TAB STATE ---');
    const galaxyHex = document.getElementById('ui_galaxy_hex');
    if (galaxyHex) {
        console.log('Galaxy hex found:', galaxyHex.className);
        console.log('Is active:', galaxyHex.classList.contains('active'));
        console.log('Has galaxy_map_open:', galaxyHex.classList.contains('galaxy_map_open'));
    } else {
        console.warn('Galaxy hex tab NOT found. Make sure you can see the left sidebar.');
    }
    console.log('');

    // --- 3. Find ALL canvas elements ---
    console.log('--- 3. CANVAS ELEMENTS ---');
    const canvases = document.querySelectorAll('canvas');
    console.log(`Found ${canvases.length} canvas element(s)`);
    canvases.forEach((canvas, i) => {
        const info = {
            index: i,
            id: canvas.id || '(none)',
            className: canvas.className || '(none)',
            width: canvas.width,
            height: canvas.height,
            clientWidth: canvas.clientWidth,
            clientHeight: canvas.clientHeight,
            visible: canvas.offsetParent !== null,
            parentId: canvas.parentElement?.id || '(none)',
            parentClass: canvas.parentElement?.className?.substring(0, 80) || '(none)',
            style: canvas.style.cssText.substring(0, 200)
        };
        results.canvasElements.push(info);
        console.log(`  Canvas[${i}]:`, JSON.stringify(info, null, 2));
    });
    console.log('');

    // --- 4. Find ALL SVG elements ---
    console.log('--- 4. SVG ELEMENTS ---');
    const svgs = document.querySelectorAll('svg');
    console.log(`Found ${svgs.length} SVG element(s)`);
    svgs.forEach((svg, i) => {
        const info = {
            index: i,
            id: svg.id || '(none)',
            className: svg.getAttribute('class') || '(none)',
            viewBox: svg.getAttribute('viewBox') || '(none)',
            width: svg.getAttribute('width'),
            height: svg.getAttribute('height'),
            childCount: svg.children.length,
            parentId: svg.parentElement?.id || '(none)',
            visible: svg.offsetParent !== null
        };
        results.svgElements.push(info);
        if (svg.children.length > 0) {
            info.childTags = [...new Set([...svg.children].map(c => c.tagName))].join(', ');
        }
        console.log(`  SVG[${i}]:`, JSON.stringify(info, null, 2));
    });
    console.log('');

    // --- 5. Look for galaxy map specific elements ---
    console.log('--- 5. GALAXY MAP ELEMENTS ---');
    const mapSelectors = [
        '#GalaxyMap', '#galaxyMap', '#galaxy_map', '#ui_galaxy_map',
        '.GalaxyMap', '.galaxyMap', '.galaxy_map', '.galaxy-map',
        '[id*="galaxy"]', '[id*="Galaxy"]', '[class*="galaxy"]', '[class*="Galaxy"]',
        '[id*="map"]', '[id*="Map"]',
        'ui-galaxy', 'ui-galaxymap', 'ui-galaxy-map',
        '[id*="star"]', '[id*="Star"]', '[class*="star"]', '[class*="Star"]',
        '[id*="system"]', '[id*="System"]',
        '[id*="sector"]', '[id*="Sector"]',
        '[id*="quadrant"]', '[id*="Quadrant"]'
    ];
    
    const seen = new Set();
    mapSelectors.forEach(sel => {
        try {
            const els = document.querySelectorAll(sel);
            els.forEach(el => {
                const key = el.tagName + '#' + el.id + '.' + el.className;
                if (seen.has(key)) return;
                seen.add(key);
                const info = {
                    selector: sel,
                    tag: el.tagName.toLowerCase(),
                    id: el.id || '(none)',
                    className: (el.className || '').toString().substring(0, 100),
                    childCount: el.children.length,
                    textContent: el.textContent?.substring(0, 100)?.trim() || '',
                    visible: el.offsetParent !== null,
                    dimensions: `${el.clientWidth}x${el.clientHeight}`
                };
                results.galaxyMapElements.push(info);
                console.log(`  [${sel}]`, info.tag, info.id, info.dimensions, info.visible ? 'VISIBLE' : 'hidden');
            });
        } catch(e) {}
    });
    console.log('');

    // --- 6. Jump lanes from System Explorer ---
    console.log('--- 6. JUMP LANES (System Explorer) ---');
    const jumpLaneItems = document.querySelectorAll('#SystemExplorer_JumpLanes_Expanded .SystemExplorer_Item');
    console.log(`Found ${jumpLaneItems.length} jump lane(s)`);
    jumpLaneItems.forEach((item, i) => {
        const nameEl = item.querySelector('.SystemExplorer_ObjectName');
        const distEl = item.querySelector('.SystemExplorer_Distance');
        const tooltip = nameEl?.getAttribute('data-ui-tooltip') || '';
        const name = nameEl?.textContent?.trim() || tooltip;
        const distance = distEl?.textContent?.trim() || '';
        const info = { name, tooltip, distance };
        results.jumpLanes.push(info);
        console.log(`  Lane[${i}]: "${name}" tooltip="${tooltip}" distance="${distance}"`);
    });
    console.log('');

    // --- 7. Look for web components (custom elements) ---
    console.log('--- 7. CUSTOM WEB COMPONENTS ---');
    const allElements = document.querySelectorAll('*');
    const customTags = new Set();
    allElements.forEach(el => {
        const tag = el.tagName.toLowerCase();
        if (tag.includes('-') || tag.startsWith('ui-')) {
            customTags.add(tag);
        }
    });
    const sortedTags = [...customTags].sort();
    results.webComponents = sortedTags;
    console.log(`Found ${sortedTags.length} custom element types:`);
    sortedTags.forEach(tag => {
        const count = document.querySelectorAll(tag).length;
        console.log(`  <${tag}> × ${count}`);
    });
    console.log('');

    // --- 8. Search for galaxy/map data in JavaScript globals ---
    console.log('--- 8. JAVASCRIPT GLOBALS (galaxy/map related) ---');
    const interestingKeys = [];
    const searchTerms = ['galaxy', 'map', 'system', 'sector', 'quadrant', 'star', 'jump', 'lane', 'coordinate', 'coord'];
    
    for (const key of Object.keys(window)) {
        const lower = key.toLowerCase();
        if (searchTerms.some(term => lower.includes(term))) {
            const val = window[key];
            const type = typeof val;
            const info = { key, type };
            if (type === 'object' && val !== null) {
                info.isArray = Array.isArray(val);
                info.keys = Object.keys(val).slice(0, 10);
                info.length = val.length;
            }
            interestingKeys.push(info);
            console.log(`  window.${key} (${type}):`, val);
        }
    }
    results.jsGlobals = interestingKeys;
    console.log('');

    // --- 9. Look for data attributes that might contain coordinates ---
    console.log('--- 9. DATA ATTRIBUTES WITH COORDINATES ---');
    const dataEls = document.querySelectorAll('[data-x], [data-y], [data-coord], [data-position], [data-system], [data-sector]');
    console.log(`Found ${dataEls.length} elements with coordinate-like data attributes`);
    dataEls.forEach((el, i) => {
        if (i > 20) return; // limit output
        const attrs = {};
        for (const attr of el.attributes) {
            if (attr.name.startsWith('data-')) attrs[attr.name] = attr.value;
        }
        console.log(`  [${i}] <${el.tagName.toLowerCase()}> ${el.id || el.className?.substring(0,40)}:`, attrs);
        results.interestingElements.push({ tag: el.tagName, id: el.id, attrs });
    });
    console.log('');

    // --- 10. Look for the galaxy map slideout/panel content ---
    console.log('--- 10. LEFT SLIDEOUT PANELS ---');
    const leftSlideouts = document.querySelectorAll('#ui_left_hex ~ div, [id*="slideout"], [class*="slideout"], [class*="SlideOut"]');
    leftSlideouts.forEach((el, i) => {
        if (i > 30) return;
        const visible = el.offsetParent !== null || el.style.display !== 'none';
        if (el.clientWidth > 50 || el.clientHeight > 50) {
            console.log(`  Panel[${i}]: <${el.tagName.toLowerCase()}> id="${el.id}" class="${(el.className||'').toString().substring(0,60)}" ${el.clientWidth}x${el.clientHeight} ${visible ? 'VISIBLE' : 'hidden'}`);
        }
    });
    console.log('');

    // --- 11. Check for WebGL context on canvases ---
    console.log('--- 11. CANVAS RENDERING CONTEXTS ---');
    canvases.forEach((canvas, i) => {
        let contextType = 'unknown';
        try {
            if (canvas.getContext('webgl2')) contextType = 'webgl2';
            else if (canvas.getContext('webgl')) contextType = 'webgl';
            else if (canvas.getContext('2d')) contextType = '2d';
        } catch(e) {
            contextType = 'error: ' + e.message;
        }
        console.log(`  Canvas[${i}] (${canvas.width}x${canvas.height}): context=${contextType}`);
    });
    console.log('');

    // --- 12. Look for any large data structures in the DOM (JSON in script tags, etc) ---
    console.log('--- 12. INLINE SCRIPT DATA ---');
    const scripts = document.querySelectorAll('script:not([src])');
    scripts.forEach((script, i) => {
        const content = script.textContent;
        if (content.length > 100) {
            const hasMapData = searchTerms.some(term => content.toLowerCase().includes(term));
            if (hasMapData) {
                console.log(`  Script[${i}] (${content.length} chars): Contains map-related keywords`);
                console.log(`    Preview: ${content.substring(0, 200)}...`);
            }
        }
    });
    console.log('');

    // --- 13. Check for Angular/React/Vue/Lit state ---
    console.log('--- 13. FRAMEWORK STATE ---');
    if (window.__REACT_DEVTOOLS_GLOBAL_HOOK__) console.log('  React detected');
    if (window.ng) console.log('  Angular detected');
    if (window.__VUE__) console.log('  Vue detected');
    if (window.litElementVersions) console.log('  Lit detected');
    // Check for Lit (the game uses ?lit$ in templates based on the blueprint scraper)
    const litEls = document.querySelectorAll('[\\?lit\\$]');
    if (document.querySelector('body')?.innerHTML?.includes('?lit$')) {
        console.log('  Lit templates detected in DOM (game uses Lit web components)');
    }
    console.log('');

    // --- SUMMARY ---
    console.log('=== SUMMARY ===');
    console.log(`Current system: ${results.currentSystem} ${results.coordinates}`);
    console.log(`Canvas elements: ${results.canvasElements.length}`);
    console.log(`SVG elements: ${results.svgElements.length}`);
    console.log(`Galaxy-related DOM elements: ${results.galaxyMapElements.length}`);
    console.log(`Jump lanes visible: ${results.jumpLanes.length}`);
    console.log(`Custom web components: ${results.webComponents.length}`);
    console.log(`JS globals with map keywords: ${results.jsGlobals.length}`);
    console.log('');
    console.log('Full results object available as: window.__galaxyExplorer');
    window.__galaxyExplorer = results;

    // Also try to find the galaxy map's actual content container
    console.log('');
    console.log('--- BONUS: Searching for large visible containers that might be the map ---');
    const allDivs = document.querySelectorAll('div');
    const largeDivs = [...allDivs]
        .filter(d => d.clientWidth > 400 && d.clientHeight > 400 && d.offsetParent !== null)
        .filter(d => !d.id?.startsWith('ui_chat') && !d.id?.startsWith('ui_top'))
        .sort((a, b) => (b.clientWidth * b.clientHeight) - (a.clientWidth * a.clientHeight))
        .slice(0, 10);
    
    largeDivs.forEach((div, i) => {
        const hasCanvas = div.querySelector('canvas') !== null;
        const hasSvg = div.querySelector('svg') !== null;
        console.log(`  Large[${i}]: <div> id="${div.id}" class="${(div.className||'').toString().substring(0,60)}" ${div.clientWidth}x${div.clientHeight} canvas=${hasCanvas} svg=${hasSvg} children=${div.children.length}`);
    });

})();
