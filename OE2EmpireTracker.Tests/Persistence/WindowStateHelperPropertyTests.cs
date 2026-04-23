using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Tests.Persistence
{
    /// <summary>
    /// Feature: blueprint-form-fixes, Property 3: WindowStateHelper ComboBox/CheckBox save-restore round trip
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WindowStateHelperPropertyTests
    {
        /// <summary>
        /// Property 3: WindowStateHelper ComboBox/CheckBox save-restore round trip.
        /// For any ComboBox with a non-empty Name, a list of string items (at least one),
        /// and a valid SelectedIndex, and for any CheckBox with a non-empty Name and a
        /// Checked value, saving control states via SaveControlStates and then restoring
        /// via RestoreControlStates shall produce the same SelectedIndex on the ComboBox
        /// and the same Checked value on the CheckBox.
        /// **Validates: Requirements 5.1, 5.2, 6.1, 6.2, 6.3, 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ComboBoxAndCheckBoxSaveRestoreRoundTrip()
        {
            var gen = from comboName in NonEmptyControlNameGen()
                      from items in Gen.NonEmptyListOf(ComboItemGen())
                      from checkName in NonEmptyControlNameGen()
                      from isChecked in Arb.Default.Bool().Generator
                      select new
                      {
                          ComboName = comboName,
                          Items = items.ToList(),
                          CheckName = checkName,
                          IsChecked = isChecked
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Ensure combo and checkbox have different names
                var checkName = data.CheckName == data.ComboName
                    ? data.CheckName + "_chk"
                    : data.CheckName;

                using (var form = new Form())
                {
                    // Set up ComboBox with random items and valid selected index
                    var combo = new ComboBox { Name = data.ComboName };
                    foreach (var item in data.Items)
                    {
                        combo.Items.Add(item);
                    }

                    var selectedIndex = data.Items.Count > 0
                        ? Math.Abs(data.Items.Count.GetHashCode()) % data.Items.Count
                        : 0;
                    // Use a deterministic index based on item count
                    selectedIndex = Math.Min(selectedIndex, data.Items.Count - 1);
                    combo.SelectedIndex = selectedIndex;

                    // Set up CheckBox
                    var checkBox = new CheckBox { Name = checkName, Checked = data.IsChecked };

                    form.Controls.Add(combo);
                    form.Controls.Add(checkBox);

                    // Save
                    var formState = new FormControlState();
                    WindowStateHelper.SaveControlStates(form, formState);

                    // Create a fresh form with same controls but default state
                    using (var form2 = new Form())
                    {
                        var combo2 = new ComboBox { Name = data.ComboName };
                        foreach (var item in data.Items)
                        {
                            combo2.Items.Add(item);
                        }

                        // combo2 starts at default (SelectedIndex = -1 or 0)

                        var checkBox2 = new CheckBox { Name = checkName, Checked = !data.IsChecked };

                        form2.Controls.Add(combo2);
                        form2.Controls.Add(checkBox2);

                        // Restore
                        WindowStateHelper.RestoreControlStates(form2, formState);

                        var comboMatch = (combo2.SelectedIndex == selectedIndex)
                            .Label($"ComboBox SelectedIndex: expected {selectedIndex}, got {combo2.SelectedIndex}");

                        var checkMatch = (checkBox2.Checked == data.IsChecked)
                            .Label($"CheckBox Checked: expected {data.IsChecked}, got {checkBox2.Checked}");

                        return comboMatch.And(checkMatch);
                    }
                }
            });
        }

        /// <summary>
        /// When the saved combo value no longer exists in the ComboBox's items
        /// AND the saved index is also out of range, the ComboBox shall remain
        /// at its default index (-1, unselected).
        /// **Validates: Requirement 6.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ComboBoxStaysAtDefaultWhenSavedValueMissing()
        {
            var gen = from comboName in NonEmptyControlNameGen()
                      from savedItemCount in Gen.Choose(3, 10)
                      from newItemCount in Gen.Choose(1, 2)
                      select new
                      {
                          ComboName = comboName,
                          SavedItemCount = savedItemCount,
                          NewItemCount = newItemCount
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                using (var form = new Form())
                {
                    // Set up ComboBox with many items and select the LAST one
                    var combo = new ComboBox { Name = data.ComboName };
                    for (int i = 0; i < data.SavedItemCount; i++)
                    {
                        combo.Items.Add("SavedItem_" + i);
                    }

                    // Select the last item -- its index will be out of range in the smaller new list
                    combo.SelectedIndex = data.SavedItemCount - 1;

                    // Save
                    var formState = new FormControlState();
                    WindowStateHelper.SaveControlStates(form, formState);

                    // Create a new form with FEWER items that don't contain the saved value
                    using (var form2 = new Form())
                    {
                        var combo2 = new ComboBox { Name = data.ComboName };
                        for (int i = 0; i < data.NewItemCount; i++)
                        {
                            combo2.Items.Add("NewItem_" + i);
                        }

                        form2.Controls.Add(combo2);

                        // Restore -- saved value not in items, saved index out of range
                        WindowStateHelper.RestoreControlStates(form2, formState);

                        // Neither value nor index valid -- stays at default (-1)
                        return (combo2.SelectedIndex == -1)
                            .Label($"Expected default index -1, got {combo2.SelectedIndex}");
                    }
                }
            });
        }

        private static Gen<string> NonEmptyControlNameGen()
        {
            return Gen.Elements(
                "cmbFilterType", "cmbFilterClass", "cmbFilterTechLevel",
                "cmbFilterEvolution", "chkEvolutionAndAbove", "cmbStatus",
                "cmbCategory", "cmbRegion", "chkActive", "chkVisible");
        }

        private static Gen<string> ComboItemGen()
        {
            return Gen.Elements(
                string.Empty, "All", "Hull", "Shield", "Reactor", "Main Drive",
                "Weapon", "Flatpack", "LL", "ML", "HL", "Milspec",
                "Class 1", "Class 2", "Class 3", "Evo 0", "Evo 1");
        }
    }
}
