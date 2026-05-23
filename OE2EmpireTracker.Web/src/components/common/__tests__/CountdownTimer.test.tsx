import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { CountdownTimer } from '../CountdownTimer';

/**
 * Unit tests for CountdownTimer component.
 * Validates: Requirements 11.2, 18.3, 18.5
 */

describe('CountdownTimer', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('displays formatted countdown for a future target time', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:05:00Z'; // 5 minutes away
    render(<CountdownTimer targetTime={target} />);

    expect(screen.getByText('05:00')).toBeInTheDocument();
  });

  it('decrements each second', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:00:03Z'; // 3 seconds away
    render(<CountdownTimer targetTime={target} />);

    expect(screen.getByText('00:03')).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(screen.getByText('00:02')).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(screen.getByText('00:01')).toBeInTheDocument();
  });

  it('calls onComplete when timer reaches zero', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const onComplete = vi.fn();
    const target = '2024-01-01T12:00:02Z'; // 2 seconds away
    render(<CountdownTimer targetTime={target} onComplete={onComplete} />);

    act(() => {
      vi.advanceTimersByTime(2000);
    });

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('returns null (hides) when expired and keepZero is false', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:00:01Z';
    const { container } = render(<CountdownTimer targetTime={target} keepZero={false} />);

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(container.firstChild).toBeNull();
  });

  it('displays 00:00 when expired and keepZero is true', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:00:01Z';
    render(<CountdownTimer targetTime={target} keepZero={true} />);

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(screen.getByText('00:00')).toBeInTheDocument();
  });

  it('shows 00:00 immediately for already-expired target with keepZero', () => {
    const now = new Date('2024-01-01T12:05:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:00:00Z'; // already past
    render(<CountdownTimer targetTime={target} keepZero={true} />);

    expect(screen.getByText('00:00')).toBeInTheDocument();
  });

  it('returns null immediately for already-expired target without keepZero', () => {
    const now = new Date('2024-01-01T12:05:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:00:00Z'; // already past
    const { container } = render(<CountdownTimer targetTime={target} keepZero={false} />);

    expect(container.firstChild).toBeNull();
  });

  it('applies className and uses monospace font', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T12:01:00Z';
    render(<CountdownTimer targetTime={target} className="text-red-500" />);

    const el = screen.getByText('01:00');
    expect(el).toHaveClass('font-mono', 'tabular-nums', 'text-red-500');
  });

  it('displays HH:MM:SS format for times over 1 hour', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    vi.setSystemTime(now);

    const target = '2024-01-01T13:30:00Z'; // 1.5 hours away
    render(<CountdownTimer targetTime={target} />);

    expect(screen.getByText('01:30:00')).toBeInTheDocument();
  });
});
