/**
 * OE2 Galaxy Map Pixi Explorer — Console Script (Phase 2)
 * 
 * Now that we know the galaxy map uses PixiJS (window.galaxy_map_pixi_app),
 * this script walks the Pixi scene graph to find system objects, their
 * positions, names, and connections (jump lanes).
 *
 * Usage:
 *   1. Open the game, click the GLXY hex tab
 *   2. Open browser console (F12 → Console)
 *   3. Paste this entire script and press Enter
 *   4. Review the output
 *
 * Results stored in window.__galaxyMapData for interactive exploration.
 */
(function() {
    'use strict';

    const app = window.galaxy_map_pixi_app;
    if (!app) {
        console.error('[GalaxyExplorer] window.galaxy_map_pixi_app not found. Is the GLXY tab open?');
        return;
    }

    console.log('=== OE2 Galaxy Map Pixi Explorer (Phase 2) ===');
    console.log('');

    const results = {
        timestamp: new Date().toISOString(),
        appInfo: {},
        stageChildren: [],
        systems: [],
        connections: [],
        textObjects: [],
        containers: [],
        allObjects: []
    };

    // --- 1. App info ---
    console.log('--- 1. PIXI APP INFO ---');
    console.log('  Renderer type:', app.renderer.type === 1 ? 'WebGL' : 'Canvas');
    console.log('  Screen:', app.screen.width, 'x', app.screen.height);
    console.log('  Stage children:', app.stage.children.length);
    console.log('  Stage position:', app.stage.x, app.stage.y);
    console.log('  Stage scale:', app.stage.scale?.x, app.stage.scale?.y);
    results.appInfo = {
        rendererType: app.renderer.type === 1 ? 'WebGL' : 'Canvas',
        screenWidth: app.screen.width,
        screenHeight: app.screen.height,
        stageChildren: app.stage.children.length,
        stageX: app.stage.x,
        stageY: app.stage.y,
        stageScaleX: app.stage.scale?.x,
        stageScaleY: app.stage.scale?.y
    };
    console.log('');

    // --- 2. Walk the scene graph ---
    console.log('--- 2. SCENE GRAPH STRUCTURE ---');
    
    function getObjectInfo(obj, depth = 0) {
        const info = {
            type: obj.constructor?.name || 'unknown',
            label: obj.label || obj.name || null,
            x: obj.x,
            y: obj.y,
            visible: obj.visible,
            alpha: obj.alpha,
            childCount: obj.children?.length || 0,
            width: obj.width,
            height: obj.height
        };

        // Check for text
        if (obj.text !== undefined) {
            info.text = obj.text;
            info.style = obj.style ? {
                fontSize: obj.style.fontSize,
                fill: obj.style.fill,
                fontFamily: obj.style.fontFamily
            } : null;
        }

        // Check for texture/sprite info
        if (obj.texture) {
            info.hasTexture = true;
            info.textureWidth = obj.texture.width;
            info.textureHeight = obj.texture.height;
        }

        // Check for graphics (lines, shapes)
        if (obj.geometry) {
            info.hasGeometry = true;
        }

        // Check for tint (colored objects)
        if (obj.tint !== undefined && obj.tint !== 0xFFFFFF) {
            info.tint = '0x' + obj.tint.toString(16);
        }

        // Check for interactive/event data
        if (obj.interactive || obj.eventMode) {
            info.interactive = true;
            info.eventMode = obj.eventMode;
            info.cursor = obj.cursor;
        }

        // Check for custom data properties
        const customKeys = Object.keys(obj).filter(k => 
            !k.startsWith('_') && 
            !['x','y','width','height','visible','alpha','children','parent',
              'scale','rotation','pivot','anchor','transform','worldTransform',
              'texture','tint','blendMode','filters','mask','interactive',
              'eventMode','cursor','label','name','destroyed'].includes(k) &&
            typeof obj[k] !== 'function'
        );
        if (customKeys.length > 0) {
            info.customProperties = {};
            customKeys.forEach(k => {
                const val = obj[k];
                if (val !== null && val !== undefined && typeof val !== 'object') {
                    info.customProperties[k] = val;
                } else if (typeof val === 'object' && val !== null && !Array.isArray(val)) {
                    info.customProperties[k] = '{' + Object.keys(val).slice(0, 5).join(', ') + '}';
                } else if (Array.isArray(val)) {
                    info.customProperties[k] = `[${val.length} items]`;
                }
            });
        }

        return info;
    }

    function walkSceneGraph(obj, depth = 0, path = 'stage', maxDepth = 4) {
        const info = getObjectInfo(obj, depth);
        info.path = path;
        info.depth = depth;
        results.allObjects.push(info);

        const prefix = '  '.repeat(depth);
        const label = info.label ? ` "${info.label}"` : '';
        const text = info.text ? ` text="${info.text.substring(0, 30)}"` : '';
        const pos = `(${Math.round(info.x)}, ${Math.round(info.y)})`;
        const extra = info.customProperties ? ' props=' + JSON.stringify(info.customProperties) : '';

        if (depth <= 3 || info.text || info.label || info.interactive) {
            console.log(`${prefix}[${info.type}]${label}${text} ${pos} children=${info.childCount} visible=${info.visible}${extra}`);
        }

        // Categorize
        if (info.text) results.textObjects.push(info);
        if (info.type === 'Container' || info.type === '_Container') results.containers.push(info);

        // Recurse into children
        if (obj.children && depth < maxDepth) {
            // If there are too many children, sample them
            const children = obj.children;
            const limit = depth < 2 ? children.length : Math.min(children.length, 20);
            
            for (let i = 0; i < limit; i++) {
                walkSceneGraph(children[i], depth + 1, `${path}[${i}]`, maxDepth);
            }
            if (children.length > limit) {
                console.log(`${prefix}  ... and ${children.length - limit} more children`);
            }
        }
    }

    walkSceneGraph(app.stage, 0, 'stage', 5);
    console.log('');

    // --- 3. Find text objects (system names) ---
    console.log('--- 3. TEXT OBJECTS (potential system names) ---');
    console.log(`Found ${results.textObjects.length} text objects`);
    const uniqueTexts = [...new Set(results.textObjects.map(t => t.text))].sort();
    console.log(`Unique text values (${uniqueTexts.length}):`);
    uniqueTexts.slice(0, 50).forEach(t => console.log(`  "${t}"`));
    if (uniqueTexts.length > 50) console.log(`  ... and ${uniqueTexts.length - 50} more`);
    console.log('');

    // --- 4. Look for system-like objects (interactive, positioned, with data) ---
    console.log('--- 4. INTERACTIVE OBJECTS (clickable systems?) ---');
    const interactiveObjects = results.allObjects.filter(o => o.interactive);
    console.log(`Found ${interactiveObjects.length} interactive objects`);
    interactiveObjects.slice(0, 20).forEach(o => {
        console.log(`  [${o.type}] "${o.label || ''}" at (${Math.round(o.x)}, ${Math.round(o.y)}) props=${JSON.stringify(o.customProperties || {})}`);
    });
    console.log('');

    // --- 5. Look for the galaxy map component's internal state ---
    console.log('--- 5. GALAXY MAP COMPONENT INTERNALS ---');
    const galMapEl = document.querySelector('galaxy-map-component');
    if (galMapEl) {
        console.log('  Tag:', galMapEl.tagName);
        console.log('  Properties:');
        const props = Object.getOwnPropertyNames(galMapEl).filter(k => !k.startsWith('__'));
        props.forEach(k => {
            const val = galMapEl[k];
            const type = typeof val;
            if (type === 'object' && val !== null) {
                if (Array.isArray(val)) {
                    console.log(`    ${k}: Array[${val.length}]`);
                    if (val.length > 0 && val.length < 5) {
                        console.log(`      Sample:`, val[0]);
                    }
                } else {
                    const keys = Object.keys(val).slice(0, 10);
                    console.log(`    ${k}: {${keys.join(', ')}}`);
                }
            } else if (type !== 'function') {
                console.log(`    ${k}: ${val}`);
            }
        });

        // Also check the prototype for interesting methods
        console.log('  Methods:');
        const proto = Object.getPrototypeOf(galMapEl);
        if (proto) {
            const methods = Object.getOwnPropertyNames(proto).filter(k => 
                typeof proto[k] === 'function' && !k.startsWith('_') && k !== 'constructor'
            );
            methods.forEach(m => console.log(`    ${m}()`));
        }
    }
    console.log('');

    // --- 6. Check the map-info-panel for data ---
    console.log('--- 6. MAP INFO PANEL ---');
    const infoPanel = document.getElementById('map-info-panel');
    if (infoPanel) {
        console.log('  Content:', infoPanel.textContent.trim().substring(0, 200));
        console.log('  Children:', infoPanel.children.length);
        [...infoPanel.children].forEach((child, i) => {
            console.log(`    [${i}] <${child.tagName.toLowerCase()}> id="${child.id}" class="${child.className}" text="${child.textContent.trim().substring(0, 60)}"`);
        });
    }
    console.log('');

    // --- 7. Check map-key for legend data ---
    console.log('--- 7. MAP KEY / LEGEND ---');
    const mapKey = document.getElementById('map-key');
    if (mapKey) {
        console.log('  Content:', mapKey.textContent.trim().substring(0, 300));
    }
    console.log('');

    // --- 8. Look for data in the Pixi app's custom properties ---
    console.log('--- 8. PIXI APP CUSTOM PROPERTIES ---');
    const appKeys = Object.keys(app).filter(k => !k.startsWith('_'));
    console.log('  App keys:', appKeys.join(', '));
    appKeys.forEach(k => {
        const val = app[k];
        if (typeof val === 'object' && val !== null && !val.constructor?.name?.startsWith('_')) {
            if (Array.isArray(val) && val.length > 0) {
                console.log(`  app.${k}: Array[${val.length}]`, val[0]);
            } else if (val.constructor?.name === 'Object') {
                console.log(`  app.${k}:`, Object.keys(val).slice(0, 10));
            }
        }
    });
    console.log('');

    // --- 9. Try to find system data by looking at stage children with many grandchildren ---
    console.log('--- 9. LARGE CONTAINERS (likely system layers) ---');
    function findLargeContainers(obj, path = 'stage') {
        if (!obj.children) return;
        obj.children.forEach((child, i) => {
            const childPath = `${path}.children[${i}]`;
            const count = child.children?.length || 0;
            if (count > 10) {
                const label = child.label || child.name || '';
                console.log(`  ${childPath}: [${child.constructor?.name}] "${label}" children=${count} visible=${child.visible} at (${Math.round(child.x)}, ${Math.round(child.y)})`);
                
                // Sample first few children of large containers
                if (count > 0) {
                    const sample = child.children[0];
                    const sampleInfo = getObjectInfo(sample);
                    console.log(`    First child: [${sampleInfo.type}] "${sampleInfo.label || ''}" at (${Math.round(sampleInfo.x)}, ${Math.round(sampleInfo.y)}) props=${JSON.stringify(sampleInfo.customProperties || {})}`);
                    if (sample.children?.length > 0) {
                        const grandchild = sample.children[0];
                        const gcInfo = getObjectInfo(grandchild);
                        console.log(`      First grandchild: [${gcInfo.type}] "${gcInfo.label || ''}" text="${gcInfo.text || ''}" at (${Math.round(gcInfo.x)}, ${Math.round(gcInfo.y)})`);
                    }
                }
            }
            if (child.children) findLargeContainers(child, childPath);
        });
    }
    findLargeContainers(app.stage);
    console.log('');

    // --- SUMMARY ---
    console.log('=== SUMMARY ===');
    console.log(`Total scene objects explored: ${results.allObjects.length}`);
    console.log(`Text objects: ${results.textObjects.length}`);
    console.log(`Interactive objects: ${interactiveObjects.length}`);
    console.log(`Containers: ${results.containers.length}`);
    console.log('');
    console.log('Results stored in window.__galaxyMapData');
    console.log('');
    console.log('TIP: To explore interactively:');
    console.log('  window.galaxy_map_pixi_app.stage.children  — top-level layers');
    console.log('  window.galaxy_map_pixi_app.stage.children[N].children  — objects in layer N');
    window.__galaxyMapData = results;

})();
