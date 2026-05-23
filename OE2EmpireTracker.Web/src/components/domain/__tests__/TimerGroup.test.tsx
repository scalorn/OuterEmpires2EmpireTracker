import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TimerGroup } from '../TimerGroup';

/**
 * Unit tests for TimerGroup component.
 * Validates: Requirements 11.1, 11.2
 */

describe('TimerGroup', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-01-01T12:00:00Z'));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders the group title and count badge', () => {
    const timers = [
      { id: '1', label: 'Iron Ore', targetTime: '2024-01-01T12:05:00Z' },
      { id: '2', label: 'Copper Ore', targetTime: '2024-01-01T12:10:00Z' },
    ];

    render(<TimerGroup title="Mining" timers={timers} />);

    expect(screen.getByText('Mining')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('renders an icon when provided', () => {
    const timers = [
      { id: '1', label: 'Steel', targetTime: '2024-01-01T12:05:00Z' },
    ];

    render(<TimerGroup title="Refining" timers={timers} icon={<span data-testid="icon">⚒</span>} />);

    expect(screen.getByTestId('icon')).toBeInTheDocument();
  });

  it('displays timer labels and countdown values', () => {
    const timers = [
      { id: '1', label: 'Iron Ore', targetTime: '2024-01-01T12:05:00Z' },
    ];

    render(<TimerGroup title="Mining" timers={timers} />);

    expect(screen.getByText('Iron Ore')).toBeInTheDocument();
    expect(screen.getByText('05:00')).toBeInTheDocument();
  });

  it('displays colony name when provided', () => {
    const timers = [
      { id: '1', label: 'Iron Ore', targetTime: '2024-01-01T12:05:00Z', colonyName: 'Alpha Base' },
    ];

    render(<TimerGroup title="Mining" timers={timers} />);

    expect(screen.getByText('Alpha Base')).toBeInTheDocument();
  });

  it('shows "No active timers" when all timers are expired', () => {
    const timers = [
      { id: '1', label: 'Iron Ore', targetTime: '2024-01-01T11:00:00Z' },
      { id: '2', label: 'Copper Ore', targetTime: '2024-01-01T11:30:00Z' },
    ];

    render(<TimerGroup title="Mining" timers={timers} />);

    expect(screen.getByText('No active timers')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('shows "No active timers" when timers array is empty', () => {
    render(<TimerGroup title="Research" timers={[]} />);

    expect(screen.getByText('No active timers')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('filters out expired timers and only shows active ones', () => {
    const timers = [
      { id: '1', label: 'Expired Task', targetTime: '2024-01-01T11:00:00Z' },
      { id: '2', label: 'Active Task', targetTime: '2024-01-01T12:10:00Z' },
    ];

    render(<TimerGroup title="Manufacturing" timers={timers} />);

    expect(screen.queryByText('Expired Task')).not.toBeInTheDocument();
    expect(screen.getByText('Active Task')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });
});
