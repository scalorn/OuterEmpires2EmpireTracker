import { useState } from 'react';
import { usePlannerStore } from './plannerStore';
import { FlatpackDropdown } from './FlatpackDropdown';
import { StructureList } from './StructureList';
import { StatusDisplay } from './StatusDisplay';
import { StructureSummary } from './StructureSummary';
import { publicApi } from '../../api/endpoints/public';
import { extractBlueprintProperties } from '../../utils/blueprintHelpers';

export function ColonyPlanner() {
  const structures = usePlannerStore((s) => s.structures);
  const blueprintCache = usePlannerStore((s) => s.blueprintCache);
  const addStructure = usePlannerStore((s) => s.addStructure);
  const cacheBlueprint = usePlannerStore((s) => s.cacheBlueprint);
  const clearPlan = usePlannerStore((s) => s.clearPlan);

  const [error, setError] = useState<string | null>(null);
  const [isAdding, setIsAdding] = useState(false);

  const handleAdd = async (uuid: string, name: string, subType: string) => {
    setIsAdding(true);
    setError(null);

    try {
      let properties = blueprintCache[uuid];

      if (!properties) {
        const detail = await publicApi.getBlueprintDetail(uuid);
        properties = extractBlueprintProperties(detail);
        cacheBlueprint(uuid, properties);
      }

      addStructure({
        id: crypto.randomUUID(),
        blueprintUUID: uuid,
        name,
        subType,
        state: 'Staged',
        buildQueuePosition: structures.length + 1,
        properties,
      });
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to load blueprint details.';
      setError(message);
    } finally {
      setIsAdding(false);
    }
  };

  const handleClear = () => {
    if (structures.length > 0) {
      const confirmed = window.confirm(
        'Are you sure you want to clear all structures from your plan?'
      );
      if (!confirmed) return;
    }
    clearPlan();
    setError(null);
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Colony Planner</h1>
        <button
          onClick={handleClear}
          className="rounded bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700"
        >
          Clear Plan
        </button>
      </div>

      {error && (
        <div className="mb-4 rounded border border-red-700 bg-red-900/30 p-3">
          <p className="text-sm text-red-300">{error}</p>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="space-y-6">
          <FlatpackDropdown onAdd={handleAdd} disabled={isAdding} />
          <StructureSummary />
        </div>
        <div className="space-y-6">
          <StructureList />
          <StatusDisplay />
        </div>
      </div>
    </div>
  );
}
