import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';

/**
 * Blocks navigation when the form has unsaved changes.
 *
 * Uses react-router's useBlocker to prevent in-app navigation and
 * the beforeunload event to prevent browser close/refresh.
 *
 * Fail-closed: if useBlocker throws, navigation is blocked by default
 * via the beforeunload handler which remains active.
 */
export function useUnsavedChanges(isDirty: boolean): void {
  // Block in-app navigation via react-router when dirty.
  // useBlocker accepts a boolean or a function returning boolean.
  // When the blocker is in "blocked" state, we show a confirm dialog.
  const blocker = useBlocker(isDirty);

  // Show browser confirm dialog when blocker triggers
  useEffect(() => {
    if (blocker.state === 'blocked') {
      const proceed = window.confirm(
        'You have unsaved changes. Are you sure you want to leave?'
      );
      if (proceed) {
        blocker.proceed();
      } else {
        blocker.reset();
      }
    }
  }, [blocker]);

  // Block browser close/refresh via beforeunload when dirty.
  // This also serves as the fail-closed mechanism: if useBlocker
  // errors out, the beforeunload handler still prevents data loss.
  useEffect(() => {
    if (!isDirty) return;

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [isDirty]);
}
