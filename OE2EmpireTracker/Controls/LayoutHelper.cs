using System.Drawing;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// Shared layout helpers for common form panel sizing patterns.
    /// </summary>
    public static class LayoutHelper
    {
        /// <summary>
        /// Sizes a ListView to fill the remaining height of a FlowLayoutPanel
        /// after subtracting the filter panel, command panel, and padding.
        /// Used by forms with a search/filter + list + command button layout.
        /// </summary>
        public static void SizeListToFillPanel(
            FlowLayoutPanel container,
            Control filterPanel,
            Control commandPanel,
            ListView listView,
            int padding = 18,
            int minHeight = 50)
        {
            int w = container.ClientSize.Width;
            int h = container.ClientSize.Height;
            int listHeight = h - filterPanel.Height - commandPanel.Height - padding;
            if (listHeight < minHeight) listHeight = minHeight;
            listView.Size = new Size(w - 6, listHeight);
        }
    }
}
