import { useState, useMemo } from 'react';
import { useColonies, useColonyDetail, useColonyMutations } from '../../api/hooks/useColonies';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { useVisibilityRecovery } from '../../hooks/useVisibilityRecovery';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { TabBar } from '../../components/common/TabBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { CountdownTimer } from '../../components/common/CountdownTimer';
import { queryKeys } from '../../api/hooks/queryKeys';
import { computeDaysSinceImport, getStalenessInfo } from '../../utils/stalenessUtils';
import type { Colony, ColonyStructure } from '../../api/types/domain';
import type { ColonyPlannerRequest, PlannerStructure } from '../../api/types/generated';

type SortKey = 'colonyName' | 'systemName' | 'planetName' | 'structures';
type SortDir = 'asc' | 'desc';

const COLONY_TABS = [
  { key: 'structures', label: 'Structures' },
  { key: 'warehousing', label: 'Warehousing' },
  { key: 'commodityRequests', label: 'Commodity Requests' },
  { key: 'administration', label: 'Administration' },
] as const;

const FILTER_DEFS: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Search colonies...' },
];

/**
 * ColonyForm — master-detail colony management page.
 *
 * Left panel: searchable, sortable colony list.
 * Right panel: colony details header + tabbed content (Structures, Warehousing,
 * Commodity Requests, Administration). Hidden when no colony is selected.
 *
 * Validates: Requirements 1.1, 1.2
 */
