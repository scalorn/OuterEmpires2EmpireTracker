using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Tests.Controls
{
    /// <summary>
    /// Property-based tests for FilteredTextComboSet value list support.
    /// Feature: filtered-combo-adoption
    /// </summary>
    [TestFixture]
    public class FilteredTextComboSetPropertyTests
    {
        private class TestableFilteredTextComboSet : FilteredTextComboSet
        {
            public new bool SuppressSelectionEvent
            {
                get => base.SuppressSelectionEvent;
                set => base.SuppressSelectionEvent = value;
            }

            public new bool IsEditing
            {
                get => base.IsEditing;
                set => base.IsEditing = value;
            }

            public TextBox FilterTextBox => TxtFilter;

            public ComboBox ComboBox => CmbItems;
        }

        // Feature: filtered-combo-adoption, Property 1: Value list index mapping through filter
        /// <summary>
        /// For any display list, parallel value list of equal length, and any filter string,
        /// selecting a filtered item returns the correct value from the original value list.
        /// When no value list is provided, SelectedValue is always null.
        /// Validates: Requirements 1.2, 1.3, 1.4, 1.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property ValueListIndexMapping_ThroughFilter()
        {
            var nonNullStringGen = Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta", "iota", "kappa")
                .ArrayOf()
                .Select(arr => arr.ToList())
                .Where(list => list.Count > 0);

            var filterGen = Gen.Elements(string.Empty, "a", "e", "ta", "al", "pp");

            return Prop.ForAll(
                nonNullStringGen.ToArbitrary(),
                filterGen.ToArbitrary(),
                (displayItems, filter) =>
                {
                    var values = displayItems.Select((_, i) => $"val-{i}").ToList();

                    using (var control = new TestableFilteredTextComboSet())
                    {
                        // Test with value list: selecting filtered item returns correct value
                        control.SetItems(displayItems, values, null);

                        // Apply filter
                        var (filtered, indexMap) = FilteredTextComboSet.ApplyFilter(displayItems, filter);

                        bool valueListCorrect = true;
                        if (filtered.Count > 0)
                        {
                            // Simulate filter by setting items and selecting each filtered item
                            control.IsEditing = true;
                            control.FilterTextBox.Text = filter;
                            control.SetItems(displayItems, values, null);

                            for (int i = 0; i < control.ComboBox.Items.Count && i < filtered.Count; i++)
                            {
                                control.ComboBox.SelectedIndex = i;
                                string expectedValue = values[indexMap[i]];
                                if (control.SelectedValue != expectedValue)
                                {
                                    valueListCorrect = false;
                                    break;
                                }
                            }
                        }

                        // Test without value list: SelectedValue is always null
                        control.IsEditing = false;
                        control.SetItems(displayItems, null);
                        bool noValueListReturnsNull = control.SelectedValue == null;
                        if (control.ComboBox.Items.Count > 0)
                        {
                            control.ComboBox.SelectedIndex = 0;
                            noValueListReturnsNull = control.SelectedValue == null;
                        }

                        return (valueListCorrect && noValueListReturnsNull)
                            .Label($"Value list correct: {valueListCorrect}, no value list returns null: {noValueListReturnsNull}");
                    }
                });
        }

        // Feature: filtered-combo-adoption, Property 2: currentValue pre-selection via value list
        /// <summary>
        /// For any display list, parallel value list, and currentValue that exists in the value list,
        /// SetItems selects the display item at the matching value's index.
        /// If currentValue does not exist, no item is selected.
        /// Validates: Requirements 1.6, 2.5, 6.5, 8.5, 10.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CurrentValuePreSelection_ViaValueList()
        {
            var nonNullStringGen = Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon")
                .ArrayOf()
                .Select(arr => arr.ToList())
                .Where(list => list.Count > 0);

            return Prop.ForAll(
                nonNullStringGen.ToArbitrary(),
                (displayItems) =>
                {
                    var values = displayItems.Select((_, i) => $"uuid-{i}").ToList();

                    using (var control = new TestableFilteredTextComboSet())
                    {
                        // Pick a random existing value to pre-select
                        var rng = new System.Random(displayItems.Count);
                        int targetIdx = rng.Next(values.Count);
                        string existingValue = values[targetIdx];

                        control.SetItems(displayItems, values, existingValue);

                        bool existingSelected = control.SelectedItem == displayItems[targetIdx]
                            && control.SelectedValue == existingValue;

                        // Test with non-existing value: no item selected
                        control.SetItems(displayItems, values, "non-existent-uuid");
                        bool nonExistingNotSelected = control.SelectedItem == null
                            || control.ComboBox.SelectedIndex < 0;

                        return (existingSelected && nonExistingNotSelected)
                            .Label($"Existing selected: {existingSelected} (item={control.SelectedItem}), non-existing not selected: {nonExistingNotSelected}");
                    }
                });
        }

        // Feature: filtered-combo-adoption, Property 3: SetItems clears filter when not editing
        /// <summary>
        /// For any previous state and new item list, calling SetItems when not editing
        /// results in empty filter text and all items displayed.
        /// Validates: Requirements 11.2
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property SetItemsClearsFilter_WhenNotEditing()
        {
            var itemsGen = Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon", "zeta")
                .ArrayOf()
                .Select(arr => arr.ToList())
                .Where(list => list.Count > 0);

            return Prop.ForAll(
                itemsGen.ToArbitrary(),
                itemsGen.ToArbitrary(),
                (initialItems, newItems) =>
                {
                    using (var control = new TestableFilteredTextComboSet())
                    {
                        // Set up initial state with some filter
                        control.IsEditing = true;
                        control.SetItems(initialItems, null);
                        control.FilterTextBox.Text = "al";

                        // Now leave edit mode and call SetItems
                        control.IsEditing = false;
                        control.SetItems(newItems, null);

                        bool filterCleared = control.FilterTextBox.Text == string.Empty;
                        bool allItemsDisplayed = control.ComboBox.Items.Count == newItems.Count;

                        return (filterCleared && allItemsDisplayed)
                            .Label($"Filter cleared: {filterCleared}, all items displayed: {allItemsDisplayed} ({control.ComboBox.Items.Count} vs {newItems.Count})");
                    }
                });
        }

        // Feature: filtered-combo-adoption, Property 4: SetItems preserves filter when editing
        /// <summary>
        /// For any filter text and new item list, calling SetItems while editing preserves
        /// the filter and displays only matching items.
        /// Validates: Requirements 11.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property SetItemsPreservesFilter_WhenEditing()
        {
            var itemsGen = Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta")
                .ArrayOf()
                .Select(arr => arr.ToList())
                .Where(list => list.Count > 0);

            var filterGen = Gen.Elements("a", "e", "ta", "al", "th");

            return Prop.ForAll(
                itemsGen.ToArbitrary(),
                filterGen.ToArbitrary(),
                (newItems, filter) =>
                {
                    using (var control = new TestableFilteredTextComboSet())
                    {
                        // Enter edit mode and set a filter
                        control.IsEditing = true;
                        control.SetItems(newItems, null);
                        control.FilterTextBox.Text = filter;

                        // Call SetItems again while editing - filter should be preserved
                        control.SetItems(newItems, null);

                        bool filterPreserved = control.FilterTextBox.Text == filter;

                        // Compute expected filtered items
                        var (expectedFiltered, _) = FilteredTextComboSet.ApplyFilter(newItems, filter);
                        bool onlyMatchingDisplayed = control.ComboBox.Items.Count == expectedFiltered.Count;

                        return (filterPreserved && onlyMatchingDisplayed)
                            .Label($"Filter preserved: {filterPreserved} ('{control.FilterTextBox.Text}' vs '{filter}'), matching displayed: {onlyMatchingDisplayed} ({control.ComboBox.Items.Count} vs {expectedFiltered.Count})");
                    }
                });
        }

        // Feature: filtered-combo-adoption, Property 5: Event suppression during programmatic updates
        /// <summary>
        /// For any item list, calling SetItems does not fire SelectedItemChanged.
        /// Validates: Requirements 12.2
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property EventSuppression_DuringProgrammaticUpdates()
        {
            var itemsGen = Gen.Elements("alpha", "beta", "gamma", "delta", "epsilon")
                .ArrayOf()
                .Select(arr => arr.ToList())
                .Where(list => list.Count > 0);

            return Prop.ForAll(
                itemsGen.ToArbitrary(),
                (items) =>
                {
                    using (var control = new TestableFilteredTextComboSet())
                    {
                        bool eventFired = false;
                        control.SelectedItemChanged += (s, e) => eventFired = true;

                        // Call SetItems with a currentValue that would cause selection
                        string currentValue = items[0];
                        control.SetItems(items, currentValue);

                        bool noEventOnPlainSetItems = !eventFired;

                        // Also test with value list overload
                        eventFired = false;
                        var values = items.Select((_, i) => $"val-{i}").ToList();
                        control.SetItems(items, values, values[0]);

                        bool noEventOnValueSetItems = !eventFired;

                        return (noEventOnPlainSetItems && noEventOnValueSetItems)
                            .Label($"No event on plain SetItems: {noEventOnPlainSetItems}, no event on value SetItems: {noEventOnValueSetItems}");
                    }
                });
        }
    }
}