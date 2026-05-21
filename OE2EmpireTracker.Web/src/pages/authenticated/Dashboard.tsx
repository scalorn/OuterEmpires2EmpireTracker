import { Link } from 'react-router-dom';
import { useAuthStore } from '../../auth/store';
import { useCharacters } from '../../api/hooks/useCharacters';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';

export function Dashboard() {
  const { characterName, characterUUID, role, switchCharacter } = useAuthStore();
  const { data: characters, isLoading, isError, refetch } = useCharacters();

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold text-white">Dashboard</h1>

      <div className="mb-6 rounded border border-gray-700 p-4">
        <h2 className="mb-2 text-lg font-semibold text-gray-200">Active Character</h2>
        <p className="text-white">{characterName ?? 'No character selected'}</p>
        <p className="text-sm text-gray-400">Role: {role ?? '—'}</p>
      </div>

      {role === 'Owner' && (
        <div className="mb-6 rounded border border-gray-700 p-4">
          <h2 className="mb-3 text-lg font-semibold text-gray-200">Character Switcher</h2>
          {isLoading && <LoadingSpinner message="Loading characters..." />}
          {isError && <RetryableError message="Failed to load characters." onRetry={() => void refetch()} />}
          {characters && Array.isArray(characters) && (
            <ul className="space-y-2">
              {(characters as { uuid: string; name: string }[]).map((char) => (
                <li key={char.uuid} className="flex items-center justify-between">
                  <span className={`text-sm ${char.uuid === characterUUID ? 'text-blue-400 font-medium' : 'text-gray-300'}`}>
                    {char.name}
                  </span>
                  {char.uuid !== characterUUID && (
                    <button
                      onClick={() => switchCharacter(char.uuid)}
                      className="text-xs text-blue-400 hover:underline"
                    >
                      Switch
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <QuickLink to="/app/colonies" label="Colonies" description="Manage your colonies" />
        <QuickLink to="/app/blueprints" label="Blueprints" description="View and manage blueprints" />
        <QuickLink to="/app/surveys" label="Surveys" description="View and manage surveys" />
        <QuickLink to="/app/profile" label="Profile" description="Edit your player profile" />
        <QuickLink to="/app/faction" label="Faction" description="View faction data" />
        <QuickLink to="/app/sharing" label="Sharing" description="Configure sharing rules" />
      </div>
    </div>
  );
}

function QuickLink({ to, label, description }: { to: string; label: string; description: string }) {
  return (
    <Link
      to={to}
      className="rounded border border-gray-700 p-4 hover:border-blue-500 hover:bg-gray-800/50 transition-colors"
    >
      <h3 className="font-medium text-white">{label}</h3>
      <p className="mt-1 text-sm text-gray-400">{description}</p>
    </Link>
  );
}
