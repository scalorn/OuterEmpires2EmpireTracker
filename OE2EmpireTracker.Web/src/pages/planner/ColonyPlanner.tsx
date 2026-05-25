import { useState } from 'react';
import { usePlannerStore, mapToWireFormat } from './plannerStore';
import { FlatpackDropdown } from './FlatpackDropdown';
import { StructureList } from './StructureList';
import { StatusDisplay } from './StatusDisplay';
import { StructureSummary } from './StructureSummary';
import { publicApi } from '../../api/endpoints/public';
import { extractBlueprintProperties } from '../../utils/blueprintHelpers';
import { useColonyPlannerBuildOrder } from '../../api/hooks/useColonyPlanner';

export function ColonyPlanner() {
  const structures = usePlannerStore((s) => s.structures);
  const blueprintCache = usePlannerStore((s) => s.blueprintCache);
  const addStructure = usePlannerStore((s) => s.addStructure);
  const cacheBlueprint = usePlannerStore((s) => s.cacheBlueprint);
  const clearPlan = usePlannerStore((s) => s.clearPlan);
  const applyOptimizedOrder = usePlannerStore((s) => s.applyOptimizedOrder);

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
        // Identify inserted structures (UUIDs not in local plan)
        const localUUIDs = new Set(structures.map((s) => s.blueprintUUID));
        const insertedEntries = result.optimizedOrder.filter(
          (entry) => !localUUIDs.has(entry.flatpackBlueprintUUID)
        );

        // For each inserted structure, fetch blueprint details and add to plan
        for (const entry of insertedEntries) {
          const uuid = entry.flatpackBlueprintUUID;

          // Find the corresponding step for name and type info
          const stepIndex = result.optimizedOrder.indexOf(entry);
          const step = result.steps[stepIndex];
          const name = step?.structureName ?? 'Support Structure';
          const blueprintType = step?.blueprintType ?? '';
          const subType = blueprintType.toLowerCase().startsWith('flatpacks/')
            ? blueprintType.slice('Flatpacks/'.length)
            : blueprintType;

          // Fetch properties (use cache if available)
          let properties = blueprintCache[uuid];
          if (!properties) {
            try {
              const detail = await publicApi.getBlueprintDetail(uuid);
              properties = extractBlueprintProperties(detail);
              cacheBlueprint(uuid, properties);
            } catch {
              // If we can't fetch details, use zeroed properties
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

          addStructure({
            id: crypto.randomUUID(),
            blueprintUUID: uuid,
            name,
            subType,
            state: 'Staged',
            buildQueuePosition: entry.buildQueueSequence,
            properties,
          });
        }

        // Now apply the full reordering (updates positions for all structures)
        applyOptimizedOrder(result.optimizedOrder);
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
