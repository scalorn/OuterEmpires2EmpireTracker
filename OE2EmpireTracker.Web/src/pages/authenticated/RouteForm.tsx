import { useState, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useDeliveryRoutes, useRouteDetail } from '../../api/hooks/useDeliveryRoutes';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { TabBar } from '../../components/common/TabBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { DeliveryRoute } from '../../api/types/domain';

const ROUTE_FILTERS: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Filter routes...' },
];

const DETAIL_TABS = [
  { key: 'stops', label: 'Stops' },
  { key: 'plans', label: 'Plans' },
];

export function RouteForm() {
  const { characterUUID } = useAuthStore();
  const { data: routes, isLoading, isError, refetch } = useDeliveryRoutes(characterUUID);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });
  const [activeTab, setActiveTab] = useState('stops');

  const filteredRoutes = useMemo(() => {
    if (!routes) return [];
    const search = (filterValues.search as string ?? '').toLowerCase();
    if (!search) return routes;
    return routes.filter((r) => r.name.toLowerCase().includes(search));
  }, [routes, filterValues.search]);

  const handleSelectRoute = (uuid: string) => {
    setSelectedId(uuid);
    setActiveTab('stops');
  };

  const handleBack = () => setSelectedId(null);

  if (isLoading) return <LoadingSpinner message="Loading delivery routes..." />;
  if (isError) return <RetryableError message="Failed to load delivery routes." onRetry={() => void refetch()} />;

  return (
    <MasterDetailLayout
      selectedId={selectedId}
      onBack={handleBack}
      listPanel={
        <RouteListPanel
          routes={filteredRoutes}
          selectedId={selectedId}
          filterValues={filterValues}
          onFilterChange={setFilterValues}
          onSelect={handleSelectRoute}
        />
      }
      detailPanel={
        selectedId ? (
          <RouteDetailPanel
            charUUID={characterUUID}
            routeUUID={selectedId}
            activeTab={activeTab}
            onTabChange={setActiveTab}
          />
        ) : (
          <EmptyState title="No route selected" message="Select a route from the list to view details." />
        )
      }
    />
  );
}


// --- List Panel ---

interface RouteListPanelProps {
  routes: DeliveryRoute[];
  selectedId: string | null;
  filterValues: FilterValues;
  onFilterChange: (values: FilterValues) => void;
  onSelect: (uuid: string) => void;
}

function RouteListPanel({ routes, selectedId, filterValues, onFilterChange, onSelect }: RouteListPanelProps) {
  return (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-3 text-lg font-semibold text-white">Delivery Routes</h2>
      <FilterBar
        filters={ROUTE_FILTERS}
        values={filterValues}
        onChange={onFilterChange}
      />
      {routes.length === 0 ? (
        <EmptyState title="No routes" message="No delivery routes match your filter." />
      ) : (
        <ul className="flex-1 space-y-1 overflow-y-auto" role="listbox" aria-label="Delivery routes">
          {routes.map((route) => (
            <li key={route.uuid}>
              <button
                role="option"
                aria-selected={route.uuid === selectedId}
                onClick={() => onSelect(route.uuid)}
                className={`w-full rounded px-3 py-2 text-left transition-colors ${
                  route.uuid === selectedId
                    ? 'bg-blue-600/20 text-blue-300'
                    : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                }`}
              >
                <div className="flex items-center justify-between">
                  <span className="font-medium">{route.name}</span>
                  <span className="text-xs text-gray-500">
                    {route.stops.length} {route.stops.length === 1 ? 'stop' : 'stops'}
                  </span>
                </div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}


// --- Detail Panel ---

interface RouteDetailPanelProps {
  charUUID: string | null;
  routeUUID: string;
  activeTab: string;
  onTabChange: (tab: string) => void;
}

function RouteDetailPanel({ charUUID, routeUUID, activeTab, onTabChange }: RouteDetailPanelProps) {
  const { data: route, isLoading, isError, refetch } = useRouteDetail(charUUID, routeUUID);

  if (isLoading) return <LoadingSpinner message="Loading route details..." />;
  if (isError) return <RetryableError message="Failed to load route details." onRetry={() => void refetch()} />;
  if (!route) return <EmptyState title="Route not found" message="The selected route could not be loaded." />;

  return (
    <div className="flex h-full flex-col p-4">
      {/* Route name header */}
      <div className="mb-4">
        <label className="mb-1 block text-xs font-medium text-gray-400">Route Name</label>
        <h2 className="text-xl font-semibold text-white">{route.name}</h2>
      </div>

      {/* Tabs */}
      <TabBar tabs={DETAIL_TABS} activeTab={activeTab} onTabChange={onTabChange} />

      {/* Tab content */}
      <div className="mt-4 flex-1 overflow-y-auto" role="tabpanel" id={`tabpanel-${activeTab}`} aria-labelledby={`tab-${activeTab}`}>
        {activeTab === 'stops' && <StopsTab stops={route.stops} />}
        {activeTab === 'plans' && <PlansTabPlaceholder />}
      </div>
    </div>
  );
}

// --- Stops Tab ---

interface StopsTabProps {
  stops: DeliveryRoute['stops'];
}

function StopsTab({ stops }: StopsTabProps) {
  if (stops.length === 0) {
    return <EmptyState title="No stops" message="This route has no stops yet." />;
  }

  const sortedStops = [...stops].sort((a, b) => a.sequence - b.sequence);

  return (
    <div className="space-y-2">
      <p className="mb-2 text-sm text-gray-400">
        {sortedStops.length} {sortedStops.length === 1 ? 'stop' : 'stops'} on this route
      </p>
      <ol className="space-y-2">
        {sortedStops.map((stop, index) => (
          <li
            key={stop.uuid}
            className="flex items-start gap-3 rounded border border-gray-700 bg-gray-800/50 px-4 py-3"
          >
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-blue-600/30 text-xs font-bold text-blue-300">
              {index + 1}
            </span>
            <div className="min-w-0 flex-1">
              <p className="font-medium text-white">{stop.colonyName}</p>
              <p className="text-sm text-gray-400">
                {stop.planetName} &middot; {stop.systemName}
              </p>
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}

// --- Plans Tab Placeholder (filled in by task 12.3) ---

function PlansTabPlaceholder() {
  return <EmptyState title="Plans" message="Delivery plan management will be available here." />;
}
