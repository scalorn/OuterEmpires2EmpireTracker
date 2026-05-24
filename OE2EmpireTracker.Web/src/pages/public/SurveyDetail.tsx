import { useParams, Link } from 'react-router-dom';
import { usePublicSurveys } from '../../api/hooks/useSurveys';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

export function SurveyDetail() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError, refetch } = usePublicSurveys();

  if (isLoading) return <LoadingSpinner message="Loading survey..." />;
  if (isError) return <RetryableError message="Failed to load survey." onRetry={() => void refetch()} />;

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const survey = (items as Record<string, unknown>[]).find(
    (s) => String(s.surveyID ?? s.SurveyID ?? s.UUID ?? s.uuid) === id
  );

  if (!survey) {
    return <EmptyState title="Survey not found" message="The requested survey does not exist." />;
  }

  const system = String(survey.systemName ?? survey.SystemName ?? 'Unknown');
  const planet = String(survey.planetName ?? survey.PlanetName ?? '—');
  const scannedBy = String(survey.scannedBy ?? survey.ScannedBy ?? '');
  const dateTime = String(survey.dateTime ?? survey.DateTime ?? '');
  const resources = survey.resources ?? survey.Resources;

  return (
    <div>
      <Link to="/surveys" className="mb-4 inline-block text-sm text-blue-400 hover:underline">
        ← Back to Surveys
      </Link>
      <h1 className="mb-6 text-2xl font-bold text-white">{system} — {planet}</h1>
      <div className="grid gap-6 lg:grid-cols-2">
        <section className="rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">Location Details</h2>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-400">System</dt>
              <dd className="text-white">{system}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-400">Planet/Asteroid</dt>
              <dd className="text-white">{planet}</dd>
            </div>
            {scannedBy && (
              <div className="flex justify-between">
                <dt className="text-gray-400">Surveyed by</dt>
                <dd className="text-white">{scannedBy}</dd>
              </div>
            )}
            {dateTime && (
              <div className="flex justify-between">
                <dt className="text-gray-400">Date</dt>
                <dd className="text-white">
                  {new Date(dateTime).toLocaleDateString()}
                </dd>
              </div>
            )}
          </dl>
        </section>

        {resources != null && typeof resources === 'object' ? (
          <section className="rounded border border-gray-700 p-4 lg:col-span-2">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Resources</h2>
            <ul className="space-y-1 text-sm">
              {Object.entries(resources as Record<string, Record<string, unknown>>).map(([key, res]) => (
                <li key={key} className="flex justify-between border-b border-gray-700 py-1">
                  <span className="text-gray-300">
                    {String(res.resource ?? res.Resource ?? key)}
                  </span>
                  <span className="text-gray-400">
                    {String(res.purity ?? res.Purity ?? '')} — {String(res.amount ?? res.Amount ?? '')}/h
                  </span>
                </li>
              ))}
            </ul>
          </section>
        ) : null}
      </div>
    </div>
  );
}
