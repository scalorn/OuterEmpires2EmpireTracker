import { useState } from 'react';
import { useColonies, useColonyMutations } from '../../api/hooks/useColonies';
import { useAuthStore } from '../../auth/store';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

const columns: Column<Record<string, unknown>>[] = [
  { key: 'colonyName', header: 'Name' },
  { key: 'systemName', header: 'System' },
  { key: 'planetName', header: 'Planet' },
  {
    key: 'structures',
    header: 'Structures',
    render: (item) => {
      const structures = item.Structures ?? item.structures;
      return <span>{Array.isArray(structures) ? structures.length : '—'}</span>;
    },
  },
];

export function ColonyManager() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useColonies(characterUUID);
  const { createOrUpdate, remove } = useColonyMutations();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');

  if (isLoading) return <LoadingSpinner message="Loading colonies..." />;
  if (isError) return <RetryableError message="Failed to load colonies." onRetry={() => void refetch()} />;

  const colonies = (Array.isArray(data) ? data : []) as Record<string, unknown>[];

  const handleCreate = () => {
    const uuid = crypto.randomUUID();
    createOrUpdate.mutate({
      entityUUID: uuid,
      data: { UUID: uuid, Name: 'New Colony', Structures: [] },
    });
  };

  const handleDelete = (uuid: string) => {
    if (confirm('Delete this colony?')) {
      remove.mutate(uuid);
    }
  };

  const handleSaveEdit = (uuid: string) => {
    const colony = colonies.find((c) => String(c.UUID ?? c.uuid) === uuid);
    if (colony) {
      createOrUpdate.mutate({
        entityUUID: uuid,
        data: { ...colony, Name: editName },
      });
    }
    setEditingId(null);
  };

  const actionColumns: Column<Record<string, unknown>>[] = [
    ...columns,
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
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
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
              onClick={() => { setEditingId(uuid); setEditName(String(item.Name ?? '')); }}
              className="text-xs text-blue-400 hover:underline"
            >
              Edit
            </button>
            <button
              onClick={() => handleDelete(uuid)}
              className="text-xs text-red-400 hover:underline"
            >
              Delete
            </button>
          </div>
        );
      },
    },
  ];

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Colony Manager</h1>
        <button
          onClick={handleCreate}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Create Colony
        </button>
      </div>

      {colonies.length === 0 ? (
        <EmptyState title="No colonies" message="Create your first colony to get started." />
      ) : (
        <DataTable
          data={colonies}
          columns={actionColumns}
          keyExtractor={(item) => String(item.UUID ?? item.uuid ?? Math.random())}
        />
      )}
    </div>
  );
}
