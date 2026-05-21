import { Link } from 'react-router-dom';

interface BlueprintCardProps {
  blueprint: Record<string, unknown>;
}

export function BlueprintCard({ blueprint }: BlueprintCardProps) {
  const uuid = String(blueprint.UUID ?? blueprint.uuid ?? '');
  const name = String(blueprint.Name ?? blueprint.name ?? 'Unknown Blueprint');
  const type = String(blueprint.BlueprintType ?? blueprint.blueprintType ?? '');
  const techLevel = String(blueprint.TechLevel ?? blueprint.techLevel ?? '');
  const shipClass = String(blueprint.ShipClass ?? blueprint.shipClass ?? '');
  const owner = String(blueprint.OwnerName ?? blueprint.ownerName ?? '');

  return (
    <Link
      to={`/blueprints/${uuid}`}
      className="block rounded border border-gray-700 p-4 hover:border-blue-500 hover:bg-gray-800/50 transition-colors"
    >
      <div className="flex items-start justify-between">
        <div>
          <h3 className="font-medium text-white">{name}</h3>
          <p className="mt-1 text-sm text-gray-400">
            {type && <span className="mr-3">{type}</span>}
            {techLevel && <span className="mr-3">TL{techLevel}</span>}
            {shipClass && <span>{shipClass}</span>}
          </p>
        </div>
      </div>
      {owner && (
        <p className="mt-2 text-xs text-gray-500">
          Owned by: <span className="text-gray-400">{owner}</span>
        </p>
      )}
    </Link>
  );
}
