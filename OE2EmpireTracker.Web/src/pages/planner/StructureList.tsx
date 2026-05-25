import { usePlannerStore } from './plannerStore';
import { StructureItem } from './StructureItem';

export function StructureList() {
  const structures = usePlannerStore((s) => s.structures);
  const removeStructure = usePlannerStore((s) => s.removeStructure);
  const setStructureState = usePlannerStore((s) => s.setStructureState);

  return (
    <div>
      <h2 className="mb-3 text-lg font-semibold text-white">Structures</h2>
      {structures.length === 0 ? (
        <p className="text-sm text-gray-400">
          No structures added yet. Use the dropdown above to add flatpacks.
        </p>
      ) : (
        <div className="space-y-2" role="list">
          {structures.map((structure) => (
            <StructureItem
              key={structure.id}
              structure={structure}
              onStateChange={setStructureState}
              onRemove={removeStructure}
            />
          ))}
        </div>
      )}
    </div>
  );
}
