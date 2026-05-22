import { type HTTPError } from 'ky';

/**
 * Result of an API operation with error handling.
 * On success: ok=true, data contains the response, error is null.
 * On failure: ok=false, data is null, error contains the display message.
 *
 * Satisfies: Req 3, Criteria 8–10
 */
export interface ApiResult<T> {
  ok: boolean;
  data: T | null;
  error: string | null;
}

/**
 * Generic error message displayed when a 400 response body cannot be parsed.
 * Satisfies: Req 3, Criterion 10
 */
const GENERIC_VALIDATION_ERROR = 'Operation failed: validation error';

/**
 * Parses a 400 response body for a structured error message.
 * Expects `{ "error": "..." }` format from typed endpoints.
 *
 * Returns the error string on success, or the generic message if unparseable.
 * Satisfies: Req 3, Criteria 8, 10
 */
async function parseValidationError(response: Response): Promise<string> {
  try {
    const body = await response.json() as Record<string, unknown>;
    if (typeof body.error === 'string' && body.error.length > 0) {
      return body.error;
    }
    return GENERIC_VALIDATION_ERROR;
  } catch {
    return GENERIC_VALIDATION_ERROR;
  }
}

/**
 * Wraps an API call and returns a structured result with error handling.
 *
 * - On HTTP 2xx: returns { ok: true, data, error: null }
 *   (caller should clear any previously displayed error)
 * - On HTTP 400: parses error message and returns { ok: false, data: null, error: message }
 * - On unparseable 400: returns { ok: false, data: null, error: generic message }
 * - On other errors: re-throws (network errors, 401, 403, 500 handled elsewhere)
 *
 * Satisfies: Req 3, Criteria 8–10
 */
export async function handleApiCall<T>(apiCall: () => Promise<T>): Promise<ApiResult<T>> {
  try {
    const data = await apiCall();
    // HTTP 2xx — clear previous errors (Req 3, Criterion 9)
    return { ok: true, data, error: null };
  } catch (err: unknown) {
    const httpError = err as HTTPError;
    if (httpError?.response?.status === 400) {
      // HTTP 400 — parse and display validation error (Req 3, Criteria 8, 10)
      const message = await parseValidationError(httpError.response);
      return { ok: false, data: null, error: message };
    }
    // Other errors (network, 401, 403, 5xx) — re-throw for upstream handling
    throw err;
  }
}

/**
 * Extracts a user-displayable error message from an unknown error.
 * Useful for catch blocks in components that don't use handleApiCall.
 *
 * - HTTP 400: parses the error body
 * - Other HTTP errors: returns "Server error (status)"
 * - Network errors: returns network error message
 * - Unknown: returns generic message
 *
 * Satisfies: Req 3, Criteria 8, 10
 */
export async function getDisplayError(err: unknown): Promise<string> {
  const httpError = err as HTTPError;
  if (httpError?.response) {
    if (httpError.response.status === 400) {
      return parseValidationError(httpError.response);
    }
    return `Server error (${httpError.response.status})`;
  }
  if (err instanceof TypeError && err.message === 'Failed to fetch') {
    return 'Network error — unable to reach the server.';
  }
  return 'An unexpected error occurred.';
}
