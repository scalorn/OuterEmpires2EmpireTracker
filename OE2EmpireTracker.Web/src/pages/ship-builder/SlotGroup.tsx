import type { ComponentSlot } from './slotTypes';
import { SlotItem } from './SlotItem';

interface SlotGroupProps {
  label: string;
  slots: ComponentSlot[];
}

export function SlotGroup({ label, slots }: SlotGroupProps) {
  return (
    <div>
      <h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-gray-400">
        {label}
      </h3>
      <div className="space-y-1">
        {slots.map((slot) => (
          <SlotItem key={`${slot.slotType}-${slot.slotIndex}`} slot={slot} />
        ))}
      </div>
    </div>
  );
}
