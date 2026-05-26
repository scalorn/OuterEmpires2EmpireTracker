import { useShipBuilderStore } from './shipBuilderStore';
import { SLOT_GROUPS } from './slotTypes';
import { SlotGroup } from './SlotGroup';

export function SlotGrid() {
  const slots = useShipBuilderStore((s) => s.slots);

  return (
    <div className="space-y-4">
      {SLOT_GROUPS.map((group) => {
        const groupSlots = slots.filter((slot) => group.slotTypes.includes(slot.slotType));
        if (groupSlots.length === 0) return null;
        return <SlotGroup key={group.label} label={group.label} slots={groupSlots} />;
      })}
    </div>
  );
}
