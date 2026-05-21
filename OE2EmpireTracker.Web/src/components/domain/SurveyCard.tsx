import { Link } from 'react-router-dom';

interface SurveyCardProps {
  survey: Record<string, unknown>;
}

export function SurveyCard({ survey }: SurveyCardProps) {
  const uuid = String(survey.UUID ?? survey.uuid ?? '');
  const system = String(survey.System ?? survey.system ?? 'Unknown System');
  const planet = String(survey.Planet ?? survey.planet ?? '');
  const resourceType = String(survey.ResourceType ?? survey.resourceType ?? '');
  const purity = String(survey.Purity ?? survey.purity ?? '');
  const owner = String(survey.OwnerName ?? survey.ownerName ?? '');

  return (
    <Link
      to={`/surveys/${uuid}`}
      className="block rounded border border-gray-700 p-4 hover:border-blue-500 hover:bg-gray-800/50 transition-colors"
    >
      <div>
        <h3 className="font-medium text-white">{system}</h3>
        {planet && <p className="text-sm text-gray-400">{planet}</p>}
        <p className="mt-1 text-sm text-gray-400">
          {resourceType && <span className="mr-3">{resourceType}</span>}
          {purity && <span className="text-yellow-400">{purity}</span>}
        </p>
      </div>
      {owner && (
        <p className="mt-2 text-xs text-gray-500">
          Surveyed by: <span className="text-gray-400">{owner}</span>
        </p>
      )}
    </Link>
  );
}
