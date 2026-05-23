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
 * Uses the /shared-with-me/{dataType} endpoint grouped by owner.
 * TODO(task 5.2): Full restructure with per-type tabs and grouped display.
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

      <div className="flex-1 overflow-y-auto" role="tabpanel">
        {isLoading && <LoadingSpinner message={`Loading shared ${activeTab}...`} />}
        {isError && (
          <RetryableError
            message={`Failed to load shared ${activeTab}.`}
            onRetry={() => void refetch()}
          />
        )}
        {!isLoading && !isError && (!data || data.length === 0) && (
          <EmptyState
            title="No shared data"
            message="No data has been shared with you yet."
          />
        )}
        {!isLoading && !isError && data && data.length > 0 && (
          <ul className="divide-y divide-gray-700">
            {data.map((group) => (
              <li key={group.ownerCharacterUUID} className="px-4 py-3">
                <div className="text-sm font-medium text-gray-200">
                  {group.ownerCharacterName}
                </div>
                <div className="text-xs text-gray-500">
                  {group.entities.length} item{group.entities.length !== 1 ? 's' : ''}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
