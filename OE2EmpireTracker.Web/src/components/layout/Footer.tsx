import { getRuntimeConfig } from '../../hooks/useRuntimeConfig';

export function Footer() {
  let version = 'unknown';
  try {
    version = getRuntimeConfig().version;
  } catch {
    // Config not loaded yet
  }

  return (
    <footer className="border-t border-gray-700 bg-gray-800 px-6 py-2">
      <p className="text-xs text-gray-500">
        OE2 Empire Tracker v{version}
      </p>
    </footer>
  );
}
