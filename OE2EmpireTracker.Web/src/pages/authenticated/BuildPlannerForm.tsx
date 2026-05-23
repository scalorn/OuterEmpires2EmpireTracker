/**
 * BuildPlannerForm — manages build plans for manufacturing orders.
 *
 * Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5
 */
import { useState, useCallback, useMemo } from 'react';
import { useAuthStore } from '../../auth/store';
import { useBuildPlans, useBuildPlanDetail, useBuildPlanMutations } from '../../api/hooks/useBuildPlans';
import { useBlueprints } from '../../api/hooks/useBlueprints';
import { useColonies } from '../../api/hooks/useColonies';
import { useUnsavedChanges } from '../../hooks/useUnsavedChanges';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { BuildPlan, BuildPlanItem, BlueprintResource } from '../../api/types/domain';

interface FormState {
  name: string;
  items: BuildPlanItem[];
}

const emptyForm: FormState = {
  name: '',
  items: [],
};

function formFromPlan(plan: BuildPlan): FormState {
  return {
    name: plan.name,
    items: plan.items ?? [],
  };
}

/** Aggregate resource requirements for all pending items in the plan. */
function aggregateResources(
  items: BuildPlanItem[],
  blueprintLookup: Map<string, BlueprintResource[]>,
): { resourceName: string; purity: string; totalQuantity: number }[] {
  const pending = items.filter((i) => i.status === 'pending');
  const totals = new Map<string, { resourceName: string; purity: string; totalQuantity: number }>();

  for (const item of pending) {
    const resources = blueprintLookup.get(item.blueprintUUID);
    if (!resources) continue;
    for (const res of resources) {
      const key = `${res.resourceName}|${res.purity ?? ''}`;
      const existing = totals.get(key);
      if (existing) {
        existing.totalQuantity += res.quantity * item.quantity;
      } else {
        totals.set(key, {
          resourceName: res.resourceName,
          purity: res.purity ?? '',
          totalQuantity: res.quantity * item.quantity,
        });
      }
    }
  }

  return Array.from(totals.values()).sort((a, b) => a.resourceName.localeCompare(b.resourceName));
}

