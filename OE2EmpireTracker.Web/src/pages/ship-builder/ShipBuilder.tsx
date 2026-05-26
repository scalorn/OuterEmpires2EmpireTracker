import { useEffect } from 'react';
import { useShipBuilderStore } from './shipBuilderStore';
import { encodeBuild } from './urlCodec';
import { HullSelector } from './HullSelector';
import { SlotGrid } from './SlotGrid';
import { ShipStatsDisplay } from './ShipStatsDisplay';

/**
 * ShipBuilder — main page component for the public ship template builder.
 * Composes HullSelector, SlotGrid, ShipStatsDisplay, and a Clear Build toolbar.
 * Responsive layout: two-column at lg breakpoint, stacked below.
 * Syncs build state to/from the URL hash fragment.
 *
 * Requirements: 1.1, 1.3, 1.4, 7.4, 11.1, 11.2, 11.3, 12.1, 12.2, 12.3
 */
export default function ShipBuilder() {
  const loadBlueprintList = useShipBuilderStore((s) => s.loadBlueprintList);
  const restoreFromURL = useShipBuilderStore((s) => s.restoreFromURL);
  const clearBuild = useShipBuilderStore((s) => s.clearBuild);
  const selectedHullUUID = useShipBuilderStore((s) => s.selectedHullUUID);
  const slots = useShipBuilderStore((s) => s.slots);

  // Load blueprint list on mount and restore from URL if present
  useEffect(() => {
    const init = async () => {
      await loadBlueprintList();
      const hash = window.location.hash;
      if (hash) {
        await restoreFromURL(hash);
      }
    };
    init();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Sync build state to URL hash when hull or slots change
  useEffect(() => {
    if (selectedHullUUID && slots.length > 0) {
      const hash = encodeBuild(selectedHullUUID, slots);
      window.history.replaceState(null, '', hash);
    } else {
      window.history.replaceState(null, '', window.location.pathname);
    }
  }, [selectedHullUUID, slots]);

  const handleClearBuild = () => {
    clearBuild();
    window.history.replaceState(null, '', window.location.pathname);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-100">Ship Builder</h1>
        {selectedHullUUID && (
          <button
            type="button"
            onClick={handleClearBuild}
            className="rounded bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-700 transition-colors"
          >
            Clear Build
          </button>
        )}
      </div>

      <HullSelector />

      {!selectedHullUUID && (
        <p className="text-sm text-gray-400">Select a hull to begin</p>
      )}

      {selectedHullUUID && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="space-y-4">
            <SlotGrid />
          </div>
          <div>
            <ShipStatsDisplay />
          </div>
        </div>
      )}
    </div>
  );
}
