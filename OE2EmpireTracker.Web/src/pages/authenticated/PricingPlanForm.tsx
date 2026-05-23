import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { usePricingPlans, usePricingPlanDetail, usePricingPlanMutations } from '../../api/hooks/usePricingPlans';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { EditableGrid } from '../../components/common/EditableGrid';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { GridColumn } from '../../components/common/EditableGrid';
import type { PricingPlan, PricingPlanItem } from '../../api/types/domain';

interface FormState {
  name: string;
  items: PricingPlanItem[];
}

const emptyForm: FormState = {
  name: '',
  items: [],
};

function formFromPlan(plan: PricingPlan): FormState {
  return {
    name: plan.name,
    items: plan.items ?? [],
  };
}

const itemColumns: GridColumn<PricingPlanItem>[] = [
  { key: 'itemName', header: 'Item Name', type: 'text' },
  { key: 'itemType', header: 'Item Type', type: 'text' },
  { key: 'unitPrice', header: 'Unit Price', type: 'number' },
];


export function PricingPlanForm() {
  const { characterUUID } = useAuthStore();
  const { data: plans, isLoading, isError, refetch } = usePricingPlans(characterUUID);
  const { save, remove } = usePricingPlanMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [filterText, setFilterText] = useState('');

  // Fetch detail for selected plan
  const { data: selectedPlan } = usePricingPlanDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Filtered plan list
  const filteredPlans = useMemo(() => {
    if (!plans) return [];
    const search = filterText.toLowerCase();
    if (!search) return plans;
    return plans.filter((p) => p.name.toLowerCase().includes(search));
  }, [plans, filterText]);

  const handleSelect = useCallback((uuid: string) => {
    setSelectedId(uuid);
    setIsNewMode(false);
    const plan = plans?.find((p) => p.uuid === uuid);
    if (plan) {
      setForm(formFromPlan(plan));
      setIsDirty(false);
    }
  }, [plans]);

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

  const handleNameChange = useCallback((value: string) => {
    setForm((prev) => ({ ...prev, name: value }));
    setIsDirty(true);
  }, []);

  // Item management handlers
  const handleItemChange = useCallback((index: number, row: PricingPlanItem) => {
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
        { uuid: crypto.randomUUID(), itemName: '', itemType: '', unitPrice: 0 },
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
      uuid: item.uuid,
      itemName: item.itemName,
      itemType: item.itemType,
      unitPrice: item.unitPrice,
    }));

    const data: Partial<PricingPlan> = { name: form.name, items };

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
  const detailUUID = selectedPlan?.uuid;
  const [lastSyncedUUID, setLastSyncedUUID] = useState<string | null>(null);
  if (selectedPlan && detailUUID !== lastSyncedUUID && !isNewMode && !isDirty) {
    setForm(formFromPlan(selectedPlan));
    setLastSyncedUUID(detailUUID ?? null);
  }

  // --- Render ---

  if (isLoading) return <LoadingSpinner message="Loading pricing plans..." />;
  if (isError) return <RetryableError message="Failed to load pricing plans." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Pricing Plans</h2>
      </div>
      <div className="px-3 pt-3">
        <input
          type="text"
          value={filterText}
          onChange={(e) => setFilterText(e.target.value)}
          placeholder="Filter plans..."
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-sm text-white placeholder-gray-400"
        />
      </div>
      {filteredPlans.length === 0 ? (
        <EmptyState
          title="No pricing plans"
          message={plans?.length ? 'No plans match the current filter.' : 'Create a new pricing plan to get started.'}
        />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {filteredPlans.map((p) => (
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
                <div className="text-xs text-gray-500">{p.items?.length ?? 0} items</div>
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
            {isNewMode ? 'New Pricing Plan' : 'Pricing Plan Details'}
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
        <EmptyState title="No plan selected" message="Select a pricing plan from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="max-w-lg space-y-4">
            <div>
              <label htmlFor="plan-name" className="mb-1 block text-sm text-gray-400">Plan Name</label>
              <input
                id="plan-name"
                type="text"
                value={form.name}
                onChange={(e) => handleNameChange(e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>

            <div className="pt-4">
              <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-400">
                Item Prices
              </h3>
              <EditableGrid
                columns={itemColumns}
                rows={form.items}
                onRowChange={handleItemChange}
                onRowAdd={handleItemAdd}
                onRowRemove={handleItemRemove}
                keyExtractor={(row) => row.uuid}
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
        title="Delete Pricing Plan"
        message={`Are you sure you want to delete "${form.name || 'this plan'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={() => void handleDelete()}
        onCancel={() => setShowDeleteConfirm(false)}
        variant="danger"
      />
    </>
  );
}
