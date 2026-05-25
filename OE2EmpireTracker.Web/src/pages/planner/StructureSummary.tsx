import { usePlannerStore } from './plannerStore';

/**
 * Extracts the top-level group from a sub-type string.
 * e.g. "CommodityFactory/Agridome" → "CommodityFactory"
 *      "MiningRig" → "MiningRig"
 */
function getGroupName(subType: string): string {
  const slashIndex = subType.indexOf('/');
  return slashIndex === -1 ? subType : subType.substring(0, slashIndex);
}

export function StructureSummary() {
  const structures = usePlannerStore((s) => s.structures);

  if (structures.length === 0) return null;

  const groupCounts: Record<string, number> = {};
  for (const structure of structures) {
    const group = getGroupName(structure.subType);
    groupCounts[group] = (groupCounts[group] ?? 0) + 1;
  }

  const sortedGroups = Object.entries(groupCounts).sort(([a], [b]) =>
    a.localeCompare(b)
  );

  return (
    <div className="rounded border border-gray-700 p-4">
      <h2 className="mb-2 text-lg font-semibold text-gray-200">Summary</h2>
      <p className="mb-1 text-sm text-gray-300">
        Total: {structures.length} structure{structures.length !== 1 ? 's' : ''}
      </p>
      <ul className="space-y-0.5 text-sm text-gray-400">
        {sortedGroups.map(([group, count]) => (
          <li key={group}>
            {group}: {count}
          </li>
        ))}
      </ul>
    </div>
  );
}
