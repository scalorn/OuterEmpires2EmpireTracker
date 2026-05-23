import { useState } from 'react';
import { useBlueprints, useBlueprintMutations } from '../../api/hooks/useBlueprints';
import { useAuthStore } from '../../auth/store';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

const baseColumns: Column<Record<string, unknown>>[] = [
  { key: 'name', header: 'Name' },
  { key: 'bluePrintType', header: 'Type' },
  { key: 'techLevel', header: 'Tech Level' },
  { key: 'class', header: 'Class' },
];

export function BlueprintManager() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useBlueprints(characterUUID);
  const { save: createOrUpdate, remove } = useBlueprintMutations();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');

  if (isLoading) return <LoadingSpinner message="Loading blueprints..." />;
  if (isError) return <RetryableError message="Failed to load blueprints." onRetry={() => void refetch()} />;

  const blueprints = (Array.isArray(data) ? data : []) as unknown as Record<string, unknown>[];

  const handleCreate = () => {
    const uuid = crypto.randomUUID();
    createOrUpdate.mutate({
      entityUUID: uuid,
      data: { name: 'New Blueprint', blueprintType: 'Ship', techLevel: 1, evolution: 0, isGlobal: false, properties: {}, resources: [] },
    });
  };

  const handleDelete = (uuid: string) => {
    if (confirm('Delete this blueprint?')) {
      remove.mutate(uuid);
    }
  };

  const handleSaveEdit = (uuid: string) => {
    const bp = blueprints.find((b) => String(b.UUID ?? b.uuid) === uuid);
    if (bp) {
      createOrUpdate.mutate({ entityUUID: uuid, data: { name: editName } });
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
        <h1 className="text-2xl font-bold text-white">Blueprint Manager</h1>
        <button onClick={handleCreate} className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700">
          Create Blueprint
        </button>
      </div>
      {blueprints.length === 0 ? (
        <EmptyState title="No blueprints" message="Create your first blueprint to get started." />
      ) : (
        <DataTable
          data={blueprints}
          columns={actionColumns}
          keyExtractor={(item) => String(item.UUID ?? item.uuid ?? Math.random())}
        />
      )}
    </div>
  );
}
