import ky, { type HTTPError } from 'ky';
import { getRuntimeConfig } from '../hooks/useRuntimeConfig';

export interface ApiError {
  status: number;
  message: string;
  isNetworkError: boolean;
  isRetryable: boolean;
  validationErrors?: Record<string, string[]>;
}

function getApiBaseUrl(): string {
  try {
    return getRuntimeConfig().apiBaseUrl;
  } catch {
    return window.__OE2_CONFIG__?.apiBaseUrl ?? '';
  }
}

// Lazy reference to auth store to break circular dependency.
// The store module sets this after it initializes.
let getAuthState: (() => { token: string | null; logout: () => void }) | null = null;

export function registerAuthStore(getter: typeof getAuthState): void {
  getAuthState = getter;
}

export const apiClient = ky.create({
  prefix: getApiBaseUrl() || undefined,
  hooks: {
    beforeRequest: [
      ({ request }) => {
        const state = getAuthState?.();
        if (state?.token) {
          request.headers.set('Authorization', `Bearer ${state.token}`);
        }
      },
    ],
    afterResponse: [
      ({ response }) => {
        if (response.status === 401) {
          getAuthState?.()?.logout();
        }
      },
    ],
  },
});

export async function parseApiError(error: unknown): Promise<ApiError> {
  if (error instanceof TypeError && error.message === 'Failed to fetch') {
    return {
      status: 0,
      message: 'Network error — unable to reach the server.',
      isNetworkError: true,
      isRetryable: true,
    };
  }

  const httpError = error as HTTPError;
  if (httpError?.response) {
    const status = httpError.response.status;
    let message = `Server error (${status})`;
    let validationErrors: Record<string, string[]> | undefined;

    try {
      const body = await httpError.response.json() as Record<string, unknown>;
      if (typeof body.error === 'string') message = body.error;
      if (body.details && typeof body.details === 'object') {
        validationErrors = body.details as Record<string, string[]>;
      }
    } catch {
      // Response body not JSON
    }

    return {
      status,
      message,
      isNetworkError: false,
      isRetryable: status >= 500,
      validationErrors,
    };
  }

  return {
    status: 0,
    message: 'An unexpected error occurred.',
    isNetworkError: false,
    isRetryable: false,
  };
}
