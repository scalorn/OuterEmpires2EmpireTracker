import { useState } from 'react';
import { useAuthStore } from '../../auth/store';
import { useSharing, useSharingMutation } from '../../api/hooks/useSharing';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { SharingRule, SharingTargetType, DataType } from '../../api/types/generated';

const DATA_TYPES: DataType[] = ['Colonies', 'Blueprints', 'Surveys'];
const TARGET_TYPES: SharingTargetType[] = ['Character', 'Faction', 'Public'];

export function SharingConfig() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useSharing(characterUUID);
  const mutation = useSharingMutation();
  const [showEditor, setShowEditor] = useState(false);
  const [editTargetType, setEditTargetType] = useState<SharingTargetType>('Faction');
  const [editTargetUUID, setEditTargetUUID] = useState('');
  const [editDataType, setEditDataType] = useState<DataType | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);

  if (isLoading) return <LoadingSpinner message="Loading sharing rules..." />;
  if (isError) return <RetryableError message="Failed to load sharing rules." onRetry={() => void refetch()} />;

  const rules = (Array.isArray(data) ? data : []) as SharingRule[];

  const isPublicTarget = editTargetType === 'Public';
  const isSaveDisabled = !isPublicTarget && !editTargetUUID.trim();

  const handleAddRule = () => {
    const targetUUID = isPublicTarget ? 'public' : editTargetUUID.trim();
    if (!targetUUID) return;

    const newRule: SharingRule = {
      id: crypto.randomUUID(),
      ownerCharacterUUID: characterUUID ?? '',
      targetType: editTargetType,
      targetUUID,
      dataType: (editDataType as string) ?? '',
      entityUUID: '',
    };
    const updated = [...rules, newRule];
    setMutationError(null);
    mutation.mutate(updated, {
      onSuccess: () => {
        setShowEditor(false);
        setEditTargetUUID('');
        setEditDataType(null);
        setEditTargetType('Faction');
      },
      onError: () => {
        setMutationError('Failed to save sharing rule. Please try again.');
      },
    });
  };

  const handleDeleteRule = (id: string) => {
    const updated = rules.filter((r) => r.id !== id);
    setMutationError(null);
    mutation.mutate(updated, {
      onError: () => {
        setMutationError('Failed to delete sharing rule. Please try again.');
      },
    });
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Sharing Configuration</h1>
        <button
          onClick={() => setShowEditor(!showEditor)}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          {showEditor ? 'Cancel' : 'Add Rule'}
        </button>
      </div>

      {mutationError && (
        <div className="mb-4 rounded border border-red-700 bg-red-900/30 p-3 text-sm text-red-300">
          {mutationError}
        </div>
      )}

      {showEditor && (
        <div className="mb-6 rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">New Sharing Rule</h2>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm text-gray-400">Target Type</label>
              <select
                value={editTargetType}
                onChange={(e) => {
                  setEditTargetType(e.target.value as SharingTargetType);
                  if (e.target.value === 'Public') {
                    setEditTargetUUID('');
                  }
                }}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              >
                {TARGET_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
            {!isPublicTarget && (
              <div>
                <label className="mb-1 block text-sm text-gray-400">Target UUID</label>
                <input
                  type="text"
                  value={editTargetUUID}
                  onChange={(e) => setEditTargetUUID(e.target.value)}
                  placeholder={`${editTargetType} UUID`}
                  className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white placeholder-gray-500"
                />
              </div>
            )}
            <div>
              <label className="mb-1 block text-sm text-gray-400">Data Type</label>
              <select
                value={editDataType ?? ''}
                onChange={(e) => setEditDataType(e.target.value ? e.target.value as DataType : null)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              >
                <option value="">All data types</option>
                {DATA_TYPES.map((dt) => (
                  <option key={dt} value={dt}>{dt}</option>
                ))}
              </select>
            </div>
            <button
              onClick={handleAddRule}
              disabled={isSaveDisabled || mutation.isPending}
              className="rounded bg-green-600 px-4 py-2 text-sm text-white hover:bg-green-700 disabled:opacity-50"
            >
              {mutation.isPending ? 'Saving...' : 'Save Rule'}
            </button>
          </div>
        </div>
      )}

      {rules.length === 0 ? (
        <EmptyState title="No sharing rules" message="Add a rule to share data with others." />
      ) : (
        <ul className="space-y-3">
          {rules.map((rule) => (
            <li key={rule.id} className="flex items-center justify-between rounded border border-gray-700 p-3">
              <div>
                <p className="text-sm text-white">
                  <span className="font-medium">{rule.targetType}</span>
                  {rule.targetType !== 'Public' && (
                    <span className="ml-2 text-gray-400">{rule.targetUUID}</span>
                  )}
                </p>
                <p className="mt-1 text-xs text-gray-400">
                  Sharing: {rule.dataType ?? 'All data types'}
                </p>
              </div>
              <button
                onClick={() => handleDeleteRule(rule.id)}
                disabled={mutation.isPending}
                className="text-sm text-red-400 hover:text-red-300 disabled:opacity-50"
              >
                Delete
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
