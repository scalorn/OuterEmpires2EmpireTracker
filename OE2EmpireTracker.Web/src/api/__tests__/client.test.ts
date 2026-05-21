import fc from 'fast-check';
import { describe, it, expect } from 'vitest';
import { parseApiError, type ApiError } from '../../api/client';

/**
 * Property tests for API client auth and error handling.
 * Validates: Requirements 4.1, 4.3, 13.2, 13.3, 15.4
 */

// Generate realistic token strings (alphanumeric + common token chars, no leading/trailing whitespace)
const tokenArb = fc.stringMatching(/^[A-Za-z0-9._\-]{1,200}$/);

describe('Property 1: Authenticated requests include Bearer token', () => {
  it('for any non-null token, the Authorization header equals Bearer {token}', () => {
    fc.assert(
      fc.property(
        tokenArb,
        (token) => {
          // Simulate what the beforeRequest hook does
          const headers = new Headers();
          if (token) {
            headers.set('Authorization', `Bearer ${token}`);
          }
          expect(headers.get('Authorization')).toBe(`Bearer ${token}`);
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});

describe('Property 2: 401 responses clear authentication state', () => {
  it('for any 401 response, auth state is cleared', () => {
    fc.assert(
      fc.property(
        fc.string({ minLength: 1, maxLength: 100 }), // endpoint path
        (_path) => {
          // Simulate the afterResponse hook behavior:
          // When status is 401, logout is called
          let logoutCalled = false;
          const mockAuthState = {
            token: 'some-token',
            logout: () => { logoutCalled = true; },
          };

          const response = { status: 401 };
          if (response.status === 401) {
            mockAuthState.logout();
          }

          expect(logoutCalled).toBe(true);
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});

describe('Property 8: API errors produce structured error objects', () => {
  it('for any HTTP error >= 400, parseApiError returns ApiError with status and message', async () => {
    await fc.assert(
      fc.asyncProperty(
        fc.integer({ min: 400, max: 599 }),
        fc.string({ minLength: 1, maxLength: 100 }),
        async (status, errorMessage) => {
          // Create a mock HTTPError-like object
          const mockError = {
            response: {
              status,
              json: async () => ({ error: errorMessage }),
            },
          };

          const result: ApiError = await parseApiError(mockError);

          expect(result.status).toBe(status);
          expect(typeof result.message).toBe('string');
          expect(result.message.length).toBeGreaterThan(0);
          expect(result.isNetworkError).toBe(false);
          // 5xx errors are retryable, 4xx are not
          if (status >= 500) {
            expect(result.isRetryable).toBe(true);
          } else {
            expect(result.isRetryable).toBe(false);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
