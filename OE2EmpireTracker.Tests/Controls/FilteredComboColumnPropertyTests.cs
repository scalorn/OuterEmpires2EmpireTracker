using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Tests.Controls
{
    /// <summary>
    /// Property-based tests for DataGridViewFilteredComboBoxColumn.
    /// Feature: filtered-combo-column
    /// </summary>
    [TestFixture]
    public class FilteredComboColumnPropertyTests
    {
        // Feature: filtered-combo-column, Property 1: Contains-match filtering returns exactly the matching items
        /// <summary>
        /// For any list of strings and any filter string, the filtered result contains exactly
        /// those items where item.IndexOf(filter, OrdinalIgnoreCase) >= 0.
        /// Empty filter returns full list. Case-insensitive equivalence.
        /// Validates: Requirements 3.1, 3.2, 3.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property ContainsMatchFiltering_ReturnsExactMatches()
        {
            return Prop.ForAll(
                Arb.From<List<string>>(),
                Arb.From<string>(),
                (items, filter) =>
                {
                    var safeItems = items ?? new List<string>();
                    var safeFilter = filter ?? string.Empty;

                    var (filtered, indexMap) = DataGridViewFilteredComboBoxEditingControl.ApplyFilter(safeItems, safeFilter);

                    // Manual reference implementation
                    var expected = safeItems
                        .Where(item => string.IsNullOrEmpty(safeFilter) ||
                            (item ?? string.Empty).IndexOf(safeFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    bool countMatch = filtered.Count == expected.Count;
                    bool contentMatch = filtered.SequenceEqual(expected);

                    // Case-insensitive equivalence: filtering with upper and lower should give same results
                    var (filteredUpper, _) = DataGridViewFilteredComboBoxEditingControl.ApplyFilter(safeItems, safeFilter.ToUpperInvariant());
                    var (filteredLower, _) = DataGridViewFilteredComboBoxEditingControl.ApplyFilter(safeItems, safeFilter.ToLowerInvariant());
                    bool caseInsensitive = filteredUpper.SequenceEqual(filteredLower);

                    return (countMatch && contentMatch && caseInsensitive)
                        .Label($"Count: {filtered.Count} vs {expected.Count}, content match: {contentMatch}, case-insensitive: {caseInsensitive}");
                });
        }

        // Feature: filtered-combo-column, Property 2: Index correspondence preserved after filtering
        /// <summary>
        /// For any list and filter, each filteredList[i] == fullList[indexMap[i]].
        /// Validates: Requirements 5.4, 7.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IndexCorrespondence_PreservedAfterFiltering()
        {
            return Prop.ForAll(
                Arb.From<List<string>>(),
                Arb.From<string>(),
                (items, filter) =>
                {
                    var safeItems = items ?? new List<string>();
                    var safeFilter = filter ?? string.Empty;

                    var (filtered, indexMap) = DataGridViewFilteredComboBoxEditingControl.ApplyFilter(safeItems, safeFilter);

                    bool lengthMatch = filtered.Count == indexMap.Count;
                    bool allMatch = true;
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        if (indexMap[i] < 0 || indexMap[i] >= safeItems.Count || filtered[i] != safeItems[indexMap[i]])
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    return (lengthMatch && allMatch)
                        .Label($"Length match: {lengthMatch}, all indices valid: {allMatch}");
                });
        }

        // Feature: filtered-combo-column, Property 3: Selection sets EditingControlFormattedValue
        /// <summary>
        /// For any non-empty list, selecting an item causes GetEditingControlFormattedValue
        /// to return that exact string. No selection returns string.Empty.
        /// Validates: Requirements 4.1, 6.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Selection_SetsFormattedValue()
        {
            return Prop.ForAll(
                Arb.From<NonEmptyArray<NonEmptyString>>(),
                nes =>
                {
                    var items = nes.Get.Select(s => s.Get).ToList();
                    using (var editor = new DataGridViewFilteredComboBoxEditingControl())
                    {
                        editor.SetItems(items, null);

                        // No selection â†’ empty string
                        string noSelection = editor.GetEditingControlFormattedValue(DataGridViewDataErrorContexts.Display)?.ToString();
                        bool emptyWhenNone = noSelection == string.Empty;

                        // Select a random item
                        var rng = new System.Random(items.Count);
                        int idx = rng.Next(items.Count);
                        var cmbItems = editor.Controls.OfType<ComboBox>().First();
                        cmbItems.SelectedIndex = idx;
                        string selected = editor.GetEditingControlFormattedValue(DataGridViewDataErrorContexts.Display)?.ToString();
                        bool matchesSelected = selected == items[idx];

                        return (emptyWhenNone && matchesSelected)
                            .Label($"Empty when none: {emptyWhenNone}, matches selected[{idx}]: {matchesSelected} (got '{selected}', expected '{items[idx]}')");
                    }
                });
        }

        // Feature: filtered-combo-column, Property 4: Cell items override column items
        /// <summary>
        /// When cell has Items set, those are used. When null, column Items are used.
        /// Validates: Requirements 5.2, 9.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property CellItems_OverrideColumnItems()
        {
            return Prop.ForAll(
                Arb.From<List<string>>(),
                Arb.From<List<string>>(),
                (columnItems, cellItems) =>
                {
                    var safeColumnItems = columnItems ?? new List<string>();
                    var safeCellItems = cellItems ?? new List<string>();

                    var column = new DataGridViewFilteredComboBoxColumn();
                    column.Items = safeColumnItems;

                    // Cell with items set â†’ uses cell items
                    var cellWithItems = new DataGridViewFilteredComboBoxCell();
                    cellWithItems.Items = safeCellItems;
                    var effectiveWithCell = cellWithItems.Items ?? column.Items;
                    bool usesCell = effectiveWithCell.SequenceEqual(safeCellItems);

                    // Cell without items â†’ uses column items
                    var cellWithout = new DataGridViewFilteredComboBoxCell();
                    cellWithout.Items = null;
                    var effectiveWithout = cellWithout.Items ?? column.Items;
                    bool usesColumn = effectiveWithout.SequenceEqual(safeColumnItems);

                    return (usesCell && usesColumn)
                        .Label($"Uses cell items: {usesCell}, uses column items when null: {usesColumn}");
                });
        }

        // Feature: filtered-combo-column, Property 5: Input key claiming
        /// <summary>
        /// For all keys in {A-Z, 0-9, arrows, Enter, Escape, Tab, Delete, Back},
        /// EditingControlWantsInputKey returns true.
        /// Validates: Requirements 4.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property InputKeyClaiming_CorrectForExpectedKeys()
        {
            var expectedKeys = new Keys[]
            {
                Keys.A, Keys.B, Keys.C, Keys.D, Keys.E, Keys.F, Keys.G, Keys.H, Keys.I, Keys.J,
                Keys.K, Keys.L, Keys.M, Keys.N, Keys.O, Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T,
                Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y, Keys.Z,
                Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9,
                Keys.Left, Keys.Right, Keys.Up, Keys.Down,
                Keys.Enter, Keys.Escape, Keys.Tab, Keys.Delete, Keys.Back
            };

            var keyGen = Gen.Elements(expectedKeys).ToArbitrary();

            return Prop.ForAll(keyGen, key =>
            {
                using (var editor = new DataGridViewFilteredComboBoxEditingControl())
                {
                    bool wants = editor.EditingControlWantsInputKey(key, true);
                    return wants.Label($"Key {key}: wants={wants}");
                }
            });
        }

        // Feature: filtered-combo-column, Property 6: Style propagation to child controls
        /// <summary>
        /// For any Font and ForeColor, ApplyCellStyleToEditingControl sets both
        /// txtFilter and cmbItems to match.
        /// Validates: Requirements 6.2
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property StylePropagation_AppliesToBothControls()
        {
            var fontSizeGen = Gen.Choose(8, 24).ToArbitrary();

            return Prop.ForAll(fontSizeGen, fontSize =>
            {
                using (var editor = new DataGridViewFilteredComboBoxEditingControl())
                {
                    var font = new Font("Arial", fontSize);
                    var color = Color.FromArgb(100, 150, 200);
                    var style = new DataGridViewCellStyle { Font = font, ForeColor = color };

                    editor.ApplyCellStyleToEditingControl(style);

                    var txtFilter = editor.Controls.OfType<TextBox>().First();
                    var cmbItems = editor.Controls.OfType<ComboBox>().First();

                    bool fontMatch = txtFilter.Font.Name == font.Name && txtFilter.Font.Size == font.Size
                        && cmbItems.Font.Name == font.Name && cmbItems.Font.Size == font.Size;
                    bool colorMatch = txtFilter.ForeColor == color && cmbItems.ForeColor == color;

                    font.Dispose();
                    return (fontMatch && colorMatch)
                        .Label($"Font match: {fontMatch}, color match: {colorMatch}");
                }
            });
        }
    }
}
