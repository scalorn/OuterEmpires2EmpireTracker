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
            string textX = ((ListViewItem)x).SubItems[_column].Text;
            string textY = ((ListViewItem)y).SubItems[_column].Text;

            int result;
            if (int.TryParse(textX, out int numX) && int.TryParse(textY, out int numY))
                result = numX.CompareTo(numY);
            else
                result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);

            return _order == SortOrder.Descending ? -result : result;
        }
    }
}
