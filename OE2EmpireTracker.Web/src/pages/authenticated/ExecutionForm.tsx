import { useState, useMemo, useCallback } from 'react';
import { useDeliveryRoutes } from '../../api/hooks/useDeliveryRoutes';
import { usePlans, usePlanMutations } from '../../api/hooks/useDeliveryPlans';
import { useColonyMutations } from '../../api/hooks/useColonies';
import { useAuthStore } from '../../auth/store';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import type { DeliveryRoute, DeliveryPlan, DeliveryItem, RouteStop, StopItemSet } from '../../api/types/domain';

interface LoadListItem {
  name: string;
  itemType: string;
  purity?: string;
  totalQuantity: number;
  volume: number;
}

/**
 * ExecutionForm — delivery plan execution page.
 *
 * Top section: route and plan selection via FilteredDropdowns.
 * Below: consolidated load list showing all items to load across all stops
 * with columns: item name, type, purity, total quantity, volume.
 *
 * Stop-by-stop execution section: each stop rendered as a section with
 * checkable drop-off and pick-up items. Checking items triggers API calls
 * to mark commodity requests fulfilled or structures staged.
 *
 * Complete Stop button appears when all items at a stop are checked.
 * Visual completion (green badge, grayed-out section) only after clicking
 * Complete Stop. When all stops are completed, a Complete Plan button
 * marks the plan as completed via the API.
 *
 * Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8
 */
