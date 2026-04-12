using System;
using System.Collections;
using System.Windows.Forms;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// Compares ListView items by a specified column for sorting.
    /// Numeric values are compared numerically; all others lexicographically.
    /// </summary>
    public class ListViewItemComparer : IComparer
    {
        private readonly int _column;
        private readonly SortOrder _order;

        public int Column => _column;
        public SortOrder Order => _order;

        public ListViewItemComparer(int column, SortOrder order)
        {
            _column = column;
            _order = order;
        }

        public int Compare(object x, object y)
        {
            var subItemX = ((ListViewItem)x).SubItems[_column];
            var subItemY = ((ListViewItem)y).SubItems[_column];

            // If both SubItems have non-null Tags, compare Tags as strings.
            // This enables ISO-string-based chronological sorting for date columns
            // without hardcoding column indices.
            if (subItemX.Tag != null && subItemY.Tag != null)
            {
                int tagResult = string.Compare(
                    subItemX.Tag.ToString(), subItemY.Tag.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                return _order == SortOrder.Descending ? -tagResult : tagResult;
            }

            string textX = subItemX.Text;
            string textY = subItemY.Text;

            int result;
            if (int.TryParse(textX, out int numX) && int.TryParse(textY, out int numY))
                result = numX.CompareTo(numY);
            else
                result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);

            return _order == SortOrder.Descending ? -result : result;
        }
    }
}
