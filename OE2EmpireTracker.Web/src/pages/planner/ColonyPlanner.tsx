import { useState } from 'react';
import { usePlannerStore, mapToWireFormat } from './plannerStore';
import { FlatpackDropdown } from './FlatpackDropdown';
import { StructureList } from './StructureList';
import { StatusDisplay } from './StatusDisplay';
import { StructureSummary } from './StructureSummary';
import { publicApi } from '../../api/endpoints/public';
import { extractBlueprintProperties } from '../../utils/blueprintHelpers';
import { useColonyPlannerBuildOrder } from '../../api/hooks/useColonyPlanner';
import type { PlannedStructure } from './computeColonyStatus';

export function ColonyPlanner() {
  const structures = usePlannerStore((s) => s.structures);
  const blueprintCache = usePlannerStore((s) => s.blueprintCache);
  const addStructure = usePlannerStore((s) => s.addStructure);
  const cacheBlueprint = usePlannerStore((s) => s.cacheBlueprint);
  const clearPlan = usePlannerStore((s) => s.clearPlan);
  const replaceStructures = usePlannerStore((s) => s.replaceStructures);

  const [error, setError] = useState<string | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [isOptimizing, setIsOptimizing] = useState(false);

  const buildOrderMutation = useColonyPlannerBuildOrder();

  const isOptimizeDisabled = structures.length <= 1 || isOptimizing;

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

  const handleOptimize = async () => {
    setIsOptimizing(true);
    setError(null);

    const wireStructures = mapToWireFormat(structures);

    try {
      const result = await buildOrderMutation.mutateAsync({
        structures: wireStructures,
      });
      if (result.optimizedOrder && result.optimizedOrder.length > 0) {
        // Build the complete structure list from the optimizer response.
        // The optimizedOrder array is positional — index = build sequence.
        // It may contain duplicate blueprint UUIDs (e.g. 3 reactors) and
        // structures not in the original plan (optimizer-inserted support).
        const localByUUID = new Map<string, PlannedStructure[]>();
        for (const s of structures) {
          const list = localByUUID.get(s.blueprintUUID) ?? [];
          list.push(s);
          localByUUID.set(s.blueprintUUID, list);
        }

        const newStructures: PlannedStructure[] = [];

        for (let i = 0; i < result.optimizedOrder.length; i++) {
          const entry = result.optimizedOrder[i];
          const uuid = entry.flatpackBlueprintUUID;

          // Try to reuse an existing local structure (preserves state, id)
          const localList = localByUUID.get(uuid);
          if (localList && localList.length > 0) {
            const existing = localList.shift()!;
            newStructures.push({
              ...existing,
              buildQueuePosition: i + 1,
            });
          } else {
            // Inserted by optimizer — create a new structure
            const step = result.steps[i];
            const name = step?.structureName ?? 'Support Structure';
            const blueprintType = step?.blueprintType ?? '';
            const subType = blueprintType.toLowerCase().startsWith('flatpacks/')
              ? blueprintType.slice('Flatpacks/'.length)
              : blueprintType;

            // Get properties (use cache or fetch)
            let properties = blueprintCache[uuid];
            if (!properties) {
              try {
                const detail = await publicApi.getBlueprintDetail(uuid);
                properties = extractBlueprintProperties(detail);
                cacheBlueprint(uuid, properties);
              } catch {
                properties = {
                  powerProvided: 0,
                  powerRequired: 0,
                  habitationProvision: 0,
                  foodProvision: 0,
                  entertainmentProvided: 0,
                  warehouseCapacity: 0,
                  workerSlots: 0,
                };
              }
            }

            newStructures.push({
              id: crypto.randomUUID(),
              blueprintUUID: uuid,
              name,
              subType,
              state: 'Staged',
              buildQueuePosition: i + 1,
              properties,
            });
          }
        }

        replaceStructures(newStructures);
      }
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to optimize build order. Please try again.';
      setError(message);
    } finally {
      setIsOptimizing(false);
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
        <div className="flex items-center gap-2">
          <button
            onClick={handleOptimize}
            disabled={isOptimizeDisabled}
            className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isOptimizing ? (
              <span className="flex items-center gap-2">
                <svg
                  className="h-4 w-4 animate-spin"
                  viewBox="0 0 24 24"
                  fill="none"
                  aria-hidden="true"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                  />
                </svg>
                Optimizing…
              </span>
            ) : (
              'Optimize Build Order'
            )}
          </button>
          <button
            onClick={handleClear}
            className="rounded bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700"
          >
            Clear Plan
          </button>
        </div>
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
