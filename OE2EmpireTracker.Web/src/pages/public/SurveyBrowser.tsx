import { useState } from 'react';
import { usePublicSurveys } from '../../api/hooks/useSurveys';
import { FilterBar, type FilterField } from '../../components/common/FilterBar';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { SurveyCard } from '../../components/domain/SurveyCard';
import { Link } from 'react-router-dom';
import { AuthPrompt } from '../../components/common/AuthPrompt';
import type { SurveyFilters } from '../../api/endpoints/public';

const filterFields: FilterField[] = [
  { key: 'system', label: 'System', type: 'text', placeholder: 'Filter by system...' },
  {
    key: 'resourceType',
    label: 'Resource Type',
    type: 'select',
    options: [
      { value: 'Ore', label: 'Ore' },
      { value: 'Gas', label: 'Gas' },
      { value: 'Crystal', label: 'Crystal' },
      { value: 'Organic', label: 'Organic' },
    ],
  },
  {
    key: 'purityLevel',
    label: 'Purity',
    type: 'select',
    options: [
      { value: 'High', label: 'High' },
      { value: 'Medium', label: 'Medium' },
      { value: 'Low', label: 'Low' },
    ],
  },
  { key: 'search', label: 'Search', type: 'text', placeholder: 'Search surveys...' },
];

const columns: Column<Record<string, unknown>>[] = [
  {
    key: 'System',
    header: 'System',
    render: (item) => (
      <Link to={`/surveys/${item.UUID ?? item.uuid}`} className="text-blue-400 hover:underline">
        {String(item.System ?? item.system ?? 'Unknown')}
      </Link>
    ),
  },
  { key: 'Planet', header: 'Planet' },
  { key: 'ResourceType', header: 'Resource' },
  { key: 'Purity', header: 'Purity' },
  {
    key: 'OwnerName',
    header: 'Surveyor',
    render: (item) => (
      <span className="text-gray-400">{String(item.OwnerName ?? item.ownerName ?? '—')}</span>
    ),
  },
];

type ViewMode = 'table' | 'cards';

export function SurveyBrowser() {
  const [filters, setFilters] = useState<SurveyFilters>({});
  const [viewMode, setViewMode] = useState<ViewMode>('table');
  const { data, isLoading, isError, refetch } = usePublicSurveys(filters);

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  if (isLoading) return <LoadingSpinner message="Loading surveys..." />;
  if (isError) return <RetryableError message="Failed to load surveys." onRetry={() => void refetch()} />;

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const surveys = items as Record<string, unknown>[];

  return (
    <div>
      <AuthPrompt />
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Survey Browser</h1>
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

      {surveys.length === 0 ? (
        <EmptyState title="No surveys found" message="Try adjusting your filters." />
      ) : viewMode === 'table' ? (
        <DataTable
          data={surveys}
          columns={columns}
          keyExtractor={(item) => String(item.UUID ?? item.uuid ?? Math.random())}
        />
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {surveys.map((s) => (
            <SurveyCard key={String(s.UUID ?? s.uuid)} survey={s} />
          ))}
        </div>
      )}
    </div>
  );
}
