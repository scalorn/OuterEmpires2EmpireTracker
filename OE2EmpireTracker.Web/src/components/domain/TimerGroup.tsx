import type { ReactNode } from 'react';
import { CountdownTimer } from '../common/CountdownTimer';
import { isExpired } from '../../utils/timerUtils';

interface TimerEntry {
  id: string;
  label: string;
  targetTime: string; // ISO 8601
  colonyName?: string;
}

interface TimerGroupProps {
  title: string; // e.g. "Mining", "Refining", "Manufacturing"
  timers: TimerEntry[];
  icon?: ReactNode;
}

/**
 * Groups multiple CountdownTimers by activity type.
 * Displays the group title with an optional icon and a count badge.
 * Only shows timers that are active (not expired) — uses keepZero=true
 * so completed timers stay visible at 00:00:00 until the user navigates away.
 *
 * Validates: Requirements 11.1, 11.2
 */
export function TimerGroup({ title, timers, icon }: TimerGroupProps) {
  const activeTimers = timers.filter((t) => !isExpired(t.targetTime));

  return (
    <div className="rounded-lg border border-gray-700 bg-gray-800 p-4">
      <div className="mb-3 flex items-center gap-2">
        {icon && <span className="text-gray-400">{icon}</span>}
        <h3 className="text-sm font-semibold text-gray-200">{title}</h3>
        <span className="rounded-full bg-gray-700 px-2 py-0.5 text-xs text-gray-300">
          {activeTimers.length}
        </span>
      </div>

      {activeTimers.length === 0 ? (
        <p className="text-sm text-gray-500">No active timers</p>
      ) : (
        <ul className="space-y-2">
          {activeTimers.map((timer) => (
            <li
              key={timer.id}
              className="flex items-center justify-between rounded bg-gray-750 px-3 py-2"
            >
              <div className="min-w-0 flex-1">
                <span className="block truncate text-sm text-gray-200">
                  {timer.label}
                </span>
                {timer.colonyName && (
                  <span className="block truncate text-xs text-gray-400">
                    {timer.colonyName}
                  </span>
                )}
              </div>
              <CountdownTimer
                targetTime={timer.targetTime}
                keepZero={true}
                className="ml-3 text-sm text-emerald-400"
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
