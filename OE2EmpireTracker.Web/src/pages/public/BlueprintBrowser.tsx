import { useMemo, useState } from 'react';
import { usePublicBlueprints } from '../../api/hooks/useBlueprints';
import { FilterBar, type FilterField } from '../../components/common/FilterBar';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { BlueprintCard } from '../../components/domain/BlueprintCard';
import { Link } from 'react-router-dom';
import { AuthPrompt } from '../../components/common/AuthPrompt';
import type { BlueprintFilters } from '../../api/endpoints/public';

/** Extract sorted unique non-empty values for a field from the dataset. */
function uniqueValues(items: Record<string, unknown>[], ...keys: string[]): string[] {
  const set = new Set<string>();
  for (const item of items) {
    for (const key of keys) {
      const val = item[key];
      if (typeof val === 'string' && val.trim()) set.add(val.trim());
    }
  }
  return Array.from(set).sort();
}

/** Build filter field definitions dynamically from the loaded data. */
function buildFilterFields(blueprints: Record<string, unknown>[]): FilterField[] {
  const types = uniqueValues(blueprints, 'BluePrintType', 'BlueprintType', 'blueprintType');
  const techLevels = uniqueValues(blueprints, 'TechLevel', 'techLevel');
  const classes = uniqueValues(blueprints, 'ShipClass', 'shipClass', 'Class', 'class')
    .filter((v) => isNaN(Number(v))); // Exclude numeric class IDs

  const fields: FilterField[] = [
    {
      key: 'type',
      label: 'Type',
      type: 'select',
      options: types.map((t) => ({ value: t, label: t })),
    },
    {
      key: 'techLevel',
      label: 'Tech Level',
      type: 'select',
      options: techLevels.map((t) => ({ value: t, label: t })),
    },
  ];

  if (classes.length > 0) {
    fields.push({
      key: 'shipClass',
      label: 'Ship Class',
      type: 'select',
      options: classes.map((c) => ({ value: c, label: c })),
    });
  }

  fields.push({ key: 'search', label: 'Search', type: 'text', placeholder: 'Search blueprints...' });

  return fields;
}

const columns: Column<Record<string, unknown>>[] = [
  {
    key: 'Name',
    header: 'Name',
    render: (item) => (
      <Link to={`/blueprints/${item.UUID ?? item.uuid}`} className="text-blue-400 hover:underline">
        {String(item.Name ?? item.name ?? 'Unknown')}
      </Link>
    ),
  },
  {
    key: 'BluePrintType',
    header: 'Type',
    render: (item) => String(item.BluePrintType ?? item.BlueprintType ?? item.blueprintType ?? ''),
  },
  { key: 'TechLevel', header: 'Tech Level' },
  {
    key: 'Evolution',
    header: 'Evolution',
    render: (item) => String(item.Evolution ?? item.evolution ?? ''),
  },
  {
    key: 'NickName',
    header: 'Nickname',
    render: (item) => String(item.NickName ?? item.nickName ?? ''),
  },
];

type ViewMode = 'table' | 'cards';

export function BlueprintBrowser() {
  const [filters, setFilters] = useState<BlueprintFilters>({});
  const [viewMode, setViewMode] = useState<ViewMode>('table');

  // Fetch all public blueprints (no server-side filtering — server doesn't support it)
  const { data, isLoading, isFetching, isError, refetch } = usePublicBlueprints({});

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const allBlueprints = items as Record<string, unknown>[];

  // Build filter field definitions dynamically from the loaded data
  const filterFields = useMemo(() => buildFilterFields(allBlueprints), [allBlueprints]);

  // Client-side filtering for all filter fields
  const blueprints = useMemo(() => {
    let result = allBlueprints;

    if (filters.type) {
      result = result.filter((bp) => {
        const val = String(bp.BluePrintType ?? bp.BlueprintType ?? bp.blueprintType ?? '');
        return val === filters.type;
      });
    }

    if (filters.techLevel) {
      result = result.filter((bp) => {
        const val = String(bp.TechLevel ?? bp.techLevel ?? '');
        return val === filters.techLevel;
      });
    }

    if (filters.shipClass) {
      result = result.filter((bp) => {
        const val = String(bp.ShipClass ?? bp.shipClass ?? '');
        return val === filters.shipClass;
      });
    }

    const searchTerm = (filters.search ?? '').toLowerCase();
    if (searchTerm) {
      result = result.filter((bp) => {
        const name = String(bp.Name ?? bp.name ?? '').toLowerCase();
        const bpType = String(bp.BluePrintType ?? bp.BlueprintType ?? bp.blueprintType ?? '').toLowerCase();
        const shipClass = String(bp.ShipClass ?? bp.shipClass ?? '').toLowerCase();
        const nickName = String(bp.NickName ?? bp.nickName ?? '').toLowerCase();
        return name.includes(searchTerm) || bpType.includes(searchTerm) ||
          shipClass.includes(searchTerm) || nickName.includes(searchTerm);
      });
    }

    return result;
  }, [allBlueprints, filters]);

  return (
    <div>
      <AuthPrompt />
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Blueprint Browser</h1>
        <div className="flex gap-2">
          <button
            onClick={() => setViewMode('table')}
            className={`rounded px-3 py-1 text-sm ${viewMode === 'table' ? 'bg-blue-600 text-white' : 'bg-gray-700 text-gray-300'}`}
          >
            Table
          </button>
          <button
            onClick={() => setViewMode('cards')}
            className={`rounded px-3 py-1 text-sm ${viewMode === 'cards' ? 'bg-blue-600 text-white' : 'bg-gray-700 text-gray-300'}`}
          >
            Cards
          </button>
        </div>
      </div>

      <FilterBar
        fields={filterFields}
        values={filters as Record<string, string>}
        onChange={handleFilterChange}
        onClear={handleClear}
      />

      {isLoading && !data && <LoadingSpinner message="Loading blueprints..." />}
      {isError && <RetryableError message="Failed to load blueprints." onRetry={() => void refetch()} />}
      {!isError && data && blueprints.length === 0 && (
        <EmptyState title="No blueprints found" message="Try adjusting your filters." />
      )}
      {!isError && blueprints.length > 0 && (
        <div className={isFetching ? 'opacity-60 transition-opacity' : ''}>
          {viewMode === 'table' ? (
            <DataTable
              data={blueprints}
              columns={columns}
              keyExtractor={(item) => String(item.UUID ?? item.uuid ?? Math.random())}
            />
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {blueprints.map((bp) => (
                <BlueprintCard key={String(bp.UUID ?? bp.uuid)} blueprint={bp} />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
