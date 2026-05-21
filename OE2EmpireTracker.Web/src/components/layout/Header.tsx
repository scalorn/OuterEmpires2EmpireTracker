import { useAuthStore } from '../../auth/store';

export function Header() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const characterName = useAuthStore((s) => s.characterName);
  const logout = useAuthStore((s) => s.logout);

  return (
    <header className="flex items-center justify-between border-b border-gray-700 bg-gray-800 px-6 py-3">
      <div className="flex items-center gap-4">
        <h1 className="text-lg font-bold text-white">OE2 Empire Tracker</h1>
        <span className="rounded bg-gray-700 px-2 py-0.5 text-xs text-gray-300">
          {isAuthenticated ? 'Authenticated' : 'Public'}
        </span>
      </div>

      <div className="flex items-center gap-4">
        {isAuthenticated && characterName && (
          <span className="text-sm text-gray-300">{characterName}</span>
        )}
        {isAuthenticated ? (
          <button
            onClick={logout}
            className="rounded bg-red-600 px-3 py-1 text-sm text-white hover:bg-red-700"
          >
            Logout
          </button>
        ) : (
          <a
            href="/login"
            className="rounded bg-blue-600 px-3 py-1 text-sm text-white hover:bg-blue-700"
          >
            Sign In
          </a>
        )}
      </div>
    </header>
  );
}
