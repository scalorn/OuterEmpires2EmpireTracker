import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { MasterDetailLayout } from '../MasterDetailLayout';

describe('MasterDetailLayout', () => {
  const listContent = <div data-testid="list-panel">List Content</div>;
  const detailContent = <div data-testid="detail-panel">Detail Content</div>;

  it('renders both panels', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
      />
    );
    expect(screen.getByTestId('list-panel')).toBeInTheDocument();
    expect(screen.getByTestId('detail-panel')).toBeInTheDocument();
  });

  it('shows list panel and hides detail panel on mobile when no selection', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId={null}
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    const detailContainer = screen.getByTestId('detail-panel').parentElement!;

    // List panel container should have 'flex' (visible), not 'hidden'
    expect(listContainer.className).toContain('flex');
    expect(listContainer.className).not.toMatch(/\bhidden\b/);

    // Detail panel container should have 'hidden' on mobile
    expect(detailContainer.className).toContain('hidden');
  });

  it('shows detail panel and hides list panel on mobile when item selected', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId="abc-123"
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    const detailContainer = screen.getByTestId('detail-panel').parentElement!;

    // List panel container should be hidden on mobile
    expect(listContainer.className).toContain('hidden');

    // Detail panel container should be visible
    expect(detailContainer.className).toContain('flex');
    expect(detailContainer.className).not.toMatch(/\bhidden\b/);
  });

  it('renders back button on mobile when item is selected and onBack provided', () => {
    const onBack = vi.fn();
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId="abc-123"
        onBack={onBack}
      />
    );
    expect(screen.getByText('Back to list')).toBeInTheDocument();
  });

  it('does not render back button when no selection', () => {
    const onBack = vi.fn();
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId={null}
        onBack={onBack}
      />
    );
    expect(screen.queryByText('Back to list')).not.toBeInTheDocument();
  });

  it('does not render back button when onBack is not provided', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId="abc-123"
      />
    );
    expect(screen.queryByText('Back to list')).not.toBeInTheDocument();
  });

  it('calls onBack when back button is clicked', async () => {
    const user = userEvent.setup();
    const onBack = vi.fn();
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId="abc-123"
        onBack={onBack}
      />
    );
    await user.click(screen.getByText('Back to list'));
    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('treats empty string selectedId as no selection', () => {
    const onBack = vi.fn();
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId=""
        onBack={onBack}
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    expect(listContainer.className).not.toMatch(/\bhidden\b/);
    expect(screen.queryByText('Back to list')).not.toBeInTheDocument();
  });

  it('applies custom listWidth class', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        listWidth="lg:w-1/4"
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    expect(listContainer.className).toContain('lg:w-1/4');
  });

  it('applies default listWidth class when not specified', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    expect(listContainer.className).toContain('lg:w-1/3');
  });

  it('includes md:flex on both panels for tablet/desktop visibility', () => {
    render(
      <MasterDetailLayout
        listPanel={listContent}
        detailPanel={detailContent}
        selectedId="abc-123"
      />
    );
    const listContainer = screen.getByTestId('list-panel').parentElement!;
    const detailContainer = screen.getByTestId('detail-panel').parentElement!;

    // Both panels should have md:flex so they're visible on tablet+
    expect(listContainer.className).toContain('md:flex');
    expect(detailContainer.className).toContain('md:flex');
  });
});
