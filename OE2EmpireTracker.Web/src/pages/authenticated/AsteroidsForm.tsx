import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useAsteroids, useAsteroidDetail, useAsteroidMutations } from '../../api/hooks/useAsteroids';
import { useSurveys } from '../../api/hooks/useSurveys';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { Asteroid } from '../../api/types/domain';

interface FormState {
  name: string;
  systemName: string;
  linkedSurveyUUID: string;
}

const emptyForm: FormState = {
  name: '',
  systemName: '',
  linkedSurveyUUID: '',
};

function formFromAsteroid(asteroid: Asteroid): FormState {
  return {
    name: asteroid.name,
    systemName: asteroid.systemName,
    linkedSurveyUUID: asteroid.linkedSurveyUUID ?? '',
  };
}

const filterDefs: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Search asteroids...' },
];

export function AsteroidsForm() {
  const { characterUUID } = useAuthStore();
  const { data: asteroids, isLoading, isError, refetch } = useAsteroids(characterUUID);
  const { data: surveys } = useSurveys(characterUUID);
  const { save, remove } = useAsteroidMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });

  // Fetch detail for selected asteroid
  const { data: selectedAsteroid } = useAsteroidDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Survey options for linked survey dropdown
  const surveyOptions = useMemo(() => {
    if (!surveys) return [{ value: '', label: '(none)' }];
    const opts = surveys
      .filter((s) => s.surveyType === 'Asteroid')
      .map((s) => ({
        value: s.uuid,
        label: `${s.planetName} (${s.systemName})`,
      }));
    return [{ value: '', label: '(none)' }, ...opts];
  }, [surveys]);

  // Build a lookup for survey display in the list
  const surveyLookup = useMemo(() => {
    if (!surveys) return new Map<string, string>();
    return new Map(surveys.map((s) => [s.uuid, `${s.planetName} (${s.systemName})`]));
  }, [surveys]);

  // Filtered asteroid list
  const filteredAsteroids = useMemo(() => {
    if (!asteroids) return [];
    const search = ((filterValues.search as string) ?? '').toLowerCase();
    if (!search) return asteroids;
    return asteroids.filter(
      (a) =>
        a.name.toLowerCase().includes(search) ||
        a.systemName.toLowerCase().includes(search) ||
        (a.linkedSurveyUUID ? (surveyLookup.get(a.linkedSurveyUUID) ?? '').toLowerCase().includes(search) : false),
    );
  }, [asteroids, filterValues.search, surveyLookup]);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const asteroid = asteroids?.find((a) => a.uuid === uuid);
    if (asteroid) {
      setForm(formFromAsteroid(asteroid));
      setIsDirty(false);
    }
  }, [asteroids]);

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
    const data: Partial<Asteroid> = {
      name: form.name,
      systemName: form.systemName,
      linkedSurveyUUID: form.linkedSurveyUUID || undefined,
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
  const detailUUID = selectedAsteroid?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedAsteroid && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromAsteroid(selectedAsteroid));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading asteroids..." />;
  if (isError) return <RetryableError message="Failed to load asteroids." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Asteroids</h2>
      </div>
      <div className="px-3 pt-3">
        <FilterBar filters={filterDefs} values={filterValues} onChange={setFilterValues} />
      </div>
      {filteredAsteroids.length === 0 ? (
        <EmptyState
          title="No asteroids"
          message={asteroids?.length ? 'No asteroids match the current filter.' : 'Create a new asteroid to get started.'}
        />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {filteredAsteroids.map((a) => (
            <li key={a.uuid}>
              <button
                onClick={() => handleSelect(a.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === a.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{a.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">
                  {a.systemName}
                  {a.linkedSurveyUUID ? ` — ${surveyLookup.get(a.linkedSurveyUUID) ?? 'Linked survey'}` : ''}
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
            {isNewMode ? 'New Asteroid' : 'Asteroid Details'}
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
        <EmptyState title="No asteroid selected" message="Select an asteroid from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="asteroid-name" className="mb-1 block text-sm text-gray-400">Name</label>
              <input
                id="asteroid-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="asteroid-system" className="mb-1 block text-sm text-gray-400">System</label>
              <input
                id="asteroid-system"
                type="text"
                value={form.systemName}
                onChange={(e) => handleFieldChange('systemName', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="asteroid-survey" className="mb-1 block text-sm text-gray-400">Linked Survey</label>
              <FilteredDropdown
                options={surveyOptions}
                value={form.linkedSurveyUUID}
                onChange={(value) => handleFieldChange('linkedSurveyUUID', value)}
                placeholder="Select a survey..."
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
        title="Delete Asteroid"
        message={`Are you sure you want to delete "${form.name || 'this asteroid'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
