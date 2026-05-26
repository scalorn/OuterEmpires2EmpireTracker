import { useMemo } from 'react';
import type { ComponentSlot } from './slotTypes';
import { SLOT_TYPE_DISPLAY_NAMES, getCompatibleBlueprintTypes } from './slotTypes';
import { useShipBuilderStore } from './shipBuilderStore';
import { FilteredDropdown } from '../../components/common/FilteredDropdown';

interface SlotItemProps {
  slot: ComponentSlot;
}

export function SlotItem({ slot }: SlotItemProps) {
  const blueprintList = useShipBuilderStore((s) => s.blueprintList);
  const hullDetail = useShipBuilderStore((s) => s.hullDetail);
  const installComponent = useShipBuilderStore((s) => s.installComponent);
  const slotErrors = useShipBuilderStore((s) => s.slotErrors);
  const loadingUUIDs = useShipBuilderStore((s) => s.loadingUUIDs);

  const slotKey = `${slot.slotType}-${slot.slotIndex}`;
  const displayName = SLOT_TYPE_DISPLAY_NAMES[slot.slotType] ?? slot.slotType;
  const label = `${displayName} ${slot.slotIndex}`;
  const error = slotErrors[slotKey];
  const isLoading = slot.blueprintUUID !== null && loadingUUIDs.has(slot.blueprintUUID);

  const options = useMemo(() => {
    if (!hullDetail) return [];
    const compatibleTypes = getCompatibleBlueprintTypes(slot.slotType);
    const filtered = blueprintList.filter(
      (bp) => compatibleTypes.includes(bp.bluePrintType) && bp.class === hullDetail.class,
    );
    return [
      { value: '', label: '— None —' },
      ...filtered.map((bp) => ({ value: bp.uuid, label: bp.name })),
    ];
  }, [blueprintList, hullDetail, slot.slotType]);

  const handleChange = (value: string) => {
    installComponent(slot.slotType, slot.slotIndex, value || null);
  };

  return (
    <div className="flex items-center gap-2">
      <span className="w-32 shrink-0 text-sm text-gray-300">{label}</span>
      <div className="flex-1">
        <FilteredDropdown
          options={options}
          value={slot.blueprintUUID ?? ''}
          onChange={handleChange}
          placeholder={isLoading ? 'Loading...' : 'Select component...'}
        />
      </div>
      {error && (
        <span className="shrink-0 text-xs text-red-400" title={error}>⚠</span>
      )}
      {isLoading && (
        <span className="shrink-0 text-xs text-blue-400">⏳</span>
      )}
    </div>
  );
}
