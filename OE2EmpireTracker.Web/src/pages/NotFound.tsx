import { Link } from 'react-router-dom';

export function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center py-20">
      <h1 className="mb-2 text-4xl font-bold text-gray-300">404</h1>
      <p className="mb-6 text-gray-500">Page not found</p>
      <Link
        to="/"
        className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
      >
        Go Home
      </Link>
    </div>
  );
}
