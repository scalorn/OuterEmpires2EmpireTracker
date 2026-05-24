import { useMemo, useState } from 'react';
import { usePublicBlueprints, useGlobalData } from '../../api/hooks/useBlueprints';
import { FilterBar, type FilterField } from '../../components/common/FilterBar';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { BlueprintCard } from '../../components/domain/BlueprintCard';
import { Link } from 'react-router-dom';
import { AuthPrompt } from '../../components/common/AuthPrompt';
import type { BlueprintFilters } from '../../api/endpoints/public';

interface BaselineItem {
  Id?: string;
  Name?: string;
}

/** Build filter field definitions from baseline data (same source as WinForms). */
function buildFilterFields(
  blueprintTypes: BaselineItem[],
  techLevels: BaselineItem[],
  shipClasses: BaselineItem[],
  evolutionValues: string[],
): FilterField[] {
  const fields: FilterField[] = [
    {
      key: 'type',
      label: 'Type',
      type: 'select',
      options: blueprintTypes.map((t) => ({ value: t.Id ?? t.Name ?? '', label: t.Name ?? t.Id ?? '' })),
    },
    {
      key: 'techLevel',
      label: 'Tech Level',
      type: 'select',
      options: techLevels.map((t) => ({ value: t.Name ?? t.Id ?? '', label: t.Name ?? t.Id ?? '' })),
    },
    {
      key: 'shipClass',
      label: 'Ship Class',
      type: 'select',
      options: shipClasses.map((c) => ({ value: c.Name ?? c.Id ?? '', label: c.Name ?? c.Id ?? '' })),
    },
    {
      key: 'evolution',
      label: 'Evolution',
      type: 'select',
      options: evolutionValues.map((v) => ({ value: v, label: v })),
    },
    { key: 'search', label: 'Search', type: 'text', placeholder: 'Search blueprints...' },
  ];

  return fields;
}

const columns: Column<Record<string, unknown>>[] = [
  {
    key: 'bluePrintType',
    header: 'Type',
    render: (item) => String(item.bluePrintType ?? item.BluePrintType ?? item.BlueprintType ?? ''),
  },
  {
    key: 'name',
    header: 'Name',
    render: (item) => (
      <Link to={`/blueprints/${item.uuid ?? item.UUID}`} className="text-blue-400 hover:underline">
        {String(item.name ?? item.Name ?? 'Unknown')}
      </Link>
    ),
  },
  {
    key: 'techLevel',
    header: 'Tech Level',
    render: (item) => String(item.techLevel ?? item.TechLevel ?? ''),
  },
  {
    key: 'evolution',
    header: 'Evolution',
    render: (item) => String(item.evolution ?? item.Evolution ?? ''),
  },
  {
    key: 'nickName',
    header: 'Nickname',
    render: (item) => String(item.nickName ?? item.NickName ?? ''),
  },
];

type ViewMode = 'table' | 'cards';

export function BlueprintBrowser() {
  const [filters, setFilters] = useState<BlueprintFilters>({});
  const [viewMode, setViewMode] = useState<ViewMode>('table');

  // Fetch baseline/global data for filter dropdowns (same data as WinForms EmpireContext)
  const { data: blueprintTypes = [] } = useGlobalData<BaselineItem>('BlueprintType');
  const { data: techLevels = [] } = useGlobalData<BaselineItem>('TechLevel');
  const { data: shipClasses = [] } = useGlobalData<BaselineItem>('ShipClass');

  // Fetch all public blueprints
  const { data, isLoading, isFetching, isError, refetch } = usePublicBlueprints({});

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const allBlueprints = items as Record<string, unknown>[];

  // Derive distinct evolution values from loaded blueprint data
  const evolutionValues = useMemo(() => {
    const values = new Set<string>();
    for (const bp of allBlueprints) {
      const evo = String(bp.Evolution ?? bp.evolution ?? '');
      if (evo) values.add(evo);
    }
    return Array.from(values).sort();
  }, [allBlueprints]);

  // Build filter fields from baseline data
  const filterFields = useMemo(
    () => buildFilterFields(blueprintTypes, techLevels, shipClasses, evolutionValues),
    [blueprintTypes, techLevels, shipClasses, evolutionValues],
  );

  // Client-side filtering for all filter fields
  const blueprints = useMemo(() => {
    let result = allBlueprints;

    if (filters.type) {
      result = result.filter((bp) => {
        const val = String(bp.bluePrintType ?? bp.BluePrintType ?? bp.BlueprintType ?? bp.blueprintType ?? '');
        return val === filters.type;
      });
    }

    if (filters.techLevel) {
      result = result.filter((bp) => {
        const val = String(bp.techLevel ?? bp.TechLevel ?? '');
        return val === filters.techLevel;
      });
    }

    if (filters.shipClass) {
      result = result.filter((bp) => {
        const val = String(bp.class ?? bp.shipClass ?? bp.ShipClass ?? '');
        return val === filters.shipClass;
      });
    }

    if (filters.evolution) {
      result = result.filter((bp) => {
        const val = String(bp.evolution ?? bp.Evolution ?? '');
        return val === filters.evolution;
      });
    }

    const searchTerm = (filters.search ?? '').toLowerCase();
    if (searchTerm) {
      result = result.filter((bp) => {
        const name = String(bp.name ?? bp.Name ?? '').toLowerCase();
        const bpType = String(bp.bluePrintType ?? bp.BluePrintType ?? '').toLowerCase();
        const shipClass = String(bp.class ?? bp.shipClass ?? bp.ShipClass ?? '').toLowerCase();
        const nickName = String(bp.nickName ?? bp.NickName ?? '').toLowerCase();
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
