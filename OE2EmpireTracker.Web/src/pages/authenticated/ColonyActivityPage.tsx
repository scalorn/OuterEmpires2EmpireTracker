import { useState, useMemo } from 'react';
import { useColonies } from '../../api/hooks/useColonies';
import { useAuthStore } from '../../auth/store';
import { useVisibilityRecovery } from '../../hooks/useVisibilityRecovery';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { RetryableError } from '../../components/common/RetryableError';
import { TimerGroup } from '../../components/domain/TimerGroup';
import { queryKeys } from '../../api/hooks/queryKeys';
import type { Colony, ColonyStructure } from '../../api/types/domain';

/**
 * Activity type categories derived from blueprintType.
 * Mining rigs → Mining, Refineries → Refining, Manufactories → Manufacturing,
 * Research labs → Research, Building structures → Building.
 */
type ActivityType = 'Mining' | 'Refining' | 'Manufacturing' | 'Research' | 'Building';

const ACTIVITY_ORDER: ActivityType[] = [
  'Mining',
  'Refining',
  'Manufacturing',
  'Research',
  'Building',
];

/**
 * Derives the activity type from a structure's blueprintType string.
 * Returns null if the type doesn't map to a known activity category.
 */
function deriveActivityType(blueprintType: string): ActivityType | null {
  const lower = blueprintType.toLowerCase();
  if (lower.includes('mining') || lower.includes('rig')) return 'Mining';
  if (lower.includes('refin')) return 'Refining';
  if (lower.includes('manufact') || lower.includes('factory')) return 'Manufacturing';
  if (lower.includes('research') || lower.includes('lab')) return 'Research';
  if (lower.includes('build') || lower.includes('construct')) return 'Building';
  return null;
}

interface TimerEntry {
  id: string;
  label: string;
  targetTime: string;
  colonyName?: string;
}

/**
 * ColonyActivityPage — displays active processing timers across all colonies,
 * grouped by activity type (mining, refining, manufacturing, research, building).
 *
 * Features:
 * - Timers only display when active; show zero until user navigates away (keepZero)
 * - Inactivity mode toggle shows idle structures that need attention
 *
 * Validates: Requirements 11.1, 11.2, 11.3
 */
export function ColonyActivityPage() {
  const { characterUUID } = useAuthStore();
  const { data, isLoading, isError, refetch } = useColonies(characterUUID);
  const [inactivityMode, setInactivityMode] = useState(false);

  // Recalculate timers from end timestamps when tab regains focus
  const visibilityKeys = useMemo(
    () => (characterUUID ? [queryKeys.colonies(characterUUID) as string[]] : []),
    [characterUUID],
  );
  useVisibilityRecovery(visibilityKeys);

  const colonies = useMemo(() => (Array.isArray(data) ? data : []) as Colony[], [data]);

  // Group structures with active timers by activity type
  const timerGroups = useMemo(() => {
    const groups: Record<ActivityType, TimerEntry[]> = {
      Mining: [],
      Refining: [],
      Manufacturing: [],
      Research: [],
      Building: [],
    };

    for (const colony of colonies) {
      for (const structure of colony.structures ?? []) {
        if (!structure.processingEndUtc) continue;
        const activityType = deriveActivityType(structure.blueprintType);
        if (!activityType) continue;

        groups[activityType].push({
          id: structure.uuid,
          label: structure.blueprintType,
          targetTime: structure.processingEndUtc,
          colonyName: colony.colonyName,
        });
      }
    }

    return groups;
  }, [colonies]);

  // Idle structures: online structures with no active processing timer
  const idleStructures = useMemo(() => {
    const idle: { structure: ColonyStructure; colonyName: string; activityType: ActivityType }[] = [];

    for (const colony of colonies) {
      for (const structure of colony.structures ?? []) {
        if (structure.status !== 'online') continue;
        if (structure.processingEndUtc) continue;
        const activityType = deriveActivityType(structure.blueprintType);
        if (!activityType) continue;

        idle.push({ structure, colonyName: colony.colonyName, activityType });
      }
    }

    return idle;
  }, [colonies]);

  if (isLoading) {
    return <LoadingSpinner message="Loading colony activity..." />;
  }

  if (isError) {
    return (
      <RetryableError
        message="Failed to load colony activity data."
        onRetry={() => void refetch()}
      />
    );
  }

  const totalActiveTimers = ACTIVITY_ORDER.reduce(
    (sum, type) => sum + timerGroups[type].length,
    0,
  );

  return (
    <div className="mx-auto max-w-5xl p-6">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Colony Activity</h1>
          <p className="mt-1 text-sm text-gray-400">
            {totalActiveTimers} active timer{totalActiveTimers !== 1 ? 's' : ''} across{' '}
            {colonies.length} colon{colonies.length !== 1 ? 'ies' : 'y'}
          </p>
        </div>

        {/* Inactivity Mode Toggle */}
        <label className="flex cursor-pointer items-center gap-2">
          <input
            type="checkbox"
            checked={inactivityMode}
            onChange={(e) => setInactivityMode(e.target.checked)}
            className="h-4 w-4 rounded border-gray-600 bg-gray-700 text-blue-500 focus:ring-blue-500 focus:ring-offset-gray-800"
          />
          <span className="text-sm text-gray-300">Inactivity Mode</span>
        </label>
      </div>

      {!inactivityMode ? (
        /* Active timers grouped by activity type */
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {ACTIVITY_ORDER.map((activityType) => (
            <TimerGroup
              key={activityType}
              title={activityType}
              timers={timerGroups[activityType]}
            />
          ))}
        </div>
      ) : (
        /* Inactivity mode: show idle structures */
        <div>
          <p className="mb-4 text-sm text-gray-400">
            Showing {idleStructures.length} idle structure{idleStructures.length !== 1 ? 's' : ''}{' '}
            that need attention (online with no active timer).
          </p>

          {idleStructures.length === 0 ? (
            <div className="rounded-lg border border-gray-700 bg-gray-800 p-6 text-center">
              <p className="text-sm text-gray-400">
                All online structures are currently processing. Nothing idle.
              </p>
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {ACTIVITY_ORDER.map((activityType) => {
                const idle = idleStructures.filter((s) => s.activityType === activityType);
                if (idle.length === 0) return null;

                return (
                  <div
                    key={activityType}
                    className="rounded-lg border border-gray-700 bg-gray-800 p-4"
                  >
                    <div className="mb-3 flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-gray-200">{activityType}</h3>
                      <span className="rounded-full bg-amber-700 px-2 py-0.5 text-xs text-amber-200">
                        {idle.length} idle
                      </span>
                    </div>
                    <ul className="space-y-2">
                      {idle.map((entry) => (
                        <li
                          key={entry.structure.uuid}
                          className="flex items-center justify-between rounded bg-gray-750 px-3 py-2"
                        >
                          <div className="min-w-0 flex-1">
                            <span className="block truncate text-sm text-gray-200">
                              {entry.structure.blueprintType}
                            </span>
                            <span className="block truncate text-xs text-gray-400">
                              {entry.colonyName}
                            </span>
                          </div>
                          <span className="ml-3 text-xs text-amber-400">Idle</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
