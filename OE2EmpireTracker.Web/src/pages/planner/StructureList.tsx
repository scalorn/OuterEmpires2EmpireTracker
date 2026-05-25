import { usePlannerStore } from './plannerStore';
import { StructureItem } from './StructureItem';
import { computeDisableFlags } from './computeDisableFlags';

export function StructureList() {
  const structures = usePlannerStore((s) => s.structures);
  const removeStructure = usePlannerStore((s) => s.removeStructure);
  const setStructureState = usePlannerStore((s) => s.setStructureState);
  const moveStructureUp = usePlannerStore((s) => s.moveStructureUp);
  const moveStructureDown = usePlannerStore((s) => s.moveStructureDown);

  const sorted = [...structures].sort((a, b) => a.buildQueuePosition - b.buildQueuePosition);
  const flags = computeDisableFlags(structures);

  return (
    <div>
      <h2 className="mb-3 text-lg font-semibold text-white">Structures</h2>
      {sorted.length === 0 ? (
        <p className="text-sm text-gray-400">
          No structures added yet. Use the dropdown above to add flatpacks.
        </p>
      ) : (
        <div className="space-y-2" role="list">
          {sorted.map((structure, index) => (
            <StructureItem
              key={structure.id}
              structure={structure}
              onStateChange={setStructureState}
              onRemove={removeStructure}
              onMoveUp={moveStructureUp}
              onMoveDown={moveStructureDown}
              isMoveUpDisabled={flags[index].isMoveUpDisabled}
              isMoveDownDisabled={flags[index].isMoveDownDisabled}
            />
          ))}
        </div>
      )}
    </div>
  );
}
