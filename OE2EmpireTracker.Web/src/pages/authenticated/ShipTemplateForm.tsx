import { useState, useMemo, useCallback } from 'react';
import { useShipTemplates, useTemplateMutations } from '../../api/hooks/useShipTemplates';
import { useBlueprints } from '../../api/hooks/useBlueprints';
import { useBaseline } from '../../api/hooks/useBaseline';
import { usePricingPlans } from '../../api/hooks/usePricingPlans';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { ShipStatsPanel } from '../../components/domain/ShipStatsPanel';
import type { ShipTemplate, TemplateSlot, Blueprint, SlotDefinition, PricingPlan } from '../../api/types/domain';

/** Slot type to blueprint type mapping for filtering compatible blueprints */
const SLOT_TYPE_TO_BLUEPRINT_TYPE: Record<string, string> = {
  reactors: 'Reactor',
  drives: 'Drive',
  weapons: 'Weapon',
  cargo: 'Cargo',
  shields: 'Shield',
};

/** Compute ship stats from assigned blueprints */
function computeStats(
  slots: TemplateSlot[],
  blueprints: Blueprint[],
): { mass: number; powerBalance: number; cargoCapacity: number; defenceRating: number; propulsion: number } {
  let mass = 0;
  let powerBalance = 0;
  let cargoCapacity = 0;
  let defenceRating = 0;
  let propulsion = 0;

  for (const slot of slots) {
    if (!slot.blueprintUUID) continue;
    const bp = blueprints.find((b) => b.uuid === slot.blueprintUUID);
    if (!bp) continue;

    const props = bp.properties ?? {};
    mass += props['Mass'] ?? props['mass'] ?? 0;
    powerBalance += props['Power'] ?? props['power'] ?? 0;
    cargoCapacity += props['Cargo'] ?? props['cargo'] ?? 0;
    defenceRating += props['Defence'] ?? props['defence'] ?? 0;
    propulsion += props['Propulsion'] ?? props['propulsion'] ?? 0;
  }

  return { mass, powerBalance, cargoCapacity, defenceRating, propulsion };
}

/** Compute total estimated build cost from a pricing plan and assigned blueprints */
function computeBuildCost(
  slots: TemplateSlot[],
  blueprints: Blueprint[],
  pricingPlan: PricingPlan | null,
): number {
  if (!pricingPlan) return 0;
  let total = 0;

  for (const slot of slots) {
    if (!slot.blueprintUUID) continue;
    const bp = blueprints.find((b) => b.uuid === slot.blueprintUUID);
    if (!bp) continue;

    const planItem = pricingPlan.items.find(
      (item) => item.itemName.toLowerCase() === (bp.nickName || bp.name || '').toLowerCase(),
    );
    if (planItem) {
      total += planItem.unitPrice;
    }
  }

  return total;
}

/** Expand slot definitions into individual slot entries */
function expandSlotDefs(slotDefs: SlotDefinition[]): { slotType: string; slotIndex: number }[] {
  const result: { slotType: string; slotIndex: number }[] = [];
  for (const def of slotDefs) {
    for (let i = 0; i < def.count; i++) {
      result.push({ slotType: def.slotType, slotIndex: i });
    }
  }
  return result;
}

