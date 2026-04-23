using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ListViewItemComparerTests
    {
        /// <summary>
        /// Helper: creates a ListViewItem with a SubItem at the given column index,
        /// optionally setting the SubItem's Tag and Text.
        /// Column 0 is the item itself; columns 1+ are additional SubItems.
        /// </summary>
        private ListViewItem CreateItem(int columnIndex, string text, object tag = null)
        {
            var item = new ListViewItem("row");
            // Pad SubItems up to the target column index
            for (int i = 1; i <= columnIndex; i++)
                item.SubItems.Add(string.Empty);

            item.SubItems[columnIndex].Text = text;
            item.SubItems[columnIndex].Tag = tag;
            return item;
        }

        // -------------------------------------------------------------------
        // Tag-based ISO string sorting (chronological)
        // Validates: Requirements 4.1, 4.2
        // -------------------------------------------------------------------

        [Test]
        public void Compare_WithIsoTags_SortsChronologically_Ascending()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var earlier = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");
            var later   = CreateItem(1, "19FEB26-08:41p", "2026-02-19T20:41:00");

            int result = comparer.Compare(earlier, later);
            Assert.That(result, Is.LessThan(0), "Earlier ISO date should sort before later date in ascending order");
        }

        [Test]
        public void Compare_WithIsoTags_SortsChronologically_Descending()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Descending);

            var earlier = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");
            var later   = CreateItem(1, "19FEB26-08:41p", "2026-02-19T20:41:00");

            int result = comparer.Compare(earlier, later);
            Assert.That(result, Is.GreaterThan(0), "Earlier ISO date should sort after later date in descending order");
        }

        [Test]
        public void Compare_WithIsoTags_EqualDates_ReturnsZero()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var a = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");
            var b = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");

            int result = comparer.Compare(a, b);
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Compare_WithIsoTags_SameMonthDifferentDay_SortsCorrectly()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var day1  = CreateItem(1, "01JAN25-09:00a", "2025-01-01T09:00:00");
            var day15 = CreateItem(1, "15JAN25-09:00a", "2025-01-15T09:00:00");

            Assert.That(comparer.Compare(day1, day15), Is.LessThan(0));
            Assert.That(comparer.Compare(day15, day1), Is.GreaterThan(0));
        }

        // -------------------------------------------------------------------
        // Fallback to display-text comparison when Tags are null
        // Validates: Requirements 4.3
        // -------------------------------------------------------------------

        [Test]
        public void Compare_WithNullTags_FallsBackToTextComparison_Ascending()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var a = CreateItem(1, "Alpha", null);
            var b = CreateItem(1, "Beta", null);

            int result = comparer.Compare(a, b);
            Assert.That(result, Is.LessThan(0), "Should sort alphabetically when Tags are null");
        }

        [Test]
        public void Compare_WithNullTags_FallsBackToTextComparison_Descending()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Descending);

            var a = CreateItem(1, "Alpha", null);
            var b = CreateItem(1, "Beta", null);

            int result = comparer.Compare(a, b);
            Assert.That(result, Is.GreaterThan(0), "Should sort reverse-alphabetically in descending when Tags are null");
        }

        [Test]
        public void Compare_WithNullTags_NumericText_SortsNumerically()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var two = CreateItem(1, "2", null);
            var ten = CreateItem(1, "10", null);

            int result = comparer.Compare(two, ten);
            Assert.That(result, Is.LessThan(0), "Numeric text should sort numerically, not lexicographically");
        }

        [Test]
        public void Compare_OneTagNull_FallsBackToTextComparison()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            var withTag    = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");
            var withoutTag = CreateItem(1, "19FEB26-08:41p", null);

            // When one tag is null, should fall back to display text comparison
            int result = comparer.Compare(withTag, withoutTag);
            // "19FEB26..." < "27JUL24..." lexicographically, so withoutTag < withTag
            Assert.That(result, Is.GreaterThan(0));
        }

        // -------------------------------------------------------------------
        // Tag-based comparison takes precedence over display text
        // Validates: Requirements 4.1, 4.2
        // -------------------------------------------------------------------

        [Test]
        public void Compare_TagTakesPrecedenceOverDisplayText()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            // Display text would sort "A" before "Z", but Tags reverse the order
            var itemA = CreateItem(1, "A-display", "ZZZ");
            var itemZ = CreateItem(1, "Z-display", "AAA");

            int result = comparer.Compare(itemA, itemZ);
            Assert.That(result, Is.GreaterThan(0),
                "Tag-based comparison should take precedence: 'ZZZ' > 'AAA' regardless of display text");
        }

        [Test]
        public void Compare_IsoTagOverridesGameFormatDisplayText()
        {
            var comparer = new ListViewItemComparer(1, SortOrder.Ascending);

            // Display text in game format would sort incorrectly (19FEB < 27JUL lexicographically)
            // but ISO tags sort correctly (2026 > 2024)
            var older = CreateItem(1, "27JUL24-11:44p", "2024-07-27T23:44:00");
            var newer = CreateItem(1, "19FEB26-08:41p", "2026-02-19T20:41:00");

            int result = comparer.Compare(older, newer);
            Assert.That(result, Is.LessThan(0),
                "ISO tag should sort 2024 before 2026, even though display text '27JUL' > '19FEB'");
        }

        // -------------------------------------------------------------------
        // Column and Order properties
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_SetsColumnAndOrder()
        {
            var comparer = new ListViewItemComparer(4, SortOrder.Descending);
            Assert.That(comparer.Column, Is.EqualTo(4));
            Assert.That(comparer.Order, Is.EqualTo(SortOrder.Descending));
        }
    }
}
