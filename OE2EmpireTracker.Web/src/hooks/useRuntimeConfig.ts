interface RuntimeConfig {
  apiBaseUrl: string;
  version: string;
  pathPrefix?: string;
}

let cachedConfig: RuntimeConfig | null = null;

export async function loadRuntimeConfig(): Promise<RuntimeConfig> {
  if (cachedConfig) return cachedConfig;
  const response = await fetch('/config.json');
  cachedConfig = await response.json();
  return cachedConfig!;
}

export function getRuntimeConfig(): RuntimeConfig {
  if (!cachedConfig) throw new Error('Runtime config not loaded. Call loadRuntimeConfig() first.');
  return cachedConfig;
}
