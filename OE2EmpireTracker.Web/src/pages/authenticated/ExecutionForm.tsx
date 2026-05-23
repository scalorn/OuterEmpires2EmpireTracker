import { useState, useMemo } from 'react';
import { useDeliveryRoutes } from '../../api/hooks/useDeliveryRoutes';
import { usePlans } from '../../api/hooks/useDeliveryPlans';
import { useAuthStore } from '../../auth/store';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import type { DeliveryRoute, DeliveryPlan, DeliveryItem } from '../../api/types/domain';

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
 * The stop-by-stop execution section is added in tasks 13.2 and 13.3.
 *
 * Validates: Requirements 7.1, 7.2
 */
export function ExecutionForm() {
  const { characterUUID } = useAuthStore();
  const { data: routesData, isLoading: routesLoading } = useDeliveryRoutes(characterUUID);
  const { data: plansData, isLoading: plansLoading } = usePlans(characterUUID);

  const [selectedRouteId, setSelectedRouteId] = useState<string>('');
  const [selectedPlanId, setSelectedPlanId] = useState<string>('');

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

  const handleRouteChange = (value: string) => {
    setSelectedRouteId(value);
    setSelectedPlanId('');
  };

  const handlePlanChange = (value: string) => {
    setSelectedPlanId(value);
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
        <div className="min-h-0 flex-1 overflow-hidden">
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
