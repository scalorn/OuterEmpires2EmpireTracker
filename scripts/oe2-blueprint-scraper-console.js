/**
 * OE2 Blueprint Crate Scraper — Console Version
 * 
 * Usage:
 *   1. Open a crate in the game so blueprints are visible
 *   2. Open browser console (F12 → Console)
 *   3. Paste this entire script and press Enter
 *   4. Wait for it to finish (progress shown in console)
 *   5. A JSON file will be downloaded automatically
 *   6. Import the JSON file into OE2EmpireTracker
 *
 * Configuration: Change maxItems below to control how many to process.
 *   Set to Infinity to process all items in the crate.
 */
(async function() {
    'use strict';

    // === CONFIGURATION ===
    const maxItems = Infinity;       // How many items to process (Infinity = all)
    const afterClickInfo = 2000;    // ms after clicking info button
    const afterClickTab = 250;     // ms after clicking resources tab
    const afterClose = 250;        // ms after closing popup
    const pollTimeout = 1000;      // ms max wait for content to load

    // === UTILITIES ===
    const sleep = ms => new Promise(r => setTimeout(r, ms));
    const waitFor = (fn, timeout = pollTimeout) => new Promise(resolve => {
        const start = Date.now();
        const check = () => {
            if (fn()) return resolve(true);
            if (Date.now() - start > timeout) return resolve(false);
            setTimeout(check, 200);
        };
        check();
    });

    function getCleanText(node) {
        let txt = '';
        for (const c of node.childNodes) {
            if (c.nodeType === Node.TEXT_NODE) {
                const t = c.textContent.trim();
                if (t && !t.startsWith('?lit$')) txt += t + ' ';
            }
        }
        return txt.trim();
    }

    function findPanel() {
        const statsEl = document.querySelector('[id^="BlueprintStats_"]');
        if (!statsEl) return null;
        let panel = statsEl;
        for (let i = 0; i < 6 && panel; i++) {
            panel = panel.parentElement;
            if (panel?.classList?.contains('SmallSlideOut_Right_SectionContent')) return panel;
        }
        return statsEl.parentElement?.parentElement;
    }

    function closePopup() {
        const panel = findPanel();
        if (!panel) return false;
        for (let el = panel; el; el = el.parentElement) {
            for (const div of el.querySelectorAll('div')) {
                if (div.textContent.trim() === 'Close' && div.children.length === 0) {
                    div.click();
                    return true;
                }
            }
        }
        return false;
    }

    // === FIND BLUEPRINT ITEMS ===
    function getBlueprintItems() {
        // Scope to the open crate window — not the ship hold or station hold
        const crateWindow = document.querySelector('.CrateContentsWindow');
        if (!crateWindow) {
            console.error('[Scraper] No CrateContentsWindow found. Make sure a crate is open.');
            return [];
        }
        const allItems = crateWindow.querySelectorAll('.CargoItem');
        return [...allItems].filter(el => {
            if (!el.querySelector('.ui_icon_information')) return false;
            const types = el.querySelectorAll('.CargoType');
            const inner = types[types.length - 1];
            if (!inner) return false;
            return getCleanText(inner).length > 1;
        });
    }

    // === SCRAPE ONE BLUEPRINT ===
    async function scrapeBlueprint(item, index, total) {
        const types = item.querySelectorAll('.CargoType');
        const inner = types[types.length - 1];
        const listName = getCleanText(inner);
        const extraId = item.querySelector('.CargoExtraProperties')?.id
            ?.replace('CargoExtraPropertiesShip', '') || null;

        console.log(`[Scraper] [${index + 1}/${total}] Opening: "${listName}"`);

        item.querySelector('.ui_icon_information').click();
        await sleep(afterClickInfo);

        const loaded = await waitFor(() => !!document.querySelector('[id^="BlueprintStats_"]'));
        if (!loaded) { console.warn(`[Scraper] [${index + 1}] Panel did not load — skipping`); return null; }

        const statsEl = document.querySelector('[id^="BlueprintStats_"]');
        const gameId = statsEl.id.replace('BlueprintStats_', '');
        const panel = findPanel();
        if (!panel) { console.warn(`[Scraper] [${index + 1}] Panel not found — skipping`); return null; }

        // Title, evolution, tech level
        const titleDiv = panel.querySelector('.SmallSlideOut_Form_Row_Text_Bold');
        let name = '', evo = 0, tech = '';
        if (titleDiv) {
            const evoDiv = titleDiv.querySelector('.EvolutionNumber');
            if (evoDiv) { const n = parseInt(evoDiv.textContent.trim(), 10); if (!isNaN(n)) evo = n; }
            name = getCleanText(titleDiv);
            const m = name.match(/^(.*)\((.+)\)$/);
            if (m) { name = m[1].trim(); tech = m[2].trim(); }
        }

        const desc = panel.querySelector('.SmallSlideOut_Form_Row_Description')?.textContent?.trim() || '';
        const iconEl = panel.querySelector('.ui_icon_base');
        const iconClass = iconEl ? [...iconEl.classList].find(c => c.startsWith('ui_icon_') && c !== 'ui_icon_base') : null;
        // Extract sprite position from computed style — matches BlueprintType.IconPosition in baseline data
        let iconPosition = null;
        if (iconEl) {
            const computed = getComputedStyle(iconEl);
            const bgPos = computed.backgroundPosition;
            if (bgPos && bgPos !== '0px 0px' && bgPos !== '0% 0%') {
                iconPosition = bgPos;
            }
        }

        // Statistics
        const stats = {};
        for (const row of panel.querySelectorAll('.ShipComponentProperty')) {
            const k = row.querySelector('.CargoInfoDialogue')?.textContent?.trim();
            let v = row.querySelector('.ui_text_blue_light')?.textContent?.trim();
            if (k && v) { v = v.replace(/\s+/g, ' ').replace(/\(.*?\)/g, '').trim(); stats[k] = v; }
        }

        // Resources tab
        let resources = {};
        const recipeTab = document.querySelector('[id^="BlueprintRecipeTab_"]');
        if (recipeTab) {
            recipeTab.click();
            await sleep(afterClickTab);
            await waitFor(() => document.querySelectorAll('.ScanDetailOutputResourceName').length > 0);
            const rNames = document.querySelectorAll('.ScanDetailOutputResourceName');
            const rDetails = document.querySelectorAll('.ScanDetailOutputResourceDetail');
            for (let j = 0; j < Math.min(rNames.length, rDetails.length); j++) {
                const n = rNames[j].textContent.trim();
                const q = rDetails[j].textContent.trim().replace(/[^\d]/g, '') || rDetails[j].textContent.trim();
                if (n) resources[n] = q;
            }
        }

        // Close
        closePopup();
        await sleep(afterClose);
        await waitFor(() => !document.querySelector('[id^="BlueprintStats_"]'));

        console.log(`[Scraper] [${index + 1}/${total}] Done: "${name}" evo=${evo} tech="${tech}" icon=${iconPosition} stats=${Object.keys(stats).length} res=${Object.keys(resources).length}`);

        return { gameId, extraId, name, techLevel: tech, evolution: evo, description: desc, iconClass, iconPosition, properties: stats, resources };
    }

    // === MAIN ===
    const items = getBlueprintItems();
    const total = Math.min(maxItems, items.length);
    console.log(`[Scraper] Found ${items.length} blueprint items, will process ${total}`);

    if (items.length === 0) {
        console.error('[Scraper] No blueprint items found. Make sure a crate is open.');
        return;
    }

    const startTime = Date.now();
    const results = [];

    for (let i = 0; i < total; i++) {
        try {
            const bp = await scrapeBlueprint(items[i], i, total);
            if (bp) results.push(bp);
        } catch (err) {
            console.error(`[Scraper] [${i + 1}] ERROR:`, err);
        }
    }

    const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);
    console.log(`[Scraper] Complete: ${results.length}/${total} blueprints in ${elapsed}s`);

    // Download as JSON file
    const json = JSON.stringify(results, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `blueprints_${new Date().toISOString().slice(0, 19).replace(/[:.]/g, '-')}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    console.log(`[Scraper] JSON file downloaded (${results.length} blueprints, ${json.length} bytes)`);
    console.log('[Scraper] Results also available in console above');
})();
