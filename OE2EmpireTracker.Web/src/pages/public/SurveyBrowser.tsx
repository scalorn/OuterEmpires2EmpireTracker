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
    key: 'systemName',
    header: 'System',
    render: (item) => (
      <Link to={`/surveys/${item.surveyID ?? item.UUID ?? item.uuid}`} className="text-blue-400 hover:underline">
        {String(item.systemName ?? item.SystemName ?? item.System ?? 'Unknown')}
      </Link>
    ),
  },
  {
    key: 'planetName',
    header: 'Planet',
    render: (item) => String(item.planetName ?? item.PlanetName ?? item.Planet ?? ''),
  },
  {
    key: 'scannedBy',
    header: 'Surveyor',
    render: (item) => (
      <span className="text-gray-400">{String(item.scannedBy ?? item.ScannedBy ?? item.OwnerName ?? '—')}</span>
    ),
  },
  {
    key: 'dateTime',
    header: 'Date',
    render: (item) => {
      const dt = item.dateTime ?? item.DateTime;
      if (!dt) return '—';
      try { return new Date(String(dt)).toLocaleDateString(); } catch { return String(dt); }
    },
  },
];

type ViewMode = 'table' | 'cards';

export function SurveyBrowser() {
  const [filters, setFilters] = useState<SurveyFilters>({});
  const [viewMode, setViewMode] = useState<ViewMode>('table');
  const { data, isLoading, isError, refetch } = usePublicSurveys();

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const allSurveys = items as Record<string, unknown>[];

  // Client-side filtering
  const surveys = allSurveys.filter((s) => {
    if (filters.system) {
      const sys = String(s.systemName ?? s.SystemName ?? '').toLowerCase();
      if (!sys.includes(filters.system.toLowerCase())) return false;
    }
    if (filters.search) {
      const term = filters.search.toLowerCase();
      const sys = String(s.systemName ?? s.SystemName ?? '').toLowerCase();
      const planet = String(s.planetName ?? s.PlanetName ?? '').toLowerCase();
      const scanned = String(s.scannedBy ?? s.ScannedBy ?? '').toLowerCase();
      if (!sys.includes(term) && !planet.includes(term) && !scanned.includes(term)) return false;
    }
    return true;
  });

  if (isLoading && !data) return <LoadingSpinner message="Loading surveys..." />;
  if (isError) return <RetryableError message="Failed to load surveys." onRetry={() => void refetch()} />;

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
