import { useState } from 'react';
import { usePlannerStore } from './plannerStore';
import type { PlannerStructure } from '../../api/types/generated';

export function StructurePanel() {
  const { structures, addStructure, removeStructure } = usePlannerStore();
  const [newUUID, setNewUUID] = useState('');

  const handleAdd = () => {
    if (!newUUID.trim()) return;
    const structure: PlannerStructure = {
      flatpackBlueprintUUID: newUUID.trim(),
      isBuilt: false,
      isStaged: true,
      isOnline: false,
      buildQueueSequence: structures.length + 1,
    };
    addStructure(structure);
    setNewUUID('');
  };

  return (
    <div className="rounded border border-gray-700 p-4">
      <h2 className="mb-3 text-lg font-semibold text-gray-200">Structures</h2>

      <div className="mb-4 flex gap-2">
        <input
          type="text"
          value={newUUID}
          onChange={(e) => setNewUUID(e.target.value)}
          placeholder="Flatpack Blueprint UUID"
          className="flex-1 rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500"
          onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
        />
        <button
          onClick={handleAdd}
          className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700"
        >
          Add
        </button>
      </div>

      {structures.length === 0 ? (
        <p className="text-sm text-gray-500">No structures added yet.</p>
      ) : (
        <ul className="space-y-2">
          {structures.map((s, i) => (
            <li key={i} className="flex items-center justify-between rounded bg-gray-800 px-3 py-2">
              <div>
                <p className="text-sm text-white">{s.flatpackBlueprintUUID}</p>
                <p className="text-xs text-gray-400">
                  {s.isBuilt ? 'Built' : s.isStaged ? 'Staged' : 'Queued'}
                  {s.isOnline ? ' • Online' : ''}
                </p>
              </div>
              <button
                onClick={() => removeStructure(i)}
                className="text-sm text-red-400 hover:text-red-300"
              >
                Remove
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
