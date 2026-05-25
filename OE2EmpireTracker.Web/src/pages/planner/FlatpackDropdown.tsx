import { useState, useMemo } from 'react';
import { useFlatpacks } from '../../api/hooks/useFlatpacks';
import type { FlatpackOption } from '../../api/hooks/useFlatpacks';

interface FlatpackDropdownProps {
  onAdd: (uuid: string, name: string, subType: string) => void;
  disabled?: boolean;
}

function getGroup(subType: string): string {
  const slashIndex = subType.indexOf('/');
  return slashIndex === -1 ? subType : subType.substring(0, slashIndex);
}

export function FlatpackDropdown({ onAdd, disabled }: FlatpackDropdownProps) {
  const { data, isLoading, isError, error, refetch } = useFlatpacks();
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<FlatpackOption | null>(null);

  const filtered = useMemo(() => {
    if (!data) return [];
    if (!search.trim()) return data;
    const term = search.toLowerCase();
    return data.filter((opt) => opt.name.toLowerCase().includes(term));
  }, [data, search]);

  const grouped = useMemo(() => {
    const groups: Record<string, FlatpackOption[]> = {};
    for (const opt of filtered) {
      const group = getGroup(opt.subType);
      if (!groups[group]) groups[group] = [];
      groups[group].push(opt);
    }
    return groups;
  }, [filtered]);

  const handleAdd = () => {
    if (!selected) return;
    onAdd(selected.uuid, selected.name, selected.subType);
    setSelected(null);
  };

  if (isError) {
    return (
      <div className="rounded border border-red-700 bg-red-900/30 p-4">
        <p className="text-sm text-red-300">
          Failed to load flatpacks{error instanceof Error ? `: ${error.message}` : '.'}
        </p>
        <button
          onClick={() => refetch()}
          className="mt-2 rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="rounded border border-gray-700 p-4">
      <h2 className="mb-3 text-lg font-semibold text-gray-200">Add Flatpack</h2>

      <input
        type="text"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search flatpacks..."
        disabled={disabled || isLoading}
        className="mb-2 w-full rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500 disabled:opacity-50"
      />

      {isLoading && (
        <p className="text-sm text-gray-400">Loading flatpacks...</p>
      )}

      {!isLoading && data && (
        <>
          <div className="max-h-64 overflow-y-auto rounded border border-gray-600 bg-gray-800">
            {Object.keys(grouped).length === 0 ? (
              <p className="p-3 text-sm text-gray-500">No matching flatpacks.</p>
            ) : (
              Object.entries(grouped).map(([group, options]) => (
                <div key={group}>
                  <div className="sticky top-0 bg-gray-700 px-3 py-1 text-xs font-bold uppercase tracking-wide text-gray-300">
                    {group}
                  </div>
                  {options.map((opt) => (
                    <button
                      key={opt.uuid}
                      type="button"
                      onClick={() => setSelected(opt)}
                      disabled={disabled}
                      className={`w-full px-3 py-1.5 text-left text-sm hover:bg-gray-600 disabled:opacity-50 ${
                        selected?.uuid === opt.uuid
                          ? 'bg-blue-700 text-white'
                          : 'text-gray-200'
                      }`}
                    >
                      {opt.name}
                    </button>
                  ))}
                </div>
              ))
            )}
          </div>

          <button
            onClick={handleAdd}
            disabled={disabled || !selected}
            className="mt-2 w-full rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            Add{selected ? `: ${selected.name}` : ''}
          </button>
        </>
      )}
    </div>
  );
}
