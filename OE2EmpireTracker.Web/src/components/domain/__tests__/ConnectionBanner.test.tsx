import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ConnectionBanner } from '../ConnectionBanner';

describe('ConnectionBanner', () => {
  it('renders nothing when connected', () => {
    const { container } = render(<ConnectionBanner status="connected" />);
    expect(container).toBeEmptyDOMElement();
  });

  it('renders reconnecting banner with spinner', () => {
    render(<ConnectionBanner status="reconnecting" />);
    expect(screen.getByText('Reconnecting...')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders disconnected banner with connection lost text', () => {
    render(<ConnectionBanner status="disconnected" />);
    expect(screen.getByText('Connection lost')).toBeInTheDocument();
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('renders retry button when onRetry is provided in disconnected state', () => {
    const onRetry = vi.fn();
    render(<ConnectionBanner status="disconnected" onRetry={onRetry} />);
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('does not render retry button when onRetry is not provided', () => {
    render(<ConnectionBanner status="disconnected" />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('calls onRetry when retry button is clicked', () => {
    const onRetry = vi.fn();
    render(<ConnectionBanner status="disconnected" onRetry={onRetry} />);

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('does not render retry button in reconnecting state', () => {
    const onRetry = vi.fn();
    render(<ConnectionBanner status="reconnecting" onRetry={onRetry} />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
