import { useState } from 'react';
import { useAuthStore } from '../../auth/store';
import { useSharedWithMe } from '../../api/hooks/useSharedData';
import { TabBar } from '../../components/common/TabBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

const DATA_TYPES = [
  { key: 'Blueprints', label: 'Blueprints' },
  { key: 'Surveys', label: 'Surveys' },
  { key: 'Colonies', label: 'Colonies' },
];

/**
 * Shared Data View — displays data shared with the current character.
 * Top-level tabs: Blueprints | Surveys | Colonies.
 * Per tab: fetches /shared-with-me/{dataType} and displays entities grouped
 * by ownerCharacterName.
 */
export function SharedDataView() {
  const { characterUUID } = useAuthStore();
  const [activeTab, setActiveTab] = useState<string>('Blueprints');

  const { data, isLoading, isError, refetch } = useSharedWithMe(characterUUID, activeTab);

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-gray-700 p-3">
        <h2 className="text-lg font-semibold text-white">Shared Data</h2>
      </div>

      <TabBar tabs={DATA_TYPES} activeTab={activeTab} onTabChange={setActiveTab} />

      <div
        className="flex-1 overflow-y-auto"
        role="tabpanel"
        id={`tabpanel-${activeTab}`}
        aria-labelledby={`tab-${activeTab}`}
      >
        {isLoading && <LoadingSpinner message={`Loading shared ${activeTab.toLowerCase()}...`} />}

        {isError && (
          <RetryableError
            message={`Failed to load shared ${activeTab.toLowerCase()}.`}
            onRetry={() => void refetch()}
          />
        )}

        {!isLoading && !isError && (!data || data.length === 0) && (
          <EmptyState message="No data has been shared with you yet." />
        )}

        {!isLoading && !isError && data && data.length > 0 && (
          <div className="divide-y divide-gray-700">
            {data.map((group) => (
              <section key={group.ownerCharacterUUID} className="px-4 py-3">
                <h3 className="mb-2 text-sm font-medium text-gray-200">
                  {group.ownerCharacterName}
                </h3>
                {group.entities.length === 0 ? (
                  <p className="text-xs text-gray-500">No items</p>
                ) : (
                  <ul className="space-y-1">
                    {group.entities.map((entity, idx) => (
                      <li
                        key={getEntityKey(entity, idx)}
                        className="rounded bg-gray-800 px-3 py-2 text-xs text-gray-300"
                      >
                        {getEntityDisplayName(entity)}
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/** Extract a stable key from an entity object, falling back to index. */
function getEntityKey(entity: unknown, index: number): string {
  if (entity && typeof entity === 'object') {
    const obj = entity as Record<string, unknown>;
    if (typeof obj.uuid === 'string') return obj.uuid;
    if (typeof obj.id === 'string') return obj.id;
  }
  return String(index);
}

/** Extract a human-readable display name from an entity object. */
function getEntityDisplayName(entity: unknown): string {
  if (entity && typeof entity === 'object') {
    const obj = entity as Record<string, unknown>;
    if (typeof obj.name === 'string') return obj.name;
    if (typeof obj.title === 'string') return obj.title;
    if (typeof obj.uuid === 'string') return obj.uuid;
  }
  return 'Unknown item';
}
