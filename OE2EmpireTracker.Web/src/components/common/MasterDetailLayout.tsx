import { type ReactNode } from 'react';

export interface MasterDetailLayoutProps {
  /** Left panel content: filterable entity list */
  listPanel: ReactNode;
  /** Right panel content: selected entity details */
  detailPanel: ReactNode;
  /** Tailwind width class for the list panel at lg breakpoint. Default: 'lg:w-1/3' */
  listWidth?: string;
  /** Controls mobile panel visibility — when set, shows detail panel on mobile */
  selectedId?: string | null;
  /** Mobile: callback to return to list view */
  onBack?: () => void;
}

/**
 * A responsive master-detail layout component.
 *
 * - Desktop (≥1024px): side-by-side list + detail panels
 * - Tablet (768–1023px): narrower list panel, detail fills remaining space
 * - Mobile (<768px): single panel — shows list when no selection,
 *   shows detail with back button when an item is selected
 */
export function MasterDetailLayout({
  listPanel,
  detailPanel,
  listWidth = 'lg:w-1/3',
  selectedId,
  onBack,
}: MasterDetailLayoutProps) {
  const hasSelection = selectedId != null && selectedId !== '';

  return (
    <div className="flex h-full min-h-0 flex-col md:flex-row">
      {/* List panel: hidden on mobile when item selected, always visible on md+ */}
      <div
        className={[
          hasSelection ? 'hidden' : 'flex',
          'min-h-0 flex-col overflow-y-auto border-r border-gray-700',
          'md:flex md:w-2/5',
          listWidth,
        ].join(' ')}
      >
        {listPanel}
      </div>

      {/* Detail panel: hidden on mobile when no item selected, always visible on md+ */}
      <div
        className={[
          hasSelection ? 'flex' : 'hidden',
          'min-h-0 flex-1 flex-col overflow-y-auto',
          'md:flex',
        ].join(' ')}
      >
        {/* Mobile back button — only visible below md breakpoint */}
        {hasSelection && onBack && (
          <div className="border-b border-gray-700 p-2 md:hidden">
            <button
              onClick={onBack}
              className="inline-flex items-center gap-1 rounded px-3 py-1.5 text-sm text-gray-300 hover:bg-gray-800 hover:text-white"
            >
              <span aria-hidden="true">←</span>
              <span>Back to list</span>
            </button>
          </div>
        )}
        {detailPanel}
      </div>
    </div>
  );
}
