import { useState, useMemo, useCallback } from 'react';
import { useAuthStore } from '../../auth/store';
import { useDeliveryRoutes, useRouteDetail, useRouteMutations } from '../../api/hooks/useDeliveryRoutes';
import { usePlans, usePlanMutations } from '../../api/hooks/useDeliveryPlans';
import { useColonies } from '../../api/hooks/useColonies';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { TabBar } from '../../components/common/TabBar';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { moveUp, moveDown, resequence } from '../../utils/reorderUtils';
import type { DeliveryRoute, RouteStop, DeliveryPlan, DeliveryItem } from '../../api/types/domain';

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

  const handleNew = () => {
    setSelectedId('__new__');
    setActiveTab('stops');
  };

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
            onNew={handleNew}
            onDeleted={() => setSelectedId(null)}
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
  onNew: () => void;
  onDeleted: () => void;
}

function RouteDetailPanel({ charUUID, routeUUID, activeTab, onTabChange, onNew, onDeleted }: RouteDetailPanelProps) {
  const isNew = routeUUID === '__new__';
  const { data: route, isLoading, isError, refetch } = useRouteDetail(
    isNew ? null : charUUID,
    isNew ? null : routeUUID,
  );
  const { save, remove } = useRouteMutations();

  // Local editable state
  const [routeName, setRouteName] = useState('');
  const [localStops, setLocalStops] = useState<RouteStop[]>([]);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Sync server data into local state when route loads
  const routeUUIDForSync = route?.uuid ?? (isNew ? '__new__' : null);

  // Reset local state when route changes
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (routeUUIDForSync && routeUUIDForSync !== lastSyncedUUID) {
    if (isNew) {
      setRouteName('');
      setLocalStops([]);
      setIsDirty(false);
    } else if (route) {
      setRouteName(route.name);
      setLocalStops([...route.stops].sort((a, b) => a.sequence - b.sequence));
      setIsDirty(false);
    }
    setLastSyncedUUID(routeUUIDForSync);
  }

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setRouteName(e.target.value);
    setIsDirty(true);
  };

  const handleAddStop = (colonyUUID: string, colonyName: string, planetName: string, systemName: string) => {
    const newStop: RouteStop = {
      uuid: crypto.randomUUID(),
      colonyUUID,
      colonyName,
      planetName,
      systemName,
      sequence: localStops.length + 1,
    };
    setLocalStops((prev) => [...prev, newStop]);
    setIsDirty(true);
  };

  const handleMoveUp = (index: number) => {
    setLocalStops((prev) => resequence(moveUp(prev, index)));
    setIsDirty(true);
  };

  const handleMoveDown = (index: number) => {
    setLocalStops((prev) => resequence(moveDown(prev, index)));
    setIsDirty(true);
  };

  const handleRemoveStop = (index: number) => {
    setLocalStops((prev) => resequence(prev.filter((_, i) => i !== index)));
    setIsDirty(true);
  };

  const handleSave = () => {
    const stopsPayload = localStops.map((s) => ({
      colonyUUID: s.colonyUUID,
      colonyName: s.colonyName,
      planetName: s.planetName,
      systemName: s.systemName,
      sequence: s.sequence,
    }));

    save.mutate(
      {
        entityUUID: isNew ? undefined : routeUUID,
        data: { name: routeName, stops: stopsPayload },
      },
      { onSuccess: () => setIsDirty(false) },
    );
  };

  const handleDelete = () => {
    remove.mutate(routeUUID, {
      onSuccess: () => {
        setShowDeleteConfirm(false);
        onDeleted();
      },
    });
  };

  if (!isNew && isLoading) return <LoadingSpinner message="Loading route details..." />;
  if (!isNew && isError) return <RetryableError message="Failed to load route details." onRetry={() => void refetch()} />;
  if (!isNew && !route) return <EmptyState title="Route not found" message="The selected route could not be loaded." />;

  return (
    <div className="flex h-full flex-col p-4">
      {/* Header with action buttons */}
      <div className="mb-4 flex items-center justify-between">
        <div className="flex-1">
          <label className="mb-1 block text-xs font-medium text-gray-400">Route Name</label>
          <input
            type="text"
            value={routeName}
            onChange={handleNameChange}
            placeholder="Enter route name..."
            className="w-full max-w-sm rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="flex gap-2">
          <button
            onClick={handleSave}
            disabled={save.isPending || !routeName.trim()}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {save.isPending ? 'Saving...' : 'Save'}
          </button>
          <button
            onClick={onNew}
            className="rounded border border-gray-600 px-3 py-1.5 text-sm text-gray-300 hover:bg-gray-700"
          >
            New
          </button>
          {!isNew && (
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={remove.isPending}
              className="rounded bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
            >
              Delete
            </button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <TabBar tabs={DETAIL_TABS} activeTab={activeTab} onTabChange={onTabChange} />

      {/* Tab content */}
      <div className="mt-4 flex-1 overflow-y-auto" role="tabpanel" id={`tabpanel-${activeTab}`} aria-labelledby={`tab-${activeTab}`}>
        {activeTab === 'stops' && (
          <StopsTab
            charUUID={charUUID}
            stops={localStops}
            onAddStop={handleAddStop}
            onMoveUp={handleMoveUp}
            onMoveDown={handleMoveDown}
            onRemoveStop={handleRemoveStop}
          />
        )}
        {activeTab === 'plans' && (
          <PlansTab
            charUUID={charUUID}
            routeUUID={isNew ? undefined : routeUUID}
            routeName={routeName}
            stops={localStops}
          />
        )}
      </div>

      {/* Delete confirmation dialog */}
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        title="Delete Route"
        message={`Are you sure you want to delete "${routeName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={handleDelete}
        onCancel={() => setShowDeleteConfirm(false)}
      />
    </div>
  );
}


// --- Stops Tab ---

interface StopsTabProps {
  charUUID: string | null;
  stops: RouteStop[];
  onAddStop: (colonyUUID: string, colonyName: string, planetName: string, systemName: string) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
  onRemoveStop: (index: number) => void;
}

function StopsTab({ charUUID, stops, onAddStop, onMoveUp, onMoveDown, onRemoveStop }: StopsTabProps) {
  const { data: colonies } = useColonies(charUUID);
  const [selectedColonyUUID, setSelectedColonyUUID] = useState('');

  const colonyOptions = useMemo(() => {
    if (!colonies) return [];
    return colonies.map((c) => ({
      value: c.uuid,
      label: `${c.colonyName} (${c.planetName}, ${c.systemName})`,
    }));
  }, [colonies]);

  const handleAddStop = useCallback(() => {
    if (!selectedColonyUUID || !colonies) return;
    const colony = colonies.find((c) => c.uuid === selectedColonyUUID);
    if (!colony) return;
    onAddStop(colony.uuid, colony.colonyName, colony.planetName, colony.systemName);
    setSelectedColonyUUID('');
  }, [selectedColonyUUID, colonies, onAddStop]);

  return (
    <div className="space-y-4">
      {/* Add Stop section */}
      <div className="rounded border border-gray-700 bg-gray-800/50 p-3">
        <p className="mb-2 text-sm font-medium text-gray-300">Add Stop</p>
        <div className="flex gap-2">
          <div className="flex-1">
            <FilteredDropdown
              options={colonyOptions}
              value={selectedColonyUUID}
              onChange={setSelectedColonyUUID}
              placeholder="Select colony..."
            />
          </div>
          <button
            onClick={handleAddStop}
            disabled={!selectedColonyUUID}
            className="rounded bg-green-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
          >
            Add Stop
          </button>
        </div>
      </div>

      {/* Stops list */}
      {stops.length === 0 ? (
        <EmptyState title="No stops" message="This route has no stops yet. Add a colony above." />
      ) : (
        <div className="space-y-2">
          <p className="text-sm text-gray-400">
            {stops.length} {stops.length === 1 ? 'stop' : 'stops'} on this route
          </p>
          <ol className="space-y-2">
            {stops.map((stop, index) => (
              <li
                key={stop.uuid}
                className="flex items-center gap-3 rounded border border-gray-700 bg-gray-800/50 px-4 py-3"
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
                {/* Reorder and remove controls */}
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => onMoveUp(index)}
                    disabled={index === 0}
                    title="Move up"
                    className="rounded p-1 text-gray-400 hover:bg-gray-700 hover:text-white disabled:opacity-30"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M14.707 12.707a1 1 0 01-1.414 0L10 9.414l-3.293 3.293a1 1 0 01-1.414-1.414l4-4a1 1 0 011.414 0l4 4a1 1 0 010 1.414z" clipRule="evenodd" />
                    </svg>
                  </button>
                  <button
                    onClick={() => onMoveDown(index)}
                    disabled={index === stops.length - 1}
                    title="Move down"
                    className="rounded p-1 text-gray-400 hover:bg-gray-700 hover:text-white disabled:opacity-30"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd" />
                    </svg>
                  </button>
                  <button
                    onClick={() => onRemoveStop(index)}
                    title="Remove stop"
                    className="rounded p-1 text-red-400 hover:bg-red-900/30 hover:text-red-300"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                    </svg>
                  </button>
                </div>
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  );
}

// --- Plans Tab ---

interface PlansTabProps {
  charUUID: string | null;
  routeUUID: string | undefined;
  routeName: string;
  stops: RouteStop[];
}

function PlansTab({ charUUID, routeUUID, routeName, stops }: PlansTabProps) {
  const { data: allPlans } = usePlans(charUUID);
  const { save } = usePlanMutations();
  const [selectedPlanUUID, setSelectedPlanUUID] = useState('');
  const [selectedStopUUID, setSelectedStopUUID] = useState<string | null>(null);

  // Filter plans to only those belonging to this route
  const routePlans = useMemo(() => {
    if (!allPlans || !routeUUID) return [];
    return allPlans.filter((p) => p.routeUUID === routeUUID);
  }, [allPlans, routeUUID]);

  const planOptions = useMemo(() => {
    return routePlans.map((p) => ({ value: p.uuid, label: p.name }));
  }, [routePlans]);

  const selectedPlan = useMemo(() => {
    return routePlans.find((p) => p.uuid === selectedPlanUUID) ?? null;
  }, [routePlans, selectedPlanUUID]);

  const handleNewPlan = () => {
    if (!routeUUID) return;
    const today = new Date().toISOString().slice(0, 10);
    const planName = `${routeName || 'Route'} - ${today}`;
    save.mutate(
      { data: { name: planName, routeUUID } },
      {
        onSuccess: (created: DeliveryPlan) => {
          setSelectedPlanUUID(created.uuid);
        },
      },
    );
  };

  const handleSelectStop = (stopUUID: string) => {
    setSelectedStopUUID((prev) => (prev === stopUUID ? null : stopUUID));
  };

  // Get items for the selected stop from the selected plan
  const stopItems = useMemo(() => {
    if (!selectedPlan || !selectedStopUUID) return null;
    return selectedPlan.stopItems[selectedStopUUID] ?? null;
  }, [selectedPlan, selectedStopUUID]);

  if (!routeUUID) {
    return <EmptyState title="Save route first" message="Save the route before managing delivery plans." />;
  }

  return (
    <div className="space-y-4">
      {/* Plan selection */}
      <div className="rounded border border-gray-700 bg-gray-800/50 p-3">
        <p className="mb-2 text-sm font-medium text-gray-300">Delivery Plan</p>
        <div className="flex gap-2">
          <div className="flex-1">
            <FilteredDropdown
              options={planOptions}
              value={selectedPlanUUID}
              onChange={setSelectedPlanUUID}
              placeholder="Select a plan..."
            />
          </div>
          <button
            onClick={handleNewPlan}
            disabled={save.isPending}
            className="rounded bg-green-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
          >
            {save.isPending ? 'Creating...' : 'New Plan'}
          </button>
        </div>
      </div>

      {/* Stop selection and items display */}
      {selectedPlan && (
        <div className="space-y-3">
          <p className="text-sm text-gray-400">
            Select a stop to view its delivery items:
          </p>
          <div className="space-y-2">
            {stops.map((stop) => (
              <button
                key={stop.uuid}
                onClick={() => handleSelectStop(stop.uuid)}
                className={`w-full rounded border px-4 py-3 text-left transition-colors ${
                  selectedStopUUID === stop.uuid
                    ? 'border-blue-500 bg-blue-600/20 text-blue-300'
                    : 'border-gray-700 bg-gray-800/50 text-gray-300 hover:border-gray-600 hover:bg-gray-800'
                }`}
              >
                <div className="flex items-center gap-3">
                  <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-blue-600/30 text-xs font-bold text-blue-300">
                    {stop.sequence}
                  </span>
                  <div>
                    <p className="font-medium text-white">{stop.colonyName}</p>
                    <p className="text-xs text-gray-400">
                      {stop.planetName} &middot; {stop.systemName}
                    </p>
                  </div>
                </div>
              </button>
            ))}
          </div>

          {/* Items for selected stop */}
          {selectedStopUUID && (
            <StopItemsDisplay items={stopItems} />
          )}
        </div>
      )}

      {!selectedPlan && routePlans.length === 0 && (
        <EmptyState title="No plans" message="Create a new plan to start assigning delivery items to stops." />
      )}
    </div>
  );
}

// --- Stop Items Display ---

interface StopItemsDisplayProps {
  items: { dropOff: DeliveryItem[]; pickUp: DeliveryItem[] } | null;
}

function StopItemsDisplay({ items }: StopItemsDisplayProps) {
  if (!items || (items.dropOff.length === 0 && items.pickUp.length === 0)) {
    return (
      <div className="rounded border border-gray-700 bg-gray-800/50 p-4 text-center text-sm text-gray-400">
        No items assigned to this stop yet.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Drop-off items */}
      <div className="rounded border border-gray-700 bg-gray-800/50 p-3">
        <h4 className="mb-2 text-sm font-medium text-orange-300">
          Drop-off ({items.dropOff.length})
        </h4>
        {items.dropOff.length === 0 ? (
          <p className="text-xs text-gray-500">No drop-off items.</p>
        ) : (
          <ul className="space-y-1">
            {items.dropOff.map((item) => (
              <li key={item.uuid} className="flex items-center justify-between rounded bg-gray-900/50 px-3 py-1.5 text-sm">
                <span className="text-gray-200">
                  {item.name}
                  {item.purity && <span className="ml-1 text-xs text-gray-400">({item.purity})</span>}
                </span>
                <span className="text-xs text-gray-400">
                  {item.quantity} &times; {item.itemType}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Pick-up items */}
      <div className="rounded border border-gray-700 bg-gray-800/50 p-3">
        <h4 className="mb-2 text-sm font-medium text-green-300">
          Pick-up ({items.pickUp.length})
        </h4>
        {items.pickUp.length === 0 ? (
          <p className="text-xs text-gray-500">No pick-up items.</p>
        ) : (
          <ul className="space-y-1">
            {items.pickUp.map((item) => (
              <li key={item.uuid} className="flex items-center justify-between rounded bg-gray-900/50 px-3 py-1.5 text-sm">
                <span className="text-gray-200">
                  {item.name}
                  {item.purity && <span className="ml-1 text-xs text-gray-400">({item.purity})</span>}
                </span>
                <span className="text-xs text-gray-400">
                  {item.quantity} &times; {item.itemType}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
