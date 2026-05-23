import { useState, useCallback } from 'react';
import { useAuthStore } from '../../auth/store';
import { useStockProfiles, useStockProfileDetail, useStockProfileMutations } from '../../api/hooks/useStockProfiles';
import { useColonies } from '../../api/hooks/useColonies';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { EditableGrid, type GridColumn } from '../../components/common/EditableGrid';
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

const itemColumns: GridColumn<StockTargetItem>[] = [
  { key: 'itemType', header: 'Item Type', type: 'text' },
  { key: 'name', header: 'Name', type: 'text' },
  { key: 'purity', header: 'Purity', type: 'text' },
  { key: 'targetQuantity', header: 'Target Qty', type: 'number' },
  { key: 'currentQuantity', header: 'Current Qty', type: 'readonly' },
];

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

  // Item management handlers
  const handleItemChange = useCallback((index: number, row: StockTargetItem) => {
    setForm((prev) => {
      const updated = [...prev.items];
      updated[index] = row;
      return { ...prev, items: updated };
    });
    setIsDirty(true);
  }, []);

  const handleItemAdd = useCallback(() => {
    setForm((prev) => ({
      ...prev,
      items: [
        ...prev.items,
        { uuid: crypto.randomUUID(), itemType: '', name: '', purity: '', targetQuantity: 0 },
      ],
    }));
    setIsDirty(true);
  }, []);

  const handleItemRemove = useCallback((index: number) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.filter((_, i) => i !== index),
    }));
    setIsDirty(true);
  }, []);

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
