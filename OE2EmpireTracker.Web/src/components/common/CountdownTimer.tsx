import { useEffect, useState, useCallback } from 'react';
import { computeRemaining, formatCountdown } from '../../utils/timerUtils';

interface CountdownTimerProps {
  targetTime: string; // ISO 8601 end timestamp
  onComplete?: () => void;
  className?: string;
  keepZero?: boolean; // If true, stays at 00:00 after completion instead of hiding
}

/**
 * A live-decrementing countdown timer component.
 * Computes remaining time from an ISO 8601 end timestamp and decrements each second.
 *
 * Validates: Requirements 11.2, 18.3, 18.5
 */
export function CountdownTimer({
  targetTime,
  onComplete,
  className,
  keepZero = false,
}: CountdownTimerProps) {
  const calculate = useCallback(
    () => computeRemaining(targetTime) ?? 0,
    [targetTime],
  );

  const [remaining, setRemaining] = useState<number>(calculate);
  const [completed, setCompleted] = useState<boolean>(calculate() === 0);

  useEffect(() => {
    const initial = calculate();
    setRemaining(initial);
    setCompleted(initial === 0);
  }, [calculate]);

  useEffect(() => {
    if (completed) return;

    const interval = setInterval(() => {
      const now = calculate();
      setRemaining(now);

      if (now === 0) {
        setCompleted(true);
        clearInterval(interval);
        onComplete?.();
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [calculate, completed, onComplete]);

  if (completed && !keepZero) {
    return null;
  }

  return (
    <span className={`font-mono tabular-nums ${className ?? ''}`}>
      {formatCountdown(remaining)}
    </span>
  );
}
