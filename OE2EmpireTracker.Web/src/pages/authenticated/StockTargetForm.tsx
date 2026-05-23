import { useState, useCallback } from 'react';
import { useAuthStore } from '../../auth/store';
import { useStockProfiles, useStockProfileDetail, useStockProfileMutations } from '../../api/hooks/useStockProfiles';
import { useColonies } from '../../api/hooks/useColonies';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { StockProfile, StockTargetItem } from '../../api/types/domain';

interface FormState {
  name: string;
  assignedColonyUUID: string;
  items: StockTargetItem[];
}

const emptyForm: FormState = {
  name: '',
  assignedColonyUUID: '',
  items: [],
};

function formFromProfile(profile: StockProfile): FormState {
  return {
    name: profile.name,
    assignedColonyUUID: profile.assignedColonyUUID ?? '',
    items: profile.items ?? [],
  };
}

export function StockTargetForm() {
  const { characterUUID } = useAuthStore();
  const { data: profiles, isLoading, isError, refetch } = useStockProfiles(characterUUID);
  const { data: colonies } = useColonies(characterUUID);
  const { save, remove } = useStockProfileMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Fetch detail for selected profile
  const { data: selectedProfile } = useStockProfileDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Colony dropdown options
  const colonyOptions = (colonies ?? []).map((c) => ({
    value: c.uuid,
    label: `${c.colonyName} (${c.planetName})`,
  }));

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const profile = profiles?.find((p) => p.uuid === uuid);
    if (profile) {
      setForm(formFromProfile(profile));
      setIsDirty(false);
    }
  }, [profiles]);

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

  const handleFieldChange = useCallback(
    (field: keyof Omit<FormState, 'items'>, value: string) => {
      setForm((prev) => ({ ...prev, [field]: value }));
      setIsDirty(true);
    },
    [],
  );

  const handleSave = useCallback(async () => {
    const items = form.items.map((item) => ({
      itemType: item.itemType,
      name: item.name,
      purity: item.purity || undefined,
      targetQuantity: item.targetQuantity,
    }));

    const data = {
      name: form.name,
      assignedColonyUUID: form.assignedColonyUUID || undefined,
      items,
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
  const detailUUID = selectedProfile?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedProfile && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromProfile(selectedProfile));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading stock profiles..." />;
  if (isError) return <RetryableError message="Failed to load stock profiles." onRetry={() => void refetch()} />;

  // Resolve colony name for display in list
  const getColonyName = (colonyUUID?: string) => {
    if (!colonyUUID) return 'Unassigned';
    const colony = colonies?.find((c) => c.uuid === colonyUUID);
    return colony ? colony.colonyName : 'Unknown colony';
  };

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Stock Profiles</h2>
      </div>
      {(!profiles || profiles.length === 0) ? (
        <EmptyState title="No stock profiles" message="Create a new stock profile to get started." />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {profiles.map((p) => (
            <li key={p.uuid}>
              <button
                onClick={() => handleSelect(p.uuid)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === p.uuid
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="font-medium">{p.name || '(unnamed)'}</div>
                <div className="text-xs text-gray-500">{getColonyName(p.assignedColonyUUID)}</div>
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
            {isNewMode ? 'New Stock Profile' : 'Stock Profile Details'}
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
        <EmptyState title="No profile selected" message="Select a stock profile from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="stock-profile-name" className="mb-1 block text-sm text-gray-400">Profile Name</label>
              <input
                id="stock-profile-name"
                type="text"
                value={form.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>
            <div>
              <label htmlFor="stock-profile-colony" className="mb-1 block text-sm text-gray-400">Assigned Colony</label>
              <FilteredDropdown
                options={colonyOptions}
                value={form.assignedColonyUUID}
                onChange={(value) => handleFieldChange('assignedColonyUUID', value)}
                placeholder="Select a colony..."
              />
            </div>

            {/* Stock target items display */}
            {!isNewMode && selectedProfile && selectedProfile.items.length > 0 && (
              <div className="pt-4">
                <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-400">
                  Stock Target Items
                </h3>
                <div className="rounded border border-gray-600 bg-gray-800 p-3">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs text-gray-500">
                        <th className="pb-1 font-normal">Item</th>
                        <th className="pb-1 font-normal">Type</th>
                        <th className="pb-1 font-normal">Target Qty</th>
                        <th className="pb-1 font-normal">Current Qty</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedProfile.items.map((item) => (
                        <tr key={item.uuid} className="border-t border-gray-700/50">
                          <td className="py-1 text-gray-300">
                            {item.name}{item.purity ? ` (${item.purity})` : ''}
                          </td>
                          <td className="py-1 text-gray-400">{item.itemType}</td>
                          <td className="py-1 text-gray-300">{item.targetQuantity}</td>
                          <td className="py-1 text-gray-400">{item.currentQuantity ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
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
        title="Delete Stock Profile"
        message={`Are you sure you want to delete "${form.name || 'this profile'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
