import { useState, useMemo } from 'react';
import { usePublicSurveys } from '../../api/hooks/useSurveys';
import { useGlobalData } from '../../api/hooks/useBlueprints';
import { FilterBar, type FilterField } from '../../components/common/FilterBar';
import { DataTable, type Column } from '../../components/common/DataTable';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { SurveyCard } from '../../components/domain/SurveyCard';
import { Link } from 'react-router-dom';
import { AuthPrompt } from '../../components/common/AuthPrompt';
import type { SurveyFilters } from '../../api/endpoints/public';

/** Extract all SurveyResource entries from a survey's resources dictionary. */
function getResources(survey: Record<string, unknown>): Array<{ resource: string; purity: string; amount: string }> {
  const res = survey.resources ?? survey.Resources;
  if (!res || typeof res !== 'object') return [];
  return Object.values(res as Record<string, Record<string, unknown>>).map((r) => ({
    resource: String(r.resource ?? r.Resource ?? ''),
    purity: String(r.purity ?? r.Purity ?? ''),
    amount: String(r.amount ?? r.Amount ?? ''),
  }));
}

const PURITY_OPTIONS = [
  { value: 'High', label: 'High' },
  { value: 'Medium', label: 'Medium' },
  { value: 'Low', label: 'Low' },
  { value: 'Refined', label: 'Refined' },
];

const TYPE_OPTIONS = [
  { value: 'planet', label: 'Planet' },
  { value: 'asteroid', label: 'Asteroid' },
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

  // Fetch resource list from baseline data (served via API)
  const { data: resourceData = [] } = useGlobalData<{ Name?: string }>('Resource');

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined }));
  };

  const handleClear = () => setFilters({});

  // Build resource options from API data
  const resourceOptions = useMemo(() =>
    resourceData
      .filter((r) => r.Name)
      .map((r) => ({ value: r.Name!, label: r.Name! })),
    [resourceData],
  );

  // Build filter fields with dynamic resource options
  const filterFields: FilterField[] = useMemo(() => [
    { key: 'search', label: 'Search', type: 'text', placeholder: 'System, planet, or surveyor...' },
    { key: 'resourceType', label: 'Resource', type: 'select', options: resourceOptions },
    { key: 'surveyType', label: 'Type', type: 'select', options: TYPE_OPTIONS },
    { key: 'purityLevel', label: 'Purity', type: 'select', options: PURITY_OPTIONS },
    { key: 'minAmount', label: 'Min Amount', type: 'text', placeholder: 'Min yield...' },
  ], [resourceOptions]);

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const allSurveys = items as Record<string, unknown>[];

  // Client-side filtering (matches WinForms logic)
  const surveys = useMemo(() => {
    const minAmount = filters.minAmount ? parseFloat(filters.minAmount) : 0;

    return allSurveys.filter((s) => {
      // Text search: matches system, planet, nickname, surveyID, scannedBy
      if (filters.search) {
        const term = filters.search.toLowerCase();
        const sys = String(s.systemName ?? s.SystemName ?? '').toLowerCase();
        const planet = String(s.planetName ?? s.PlanetName ?? '').toLowerCase();
        const scanned = String(s.scannedBy ?? s.ScannedBy ?? '').toLowerCase();
        const surveyId = String(s.surveyID ?? s.SurveyID ?? '').toLowerCase();
        const nick = String(s.nickName ?? s.NickName ?? '').toLowerCase();
        if (!sys.includes(term) && !planet.includes(term) && !scanned.includes(term)
            && !surveyId.includes(term) && !nick.includes(term)) return false;
      }

      // Survey type filter (planet/asteroid)
      if (filters.surveyType) {
        const type = String(s.surveyType ?? s.SurveyType ?? '').toLowerCase();
        if (type !== filters.surveyType.toLowerCase()) return false;
      }

      // Resource + Purity + MinAmount: must match the SAME resource entry
      if (filters.resourceType || filters.purityLevel || minAmount > 0) {
        const resources = getResources(s);
        const match = resources.some((r) => {
          if (filters.resourceType && r.resource !== filters.resourceType) return false;
          if (filters.purityLevel && r.purity !== filters.purityLevel) return false;
          if (minAmount > 0) {
            const amt = parseFloat(r.amount);
            if (isNaN(amt) || amt < minAmount) return false;
          }
          return true;
        });
        if (!match) return false;
      }

      return true;
    });
  }, [allSurveys, filters]);

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
