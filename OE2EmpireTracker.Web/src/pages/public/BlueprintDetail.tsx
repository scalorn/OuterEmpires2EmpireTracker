import { useParams, Link } from 'react-router-dom';
import { usePublicBlueprintDetail } from '../../api/hooks/useBlueprints';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

export function BlueprintDetail() {
  const { id } = useParams<{ id: string }>();
  const { data: blueprint, isLoading, isError, refetch } = usePublicBlueprintDetail(id);

  if (isLoading) return <LoadingSpinner message="Loading blueprint..." />;
  if (isError) return <RetryableError message="Failed to load blueprint." onRetry={() => void refetch()} />;

  if (!blueprint) {
    return <EmptyState title="Blueprint not found" message="The requested blueprint does not exist." />;
  }

  const name = String(blueprint.name ?? 'Unknown');
  const type = String(blueprint.bluePrintType ?? '—');
  const techLevel = String(blueprint.techLevel ?? '—');
  const shipClass = String(blueprint.shipClass ?? '—');
  const evolution = blueprint.evolution;
  const properties = blueprint.properties as Record<string, string> | undefined;
  const resources = blueprint.resources as Record<string, string> | undefined;

  return (
    <div>
      <Link to="/blueprints" className="mb-4 inline-block text-sm text-blue-400 hover:underline">
        ← Back to Blueprints
      </Link>
      <h1 className="mb-6 text-2xl font-bold text-white">{name}</h1>
      <div className="grid gap-6 lg:grid-cols-2">
        <section className="rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">Properties</h2>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-400">Type</dt>
              <dd className="text-white">{type}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-400">Tech Level</dt>
              <dd className="text-white">{techLevel}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-400">Ship Class</dt>
              <dd className="text-white">{shipClass}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-400">Evolution</dt>
              <dd className="text-white">{String(evolution ?? '—')}</dd>
            </div>
          </dl>
        </section>

        {properties && Object.keys(properties).length > 0 && (
          <section className="rounded border border-gray-700 p-4">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Blueprint Properties</h2>
            <dl className="space-y-2 text-sm">
              {Object.entries(properties).map(([key, value]) => (
                <div key={key} className="flex justify-between">
                  <dt className="text-gray-400">{key}</dt>
                  <dd className="text-white">{value}</dd>
                </div>
              ))}
            </dl>
          </section>
        )}

        {resources && Object.keys(resources).length > 0 && (
          <section className="rounded border border-gray-700 p-4 lg:col-span-2">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Resources</h2>
            <dl className="space-y-2 text-sm">
              {Object.entries(resources).map(([key, value]) => (
                <div key={key} className="flex justify-between border-b border-gray-700 py-1">
                  <dt className="text-gray-300">{key}</dt>
                  <dd className="text-gray-400">{value}</dd>
                </div>
              ))}
            </dl>
          </section>
        )}
      </div>
    </div>
  );
}
