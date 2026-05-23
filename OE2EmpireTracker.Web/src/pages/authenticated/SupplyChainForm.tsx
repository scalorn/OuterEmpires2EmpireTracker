import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useSupplyChains, useSupplyChainDetail, useSupplyChainMutations } from '../../api/hooks/useSupplyChains';
import { useColonies } from '../../api/hooks/useColonies';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { SupplyChain } from '../../api/types/domain';

interface FormState {
  name: string;
  sourceColonyUUID: string;
  destinationColonyUUID: string;
}

const emptyForm: FormState = {
  name: '',
  sourceColonyUUID: '',
  destinationColonyUUID: '',
};

function formFromChain(chain: SupplyChain): FormState {
  return {
    name: chain.name,
    sourceColonyUUID: chain.sourceColonyUUID,
    destinationColonyUUID: chain.destinationColonyUUID,
  };
}

const filterDefs: FilterDefinition[] = [
  { type: 'text', key: 'search', placeholder: 'Search supply chains...' },
];

export function SupplyChainForm() {
  const { characterUUID } = useAuthStore();
  const { data: chains, isLoading, isError, refetch } = useSupplyChains(characterUUID);
  const { data: colonies } = useColonies(characterUUID);
  const { save, remove } = useSupplyChainMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterValues, setFilterValues] = useState<FilterValues>({ search: '' });

  // Fetch detail for selected chain
  const { data: selectedChain } = useSupplyChainDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Colony options for dropdowns
  const colonyOptions = useMemo(() => {
    if (!colonies) return [];
    return colonies.map((c) => ({
      value: c.uuid,
      label: `${c.colonyName} (${c.planetName})`,
    }));
  }, [colonies]);

  // Filtered chain list
  const filteredChains = useMemo(() => {
    if (!chains) return [];
    const search = ((filterValues.search as string) ?? '').toLowerCase();
    if (!search) return chains;
    return chains.filter(
      (c) =>
        c.name.toLowerCase().includes(search) ||
        c.sourceColonyName.toLowerCase().includes(search) ||
        c.destinationColonyName.toLowerCase().includes(search),
    );
  }, [chains, filterValues.search]);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const chain = chains?.find((c) => c.uuid === uuid);
    if (chain) {
      setForm(formFromChain(chain));
      setIsDirty(false);
    }
  }, [chains]);

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
    if (isNewMode) {
      const result = await save.mutateAsync({
        data: {
          name: form.name,
          sourceColonyUUID: form.sourceColonyUUID,
          destinationColonyUUID: form.destinationColonyUUID,
        },
      });
      setSelectedId(result.uuid);
      setIsNewMode(false);
    } else if (selectedId) {
      await save.mutateAsync({
        entityUUID: selectedId,
        data: {
          name: form.name,
          sourceColonyUUID: form.sourceColonyUUID,
          destinationColonyUUID: form.destinationColonyUUID,
        },
      });
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
  const detailUUID = selectedChain?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedChain && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromChain(selectedChain));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading supply chains..." />;
  if (isError) return <RetryableError message="Failed to load supply chains." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Supply Chains</h2>
      </div>
      <div className="px-3 pt-3">
        <FilterBar filters={filterDefs} values={filterValues} onChange={setFilterValues} />
      </div>
      {filteredChains.length === 0 ? (
        <EmptyState
          title="No supply chains"
          message={chains?.length ? 'No chains match the current filter.' : 'Create a new supply chain to get started.'}
        />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {filteredChains.map((c) => (
            <li key={c.uuid}>
              <button
                onClick={() => handleSelect(c.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === c.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{c.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">
                  {c.sourceColonyName} → {c.destinationColonyName}
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
            {isNewMode ? 'New Supply Chain' : 'Supply Chain Details'}
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
        <EmptyState title="No chain selected" message="Select a supply chain from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="chain-name" className="mb-1 block text-sm text-gray-400">Name</label>
              <input
                id="chain-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="chain-source" className="mb-1 block text-sm text-gray-400">Source Colony</label>
              <FilteredDropdown
                options={colonyOptions}
                value={form.sourceColonyUUID}
                onChange={(value) => handleFieldChange('sourceColonyUUID', value)}
                placeholder="Select source colony..."
              />
            </div>
            <div>
              <label htmlFor="chain-destination" className="mb-1 block text-sm text-gray-400">Destination Colony</label>
              <FilteredDropdown
                options={colonyOptions}
                value={form.destinationColonyUUID}
                onChange={(value) => handleFieldChange('destinationColonyUUID', value)}
                placeholder="Select destination colony..."
              />
            </div>

            {/* Steps section placeholder — implemented in task 16.2 */}
            {!isNewMode && selectedChain && selectedChain.steps.length > 0 && (
              <div className="pt-4">
                <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-gray-400">
                  Steps ({selectedChain.steps.length})
                </h3>
                <p className="text-xs text-gray-500">Step management will be available in a future update.</p>
              </div>
            )}
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
        title="Delete Supply Chain"
        message={`Are you sure you want to delete "${form.name || 'this supply chain'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
