import { Link } from 'react-router-dom';

interface SurveyCardProps {
  survey: Record<string, unknown>;
}

export function SurveyCard({ survey }: SurveyCardProps) {
  const surveyId = String(survey.surveyID ?? survey.SurveyID ?? survey.UUID ?? survey.uuid ?? '');
  const system = String(survey.systemName ?? survey.SystemName ?? 'Unknown System');
  const planet = String(survey.planetName ?? survey.PlanetName ?? '');
  const scannedBy = String(survey.scannedBy ?? survey.ScannedBy ?? '');
  const resources = survey.resources ?? survey.Resources;
  const resourceCount = resources && typeof resources === 'object' ? Object.keys(resources).length : 0;

  return (
    <Link
      to={`/surveys/${surveyId}`}
      className="block rounded border border-gray-700 p-4 hover:border-blue-500 hover:bg-gray-800/50 transition-colors"
    >
      <div>
        <h3 className="font-medium text-white">{system}</h3>
        {planet && <p className="text-sm text-gray-400">{planet}</p>}
        {resourceCount > 0 && (
          <p className="mt-1 text-sm text-gray-400">{resourceCount} resource(s)</p>
        )}
      </div>
      {scannedBy && (
        <p className="mt-2 text-xs text-gray-500">
          Surveyed by: <span className="text-gray-400">{scannedBy}</span>
        </p>
      )}
    </Link>
  );
}
