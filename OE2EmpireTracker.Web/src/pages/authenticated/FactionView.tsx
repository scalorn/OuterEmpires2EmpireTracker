import { useAuthStore } from '../../auth/store';
import { useFaction, useFactionMembers, useFactionSharedData } from '../../api/hooks/useFaction';
import { useCharacter } from '../../api/hooks/useCharacters';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { DataTable, type Column } from '../../components/common/DataTable';

const memberColumns: Column<Record<string, unknown>>[] = [
  { key: 'name', header: 'Name' },
  { key: 'role', header: 'Role' },
];

export function FactionView() {
  const { characterUUID } = useAuthStore();
  const { data: character } = useCharacter(characterUUID);
  const factionUUID = (character as Record<string, unknown> | undefined)?.factionUUID as string | null ?? null;

  const { data: faction, isLoading: factionLoading, isError: factionError, refetch: refetchFaction } = useFaction(factionUUID);
  const { data: members, isLoading: membersLoading } = useFactionMembers(factionUUID);
  const { data: sharedBlueprints } = useFactionSharedData(factionUUID, 'Blueprints');
  const { data: sharedSurveys } = useFactionSharedData(factionUUID, 'Surveys');

  if (!factionUUID) {
    return <EmptyState title="No Faction" message="You are not a member of any faction." />;
  }

  if (factionLoading || membersLoading) return <LoadingSpinner message="Loading faction..." />;
  if (factionError) return <RetryableError message="Failed to load faction." onRetry={() => void refetchFaction()} />;

  const factionData = faction as Record<string, unknown> | undefined;
  const memberList = (Array.isArray(members) ? (members as unknown[]) : []) as Record<string, unknown>[];
  const sharedBP = (Array.isArray(sharedBlueprints) ? sharedBlueprints : []) as Record<string, unknown>[];
  const sharedSV = (Array.isArray(sharedSurveys) ? sharedSurveys : []) as Record<string, unknown>[];

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold text-white">
        Faction: {String(factionData?.name ?? 'Unknown')}
      </h1>

      <div className="mb-6 rounded border border-gray-700 p-4">
        <h2 className="mb-3 text-lg font-semibold text-gray-200">Members ({memberList.length})</h2>
        {memberList.length === 0 ? (
          <p className="text-sm text-gray-500">No members found.</p>
        ) : (
          <DataTable
            data={memberList}
            columns={memberColumns}
            keyExtractor={(item) => String(item.uuid ?? Math.random())}
          />
        )}
      </div>

      {sharedBP.length > 0 && (
        <div className="mb-6 rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">
            Shared Blueprints ({sharedBP.length})
          </h2>
          <ul className="space-y-1 text-sm">
            {sharedBP.map((bp, i) => (
              <li key={i} className="flex justify-between border-b border-gray-700 py-1">
                <span className="text-gray-300">{String(bp.Name ?? bp.name ?? 'Unknown')}</span>
                <span className="text-xs text-gray-500">
                  {String(bp.UUID ?? bp.uuid) === characterUUID ? '(yours)' : '(shared)'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {sharedSV.length > 0 && (
        <div className="mb-6 rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">
            Shared Surveys ({sharedSV.length})
          </h2>
          <ul className="space-y-1 text-sm">
            {sharedSV.map((sv, i) => (
              <li key={i} className="flex justify-between border-b border-gray-700 py-1">
                <span className="text-gray-300">
                  {String(sv.System ?? sv.system ?? 'Unknown')} — {String(sv.Planet ?? sv.planet ?? '')}
                </span>
                <span className="text-xs text-gray-500">
                  {String(sv.UUID ?? sv.uuid) === characterUUID ? '(yours)' : '(shared)'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {sharedBP.length === 0 && sharedSV.length === 0 && (
        <EmptyState title="No shared data" message="No faction members have shared data yet." />
      )}
    </div>
  );
}