export function ExecutionForm() {
  const { characterUUID } = useAuthStore();
  const { data: routesData, isLoading: routesLoading } = useDeliveryRoutes(characterUUID);
  const { data: plansData, isLoading: plansLoading } = usePlans(characterUUID);
  const { updateCommodityRequest, addStructure } = useColonyMutations();
  const { save: savePlan } = usePlanMutations();

  const [selectedRouteId, setSelectedRouteId] = useState<string>('');
  const [selectedPlanId, setSelectedPlanId] = useState<string>('');
  const [completedStops, setCompletedStops] = useState<Set<string>>(new Set());

  const routes = useMemo(
    () => (Array.isArray(routesData) ? routesData : []) as DeliveryRoute[],
    [routesData],
  );

  const plans = useMemo(
    () => (Array.isArray(plansData) ? plansData : []) as DeliveryPlan[],
    [plansData],
  );

  // Route dropdown options
  const routeOptions = useMemo(
    () => routes.map((r) => ({ value: r.uuid, label: r.name })),
    [routes],
  );

  // Plan dropdown options filtered by selected route and excluding completed plans
  const planOptions = useMemo(() => {
    if (!selectedRouteId) return [];
    return plans
      .filter((p) => p.routeUUID === selectedRouteId && !p.isCompleted)
      .map((p) => ({ value: p.uuid, label: p.name }));
  }, [plans, selectedRouteId]);

  // Get the selected plan object
  const selectedPlan = useMemo(
    () => plans.find((p) => p.uuid === selectedPlanId) ?? null,
    [plans, selectedPlanId],
  );

  // Get the selected route object for stop information
  const selectedRoute = useMemo(
    () => routes.find((r) => r.uuid === selectedRouteId) ?? null,
    [routes, selectedRouteId],
  );

  // Stops ordered by sequence for the selected route
  const orderedStops = useMemo((): RouteStop[] => {
    if (!selectedRoute) return [];
    return [...selectedRoute.stops].sort((a, b) => a.sequence - b.sequence);
  }, [selectedRoute]);

  // Build consolidated load list from all stops in the selected plan
  const loadList = useMemo((): LoadListItem[] => {
    if (!selectedPlan || !selectedPlan.stopItems) return [];

    const aggregated = new Map<string, LoadListItem>();

    for (const stopItems of Object.values(selectedPlan.stopItems)) {
      // Aggregate drop-off items (items to load before departure)
      for (const item of stopItems.dropOff) {
        aggregateItem(aggregated, item);
      }
    }

    return Array.from(aggregated.values());
  }, [selectedPlan]);

  // Total quantity and volume across all load list items
  const totalQuantity = useMemo(
    () => loadList.reduce((sum, item) => sum + item.totalQuantity, 0),
    [loadList],
  );

  const totalVolume = useMemo(
    () => loadList.reduce((sum, item) => sum + item.volume, 0),
    [loadList],
  );

  // Determine which stops have items (used for plan completion check)
  const stopsWithItems = useMemo((): string[] => {
    if (!selectedPlan || !orderedStops.length) return [];
    return orderedStops
      .filter((stop) => {
        const stopItems = selectedPlan.stopItems[stop.uuid];
        return stopItems && (stopItems.dropOff.length > 0 || stopItems.pickUp.length > 0);
      })
      .map((stop) => stop.uuid);
  }, [selectedPlan, orderedStops]);

  // Check if all stops with items are completed
  const allStopsCompleted = useMemo(() => {
    if (stopsWithItems.length === 0) return false;
    return stopsWithItems.every((uuid) => completedStops.has(uuid));
  }, [stopsWithItems, completedStops]);

  const handleRouteChange = (value: string) => {
    setSelectedRouteId(value);
    setSelectedPlanId('');
    setCompletedStops(new Set());
  };

  const handlePlanChange = (value: string) => {
    setSelectedPlanId(value);
    setCompletedStops(new Set());
  };

  const handleCompleteStop = useCallback((stopUUID: string) => {
    setCompletedStops((prev) => {
      const next = new Set(prev);
      next.add(stopUUID);
      return next;
    });
  }, []);

  const handleCompletePlan = useCallback(() => {
    if (!selectedPlanId) return;
    savePlan.mutate({
      entityUUID: selectedPlanId,
      data: { isCompleted: true },
    });
  }, [selectedPlanId, savePlan]);

  /**
   * Handles checking a drop-off item.
   * - Commodity items: marks the colony commodity request as fulfilled.
   * - Flatpack items: adds the structure to the colony as staged.
   */
  const handleDropOffCheck = (stop: RouteStop, item: DeliveryItem) => {
    if (item.isChecked) return; // Already checked, no-op

    if (item.itemType === 'Commodity') {
      updateCommodityRequest.mutate({
        colonyUUID: stop.colonyUUID,
        commodityName: item.name,
        dto: { isFulfilled: true },
      });
    } else if (item.itemType === 'Flatpack') {
      addStructure.mutate({
        colonyUUID: stop.colonyUUID,
        flatpackBlueprintUUID: item.uuid,
      });
    }
  };

  if (routesLoading || plansLoading) {
    return <LoadingSpinner message="Loading delivery data..." />;
  }

  return (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-4 text-lg font-semibold text-white">Delivery Execution</h2>

      {/* Route and Plan Selection */}
      <div className="mb-6 grid grid-cols-1 gap-4 md:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm text-gray-300">Route</label>
          <FilteredDropdown
            options={routeOptions}
            value={selectedRouteId}
            onChange={handleRouteChange}
            placeholder="Select a route..."
          />
        </div>
        <div>
          <label className="mb-1 block text-sm text-gray-300">Plan</label>
          <FilteredDropdown
            options={planOptions}
            value={selectedPlanId}
            onChange={handlePlanChange}
            placeholder={selectedRouteId ? 'Select a plan...' : 'Select a route first'}
          />
        </div>
      </div>

      {/* Consolidated Load List */}
      {selectedPlan && (
        <div className="mb-6">
          <div className="mb-2 flex items-baseline gap-4">
            <h3 className="text-sm font-semibold text-white">Load Before Departure</h3>
            {loadList.length > 0 && (
              <span className="text-xs text-gray-400">
                {loadList.length} items, {totalQuantity} qty, {totalVolume.toLocaleString()} vol
              </span>
            )}
          </div>

          {loadList.length === 0 ? (
            <p className="text-sm text-gray-500">No items to load for this plan.</p>
          ) : (
            <div className="overflow-auto rounded border border-gray-700">
              <table className="w-full text-left text-sm">
                <thead className="sticky top-0 border-b border-gray-700 bg-gray-800">
                  <tr>
                    <th className="px-3 py-2 text-gray-300">Item Name</th>
                    <th className="px-3 py-2 text-gray-300">Type</th>
                    <th className="px-3 py-2 text-gray-300">Purity</th>
                    <th className="px-3 py-2 text-right text-gray-300">Quantity</th>
                    <th className="px-3 py-2 text-right text-gray-300">Volume</th>
                  </tr>
                </thead>
                <tbody>
                  {loadList.map((item) => (
                    <tr
                      key={`${item.name}-${item.itemType}-${item.purity ?? ''}`}
                      className="border-b border-gray-700/50"
                    >
                      <td className="px-3 py-2 text-white">{item.name}</td>
                      <td className="px-3 py-2 text-gray-300">{item.itemType}</td>
                      <td className="px-3 py-2 text-gray-300">{item.purity ?? '—'}</td>
                      <td className="px-3 py-2 text-right text-white">{item.totalQuantity}</td>
                      <td className="px-3 py-2 text-right text-gray-300">{item.volume.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="border-t border-gray-600 bg-gray-800">
                  <tr>
                    <td className="px-3 py-2 font-semibold text-white" colSpan={3}>
                      Total
                    </td>
                    <td className="px-3 py-2 text-right font-semibold text-white">
                      {totalQuantity}
                    </td>
                    <td className="px-3 py-2 text-right font-semibold text-white">
                      {totalVolume.toLocaleString()}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Stop-by-Stop Execution */}
      {selectedPlan && orderedStops.length > 0 && (
        <div className="min-h-0 flex-1 overflow-auto">
          <h3 className="mb-3 text-sm font-semibold text-white">Stop-by-Stop Execution</h3>
          <div className="space-y-4">
            {orderedStops.map((stop) => {
              const stopItems = selectedPlan.stopItems[stop.uuid];
              if (!stopItems) return null;
              const hasItems = stopItems.dropOff.length > 0 || stopItems.pickUp.length > 0;
              if (!hasItems) return null;

              return (
                <StopSection
                  key={stop.uuid}
                  stop={stop}
                  stopItems={stopItems}
                  isCompleted={completedStops.has(stop.uuid)}
                  onDropOffCheck={handleDropOffCheck}
                  onCompleteStop={handleCompleteStop}
                />
              );
            })}
          </div>

          {/* Complete Plan button — shown when all stops are completed */}
          {allStopsCompleted && (
            <div className="mt-6 flex justify-center">
              <button
                type="button"
                onClick={handleCompletePlan}
                disabled={savePlan.isPending}
                className="rounded bg-green-600 px-6 py-2 text-sm font-semibold text-white hover:bg-green-500 disabled:opacity-50"
              >
                {savePlan.isPending ? 'Completing...' : 'Complete Plan'}
              </button>
            </div>
          )}
        </div>
      )}

      {!selectedPlan && selectedRouteId && (
        <p className="text-sm text-gray-500">Select a plan to view the load list.</p>
      )}

      {!selectedRouteId && (
        <p className="text-sm text-gray-500">Select a route and plan to begin execution.</p>
      )}
    </div>
  );
}

/**
 * Aggregates a delivery item into the load list map.
 * Items with the same name + type + purity are combined.
 */
function aggregateItem(map: Map<string, LoadListItem>, item: DeliveryItem): void {
  const key = `${item.name}|${item.itemType}|${item.purity ?? ''}`;
  const existing = map.get(key);
  if (existing) {
    existing.totalQuantity += item.quantity;
    existing.volume += item.quantity; // Volume = quantity (1:1 default)
  } else {
    map.set(key, {
      name: item.name,
      itemType: item.itemType,
      purity: item.purity,
      totalQuantity: item.quantity,
      volume: item.quantity, // Volume = quantity (1:1 default)
    });
  }
}

// ─── Stop Section Component ───────────────────────────────────────────────────

interface StopSectionProps {
  stop: RouteStop;
  stopItems: StopItemSet;
  isCompleted: boolean;
  onDropOffCheck: (stop: RouteStop, item: DeliveryItem) => void;
  onCompleteStop: (stopUUID: string) => void;
}

/**
 * Renders a single stop section with checkable drop-off and pick-up items.
 * Items already checked (isChecked=true) render as checked and disabled.
 *
 * When all items are checked and the stop is not yet completed, a
 * "Complete Stop" button appears. Clicking it marks the stop as completed
 * and shows a green "✓ Completed" badge with grayed-out styling.
 */
function StopSection({ stop, stopItems, isCompleted, onDropOffCheck, onCompleteStop }: StopSectionProps) {
  // Check if all items in this stop are checked
  const allItemsChecked = useMemo(() => {
    const allItems = [...stopItems.dropOff, ...stopItems.pickUp];
    if (allItems.length === 0) return false;
    return allItems.every((item) => item.isChecked);
  }, [stopItems]);

  // Show Complete Stop button when all items checked but stop not yet completed
  const showCompleteButton = allItemsChecked && !isCompleted;

  return (
    <div
      className={`rounded border p-3 ${
        isCompleted
          ? 'border-green-700/50 bg-gray-800/30 opacity-75'
          : 'border-gray-700 bg-gray-800/50'
      }`}
    >
      <div className="mb-2 flex items-center justify-between">
        <h4 className="text-sm font-semibold text-white">
          Stop {stop.sequence}: {stop.colonyName}
          <span className="ml-2 text-xs font-normal text-gray-400">
            {stop.planetName}, {stop.systemName}
          </span>
        </h4>
        {isCompleted && (
          <span className="rounded bg-green-700/30 px-2 py-0.5 text-xs font-medium text-green-400">
            ✓ Completed
          </span>
        )}
      </div>

      {/* Drop-off items */}
      {stopItems.dropOff.length > 0 && (
        <div className="mb-2">
          <p className="mb-1 text-xs font-medium text-gray-400">Drop-off</p>
          <ul className="space-y-1">
            {stopItems.dropOff.map((item) => (
              <li key={item.uuid} className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={item.isChecked}
                  disabled={item.isChecked || isCompleted}
                  onChange={() => onDropOffCheck(stop, item)}
                  className="h-4 w-4 rounded border-gray-600 bg-gray-700 text-blue-500 focus:ring-blue-500"
                />
                <span className={`text-sm ${item.isChecked ? 'text-gray-500 line-through' : 'text-white'}`}>
                  {item.name}
                  {item.purity && <span className="text-gray-400"> ({item.purity})</span>}
                  <span className="ml-1 text-gray-400">×{item.quantity}</span>
                  <span className="ml-1 text-xs text-gray-500">[{item.itemType}]</span>
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Pick-up items */}
      {stopItems.pickUp.length > 0 && (
        <div>
          <p className="mb-1 text-xs font-medium text-gray-400">Pick-up</p>
          <ul className="space-y-1">
            {stopItems.pickUp.map((item) => (
              <li key={item.uuid} className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={item.isChecked}
                  disabled={item.isChecked || isCompleted}
                  className="h-4 w-4 rounded border-gray-600 bg-gray-700 text-blue-500 focus:ring-blue-500"
                />
                <span className={`text-sm ${item.isChecked ? 'text-gray-500 line-through' : 'text-white'}`}>
                  {item.name}
                  {item.purity && <span className="text-gray-400"> ({item.purity})</span>}
                  <span className="ml-1 text-gray-400">×{item.quantity}</span>
                  <span className="ml-1 text-xs text-gray-500">[{item.itemType}]</span>
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Complete Stop button */}
      {showCompleteButton && (
        <div className="mt-3 flex justify-end">
          <button
            type="button"
            onClick={() => onCompleteStop(stop.uuid)}
            className="rounded bg-blue-600 px-3 py-1 text-xs font-medium text-white hover:bg-blue-500"
          >
            Complete Stop
          </button>
        </div>
      )}
    </div>
  );
}
