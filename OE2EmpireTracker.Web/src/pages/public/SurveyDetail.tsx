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
    (s) => String(s.UUID ?? s.uuid) === id
  );

  if (!survey) {
    return <EmptyState title="Survey not found" message="The requested survey does not exist." />;
  }

  const system = String(survey.System ?? survey.system ?? 'Unknown');
  const planet = String(survey.Planet ?? survey.planet ?? '—');
  const resourceType = String(survey.ResourceType ?? survey.resourceType ?? '—');
  const purity = String(survey.Purity ?? survey.purity ?? '—');
  const owner = String(survey.OwnerName ?? survey.ownerName ?? '');
  const resources = survey.Resources ?? survey.resources;
  const coordinates = survey.Coordinates ?? survey.coordinates;

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
              <dt className="text-gray-400">Planet</dt>
              <dd className="text-white">{planet}</dd>
            </div>
            {coordinates != null ? (
              <div className="flex justify-between">
                <dt className="text-gray-400">Coordinates</dt>
                <dd className="text-white">{String(coordinates as string)}</dd>
              </div>
            ) : null}
            {owner && (
              <div className="flex justify-between">
                <dt className="text-gray-400">Surveyed by</dt>
                <dd className="text-white">{owner}</dd>
              </div>
            )}
          </dl>
        </section>

        <section className="rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">Resource Summary</h2>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-400">Resource Type</dt>
              <dd className="text-white">{resourceType}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-400">Purity</dt>
              <dd className="text-yellow-400">{purity}</dd>
            </div>
          </dl>
        </section>

        {resources != null && Array.isArray(resources) ? (
          <section className="rounded border border-gray-700 p-4 lg:col-span-2">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Resource Breakdown</h2>
            <ul className="space-y-1 text-sm">
              {(resources as Record<string, unknown>[]).map((res: Record<string, unknown>, i: number) => (
                <li key={i} className="flex justify-between border-b border-gray-700 py-1">
                  <span className="text-gray-300">
                    {String(res.Name ?? res.name ?? res.ResourceName ?? 'Unknown')}
                  </span>
                  <span className="text-gray-400">
                    {String(res.Quantity ?? res.quantity ?? res.Amount ?? res.amount ?? '')}
                    {(res.Purity ?? res.purity) ? ` (${String(res.Purity ?? res.purity)})` : ''}
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
