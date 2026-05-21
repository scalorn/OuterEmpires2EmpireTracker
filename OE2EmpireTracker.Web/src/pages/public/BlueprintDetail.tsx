import { useParams, Link } from 'react-router-dom';
import { usePublicBlueprints } from '../../api/hooks/useBlueprints';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { EmptyState } from '../../components/common/EmptyState';

export function BlueprintDetail() {
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError, refetch } = usePublicBlueprints();

  if (isLoading) return <LoadingSpinner message="Loading blueprint..." />;
  if (isError) return <RetryableError message="Failed to load blueprint." onRetry={() => void refetch()} />;

  const items = (data as { items?: unknown[] })?.items ?? (Array.isArray(data) ? data : []);
  const blueprint = (items as Record<string, unknown>[]).find(
    (bp) => String(bp.UUID ?? bp.uuid) === id
  );

  if (!blueprint) {
    return <EmptyState title="Blueprint not found" message="The requested blueprint does not exist." />;
  }

  const name = String(blueprint.Name ?? blueprint.name ?? 'Unknown');
  const type = String(blueprint.BlueprintType ?? blueprint.blueprintType ?? '—');
  const techLevel = String(blueprint.TechLevel ?? blueprint.techLevel ?? '—');
  const shipClass = String(blueprint.ShipClass ?? blueprint.shipClass ?? '—');
  const owner = String(blueprint.OwnerName ?? blueprint.ownerName ?? '');
  const evolution = blueprint.EvolutionChain ?? blueprint.evolutionChain;
  const manufacturing = blueprint.ManufacturingRequirements ?? blueprint.manufacturingRequirements;

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
            {owner && (
              <div className="flex justify-between">
                <dt className="text-gray-400">Owner</dt>
                <dd className="text-white">{owner}</dd>
              </div>
            )}
          </dl>
        </section>

        {evolution != null ? (
          <section className="rounded border border-gray-700 p-4">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Evolution Chain</h2>
            {Array.isArray(evolution) ? (
              <ol className="list-inside list-decimal space-y-1 text-sm text-gray-300">
                {(evolution as string[]).map((step: string, i: number) => (
                  <li key={i}>{step}</li>
                ))}
              </ol>
            ) : (
              <p className="text-sm text-gray-400">{String(evolution as string)}</p>
            )}
          </section>
        ) : null}

        {manufacturing != null ? (
          <section className="rounded border border-gray-700 p-4 lg:col-span-2">
            <h2 className="mb-3 text-lg font-semibold text-gray-200">Manufacturing Requirements</h2>
            {Array.isArray(manufacturing) ? (
              <ul className="space-y-1 text-sm text-gray-300">
                {(manufacturing as Record<string, unknown>[]).map((req: Record<string, unknown>, i: number) => (
                  <li key={i} className="flex justify-between border-b border-gray-700 py-1">
                    <span>{String(req.ResourceName ?? req.resourceName ?? req.name ?? 'Unknown')}</span>
                    <span className="text-gray-400">{String(req.Quantity ?? req.quantity ?? '')}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-gray-400">{String(manufacturing as string)}</p>
            )}
          </section>
        ) : null}
      </div>
    </div>
  );
}
