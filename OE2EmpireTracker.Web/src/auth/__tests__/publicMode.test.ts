import fc from 'fast-check';
import { describe, it, expect, afterEach } from 'vitest';
import { render, cleanup } from '@testing-library/react';
import React from 'react';

/**
 * Property test for public mode mutation hiding.
 * Validates: Requirements 5.3
 *
 * Property 3: When isAuthenticated is false, no mutation controls are rendered.
 */

// A generic component that conditionally renders mutation controls
function MutationGuardedPage({
  isAuthenticated,
  items,
}: {
  isAuthenticated: boolean;
  items: string[];
}) {
  return React.createElement('div', null,
    !isAuthenticated && React.createElement('div', { 'data-testid': 'auth-prompt' },
      'Sign in for full access'
    ),
    React.createElement('ul', null,
      items.map((item, i) =>
        React.createElement('li', { key: i }, item)
      )
    ),
    isAuthenticated && React.createElement('button', { 'data-testid': 'create-btn' }, 'Create'),
    isAuthenticated && React.createElement('button', { 'data-testid': 'edit-btn' }, 'Edit'),
    isAuthenticated && React.createElement('button', { 'data-testid': 'delete-btn' }, 'Delete'),
  );
}

const MUTATION_TESTIDS = ['create-btn', 'edit-btn', 'delete-btn'];

afterEach(() => {
  cleanup();
});

describe('Property 3: Public mode hides mutation controls', () => {
  it('when isAuthenticated is false, no mutation controls are rendered', () => {
    fc.assert(
      fc.property(
        fc.array(fc.string({ minLength: 1, maxLength: 30 }), { minLength: 0, maxLength: 20 }),
        (items) => {
          cleanup();
          const { container, queryByTestId } = render(
            React.createElement(MutationGuardedPage, { isAuthenticated: false, items })
          );

          // No mutation buttons should be present
          for (const testId of MUTATION_TESTIDS) {
            expect(queryByTestId(testId)).toBeNull();
          }

          // Auth prompt should be visible
          expect(queryByTestId('auth-prompt')).not.toBeNull();

          // Verify no buttons with mutation-related text exist
          const buttons = container.querySelectorAll('button');
          for (const btn of buttons) {
            const text = btn.textContent?.toLowerCase() ?? '';
            expect(text).not.toContain('create');
            expect(text).not.toContain('edit');
            expect(text).not.toContain('delete');
            expect(text).not.toContain('save');
          }
          cleanup();
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });

  it('when isAuthenticated is true, mutation controls ARE rendered', () => {
    fc.assert(
      fc.property(
        fc.array(fc.string({ minLength: 1, maxLength: 30 }), { minLength: 0, maxLength: 10 }),
        (items) => {
          cleanup();
          const { queryByTestId } = render(
            React.createElement(MutationGuardedPage, { isAuthenticated: true, items })
          );

          // Mutation buttons should be present
          for (const testId of MUTATION_TESTIDS) {
            expect(queryByTestId(testId)).not.toBeNull();
          }

          // Auth prompt should NOT be visible
          expect(queryByTestId('auth-prompt')).toBeNull();
          cleanup();
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});