export function ShipTemplateForm() {
  const { characterUUID } = useAuthStore();
  const { data: templates, isLoading, isError, refetch } = useShipTemplates(characterUUID);
  const { data: blueprints } = useBlueprints(characterUUID);
  const { data: baseline } = useBaseline();
  const { data: pricingPlans } = usePricingPlans(characterUUID);
  const { save, remove, orderBuild } = useTemplateMutations();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editHull, setEditHull] = useState('');
  const [editSlots, setEditSlots] = useState<TemplateSlot[]>([]);
  const [selectedPricingPlanId, setSelectedPricingPlanId] = useState('');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const templateList = useMemo(() => (Array.isArray(templates) ? templates : []) as ShipTemplate[], [templates]);
  const blueprintList = useMemo(() => (Array.isArray(blueprints) ? blueprints : []) as Blueprint[], [blueprints]);
  const shipClasses = useMemo(() => baseline?.shipClasses ?? [], [baseline]);
  const pricingPlanList = useMemo(() => (Array.isArray(pricingPlans) ? pricingPlans : []) as PricingPlan[], [pricingPlans]);

  const selectedHullDef = useMemo(
    () => shipClasses.find((sc) => sc.name === editHull) ?? null,
    [shipClasses, editHull],
  );

  const expandedSlots = useMemo(
    () => (selectedHullDef ? expandSlotDefs(selectedHullDef.slots) : []),
    [selectedHullDef],
  );

  // Compute live stats from current slot assignments
  const stats = useMemo(
    () => computeStats(editSlots, blueprintList),
    [editSlots, blueprintList],
  );

  // Compute total estimated build cost from selected pricing plan
  const selectedPricingPlan = useMemo(
    () => pricingPlanList.find((p) => p.uuid === selectedPricingPlanId) ?? null,
    [pricingPlanList, selectedPricingPlanId],
  );

  const totalBuildCost = useMemo(
    () => computeBuildCost(editSlots, blueprintList, selectedPricingPlan),
    [editSlots, blueprintList, selectedPricingPlan],
  );

  // Select a template and populate edit state
  const handleSelect = useCallback(
    (uuid: string) => {
      setSelectedId(uuid);
      const tmpl = templateList.find((t) => t.uuid === uuid);
      if (tmpl) {
        setEditName(tmpl.name);
        setEditHull(tmpl.hullShipClass);
        setEditSlots(tmpl.slots ?? []);
      }
    },
    [templateList],
  );

  // Handle hull change - reset slots when hull changes
  const handleHullChange = useCallback(
    (hullName: string) => {
      setEditHull(hullName);
      const hullDef = shipClasses.find((sc) => sc.name === hullName);
      if (hullDef) {
        const newSlots: TemplateSlot[] = expandSlotDefs(hullDef.slots).map((s) => ({
          slotType: s.slotType,
          slotIndex: s.slotIndex,
          blueprintUUID: undefined,
        }));
        setEditSlots(newSlots);
      } else {
        setEditSlots([]);
      }
    },
    [shipClasses],
  );

  // Handle slot blueprint assignment
  const handleSlotAssign = useCallback(
    (slotType: string, slotIndex: number, blueprintUUID: string) => {
      setEditSlots((prev) =>
        prev.map((s) =>
          s.slotType === slotType && s.slotIndex === slotIndex
            ? { ...s, blueprintUUID: blueprintUUID || undefined }
            : s,
        ),
      );
    },
    [],
  );

  // Get compatible blueprints for a given slot type
  const getCompatibleBlueprints = useCallback(
    (slotType: string) => {
      const bpType = SLOT_TYPE_TO_BLUEPRINT_TYPE[slotType.toLowerCase()] ?? slotType;
      return blueprintList.filter(
        (bp) => bp.blueprintType.toLowerCase() === bpType.toLowerCase(),
      );
    },
    [blueprintList],
  );

  // Save handler
  const handleSave = () => {
    if (!editName.trim()) return;
    save.mutate({
      entityUUID: selectedId ?? undefined,
      data: {
        name: editName,
        hullBlueprintUUID: editHull,
        components: editSlots
          .filter((s) => s.blueprintUUID)
          .map((s) => ({
            slotType: s.slotType,
            slotIndex: s.slotIndex,
            blueprintUUID: s.blueprintUUID!,
            currentHP: 0,
            maxHP: 0,
            maxRepairPercent: 0,
          })),
      },
    });
  };

  // New template handler
  const handleNew = () => {
    setSelectedId(null);
    setEditName('');
    setEditHull('');
    setEditSlots([]);
  };

  // Delete handler
  const handleDelete = () => {
    if (!selectedId) return;
    remove.mutate(selectedId, {
      onSuccess: () => {
        setShowDeleteConfirm(false);
        handleNew();
      },
    });
  };

  // Order Build handler
  const handleOrderBuild = () => {
    if (!selectedId) return;
    orderBuild.mutate(selectedId);
  };

  if (isLoading) return <LoadingSpinner message="Loading ship templates..." />;
  if (isError) return <RetryableError message="Failed to load ship templates." onRetry={() => void refetch()} />;

  // Hull dropdown options
  const hullOptions = shipClasses.map((sc) => ({ value: sc.name, label: sc.name }));

  // Pricing plan dropdown options
  const pricingPlanOptions = pricingPlanList.map((p) => ({ value: p.uuid, label: p.name }));

  // Group expanded slots by type for display
  const slotsByType: Record<string, { slotType: string; slotIndex: number }[]> = {};
  for (const slot of expandedSlots) {
    if (!slotsByType[slot.slotType]) slotsByType[slot.slotType] = [];
    slotsByType[slot.slotType].push(slot);
  }

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Ship Templates</h2>
      </div>
      <div className="flex-1 overflow-y-auto">
        {templateList.length === 0 ? (
          <EmptyState title="No templates" message="Create a new ship template to get started." />
        ) : (
          <ul className="divide-y divide-gray-700">
            {templateList.map((tmpl) => (
              <li key={tmpl.uuid}>
                <button
                  onClick={() => handleSelect(tmpl.uuid)}
                  className={`w-full px-3 py-2 text-left text-sm hover:bg-gray-800 ${
                    selectedId === tmpl.uuid ? 'bg-gray-800 text-white' : 'text-gray-300'
                  }`}
                >
                  <div className="font-medium">{tmpl.name || '(unnamed)'}</div>
                  <div className="text-xs text-gray-500">{tmpl.hullShipClass || 'No hull'}</div>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col overflow-y-auto p-4">
      {/* Header with actions */}
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold text-white">
          {selectedId ? 'Edit Template' : 'New Template'}
        </h2>
        <div className="flex gap-2">
          <button
            onClick={handleNew}
            className="rounded bg-gray-600 px-3 py-1.5 text-sm text-white hover:bg-gray-500"
          >
            New
          </button>
          <button
            onClick={handleSave}
            disabled={!editName.trim()}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            Save
          </button>
          {selectedId && (
            <button
              onClick={() => setShowDeleteConfirm(true)}
              className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700"
            >
              Delete
            </button>
          )}
          {selectedId && (
            <button
              onClick={handleOrderBuild}
              disabled={orderBuild.isPending}
              className="rounded bg-green-600 px-3 py-1.5 text-sm text-white hover:bg-green-700 disabled:opacity-50"
            >
              Order Build
            </button>
          )}
        </div>
      </div>

      {/* Template name */}
      <div className="mb-4">
        <label className="mb-1 block text-xs text-gray-400">Template Name</label>
        <input
          type="text"
          value={editName}
          onChange={(e) => setEditName(e.target.value)}
          placeholder="Enter template name"
          className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Hull selection */}
      <div className="mb-4">
        <label className="mb-1 block text-xs text-gray-400">Hull (Ship Class)</label>
        <FilteredDropdown
          options={hullOptions}
          value={editHull}
          onChange={handleHullChange}
          placeholder="Select hull..."
        />
      </div>

      {/* Slot assignment section */}
      {editHull && Object.keys(slotsByType).length > 0 && (
        <div className="mb-4">
          <h3 className="mb-2 text-sm font-semibold text-gray-300">Component Slots</h3>
          <div className="space-y-4">
            {Object.entries(slotsByType).map(([slotType, slotsInGroup]) => {
              const compatible = getCompatibleBlueprints(slotType);
              const dropdownOptions = [
                { value: '', label: '(empty)' },
                ...compatible.map((bp) => ({
                  value: bp.uuid,
                  label: bp.nickName || bp.name || bp.uuid,
                })),
              ];

              return (
                <div key={slotType}>
                  <h4 className="mb-1 text-xs font-medium uppercase tracking-wide text-gray-500">
                    {slotType} ({slotsInGroup.length})
                  </h4>
                  <div className="space-y-1">
                    {slotsInGroup.map((slot) => {
                      const currentSlot = editSlots.find(
                        (s) => s.slotType === slot.slotType && s.slotIndex === slot.slotIndex,
                      );
                      return (
                        <div key={`${slot.slotType}-${slot.slotIndex}`} className="flex items-center gap-2">
                          <span className="w-16 text-xs text-gray-500">
                            Slot {slot.slotIndex + 1}
                          </span>
                          <div className="flex-1">
                            <FilteredDropdown
                              options={dropdownOptions}
                              value={currentSlot?.blueprintUUID ?? ''}
                              onChange={(val) => handleSlotAssign(slot.slotType, slot.slotIndex, val)}
                              placeholder={`Select ${slotType} blueprint...`}
                            />
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Live stats panel */}
      {editHull && (
        <ShipStatsPanel
          mass={stats.mass}
          powerBalance={stats.powerBalance}
          cargoCapacity={stats.cargoCapacity}
          defenceRating={stats.defenceRating}
          propulsion={stats.propulsion}
        />
      )}
