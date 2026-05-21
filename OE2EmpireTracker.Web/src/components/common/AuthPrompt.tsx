import { Link } from 'react-router-dom';
import { useAuthStore } from '../../auth/store';

export function AuthPrompt() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) return null;

  return (
    <div className="mb-4 rounded border border-blue-700 bg-blue-900/30 px-4 py-3">
      <p className="text-sm text-blue-200">
        Viewing public data only.{' '}
        <Link to="/login" className="font-medium text-blue-400 hover:underline">
          Sign in for full access
        </Link>
      </p>
    </div>
  );
}
