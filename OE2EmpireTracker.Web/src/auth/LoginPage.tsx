import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuthStore } from './store';

export function LoginPage() {
  const [tokenInput, setTokenInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { isAuthenticated, loginError, login } = useAuthStore();
  const navigate = useNavigate();

  if (isAuthenticated) {
    return <Navigate to="/app" replace />;
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!tokenInput.trim()) return;

    setIsLoading(true);
    await login(tokenInput.trim());
    setIsLoading(false);

    // Check if login succeeded (store updated)
    if (useAuthStore.getState().isAuthenticated) {
      navigate('/app', { replace: true });
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-900">
      <div className="w-full max-w-md rounded-lg bg-gray-800 p-8 shadow-lg">
        <h1 className="mb-6 text-2xl font-bold text-white">
          OE2 Empire Tracker
        </h1>
        <p className="mb-4 text-sm text-gray-400">
          Enter your Bearer token to access your character data.
        </p>

        <form onSubmit={handleSubmit}>
          <label htmlFor="token-input" className="mb-2 block text-sm font-medium text-gray-300">
            API Token
          </label>
          <input
            id="token-input"
            type="password"
            value={tokenInput}
            onChange={(e) => setTokenInput(e.target.value)}
            placeholder="Paste your token here"
            className="mb-4 w-full rounded border border-gray-600 bg-gray-700 px-3 py-2 text-white placeholder-gray-500 focus:border-blue-500 focus:outline-none"
            disabled={isLoading}
            autoComplete="off"
          />

          {loginError && (
            <p className="mb-4 text-sm text-red-400" role="alert">
              {loginError}
            </p>
          )}

          <button
            type="submit"
            disabled={isLoading || !tokenInput.trim()}
            className="w-full rounded bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isLoading ? 'Validating...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  );
}
