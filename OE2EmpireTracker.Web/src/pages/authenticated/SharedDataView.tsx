import { useState, useCallback } from 'react';
import { useAuthStore } from '../../auth/store';
import { useSharedCharacters, useSharedEntityData } from '../../api/hooks/useSharedData';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { TabBar } from '../../components/common/TabBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { SharedDataSummary } from '../../api/types/domain';

const TAB_LABELS: Record<string, string> = {
  blueprints: 'Blueprints',
  surveys: 'Surveys',
  colonies: 'Colonies',
};

function SharedBadge() {
  return (
    <span className="inline-flex items-center rounded-full bg-purple-600/30 px-2 py-0.5 text-xs font-medium text-purple-300">
      Shared
    </span>
  );
}

interface SharedEntityListProps {
  charUUID: string;
  sharerUUID: string;
  dataType: string;
}

function SharedEntityList({ charUUID, sharerUUID, dataType }: SharedEntityListProps) {
  const { data, isLoading, isError, refetch } = useSharedEntityData(charUUID, sharerUUID, dataType);

  if (isLoading) return <LoadingSpinner message={`Loading shared ${dataType}...`} />;
  if (isError) return <RetryableError message={`Failed to load shared ${dataType}.`} onRetry={() => void refetch()} />;
  if (!data || data.length === 0) {
    return <EmptyState title={`No shared ${dataType}`} message={`This character has not shared any ${dataType} with you.`} />;
  }

  return (
    <ul className="divide-y divide-gray-700">
      {(data as Record<string, unknown>[]).map((entity, index) => {
        const name = (entity.name ?? entity.colonyName ?? entity.planetName ?? `Item ${index + 1}`) as string;
        const subtitle = (entity.systemName ?? entity.blueprintType ?? entity.surveyType ?? '') as string;
        return (
          <li key={(entity.uuid as string) ?? index} className="flex items-center justify-between px-4 py-3">
            <div>
              <div className="text-sm font-medium text-gray-200">{name}</div>
              {subtitle && <div className="text-xs text-gray-500">{subtitle}</div>}
            </div>
            <SharedBadge />
          </li>
        );
      })}
    </ul>
  );
}

export function SharedDataView() {
  const { characterUUID } = useAuthStore();
  const { data: characters, isLoading, isError, refetch } = useSharedCharacters(characterUUID);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<string>('');

  const selectedCharacter = characters?.find((c) => c.characterUUID === selectedId) ?? null;

  const handleSelect = useCallback((char: SharedDataSummary) => {
    setSelectedId(char.characterUUID);
    setActiveTab(char.sharedTypes[0] ?? '');
  }, []);

  const handleBack = useCallback(() => {
    setSelectedId(null);
    setActiveTab('');
  }, []);

  if (isLoading) return <LoadingSpinner message="Loading shared data..." />;
  if (isError) return <RetryableError message="Failed to load shared data." onRetry={() => void refetch()} />;

  const tabs = (selectedCharacter?.sharedTypes ?? []).map((t) => ({
    key: t,
    label: TAB_LABELS[t] ?? t,
  }));

  const listPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Shared Characters</h2>
      </div>
      {(!characters || characters.length === 0) ? (
        <EmptyState title="No shared data" message="No faction members have shared data with you yet." />
      ) : (
        <ul className="flex-1 overflow-y-auto">
          {characters.map((char) => (
            <li key={char.characterUUID}>
              <button
                onClick={() => handleSelect(char)}
                className={[
                  'w-full px-4 py-3 text-left transition-colors',
                  selectedId === char.characterUUID
                    ? 'bg-blue-900/40 text-white'
                    : 'text-gray-300 hover:bg-gray-800',
                ].join(' ')}
              >
                <div className="flex items-center gap-2">
                  <span className="font-medium">{char.characterName}</span>
                  <SharedBadge />
                </div>
                <div className="text-xs text-gray-500">{char.faction}</div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );

  const detailPanel = (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">
          {selectedCharacter ? `${selectedCharacter.characterName}'s Shared Data` : 'Shared Data'}
        </h2>
      </div>

      {!selectedCharacter ? (
        <EmptyState title="No character selected" message="Select a character from the list to view their shared data." />
      ) : (
        <div className="flex flex-1 flex-col overflow-hidden">
          <TabBar tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />
          <div
            className="flex-1 overflow-y-auto"
            role="tabpanel"
            id={`tabpanel-${activeTab}`}
            aria-labelledby={`tab-${activeTab}`}
          >
            {activeTab && characterUUID && (
              <SharedEntityList
                charUUID={characterUUID}
                sharerUUID={selectedCharacter.characterUUID}
                dataType={activeTab}
              />
            )}
          </div>
        </div>
      )}
    </div>
  );

  return (
    <MasterDetailLayout
      listPanel={listPanel}
      detailPanel={detailPanel}
      selectedId={selectedId}
      onBack={handleBack}
    />
  );
}
