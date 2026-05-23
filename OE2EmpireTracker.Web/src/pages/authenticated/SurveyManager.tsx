import { useState } from 'react';
import { useSurveys, useSurveyMutations } from '../../api/hooks/useSurveys';
import { useAuthStore } from '../../auth/store';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

const baseColumns: Column<Record<string, unknown>>[] = [
  { key: 'systemName', header: 'System' },
  { key: 'planetName', header: 'Planet' },
  { key: 'surveyType', header: 'Type' },
];

export function SurveyManager() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useSurveys(characterUUID);
  const { save: createOrUpdate, remove } = useSurveyMutations();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editSystem, setEditSystem] = useState('');

  if (isLoading) return <LoadingSpinner message="Loading surveys..." />;
  if (isError) return <RetryableError message="Failed to load surveys." onRetry={() => void refetch()} />;

  const surveys = (Array.isArray(data) ? data : []) as unknown as Record<string, unknown>[];

  const handleCreate = () => {
    const uuid = crypto.randomUUID();
    createOrUpdate.mutate({
      entityUUID: uuid,
      data: { systemName: 'New System', planetName: 'Unknown', surveyType: 'Planet', resources: [] },
    });
  };

  const handleDelete = (uuid: string) => {
    if (confirm('Delete this survey?')) {
      remove.mutate(uuid);
    }
  };

  const handleSaveEdit = (uuid: string) => {
    const survey = surveys.find((s) => String(s.UUID ?? s.uuid) === uuid);
    if (survey) {
      createOrUpdate.mutate({ entityUUID: uuid, data: { systemName: editSystem } });
    }
    setEditingId(null);
  };

  const actionColumns: Column<Record<string, unknown>>[] = [
    ...baseColumns,
    {
      key: '_actions',
      header: 'Actions',
      sortable: false,
      render: (item) => {
        const uuid = String(item.UUID ?? item.uuid);
        if (editingId === uuid) {
          return (
            <div className="flex gap-2">
              <input
                type="text"
                value={editSystem}
                onChange={(e) => setEditSystem(e.target.value)}
                className="rounded border border-gray-600 bg-gray-700 px-2 py-1 text-xs text-white"
              />
              <button onClick={() => handleSaveEdit(uuid)} className="text-xs text-green-400">Save</button>
              <button onClick={() => setEditingId(null)} className="text-xs text-gray-400">Cancel</button>
            </div>
          );
        }
        return (
          <div className="flex gap-2">
            <button
              onClick={() => { setEditingId(uuid); setEditSystem(String(item.System ?? '')); }}
              className="text-xs text-blue-400 hover:underline"
            >Edit</button>
            <button onClick={() => handleDelete(uuid)} className="text-xs text-red-400 hover:underline">Delete</button>
          </div>
        );
      },
    },
  ];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Survey Manager</h1>
        <button onClick={handleCreate} className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700">
          Create Survey
        </button>
      </div>
      {surveys.length === 0 ? (
        <EmptyState title="No surveys" message="Create your first survey to get started." />
      ) : (
        <DataTable
          data={surveys}
          columns={actionColumns}
          keyExtractor={(item) => String(item.UUID ?? item.uuid ?? Math.random())}
        />
      )}
    </div>
  );
}
