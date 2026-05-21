import { useState } from 'react';
import { useAuthStore } from '../../auth/store';
import { useSharing, useSharingMutation } from '../../api/hooks/useSharing';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import type { SharingRule, SharingTargetType, DataType } from '../../api/types/generated';

const DATA_TYPES: DataType[] = ['Colonies', 'Blueprints', 'Surveys', 'Profiles', 'Intel'];
const TARGET_TYPES: SharingTargetType[] = ['Character', 'Faction', 'Public'];

export function SharingConfig() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useSharing(characterUUID);
  const mutation = useSharingMutation();
  const [showEditor, setShowEditor] = useState(false);
  const [editTargetType, setEditTargetType] = useState<SharingTargetType>('Faction');
  const [editTargetUUID, setEditTargetUUID] = useState('');
  const [editDataTypes, setEditDataTypes] = useState<DataType[]>([]);

  if (isLoading) return <LoadingSpinner message="Loading sharing rules..." />;
  if (isError) return <RetryableError message="Failed to load sharing rules." onRetry={() => void refetch()} />;

  const rules = (Array.isArray(data) ? data : []) as SharingRule[];

  const handleAddRule = () => {
    if (!editTargetUUID.trim() && editTargetType !== 'Public') return;
    const newRule: SharingRule = {
      uuid: crypto.randomUUID(),
      ownerCharacterUUID: characterUUID ?? '',
      targetType: editTargetType,
      targetUUID: editTargetType === 'Public' ? 'public' : editTargetUUID.trim(),
      dataTypes: editDataTypes,
      createdAt: new Date().toISOString(),
    };
    const updated = [...rules, newRule];
    mutation.mutate(updated);
    setShowEditor(false);
    setEditTargetUUID('');
    setEditDataTypes([]);
  };

  const handleDeleteRule = (uuid: string) => {
    const updated = rules.filter((r) => r.uuid !== uuid);
    mutation.mutate(updated);
  };

  const toggleDataType = (dt: DataType) => {
    setEditDataTypes((prev) =>
      prev.includes(dt) ? prev.filter((d) => d !== dt) : [...prev, dt]
    );
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

      {showEditor && (
        <div className="mb-6 rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">New Sharing Rule</h2>
          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-sm text-gray-400">Target Type</label>
              <select
                value={editTargetType}
                onChange={(e) => setEditTargetType(e.target.value as SharingTargetType)}
                className="w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white"
              >
                {TARGET_TYPES.map((t) => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>
            {editTargetType !== 'Public' && (
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
              <label className="mb-1 block text-sm text-gray-400">Data Types</label>
              <div className="flex flex-wrap gap-2">
                {DATA_TYPES.map((dt) => (
                  <button
                    key={dt}
                    onClick={() => toggleDataType(dt)}
                    className={`rounded px-3 py-1 text-sm ${
                      editDataTypes.includes(dt)
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-700 text-gray-300'
                    }`}
                  >
                    {dt}
                  </button>
                ))}
              </div>
            </div>
            <button
              onClick={handleAddRule}
              disabled={editDataTypes.length === 0}
              className="rounded bg-green-600 px-4 py-2 text-sm text-white hover:bg-green-700 disabled:opacity-50"
            >
              Save Rule
            </button>
          </div>
        </div>
      )}

      {rules.length === 0 ? (
        <EmptyState title="No sharing rules" message="Add a rule to share data with others." />
      ) : (
        <ul className="space-y-3">
          {rules.map((rule) => (
            <li key={rule.uuid} className="flex items-center justify-between rounded border border-gray-700 p-3">
              <div>
                <p className="text-sm text-white">
                  <span className="font-medium">{rule.targetType}</span>
                  {rule.targetType !== 'Public' && (
                    <span className="ml-2 text-gray-400">{rule.targetUUID}</span>
                  )}
                </p>
                <p className="mt-1 text-xs text-gray-400">
                  Sharing: {rule.dataTypes.join(', ')}
                </p>
              </div>
              <button
                onClick={() => handleDeleteRule(rule.uuid)}
                className="text-sm text-red-400 hover:text-red-300"
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
