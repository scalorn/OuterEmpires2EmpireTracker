import { useState, useMemo } from 'react';
import { useSurveys } from '../../api/hooks/useSurveys';
import { useBaseline } from '../../api/hooks/useBaseline';
import { useAuthStore } from '../../auth/store';
import { MasterDetailLayout } from '../../components/common/MasterDetailLayout';
import { FilterBar, type FilterDefinition, type FilterValues } from '../../components/common/FilterBar';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';
import { applyFilters, type FilterConfig } from '../../utils/filterUtils';
import type { Survey } from '../../api/types/domain';

/**
 * SurveyForm — master-detail layout for managing planet and asteroid resource surveys.
 *
 * Left panel: filterable, sortable list of surveys
 * Right panel: detail panel for selected survey (placeholder until task 10.2)
 *
 * Requirements: 3.1, 3.2
 */
export function SurveyForm() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useSurveys(characterUUID);
  const { data: baseline } = useBaseline();

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [filterValues, setFilterValues] = useState<FilterValues>({
    search: '',
    surveyType: '',
    purity: '',
    minYield: '',
  });
  const [sortField, setSortField] = useState<keyof Survey>('planetName');
  const [sortAsc, setSortAsc] = useState(true);

  const surveys: Survey[] = useMemo(
    () => (Array.isArray(data) ? data : []) as Survey[],
    [data],
  );

  const purities = baseline?.purities ?? [];

  const filterDefs: FilterDefinition[] = useMemo(() => [
    { type: 'text', key: 'search', placeholder: 'Search planet or system...' },
    {
      type: 'dropdown',
      key: 'surveyType',
      label: 'Survey Type',
      options: [
        { value: 'Planet', label: 'Planet' },
        { value: 'Asteroid', label: 'Asteroid' },
      ],
    },
    {
      type: 'dropdown',
      key: 'purity',
      label: 'Purity',
      options: purities.map((p) => ({ value: p, label: p })),
    },
  ], [purities]);

  const filteredSurveys = useMemo(() => {
    const filters: FilterConfig<Survey>[] = [];

    const searchQuery = (filterValues.search as string) ?? '';
    if (searchQuery.trim()) {
      filters.push({
        type: 'text',
        fields: ['planetName', 'systemName'],
        query: searchQuery,
      });
    }

    const surveyType = (filterValues.surveyType as string) ?? '';
    if (surveyType) {
      filters.push({
        type: 'dropdown',
        field: 'surveyType',
        selected: surveyType,
      });
    }

    let result = applyFilters(surveys, filters);

    // Purity filter: show surveys that have at least one resource matching the purity
    const purityFilter = (filterValues.purity as string) ?? '';
    if (purityFilter) {
      result = result.filter((s) =>
        s.resources.some((r) => r.purity === purityFilter),
      );
    }

    // Minimum yield filter: show surveys that have at least one resource with amount >= minYield
    const minYieldStr = (filterValues.minYield as string) ?? '';
    const minYield = Number(minYieldStr);
    if (minYieldStr && !isNaN(minYield) && minYield > 0) {
      result = result.filter((s) =>
        s.resources.some((r) => r.amount >= minYield),
      );
    }

    return result;
  }, [surveys, filterValues]);

  const sortedSurveys = useMemo(() => {
    const sorted = [...filteredSurveys].sort((a, b) => {
      const aVal = a[sortField] ?? '';
      const bVal = b[sortField] ?? '';
      const cmp = String(aVal).localeCompare(String(bVal));
      return sortAsc ? cmp : -cmp;
    });
    return sorted;
  }, [filteredSurveys, sortField, sortAsc]);

  const handleSort = (field: keyof Survey) => {
    if (field === sortField) {
      setSortAsc(!sortAsc);
    } else {
      setSortField(field);
      setSortAsc(true);
    }
  };

  const handleClearFilters = () => {
    setFilterValues({ search: '', surveyType: '', purity: '', minYield: '' });
  };

  const handleBack = () => setSelectedId(null);

  if (isLoading) return <LoadingSpinner message="Loading surveys..." />;
  if (isError) return <RetryableError message="Failed to load surveys." onRetry={() => void refetch()} />;

  const listPanel = (
    <div className="flex h-full flex-col p-4">
      <h2 className="mb-3 text-lg font-semibold text-white">Surveys</h2>
      <FilterBar
        filters={filterDefs}
        values={filterValues}
        onChange={setFilterValues}
        onClear={handleClearFilters}
      />
      {/* Minimum yield input (numeric, not a standard FilterBar type) */}
      <div className="mb-4 flex items-end gap-3">
        <div className="flex flex-col">
          <label
            htmlFor="filter-minYield"
            className="mb-1 text-xs font-medium text-gray-400"
          >
            Min Yield
          </label>
          <input
            id="filter-minYield"
            type="number"
            min="0"
            value={(filterValues.minYield as string) ?? ''}
            onChange={(e) => setFilterValues({ ...filterValues, minYield: e.target.value })}
            placeholder="0"
            className="w-24 rounded border border-gray-600 bg-gray-700 px-3 py-1.5 text-sm text-white placeholder-gray-500"
          />
        </div>
      </div>
      {sortedSurveys.length === 0 ? (
        <EmptyState title="No surveys" message="No surveys match the current filters." />
      ) : (
        <div className="min-h-0 flex-1 overflow-y-auto">
          <table className="w-full text-left text-sm">
            <thead className="sticky top-0 bg-gray-800 text-xs uppercase text-gray-400">
              <tr>
                <SortHeader field="planetName" label="Planet" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="systemName" label="System" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="surveyType" label="Type" current={sortField} asc={sortAsc} onSort={handleSort} />
                <SortHeader field="scanDate" label="Scan Date" current={sortField} asc={sortAsc} onSort={handleSort} />
              </tr>
            </thead>
            <tbody>
              {sortedSurveys.map((survey) => (
                <tr
                  key={survey.uuid}
                  onClick={() => setSelectedId(survey.uuid)}
                  className={[
                    'cursor-pointer border-b border-gray-700 hover:bg-gray-750',
                    selectedId === survey.uuid ? 'bg-gray-700' : '',
                  ].join(' ')}
                >
                  <td className="px-3 py-2 text-white">{survey.planetName}</td>
                  <td className="px-3 py-2 text-gray-300">{survey.systemName}</td>
                  <td className="px-3 py-2 text-gray-300">{survey.surveyType}</td>
                  <td className="px-3 py-2 text-gray-300">{survey.scanDate ?? '—'}</td>
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
      <p className="text-sm text-gray-500">Survey detail panel — coming in task 10.2</p>
    </div>
  ) : (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-gray-500">Select a survey from the list to view details.</p>
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
  field: keyof Survey;
  label: string;
  current: keyof Survey;
  asc: boolean;
  onSort: (field: keyof Survey) => void;
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
