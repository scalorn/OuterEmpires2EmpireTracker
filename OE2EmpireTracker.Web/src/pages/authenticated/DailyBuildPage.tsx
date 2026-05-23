import { useState, useMemo, useCallback } from 'react';
import { useColonies, useColonyDetail } from '../../api/hooks/useColonies';
import { useColonyPlannerBuildOrder } from '../../api/hooks/useColonyPlanner';
import { useAuthStore } from '../../auth/store';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import type { Colony } from '../../api/types/domain';
import type { BuildOrderResult, BuildOrderStep, PlannerStructure } from '../../api/types/generated';

/**
 * DailyBuildPage — shows the optimized daily build order for a selected colony.
 *
 * Structure:
 * 1. Colony selector: FilteredDropdown populated from useColonies
 * 2. When a colony is selected, calls the colony-planner/build-order endpoint
 *    with the colony's structures
 * 3. Displays the result: ordered build steps with sequence number, structure name,
 *    blueprint type, resource requirements, and time estimate
 * 4. Shows nothing until a colony is selected (Req 11.5)
 *
 * Validates: Requirements 11.4, 11.5
 */
export function DailyBuildPage() {
  const { characterUUID } = useAuthStore();
  const { data: coloniesData, isLoading, isError, refetch } = useColonies(characterUUID);
  const [selectedColonyUUID, setSelectedColonyUUID] = useState('');

  const colonies = useMemo(
    () => (Array.isArray(coloniesData) ? coloniesData : []) as Colony[],
    [coloniesData],
  );

  const colonyOptions = useMemo(
    () =>
      colonies.map((c) => ({
        value: c.uuid,
        label: `${c.colonyName} (${c.planetName}, ${c.systemName})`,
      })),
    [colonies],
  );

  const { data: colonyDetail } = useColonyDetail(characterUUID, selectedColonyUUID || null);

  const buildOrderMutation = useColonyPlannerBuildOrder();

  const handleColonyChange = useCallback(
    (colonyUUID: string) => {
      setSelectedColonyUUID(colonyUUID);
      // Find the colony to get its structures for the planner request
      const colony = colonies.find((c) => c.uuid === colonyUUID);
      if (!colony) return;

      const structures: PlannerStructure[] = (colony.structures ?? []).map((s) => ({
        flatpackBlueprintUUID: s.flatpackBlueprintUUID,
        isBuilt: s.status === 'built' || s.status === 'online',
        isStaged: s.status === 'staged',
        isOnline: s.status === 'online',
        buildQueueSequence: s.buildQueueSequence,
        assignedWorkers: s.assignedWorkers,
      }));

      buildOrderMutation.mutate({ structures });
    },
    [colonies, buildOrderMutation],
  );

  if (isLoading) {
    return <LoadingSpinner message="Loading colonies..." />;
  }

  if (isError) {
    return (
      <RetryableError
        message="Failed to load colonies."
        onRetry={() => void refetch()}
      />
    );
  }

  const buildResult: BuildOrderResult | undefined = buildOrderMutation.data;

  return (
    <div className="mx-auto max-w-5xl p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-white">Daily Build</h1>
        <p className="mt-1 text-sm text-gray-400">
          Select a colony to compute the optimized daily build order.
        </p>
      </div>

      {/* Colony Selector */}
      <div className="mb-6 max-w-md">
        <label className="mb-1 block text-sm font-medium text-gray-300">Colony</label>
        <FilteredDropdown
          options={colonyOptions}
          value={selectedColonyUUID}
          onChange={handleColonyChange}
          placeholder="Select a colony..."
        />
      </div>

      {/* Results: only display after colony selection (Req 11.5) */}
      {selectedColonyUUID && (
        <div>
          {buildOrderMutation.isPending && (
            <LoadingSpinner message="Computing build order..." />
          )}

          {buildOrderMutation.isError && (
            <RetryableError
              message="Failed to compute build order."
              onRetry={() => handleColonyChange(selectedColonyUUID)}
            />
          )}

          {buildResult && (
            <div className="space-y-4">
              {/* Total time estimate */}
              <div className="rounded-lg border border-gray-700 bg-gray-800 p-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium text-gray-300">
                    Total Estimated Time
                  </span>
                  <span className="text-lg font-semibold text-blue-400">
                    {buildResult.totalTimeEstimate}
                  </span>
                </div>
                <p className="mt-1 text-xs text-gray-500">
                  {buildResult.steps.length} build step
                  {buildResult.steps.length !== 1 ? 's' : ''}
                </p>
              </div>

              {/* Build steps table */}
              {buildResult.steps.length > 0 ? (
                <div className="overflow-hidden rounded-lg border border-gray-700">
                  <table className="w-full text-left text-sm">
                    <thead className="bg-gray-800 text-xs uppercase text-gray-400">
                      <tr>
                        <th className="px-4 py-3">#</th>
                        <th className="px-4 py-3">Structure</th>
                        <th className="px-4 py-3">Blueprint Type</th>
                        <th className="px-4 py-3">Resources Required</th>
                        <th className="px-4 py-3">Time Estimate</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-700">
                      {buildResult.steps.map((step: BuildOrderStep) => (
                        <tr key={step.sequence} className="bg-gray-800/50 hover:bg-gray-750">
                          <td className="px-4 py-3 font-mono text-gray-400">
                            {step.sequence}
                          </td>
                          <td className="px-4 py-3 text-gray-200">
                            {step.structureName}
                          </td>
                          <td className="px-4 py-3 text-gray-300">
                            {step.blueprintType}
                          </td>
                          <td className="px-4 py-3">
                            {step.resourcesRequired.length === 0 ? (
                              <span className="text-gray-500">None</span>
                            ) : (
                              <ul className="space-y-0.5">
                                {step.resourcesRequired.map((r) => (
                                  <li
                                    key={r.resourceName}
                                    className="text-xs text-gray-300"
                                  >
                                    {r.resourceName}:{' '}
                                    <span className="text-gray-100">
                                      {r.quantity.toLocaleString()}
                                    </span>
                                  </li>
                                ))}
                              </ul>
                            )}
                          </td>
                          <td className="px-4 py-3 text-gray-300">
                            {step.timeEstimate}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="rounded-lg border border-gray-700 bg-gray-800 p-6 text-center">
                  <p className="text-sm text-gray-400">
                    No build steps required for this colony.
                  </p>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
