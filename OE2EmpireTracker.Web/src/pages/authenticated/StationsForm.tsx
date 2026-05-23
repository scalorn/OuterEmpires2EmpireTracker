import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useStations, useStationDetail, useStationMutations } from '../../api/hooks/useStations';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { Station } from '../../api/types/domain';

interface FormState {
  name: string;
  systemName: string;
  stationType: string;
}

const emptyForm: FormState = {
  name: '',
  systemName: '',
  stationType: '',
};

function formFromStation(station: Station): FormState {
  return {
    name: station.name,
    systemName: station.systemName,
    stationType: station.stationType ?? '',
  };
}

const filterDefs: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Search stations...' },
];

export function StationsForm() {
  const { characterUUID } = useAuthStore();
  const { data: stations, isLoading, isError, refetch } = useStations(characterUUID);
  const { save, remove } = useStationMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });

  // Fetch detail for selected station
  const { data: selectedStation } = useStationDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Filtered station list
  const filteredStations = useMemo(() => {
    if (!stations) return [];
    const search = ((filterValues.search as string) ?? '').toLowerCase();
    if (!search) return stations;
    return stations.filter(
      (s) =>
        s.name.toLowerCase().includes(search) ||
        s.systemName.toLowerCase().includes(search) ||
        (s.stationType ?? '').toLowerCase().includes(search),
    );
  }, [stations, filterValues.search]);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const station = stations?.find((s) => s.uuid === uuid);
    if (station) {
      setForm(formFromStation(station));
      setIsDirty(false);
    }
  }, [stations]);

  const handleBack = useCallback(() => {
    setSelectedId(null);
    setIsNewMode(false);
    setIsDirty(false);
  }, []);

  const handleNew = useCallback(() => {
    setSelectedId('new');
    setIsNewMode(true);
    setForm(emptyForm);
    setIsDirty(false);
  }, []);

  const handleFieldChange = useCallback((field: keyof FormState, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setIsDirty(true);
  }, []);

  const handleSave = useCallback(async () => {
    const data: Partial<Station> = {
      name: form.name,
      systemName: form.systemName,
      stationType: form.stationType || undefined,
    };

    if (isNewMode) {
      const result = await save.mutateAsync({ data });
      setSelectedId(result.uuid);
      setIsNewMode(false);
    } else if (selectedId) {
      await save.mutateAsync({ entityUUID: selectedId, data });
    }
    setIsDirty(false);
  }, [isNewMode, selectedId, form, save]);

  const handleDelete = useCallback(async () => {
    if (!selectedId || isNewMode) return;
    await remove.mutateAsync(selectedId);
    setSelectedId(null);
    setForm(emptyForm);
    setIsDirty(false);
    setShowDeleteConfirm(false);
  }, [selectedId, isNewMode, remove]);

  // Sync form when detail loads from server
  const detailUUID = selectedStation?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedStation && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromStation(selectedStation));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading stations..." />;
  if (isError) return <RetryableError message="Failed to load stations." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Stations</h2>
      </div>
      <div className="px-3 pt-3">
        <FilterBar filters={filterDefs} values={filterValues} onChange={setFilterValues} />
      </div>
      {filteredStations.length === 0 ? (
        <EmptyState
          title="No stations"
          message={stations?.length ? 'No stations match the current filter.' : 'Create a new station to get started.'}
        />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {filteredStations.map((s) => (
            <li key={s.uuid}>
              <button
                onClick={() => handleSelect(s.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === s.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{s.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">
                  {s.systemName}{s.stationType ? ` — ${s.stationType}` : ''}
                </div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-white">
            {isNewMode ? 'New Station' : 'Station Details'}
          </h2>
          <div className="flex gap-2">
            <button
              onClick={handleNew}
              className="rounded bg-green-600 px-3 py-1.5 text-sm text-white hover:bg-green-700"
            >
              New
            </button>
            <button
              onClick={() => void handleSave()}
              disabled={!isDirty && !isNewMode}
              className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {save.isPending ? 'Saving...' : 'Save'}
            </button>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={isNewMode || !selectedId}
              className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700 disabled:opacity-50"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      {!selectedId && !isNewMode ? (
        <EmptyState title="No station selected" message="Select a station from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="station-name" className="mb-1 block text-sm text-gray-400">Name</label>
              <input
                id="station-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="station-system" className="mb-1 block text-sm text-gray-400">System</label>
              <input
                id="station-system"
                type="text"
                value={form.systemName}
                onChange={(e) => handleFieldChange('systemName', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="station-type" className="mb-1 block text-sm text-gray-400">Type</label>
              <input
                id="station-type"
                type="text"
                value={form.stationType}
                onChange={(e) => handleFieldChange('stationType', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <>
      <MasterDetailLayout
        listPanel={listPanel}
        detailPanel={detailPanel}
        selectedId={selectedId}
        onBack={handleBack}
      />
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        title="Delete Station"
        message={`Are you sure you want to delete "${form.name || 'this station'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
