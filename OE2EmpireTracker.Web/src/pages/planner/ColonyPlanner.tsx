import { useEffect, useRef, useCallback } from 'react';
import { usePlannerStore } from './plannerStore';
import { StructurePanel } from './StructurePanel';
import { StatusDisplay } from './StatusDisplay';
import { BuildOrderView } from './BuildOrderView';
import { useColonyPlannerStatus, useColonyPlannerBuildOrder } from '../../api/hooks/useColonyPlanner';
import { useAuthStore } from '../../auth/store';
import { useColonies } from '../../api/hooks/useColonies';

export function ColonyPlanner() {
  const { structures, setStatus, setBuildOrder, setIsComputing, setStructures } = usePlannerStore();
  const { isAuthenticated, characterUUID } = useAuthStore();
  const statusMutation = useColonyPlannerStatus();
  const buildOrderMutation = useColonyPlannerBuildOrder();
  const { data: coloniesData } = useColonies(isAuthenticated ? characterUUID : null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const computeStatus = useCallback(() => {
    if (structures.length === 0) {
      setStatus(null);
      return;
    }
    setIsComputing(true);
    statusMutation.mutate(
      { structures },
      {
        onSuccess: (data) => {
          setStatus(data);
          setIsComputing(false);
        },
        onError: () => setIsComputing(false),
      }
    );
  }, [structures, statusMutation, setStatus, setIsComputing]);

  // Debounced status computation on structure changes
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(computeStatus, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [structures, computeStatus]);

  const handleOptimizeBuildOrder = () => {
    if (structures.length === 0) return;
    buildOrderMutation.mutate(
      { structures },
      { onSuccess: (data) => setBuildOrder(data) }
    );
  };

  const handleLoadColony = () => {
    if (!coloniesData || !Array.isArray(coloniesData)) return;
    const colonies = coloniesData as Record<string, unknown>[];
    if (colonies.length === 0) return;
    const colony = colonies[0];
    const colStructures = colony.Structures ?? colony.structures;
    if (Array.isArray(colStructures)) {
      setStructures(colStructures as typeof structures);
    }
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Colony Planner</h1>
        <div className="flex gap-2">
          <button
            onClick={handleOptimizeBuildOrder}
            disabled={structures.length === 0}
            className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            Optimize Build Order
          </button>
          {isAuthenticated && (
            <>
              <button
                onClick={handleLoadColony}
                className="rounded bg-gray-600 px-4 py-2 text-sm text-white hover:bg-gray-500"
              >
                Load Colony
              </button>
              <button
                onClick={() => {
                  // Save plan via API — placeholder for Task 15.4
                }}
                className="rounded bg-green-600 px-4 py-2 text-sm text-white hover:bg-green-700"
              >
                Save Plan
              </button>
            </>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-1">
          <StructurePanel />
        </div>
        <div className="lg:col-span-2 space-y-6">
          <StatusDisplay />
          <BuildOrderView />
        </div>
      </div>
    </div>
  );
}