export function ColonyForm() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useColonies(characterUUID);

  // Recalculate timers from end timestamps when tab regains focus
  const visibilityKeys = useMemo(
    () => (characterUUID ? [queryKeys.colonies(characterUUID) as string[]] : []),
    [characterUUID],
  );
  useVisibilityRecovery(visibilityKeys);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<string>('structures');
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });
  const [sortKey, setSortKey] = useState<SortKey>('colonyName');
  const [sortDir, setSortDir] = useState<SortDir>('asc');

  const colonies = useMemo(() => (Array.isArray(data) ? data : []) as Colony[], [data]);

  const filteredColonies = useMemo(() => {
    const search = ((filterValues.search as string) ?? '').toLowerCase();
    let result = colonies;
    if (search) {
      result = result.filter(
        (c) =>
          c.colonyName.toLowerCase().includes(search) ||
          c.systemName.toLowerCase().includes(search) ||
          c.planetName.toLowerCase().includes(search),
      );
    }
    return [...result].sort((a, b) => {
      let aVal: string | number;
      let bVal: string | number;
      if (sortKey === 'structures') {
        aVal = a.structures?.length ?? 0;
        bVal = b.structures?.length ?? 0;
      } else {
        aVal = a[sortKey] ?? '';
        bVal = b[sortKey] ?? '';
      }
      const cmp = typeof aVal === 'number' && typeof bVal === 'number'
        ? aVal - bVal
        : String(aVal).localeCompare(String(bVal));
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [colonies, filterValues.search, sortKey, sortDir]);

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir('asc');
    }
  };

  const handleBack = () => setSelectedId(null);

  const sortIndicator = (key: SortKey) => {
    if (sortKey !== key) return '';
    return sortDir === 'asc' ? ' ▲' : ' ▼';
  };

  const listPanel = (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-3 text-lg font-semibold text-white">Colonies</h2>
      <FilterBar
        filters={FILTER_DEFS}
        values={filterValues}
        onChange={setFilterValues}
        onClear={() => setFilterValues({ search: '' })}
      />
      {isLoading && <LoadingSpinner message="Loading colonies..." />}
      {isError && (
        <RetryableError message="Failed to load colonies." onRetry={() => void refetch()} />
      )}
      {!isLoading && !isError && filteredColonies.length === 0 && (
        <EmptyState title="No colonies" message="No colonies match your search." />
      )}
      {!isLoading && !isError && filteredColonies.length > 0 && (
        <div className="min-h-0 flex-1 overflow-y-auto">
          <table className="w-full text-left text-sm">
            <thead className="sticky top-0 border-b border-gray-700 bg-gray-800">
              <tr>
                <th
                  className="cursor-pointer px-3 py-2 text-gray-300 hover:text-white"
                  onClick={() => handleSort('colonyName')}
                >
                  Name{sortIndicator('colonyName')}
                </th>
                <th
                  className="cursor-pointer px-3 py-2 text-gray-300 hover:text-white"
                  onClick={() => handleSort('systemName')}
                >
                  System{sortIndicator('systemName')}
                </th>
                <th
                  className="cursor-pointer px-3 py-2 text-gray-300 hover:text-white"
                  onClick={() => handleSort('planetName')}
                >
                  Planet{sortIndicator('planetName')}
                </th>
                <th
                  className="cursor-pointer px-3 py-2 text-gray-300 hover:text-white"
                  onClick={() => handleSort('structures')}
                >
                  Structures{sortIndicator('structures')}
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredColonies.map((colony) => (
                <tr
                  key={colony.uuid}
                  onClick={() => setSelectedId(colony.uuid)}
                  className={`cursor-pointer border-b border-gray-700/50 transition-colors hover:bg-gray-700/50 ${
                    selectedId === colony.uuid ? 'bg-gray-700' : ''
                  }`}
                >
                  <td className="px-3 py-2 text-white">{colony.colonyName}</td>
                  <td className="px-3 py-2 text-gray-300">{colony.systemName}</td>
                  <td className="px-3 py-2 text-gray-300">{colony.planetName}</td>
                  <td className="px-3 py-2 text-gray-300">
                    {colony.structures?.length ?? 0}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );

  const selectedColony = colonies.find((c) => c.uuid === selectedId) ?? null;

  const detailPanel = selectedColony ? (
    <ColonyDetailPanel
      colony={selectedColony}
      characterUUID={characterUUID}
      activeTab={activeTab}
      onTabChange={setActiveTab}
    />
  ) : (
    <div className="flex h-full items-center justify-center">
      <p className="text-sm text-gray-500">Select a colony to view details</p>
    </div>
  );

  return (
    <MasterDetailLayout
      listPanel={listPanel}
      detailPanel={detailPanel}
      selectedId={selectedId}
      onBack={handleBack}
    />
  );
}


// --- Detail Panel ---

interface ColonyDetailPanelProps {
  colony: Colony;
  characterUUID: string | null;
  activeTab: string;
  onTabChange: (tab: string) => void;
}

function ColonyDetailPanel({
  colony,
  characterUUID,
  activeTab,
  onTabChange,
}: ColonyDetailPanelProps) {
  const { data: detail, isLoading } = useColonyDetail(characterUUID, colony.uuid);
  const colonyData = (detail as Colony) ?? colony;

  return (
    <div className="flex h-full flex-col">
      {/* Colony details header */}
      <div className="border-b border-gray-700 p-4">
        <h2 className="text-xl font-semibold text-white">{colonyData.colonyName}</h2>
        <p className="mt-1 text-sm text-gray-400">
          {colonyData.planetName} &middot; {colonyData.systemName}
        </p>
      </div>

      {/* Tab bar */}
      <div className="px-4 pt-3">
        <TabBar
          tabs={COLONY_TABS as unknown as { key: string; label: string }[]}
          activeTab={activeTab}
          onTabChange={onTabChange}
        />
      </div>

      {/* Tab content */}
      <div className="min-h-0 flex-1 overflow-y-auto p-4" role="tabpanel" id={`tabpanel-${activeTab}`} aria-labelledby={`tab-${activeTab}`}>
        {isLoading ? (
          <LoadingSpinner message="Loading colony details..." />
        ) : (
          <TabContent tab={activeTab} colony={colonyData} />
        )}
      </div>
    </div>
  );
}

// --- Tab Content (placeholder for subsequent tasks) ---

function TabContent({ tab, colony }: { tab: string; colony: Colony }) {
  switch (tab) {
    case 'structures':
      return <StructuresTab colony={colony} colonyUUID={colony.uuid} />;
    case 'warehousing':
      return <WarehousingTab colony={colony} />;
    case 'commodityRequests':
      return <CommodityRequestsTab colony={colony} />;
    case 'administration':
      return <AdministrationTab colony={colony} />;
    default:
      return null;
  }
}

// --- Warehousing Tab ---

/**
 * WarehousingTab — displays warehouse inventory grouped by item type.
 *
 * Validates: Requirement 1.6
 */
function WarehousingTab({ colony }: { colony: Colony }) {
  const items = colony.items ?? [];

  const grouped = useMemo(() => {
    const groups: Record<string, typeof items> = {};
    for (const item of items) {
      const type = item.itemType || 'Unknown';
      if (!groups[type]) groups[type] = [];
      groups[type].push(item);
    }
    return Object.entries(groups).sort(([a], [b]) => a.localeCompare(b));
  }, [items]);

  if (items.length === 0) {
    return (
      <div>
        <h3 className="mb-2 text-sm font-medium text-gray-300">Warehouse Items (0)</h3>
        <p className="text-sm text-gray-500">No items in warehouse.</p>
      </div>
    );
  }

  return (
    <div>
      <h3 className="mb-3 text-sm font-medium text-gray-300">
        Warehouse Items ({items.length})
      </h3>
      <div className="space-y-4">
        {grouped.map(([itemType, groupItems]) => (
          <div key={itemType}>
            <h4 className="mb-1 text-xs font-semibold uppercase tracking-wide text-gray-400">
              {itemType}
            </h4>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-gray-700">
                  <th className="px-3 py-1.5 text-gray-400">Name</th>
                  <th className="px-3 py-1.5 text-gray-400">Purity</th>
                  <th className="px-3 py-1.5 text-right text-gray-400">Quantity</th>
                </tr>
              </thead>
              <tbody>
                {groupItems.map((item) => (
                  <tr key={item.uuid} className="border-b border-gray-700/50">
                    <td className="px-3 py-1.5 text-white">{item.name}</td>
                    <td className="px-3 py-1.5 text-gray-300">{item.purity ?? '—'}</td>
                    <td className="px-3 py-1.5 text-right text-gray-300">{item.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
      </div>
    </div>
  );
}

// --- Commodity Requests Tab ---

/**
 * CommodityRequestsTab — displays commodity requests with fulfillment status.
 *
 * Validates: Requirement 1.7
 */
function CommodityRequestsTab({ colony }: { colony: Colony }) {
  const requests = colony.commodityRequests ?? [];

  if (requests.length === 0) {
    return (
      <div>
        <h3 className="mb-2 text-sm font-medium text-gray-300">Commodity Requests (0)</h3>
        <p className="text-sm text-gray-500">No commodity requests.</p>
      </div>
    );
  }

  return (
    <div>
      <h3 className="mb-3 text-sm font-medium text-gray-300">
        Commodity Requests ({requests.length})
      </h3>
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-gray-700">
            <th className="px-3 py-1.5 text-gray-400">Commodity</th>
            <th className="px-3 py-1.5 text-right text-gray-400">Quantity</th>
            <th className="px-3 py-1.5 text-gray-400">Need-By Date</th>
            <th className="px-3 py-1.5 text-center text-gray-400">Fulfilled</th>
          </tr>
        </thead>
        <tbody>
          {requests.map((req, idx) => (
            <tr key={`${req.commodityName}-${idx}`} className="border-b border-gray-700/50">
              <td className="px-3 py-1.5 text-white">{req.commodityName}</td>
              <td className="px-3 py-1.5 text-right text-gray-300">{req.quantity}</td>
              <td className="px-3 py-1.5 text-gray-300">
                {req.needByDate ? new Date(req.needByDate).toLocaleDateString() : '—'}
              </td>
              <td className="px-3 py-1.5 text-center">
                {req.isFulfilled ? (
                  <span className="text-green-400" aria-label="Fulfilled">✓</span>
                ) : (
                  <span className="text-red-400" aria-label="Not fulfilled">✗</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// --- Structures Tab ---

const STATUS_COLORS: Record<ColonyStructure['status'], { bg: string; text: string; label: string }> = {
  staged: { bg: 'bg-gray-600', text: 'text-gray-200', label: 'Staged' },
  building: { bg: 'bg-yellow-600', text: 'text-yellow-100', label: 'Building' },
  built: { bg: 'bg-blue-600', text: 'text-blue-100', label: 'Built' },
  online: { bg: 'bg-green-600', text: 'text-green-100', label: 'Online' },
};

function computeColonyStatus(colony: Colony) {
  const structures = colony.structures ?? [];
  const onlineStructures = structures.filter((s) => s.status === 'online');

  let power = 0;
  let habitation = 0;
  let food = 0;
  let entertainment = 0;
  let warehouseCapacity = 0;

  for (const s of onlineStructures) {
    const type = s.blueprintType.toLowerCase();
    if (type.includes('reactor') || type.includes('power')) power++;
    if (type.includes('habitat') || type.includes('habitation')) habitation++;
    if (type.includes('farm') || type.includes('food')) food++;
    if (type.includes('entertainment') || type.includes('bar') || type.includes('casino')) entertainment++;
    if (type.includes('warehouse') || type.includes('storage')) warehouseCapacity++;
  }

  const totalWorkerSlots = structures.reduce(
    (sum, s) => sum + Object.keys(s.assignedWorkers ?? {}).length,
    0,
  );
  const assignedWorkers = structures.reduce(
    (sum, s) => sum + Object.values(s.assignedWorkers ?? {}).filter(Boolean).length,
    0,
  );

  return { power, habitation, food, entertainment, warehouseCapacity, totalWorkerSlots, assignedWorkers };
}

function StructuresTab({ colony, colonyUUID }: { colony: Colony; colonyUUID: string }) {
  const structures = colony.structures ?? [];
  const status = computeColonyStatus(colony);
  const { data: baseline } = useBaseline();
  const { addStructure, optimizeBuildOrder, bootstrap, save } = useColonyMutations();

  const [selectedFlatpack, setSelectedFlatpack] = useState('');

  const flatpackOptions = useMemo(() => {
    const types = baseline?.blueprintTypes ?? [];
    return types.map((t) => ({ value: t, label: t }));
  }, [baseline]);

  const handleAddStructure = () => {
    if (!selectedFlatpack) return;
    addStructure.mutate(
      { colonyUUID, flatpackBlueprintUUID: selectedFlatpack },
      { onSuccess: () => setSelectedFlatpack('') },
    );
  };

  const handleOptimize = () => {
    const plannerStructures: PlannerStructure[] = structures.map((s) => ({
      flatpackBlueprintUUID: s.flatpackBlueprintUUID,
      isBuilt: s.status === 'built' || s.status === 'online',
      isStaged: s.status === 'staged',
      isOnline: s.status === 'online',
      buildQueueSequence: s.buildQueueSequence,
      assignedWorkers: s.assignedWorkers,
    }));
    const request: ColonyPlannerRequest = { structures: plannerStructures };
    optimizeBuildOrder.mutate(request, {
      onSuccess: (result) => {
        // Persist the reordered structures back to the colony
        const reordered = structures.map((s) => {
          const step = result.steps.find(
            (st) => st.blueprintType === s.blueprintType,
          );
          return { ...s, buildQueueSequence: step?.sequence ?? s.buildQueueSequence };
        });
        save.mutate({ entityUUID: colonyUUID, data: { structures: reordered } });
      },
    });
  };

  const handleBootstrap = () => {
    bootstrap.mutate(colonyUUID);
  };

  return (
    <div>
      {/* Colony status summary */}
      <div className="mb-4 grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-6">
        <StatusCard label="Power" value={status.power} />
        <StatusCard label="Habitation" value={status.habitation} />
        <StatusCard label="Food" value={status.food} />
        <StatusCard label="Entertainment" value={status.entertainment} />
        <StatusCard label="Warehouse" value={status.warehouseCapacity} />
        <StatusCard label="Workers" value={`${status.assignedWorkers}/${status.totalWorkerSlots}`} />
      </div>

      {/* Structure actions */}
      <div className="mb-4 space-y-3 rounded border border-gray-700 bg-gray-800/50 p-3">
        {/* Add Structure */}
        <div className="flex items-end gap-2">
          <div className="min-w-0 flex-1">
            <label className="mb-1 block text-xs text-gray-400">Add Structure</label>
            <FilteredDropdown
              options={flatpackOptions}
              value={selectedFlatpack}
              onChange={setSelectedFlatpack}
              placeholder="Select flatpack blueprint..."
            />
          </div>
          <button
            type="button"
            onClick={handleAddStructure}
            disabled={!selectedFlatpack || addStructure.isPending}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {addStructure.isPending ? 'Adding...' : 'Add'}
          </button>
        </div>

        {/* Optimize and Bootstrap buttons */}
        <div className="flex gap-2">
          <button
            type="button"
            onClick={handleOptimize}
            disabled={structures.length === 0 || optimizeBuildOrder.isPending}
            className="rounded bg-amber-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-amber-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {optimizeBuildOrder.isPending ? 'Optimizing...' : 'Optimize'}
          </button>
          <button
            type="button"
            onClick={handleBootstrap}
            disabled={bootstrap.isPending}
            className="rounded bg-green-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {bootstrap.isPending ? 'Bootstrapping...' : 'Bootstrap'}
          </button>
        </div>
      </div>

      {/* Structures list */}
      <h3 className="mb-2 text-sm font-medium text-gray-300">
        Structures ({structures.length})
      </h3>
      {structures.length === 0 ? (
        <p className="text-sm text-gray-500">No structures in this colony.</p>
      ) : (
        <div className="space-y-2">
          {structures.map((structure) => (
            <StructureRow key={structure.uuid} structure={structure} />
          ))}
        </div>
      )}
    </div>
  );
}

function StatusCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded border border-gray-700 bg-gray-800 px-3 py-2">
      <p className="text-xs text-gray-400">{label}</p>
      <p className="text-sm font-semibold text-white">{value}</p>
    </div>
  );
}

function StructureRow({ structure }: { structure: ColonyStructure }) {
  const statusStyle = STATUS_COLORS[structure.status] ?? STATUS_COLORS.staged;
  const assignedCount = Object.values(structure.assignedWorkers ?? {}).filter(Boolean).length;
  const totalSlots = Object.keys(structure.assignedWorkers ?? {}).length;

  return (
    <div className="flex items-center gap-3 rounded border border-gray-700 bg-gray-800/50 px-3 py-2">
      {/* Blueprint type */}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium text-white">{structure.blueprintType}</p>
      </div>

      {/* Status badge */}
      <span className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-medium ${statusStyle.bg} ${statusStyle.text}`}>
        {statusStyle.label}
      </span>

      {/* Workers */}
      {totalSlots > 0 && (
        <span className="text-xs text-gray-400">
          {assignedCount}/{totalSlots} workers
        </span>
      )}

      {/* Processing timer */}
      {structure.processingEndUtc && (
        <CountdownTimer
          targetTime={structure.processingEndUtc}
          keepZero
          className="text-xs text-amber-400"
        />
      )}
    </div>
  );
}


// --- Administration Tab ---

/**
 * AdministrationTab — displays import staleness indicator and colony status report.
 *
 * Validates: Requirement 1.10
 */
function AdministrationTab({ colony }: { colony: Colony }) {
  const days = computeDaysSinceImport(colony.lastImportUtc);
  const staleness = getStalenessInfo(days);

  const structureCount = colony.structures?.length ?? 0;
  const itemCount = colony.items?.length ?? 0;
  const commodityRequestCount = colony.commodityRequests?.length ?? 0;

  return (
    <div className="space-y-6">
      {/* Import Staleness Indicator */}
      <div>
        <h3 className="mb-2 text-sm font-medium text-gray-300">Import Status</h3>
        <div className="flex items-center gap-3">
          <span
            className={`inline-flex items-center rounded px-2.5 py-1 text-xs font-medium ${staleness.badgeClass}`}
          >
            {staleness.label}
          </span>
          {colony.lastImportUtc && days !== null && (
            <span className="text-xs text-gray-500">
              Last imported: {new Date(colony.lastImportUtc).toLocaleDateString()}
            </span>
          )}
        </div>
      </div>

      {/* Colony Status Report */}
      <div>
        <h3 className="mb-2 text-sm font-medium text-gray-300">Colony Status Report</h3>
        <div className="rounded border border-gray-700 bg-gray-800/50 p-4">
          <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-xs text-gray-400">Colony Name</dt>
              <dd className="text-sm text-white">{colony.colonyName}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-400">Planet</dt>
              <dd className="text-sm text-white">{colony.planetName}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-400">System</dt>
              <dd className="text-sm text-white">{colony.systemName}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-400">Structures</dt>
              <dd className="text-sm text-white">{structureCount}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-400">Warehouse Items</dt>
              <dd className="text-sm text-white">{itemCount}</dd>
            </div>
            <div>
              <dt className="text-xs text-gray-400">Commodity Requests</dt>
              <dd className="text-sm text-white">{commodityRequestCount}</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
}
