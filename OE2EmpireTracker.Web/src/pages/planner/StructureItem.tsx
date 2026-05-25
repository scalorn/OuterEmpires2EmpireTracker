import type { PlannedStructure, StructureState } from './computeColonyStatus';

interface StructureItemProps {
  structure: PlannedStructure;
  onStateChange: (id: string, state: StructureState) => void;
  onRemove: (id: string) => void;
  onMoveUp: (id: string) => void;
  onMoveDown: (id: string) => void;
  isMoveUpDisabled: boolean;
  isMoveDownDisabled: boolean;
}

const STATE_STYLES: Record<StructureState, { border: string; badge: string; label: string }> = {
  Online: {
    border: 'border-green-500',
    badge: 'bg-green-900 text-green-300',
    label: 'Online',
  },
  Built: {
    border: 'border-amber-500',
    badge: 'bg-amber-900 text-amber-300',
    label: 'Built',
  },
  Staged: {
    border: 'border-gray-600',
    badge: 'bg-gray-700 text-gray-400',
    label: 'Staged',
  },
};

const STATES: StructureState[] = ['Staged', 'Built', 'Online'];

export function StructureItem({ structure, onStateChange, onRemove, onMoveUp, onMoveDown, isMoveUpDisabled, isMoveDownDisabled }: StructureItemProps) {
  const style = STATE_STYLES[structure.state];

  return (
    <div
      className={`rounded border-l-4 ${style.border} bg-gray-800 px-3 py-2`}
      role="listitem"
      aria-label={`${structure.name} - ${structure.state}`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0 flex-1">
          <p className="truncate font-semibold text-white">{structure.name}</p>
          <p className="text-xs text-gray-400">{structure.subType}</p>
        </div>
        <span className={`shrink-0 rounded px-2 py-0.5 text-xs font-medium ${style.badge}`}>
          {style.label}
        </span>
      </div>

      <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-gray-300">
        <span>Power Provided: <span className="text-white">{structure.properties.powerProvided}</span></span>
        <span>Power Required: <span className="text-white">{structure.properties.powerRequired}</span></span>
        <span>Workers: <span className="text-white">{structure.properties.workerSlots}</span></span>
        <span>Food Provision: <span className="text-white">{structure.properties.foodProvision}</span></span>
      </div>

      <div className="mt-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="inline-flex rounded border border-gray-600" role="group" aria-label="Reorder">
            <button
              type="button"
              onClick={() => onMoveUp(structure.id)}
              disabled={isMoveUpDisabled}
              className="px-2 py-1 text-xs font-medium transition-colors first:rounded-l last:rounded-r bg-gray-800 text-gray-400 hover:bg-gray-700 hover:text-gray-200 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-gray-800 disabled:hover:text-gray-400"
              aria-label="Move up"
            >
              ▲
            </button>
            <button
              type="button"
              onClick={() => onMoveDown(structure.id)}
              disabled={isMoveDownDisabled}
              className="px-2 py-1 text-xs font-medium transition-colors first:rounded-l last:rounded-r bg-gray-800 text-gray-400 hover:bg-gray-700 hover:text-gray-200 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-gray-800 disabled:hover:text-gray-400"
              aria-label="Move down"
            >
              ▼
            </button>
          </div>
          <div className="inline-flex rounded border border-gray-600" role="group" aria-label="Structure state">
            {STATES.map((s) => (
              <button
                key={s}
                type="button"
                onClick={() => onStateChange(structure.id, s)}
                className={`px-2 py-1 text-xs font-medium transition-colors first:rounded-l last:rounded-r ${
                  structure.state === s
                    ? 'bg-gray-600 text-white'
                    : 'bg-gray-800 text-gray-400 hover:bg-gray-700 hover:text-gray-200'
                }`}
                aria-pressed={structure.state === s}
              >
                {s}
              </button>
            ))}
          </div>
        </div>
        <button
          type="button"
          onClick={() => onRemove(structure.id)}
          className="text-xs text-red-400 hover:text-red-300"
          aria-label={`Remove ${structure.name}`}
        >
          Remove
        </button>
      </div>
    </div>
  );
}