export function BuildPlannerForm() {
  const { characterUUID } = useAuthStore();
  const { data: plans, isLoading, isError, refetch } = useBuildPlans(characterUUID);
  const { data: blueprints } = useBlueprints(characterUUID);
  const { data: colonies } = useColonies(characterUUID);
  const { save, remove } = useBuildPlanMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isNewMode, setIsNewMode] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [isDirty, setIsDirty] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  // Add-item state
  const [addBlueprintUUID, setAddBlueprintUUID] = useState('');
  const [addQuantity, setAddQuantity] = useState(1);

  // Fetch detail for selected plan
  const { data: selectedPlan } = useBuildPlanDetail(
    characterUUID,
    isNewMode ? null : selectedId,
  );

  // Unsaved changes guard
  useUnsavedChanges(isDirty);

  // Blueprint dropdown options
  const blueprintOptions = useMemo(() => {
    if (!blueprints) return [];
    return blueprints.map((bp) => ({
      value: bp.uuid,
      label: bp.nickName ? `${bp.name} (${bp.nickName})` : bp.name,
    }));
  }, [blueprints]);

  // Blueprint resource lookup for aggregation
  const blueprintResourceLookup = useMemo(() => {
    const map = new Map<string, BlueprintResource[]>();
    if (blueprints) {
      for (const bp of blueprints) {
        map.set(bp.uuid, bp.resources ?? []);
      }
    }
    return map;
  }, [blueprints]);

  // Colony name lookup
  const colonyNameLookup = useMemo(() => {
    const map = new Map<string, string>();
    if (colonies) {
      for (const c of colonies) {
        map.set(c.uuid, c.colonyName);
      }
    }
    return map;
  }, [colonies]);

  // Aggregated resources for pending items
  const aggregatedResources = useMemo(
    () => aggregateResources(form.items, blueprintResourceLookup),
    [form.items, blueprintResourceLookup],
  );

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

  // Add item to plan
  const handleAddItem = useCallback(() => {
    if (!addBlueprintUUID) return;
    const bp = blueprints?.find((b) => b.uuid === addBlueprintUUID);
    const newItem: BuildPlanItem = {
      uuid: crypto.randomUUID(),
      blueprintUUID: addBlueprintUUID,
      blueprintName: bp?.name ?? '(unknown)',
      quantity: addQuantity,
      status: 'pending',
      assignedColonyUUID: undefined,
    };
    setForm((prev) => ({ ...prev, items: [...prev.items, newItem] }));
    setIsDirty(true);
    setAddBlueprintUUID('');
    setAddQuantity(1);
  }, [addBlueprintUUID, addQuantity, blueprints]);

  // Remove item from plan
  const handleRemoveItem = useCallback((index: number) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.filter((_, i) => i !== index),
    }));
    setIsDirty(true);
  }, []);

  // Mark item status
  const handleMarkStatus = useCallback((index: number, status: 'allocated' | 'complete') => {
    setForm((prev) => {
      const updated = [...prev.items];
      updated[index] = { ...updated[index], status };
      return { ...prev, items: updated };
    });
    setIsDirty(true);
  }, []);

  // Assign colony to item
  const handleAssignColony = useCallback((index: number, colonyUUID: string) => {
    setForm((prev) => {
      const updated = [...prev.items];
      updated[index] = { ...updated[index], assignedColonyUUID: colonyUUID || undefined };
      return { ...prev, items: updated };
    });
    setIsDirty(true);
  }, []);

  const handleSave = useCallback(async () => {
    const itemsPayload = form.items.map((item) => ({
      blueprintUUID: item.blueprintUUID,
      blueprintName: item.blueprintName,
      quantity: item.quantity,
      status: item.status,
      assignedColonyUUID: item.assignedColonyUUID,
    }));

    const data = { name: form.name, items: itemsPayload };

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

  if (isLoading) return <LoadingSpinner message="Loading build plans..." />;
  if (isError) return <RetryableError message="Failed to load build plans." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Build Plans</h2>
      </div>
      {(!plans || plans.length === 0) ? (
        <EmptyState title="No build plans" message="Create a new build plan to get started." />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {plans.map((p) => (
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
                <div className="text-xs text-gray-500">
                  {p.items?.length ?? 0} item{(p.items?.length ?? 0) !== 1 ? 's' : ''}
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
            {isNewMode ? 'New Build Plan' : 'Build Plan Details'}
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
        <EmptyState title="No plan selected" message="Select a build plan from the list or create a new one." />
      ) : (
        <div className="flex-1 overflow-y-auto p-4">
          <div className="space-y-6">
            {/* Plan name */}
            <div className="max-w-md">
              <label htmlFor="plan-name" className="mb-1 block text-sm text-gray-400">Plan Name</label>
              <input
                id="plan-name"
                type="text"
                value={form.name}
                onChange={(e) => handleNameChange(e.target.value)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              />
            </div>

            {/* Add item section */}
            <div>
              <h3 className="mb-2 text-sm font-semibold text-gray-300">Add Item</h3>
              <div className="flex items-end gap-3">
                <div className="flex-1">
                  <label htmlFor="add-blueprint" className="mb-1 block text-xs text-gray-400">Blueprint</label>
                  <FilteredDropdown
                    options={blueprintOptions}
                    value={addBlueprintUUID}
                    onChange={setAddBlueprintUUID}
                    placeholder="Select blueprint..."
                  />
                </div>
                <div className="w-24">
                  <label htmlFor="add-quantity" className="mb-1 block text-xs text-gray-400">Quantity</label>
                  <input
                    id="add-quantity"
                    type="number"
                    min={1}
                    value={addQuantity}
                    onChange={(e) => setAddQuantity(Math.max(1, parseInt(e.target.value, 10) || 1))}
                    className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
                  />
                </div>
                <button
                  onClick={handleAddItem}
                  disabled={!addBlueprintUUID}
                  className="rounded bg-indigo-600 px-4 py-2 text-sm text-white hover:bg-indigo-700 disabled:opacity-50"
                >
                  Add
                </button>
              </div>
            </div>

            {/* Items table */}
            <div>
              <h3 className="mb-2 text-sm font-semibold text-gray-300">
                Items ({form.items.length})
              </h3>
              {form.items.length === 0 ? (
                <p className="text-sm text-gray-500">No items in this plan yet.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead>
                      <tr className="border-b border-gray-700 text-gray-400">
                        <th className="px-3 py-2">Blueprint</th>
                        <th className="px-3 py-2">Qty</th>
                        <th className="px-3 py-2">Status</th>
                        <th className="px-3 py-2">Colony</th>
                        <th className="px-3 py-2">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {form.items.map((item, idx) => (
                        <tr key={item.uuid} className="border-b border-gray-800 text-gray-300">
                          <td className="px-3 py-2">{item.blueprintName}</td>
                          <td className="px-3 py-2">{item.quantity}</td>
                          <td className="px-3 py-2">
                            <StatusBadge status={item.status} />
                          </td>
                          <td className="px-3 py-2">
                            {item.assignedColonyUUID
                              ? colonyNameLookup.get(item.assignedColonyUUID) ?? '(unknown)'
                              : '—'}
                          </td>
                          <td className="px-3 py-2">
                            <div className="flex flex-wrap gap-1">
                              {item.status === 'pending' && (
                                <button
                                  onClick={() => handleMarkStatus(idx, 'allocated')}
                                  className="rounded bg-yellow-700 px-2 py-0.5 text-xs text-white hover:bg-yellow-600"
                                >
                                  Allocate
                                </button>
                              )}
                              {item.status !== 'complete' && (
                                <button
                                  onClick={() => handleMarkStatus(idx, 'complete')}
                                  className="rounded bg-green-700 px-2 py-0.5 text-xs text-white hover:bg-green-600"
                                >
                                  Complete
                                </button>
                              )}
                              <button
                                onClick={() => handleRemoveItem(idx)}
                                className="rounded bg-red-800 px-2 py-0.5 text-xs text-white hover:bg-red-700"
                              >
                                Remove
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

