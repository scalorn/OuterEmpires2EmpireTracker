import { useMemo } from 'react';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { useShipBuilderStore } from './shipBuilderStore';

/**
 * HullSelector — searchable dropdown for selecting a hull blueprint.
 * Filters the blueprint list to hulls only, displays hull name and class,
 * and handles loading/error states.
 *
 * Requirements: 2.1, 2.2, 8.1, 8.2, 8.3, 8.4, 8.5
 */
export function HullSelector() {
  const blueprintList = useShipBuilderStore((s) => s.blueprintList);
  const blueprintListLoading = useShipBuilderStore((s) => s.blueprintListLoading);
  const blueprintListError = useShipBuilderStore((s) => s.blueprintListError);
  const selectedHullUUID = useShipBuilderStore((s) => s.selectedHullUUID);
  const loadingUUIDs = useShipBuilderStore((s) => s.loadingUUIDs);
  const selectHull = useShipBuilderStore((s) => s.selectHull);
  const loadBlueprintList = useShipBuilderStore((s) => s.loadBlueprintList);

  const hullOptions = useMemo(() => {
    return blueprintList
      .filter((bp) => bp.bluePrintType === 'Hull')
      .map((bp) => ({
        value: bp.uuid,
        label: `${bp.name} (Class ${bp.class})`,
      }));
  }, [blueprintList]);

  const isHullLoading = selectedHullUUID !== null && loadingUUIDs.has(selectedHullUUID);

  // Loading state: show spinner while blueprint list is being fetched
  if (blueprintListLoading) {
    return <LoadingSpinner message="Loading hulls..." />;
  }

  // Error state: show error with retry button
  if (blueprintListError && blueprintList.length === 0) {
    return (
      <RetryableError
        message={blueprintListError}
        onRetry={loadBlueprintList}
      />
    );
  }

  // Empty state: no hulls available after successful load
  if (!blueprintListLoading && blueprintList.length > 0 && hullOptions.length === 0) {
    return (
      <p className="text-sm text-gray-400">No hulls available</p>
    );
  }

  // Normal state: show the searchable dropdown
  return (
    <div className="space-y-1">
      <label className="block text-xs text-gray-400">Hull</label>
      <FilteredDropdown
        options={hullOptions}
        value={selectedHullUUID ?? ''}
        onChange={(uuid) => {
          if (uuid) {
            selectHull(uuid);
          }
        }}
        placeholder="Search hulls..."
      />
      {isHullLoading && (
        <p className="text-xs text-gray-500">Loading hull details...</p>
      )}
    </div>
  );
}
