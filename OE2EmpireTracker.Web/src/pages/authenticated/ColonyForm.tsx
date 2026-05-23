import { useState, useMemo } from 'react';
import { useColonies, useColonyDetail } from '../../api/hooks/useColonies';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { TabBar } from '../../components/common/TabBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { Colony } from '../../api/types/domain';

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
      return (
        <div>
          <h3 className="mb-2 text-sm font-medium text-gray-300">
            Structures ({colony.structures?.length ?? 0})
          </h3>
          <p className="text-sm text-gray-500">
            Structure management will be implemented in a subsequent task.
          </p>
        </div>
      );
    case 'warehousing':
      return (
        <div>
          <h3 className="mb-2 text-sm font-medium text-gray-300">
            Warehouse Items ({colony.items?.length ?? 0})
          </h3>
          <p className="text-sm text-gray-500">
            Warehouse management will be implemented in a subsequent task.
          </p>
        </div>
      );
    case 'commodityRequests':
      return (
        <div>
          <h3 className="mb-2 text-sm font-medium text-gray-300">
            Commodity Requests ({colony.commodityRequests?.length ?? 0})
          </h3>
          <p className="text-sm text-gray-500">
            Commodity request management will be implemented in a subsequent task.
          </p>
        </div>
      );
    case 'administration':
      return (
        <div>
          <h3 className="mb-2 text-sm font-medium text-gray-300">Administration</h3>
          <p className="text-sm text-gray-500">
            Administration panel will be implemented in a subsequent task.
          </p>
        </div>
      );
    default:
      return null;
  }
}
