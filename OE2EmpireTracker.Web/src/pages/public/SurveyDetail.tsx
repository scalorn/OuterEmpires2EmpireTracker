import { useParams, Link } from 'react-router-dom';
import { usePublicSurveys } from '../../api/hooks/useSurveys';
import { usePublicAsteroidDetail } from '../../api/hooks/useAsteroids';
import { matchReserveToResource } from '../../utils/reserveMatching';
import { formatMaxReserve } from '../../utils/formatters';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

export function SurveyDetail() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError, refetch } = usePublicSurveys();

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const survey = (items as Record<string, unknown>[]).find(
    (s) => String(s.surveyID ?? s.SurveyID ?? s.UUID ?? s.uuid) === id
  );

  const surveyType = String(survey?.surveyType ?? survey?.SurveyType ?? '');
  const asteroidUUID = String(survey?.asteroidUUID ?? survey?.AsteroidUUID ?? '');
  const isAsteroidSurvey = surveyType === 'Asteroid';
  const shouldFetchAsteroid = isAsteroidSurvey && asteroidUUID.length > 0;

  const {
    data: asteroidData,
    isLoading: isAsteroidLoading,
    isError: isAsteroidError,
  } = usePublicAsteroidDetail(shouldFetchAsteroid ? asteroidUUID : null);

  if (isLoading) return <LoadingSpinner message="Loading survey..." />;
  if (isError) return <RetryableError message="Failed to load survey." onRetry={() => void refetch()} />;

  if (!survey) {
    return <EmptyState title="Survey not found" message="The requested survey does not exist." />;
  }

  const system = String(survey.systemName ?? survey.SystemName ?? 'Unknown');
  const planet = String(survey.planetName ?? survey.PlanetName ?? '—');
  const scannedBy = String(survey.scannedBy ?? survey.ScannedBy ?? '');
  const dateTime = String(survey.dateTime ?? survey.DateTime ?? '');
  const resources = survey.resources ?? survey.Resources;

  // Determine whether to show the Max Reserve column
  const showMaxReserve = isAsteroidSurvey;
  const reserves = asteroidData?.reserves ?? [];
  const asteroidFetchFailed = isAsteroidError || (!isAsteroidLoading && shouldFetchAsteroid && !asteroidData);

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
            {showMaxReserve && isAsteroidLoading && (
              <div className="mb-3 flex items-center gap-2 text-sm text-gray-400" role="status">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-gray-600 border-t-blue-500" />
                <span>Loading reserves...</span>
              </div>
            )}
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-700 text-left text-gray-400">
                  <th className="py-1">Resource</th>
                  <th className="py-1">Purity</th>
                  <th className="py-1 text-right">Amount/h</th>
                  {showMaxReserve && <th className="py-1 text-right">Max Reserve</th>}
                </tr>
              </thead>
              <tbody>
                {Object.entries(resources as Record<string, Record<string, unknown>>).map(([key, res]) => {
                  const resourceName = String(res.resource ?? res.Resource ?? res.resourceName ?? res.ResourceName ?? key);
                  const purity = String(res.purity ?? res.Purity ?? '');
                  const amount = String(res.amount ?? res.Amount ?? '');

                  let maxReserveDisplay: string | undefined;
                  if (showMaxReserve) {
                    if (isAsteroidLoading) {
                      maxReserveDisplay = '…';
                    } else if (asteroidFetchFailed) {
                      maxReserveDisplay = '-';
                    } else {
                      const matched = matchReserveToResource(
                        { resourceName, purity },
                        reserves
                      );
                      maxReserveDisplay = formatMaxReserve(matched?.maxReserve ?? null);
                    }
                  }

                  return (
                    <tr key={key} className="border-b border-gray-700">
                      <td className="py-1 text-gray-300">{resourceName}</td>
                      <td className="py-1 text-gray-400">{purity}</td>
                      <td className="py-1 text-right text-gray-400">{amount}/h</td>
                      {showMaxReserve && (
                        <td className="py-1 text-right text-gray-300">{maxReserveDisplay}</td>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </section>
        ) : null}
      </div>
    </div>
  );
}
