import { useState, useMemo } from 'react';
import { useBlueprints } from '../../api/hooks/useBlueprints';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { applyFilters, type FilterConfig } from '../../utils/filterUtils';
import type { Blueprint } from '../../api/types/domain';

/**
 * BlueprintForm — master-detail layout for managing blueprints.
 *
 * Left panel: filterable, sortable list of blueprints
 * Right panel: detail panel for selected blueprint (placeholder until detail task)
 *
 * Requirements: 2.1, 2.2
 */
export function BlueprintForm() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useBlueprints(characterUUID);
  const { data: baseline } = useBaseline();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [filterValues, setFilterValues] = useState<FilterValues>({
    search: '',
    blueprintType: '',
    shipClass: '',
    techLevel: '',
    evolution: '',
  });
  const [sortField, setSortField] = useState<keyof Blueprint>('name');
  const [sortAsc, setSortAsc] = useState(true);

  const blueprints: Blueprint[] = useMemo(
    () => (Array.isArray(data) ? data : []) as Blueprint[],
    [data],
  );

  const blueprintTypes = baseline?.blueprintTypes ?? [];
  const shipClasses = baseline?.shipClasses ?? [];
  const techLevels = baseline?.techLevels ?? [];

  // Derive unique evolution levels from the blueprint data
  const evolutionLevels = useMemo(() => {
    const levels = new Set<number>();
    for (const bp of blueprints) {
      levels.add(bp.evolution);
    }
    return Array.from(levels).sort((a, b) => a - b);
  }, [blueprints]);

  // Derive unique tech levels from the blueprint data (fallback if baseline is empty)
  const techLevelOptions = useMemo(() => {
    if (techLevels.length > 0) {
      return techLevels.map((tl) => ({ value: String(tl.level), label: tl.name }));
    }
    const levels = new Set<number>();
    for (const bp of blueprints) {
      levels.add(bp.techLevel);
    }
    return Array.from(levels)
      .sort((a, b) => a - b)
      .map((l) => ({ value: String(l), label: `TL ${l}` }));
  }, [techLevels, blueprints]);

  const filterDefs: FilterDefinition[] = useMemo(() => [
    { type: 'text', key: 'search', placeholder: 'Search name or nick name...' },
    {
      type: 'dropdown',
      key: 'blueprintType',
      label: 'Type',
      options: blueprintTypes.map((t) => ({ value: t, label: t })),
    },
    {
      type: 'dropdown',
      key: 'shipClass',
      label: 'Ship Class',
      options: shipClasses.map((sc) => ({ value: sc.name, label: sc.name })),
    },
    {
      type: 'dropdown',
      key: 'techLevel',
      label: 'Tech Level',
      options: techLevelOptions,
    },
    {
      type: 'dropdown',
      key: 'evolution',
      label: 'Evolution',
      options: evolutionLevels.map((e) => ({ value: String(e), label: `Evo ${e}` })),
    },
  ], [blueprintTypes, shipClasses, techLevelOptions, evolutionLevels]);

  const filteredBlueprints = useMemo(() => {
    const filters: FilterConfig<Blueprint>[] = [];

    const searchQuery = (filterValues.search as string) ?? '';
    if (searchQuery.trim()) {
      filters.push({
        type: 'text',
        fields: ['name', 'nickName'],
        query: searchQuery,
      });
    }

    const bpType = (filterValues.blueprintType as string) ?? '';
    if (bpType) {
      filters.push({
        type: 'dropdown',
        field: 'blueprintType',
        selected: bpType,
      });
    }

    const shipClass = (filterValues.shipClass as string) ?? '';
    if (shipClass) {
      filters.push({
        type: 'dropdown',
        field: 'shipClass',
        selected: shipClass,
      });
    }

    let result = applyFilters(blueprints, filters);

    // Tech level filter (numeric comparison)
    const techLevelStr = (filterValues.techLevel as string) ?? '';
    if (techLevelStr) {
      const tl = Number(techLevelStr);
      if (!isNaN(tl)) {
        result = result.filter((bp) => bp.techLevel === tl);
      }
    }

    // Evolution filter (numeric comparison)
    const evoStr = (filterValues.evolution as string) ?? '';
    if (evoStr) {
      const evo = Number(evoStr);
      if (!isNaN(evo)) {
        result = result.filter((bp) => bp.evolution === evo);
      }
    }

    return result;
  }, [blueprints, filterValues]);

  const sortedBlueprints = useMemo(() => {
    const sorted = [...filteredBlueprints].sort((a, b) => {
      const aVal = a[sortField] ?? '';
      const bVal = b[sortField] ?? '';
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortAsc ? aVal - bVal : bVal - aVal;
      }
      const cmp = String(aVal).localeCompare(String(bVal));
      return sortAsc ? cmp : -cmp;
    });
    return sorted;
  }, [filteredBlueprints, sortField, sortAsc]);

  const handleSort = (field: keyof Blueprint) => {
    if (field === sortField) {
      setSortAsc(!sortAsc);
    } else {
      setSortField(field);
      setSortAsc(true);
    }
  };

  const handleClearFilters = () => {
    setFilterValues({ search: '', blueprintType: '', shipClass: '', techLevel: '', evolution: '' });
  };

  const handleBack = () => setSelectedId(null);

  if (isLoading) return <LoadingSpinner message="Loading blueprints..." />;
  if (isError) return <RetryableError message="Failed to load blueprints." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-3 text-lg font-semibold text-white">Blueprints</h2>
      <FilterBar
        filters={filterDefs}
        values={filterValues}
        onChange={setFilterValues}
        onClear={handleClearFilters}
      />
      {sortedBlueprints.length === 0 ? (
        <EmptyState title="No blueprints" message="No blueprints match the current filters." />
      ) : (
        <div className="min-h-0 flex-1 overflow-y-auto">
          <table className="w-full text-left text-sm">
            <thead className="sticky top-0 bg-gray-800 text-xs uppercase text-gray-400">
              <tr>
                <SortHeader field="blueprintType" label="Type" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="name" label="Name" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="techLevel" label="TL" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="evolution" label="Evo" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="nickName" label="Nick Name" current={sortField} asc={sortAsc} onSort={handleSort} />
              </tr>
            </thead>
            <tbody>
              {sortedBlueprints.map((bp) => (
                <tr
                  key={bp.uuid}
                  onClick={() => setSelectedId(bp.uuid)}
                  className={[
                    'cursor-pointer border-b border-gray-700 hover:bg-gray-750',
                    selectedId === bp.uuid ? 'bg-gray-700' : '',
                  ].join(' ')}
                >
                  <td className="px-3 py-2 text-gray-300">{bp.blueprintType}</td>
                  <td className="px-3 py-2 text-white">{bp.name}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.techLevel}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.evolution}</td>
                  <td className="px-3 py-2 text-gray-300">{bp.nickName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );

  const detailPanel = selectedId ? (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-gray-500">Blueprint detail panel — coming in a future task</p>
    </div>
  ) : (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-gray-500">Select a blueprint from the list to view details.</p>
    </div>
  );

  return (
    <MasterDetailLayout
      listPanel={listPanel}
      detailPanel={detailPanel}
      selectedId={selectedId}
      onBack={handleBack}
    />
  );
}

interface SortHeaderProps {
  field: keyof Blueprint;
  label: string;
  current: keyof Blueprint;
  asc: boolean;
  onSort: (field: keyof Blueprint) => void;
}

function SortHeader({ field, label, current, asc, onSort }: SortHeaderProps) {
  const isActive = current === field;
  return (
    <th
      className="cursor-pointer px-3 py-2 select-none hover:text-white"
      onClick={() => onSort(field)}
    >
      {label}
      {isActive && (
        <span className="ml-1">{asc ? '▲' : '▼'}</span>
      )}
    </th>
  );
}
