import { useState } from 'react';
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

const filterFields: FilterField[] = [
  {
    key: 'type',
    label: 'Type',
    type: 'select',
    options: [
      { value: 'Ship', label: 'Ship' },
      { value: 'Structure', label: 'Structure' },
      { value: 'Module', label: 'Module' },
      { value: 'Weapon', label: 'Weapon' },
    ],
  },
  {
    key: 'techLevel',
    label: 'Tech Level',
    type: 'select',
    options: [
      { value: '1', label: 'TL1' },
      { value: '2', label: 'TL2' },
      { value: '3', label: 'TL3' },
      { value: '4', label: 'TL4' },
      { value: '5', label: 'TL5' },
    ],
  },
  {
    key: 'shipClass',
    label: 'Ship Class',
    type: 'select',
    options: [
      { value: 'Fighter', label: 'Fighter' },
      { value: 'Frigate', label: 'Frigate' },
      { value: 'Destroyer', label: 'Destroyer' },
      { value: 'Cruiser', label: 'Cruiser' },
      { value: 'Battleship', label: 'Battleship' },
    ],
  },
  { key: 'search', label: 'Search', type: 'text', placeholder: 'Search blueprints...' },
];

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
  { key: 'BlueprintType', header: 'Type' },
  { key: 'TechLevel', header: 'Tech Level' },
  { key: 'ShipClass', header: 'Ship Class' },
  {
    key: 'OwnerName',
    header: 'Owner',
    render: (item) => (
      <span className="text-gray-400">{String(item.OwnerName ?? item.ownerName ?? '—')}</span>
    ),
  },
];

type ViewMode = 'table' | 'cards';

export function BlueprintBrowser() {
  const [filters, setFilters] = useState<BlueprintFilters>({});
  const [viewMode, setViewMode] = useState<ViewMode>('table');

  // Only pass server-supported filters (not search) to the API
  const serverFilters: BlueprintFilters = {
    ...(filters.type && { type: filters.type }),
    ...(filters.techLevel && { techLevel: filters.techLevel }),
    ...(filters.shipClass && { shipClass: filters.shipClass }),
  };
  const { data, isLoading, isFetching, isError, refetch } = usePublicBlueprints(serverFilters);

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const allBlueprints = items as Record<string, unknown>[];

  // Client-side search filtering
  const searchTerm = (filters.search ?? '').toLowerCase();
  const blueprints = searchTerm
    ? allBlueprints.filter((bp) => {
        const name = String(bp.Name ?? bp.name ?? '').toLowerCase();
        const bpType = String(bp.BlueprintType ?? bp.blueprintType ?? '').toLowerCase();
        const shipClass = String(bp.ShipClass ?? bp.shipClass ?? '').toLowerCase();
        return name.includes(searchTerm) || bpType.includes(searchTerm) || shipClass.includes(searchTerm);
      })
    : allBlueprints;

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
