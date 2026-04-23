using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Tests.Controls
{
    /// <summary>
    /// Unit tests for DataGridViewFilteredComboBoxColumn structural and edge-case behavior.
    /// Feature: filtered-combo-column
    /// </summary>
    [TestFixture]
    public class FilteredComboColumnTests
    {
        [Test]
        public void Column_CellTemplate_IsFilteredComboBoxCell()
        {
            var column = new DataGridViewFilteredComboBoxColumn();
            Assert.That(column.CellTemplate, Is.InstanceOf<DataGridViewFilteredComboBoxCell>());
        }

        [Test]
        public void Cell_EditType_IsEditingControl()
        {
            var cell = new DataGridViewFilteredComboBoxCell();
            Assert.That(cell.EditType, Is.EqualTo(typeof(DataGridViewFilteredComboBoxEditingControl)));
        }

        [Test]
        public void Cell_ValueType_IsString()
        {
            var cell = new DataGridViewFilteredComboBoxCell();
            Assert.That(cell.ValueType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Cell_DefaultNewRowValue_IsEmptyString()
        {
            var cell = new DataGridViewFilteredComboBoxCell();
            Assert.That(cell.DefaultNewRowValue, Is.EqualTo(string.Empty));
        }

        [Test]
        public void EditingControl_ImplementsIDataGridViewEditingControl()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                Assert.That(editor, Is.InstanceOf<IDataGridViewEditingControl>());
            }
        }

        [Test]
        public void EditingControl_ContainsTextBoxAndComboBox()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                Assert.That(editor.Controls.OfType<TextBox>().Count(), Is.EqualTo(1));
                Assert.That(editor.Controls.OfType<ComboBox>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void EditingControl_RepositionOnValueChange_ReturnsFalse()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                Assert.That(((IDataGridViewEditingControl)editor).RepositionEditingControlOnValueChange, Is.False);
            }
        }

        [Test]
        public void EditingControl_EditingPanelCursor_IsIBeam()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                Assert.That(((IDataGridViewEditingControl)editor).EditingPanelCursor, Is.EqualTo(Cursors.IBeam));
            }
        }

        [Test]
        public void EditingControl_EmptyItems_ShowsEmptyCombo()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                editor.SetItems(new List<string>(), null);
                var combo = editor.Controls.OfType<ComboBox>().First();
                Assert.That(combo.Items.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void EditingControl_NullItems_ShowsEmptyCombo()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                editor.SetItems(null, null);
                var combo = editor.Controls.OfType<ComboBox>().First();
                Assert.That(combo.Items.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void EditingControl_NoSelection_ReturnsEmptyString()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                editor.SetItems(new List<string> { "Alpha", "Beta" }, null);
                var result = editor.GetEditingControlFormattedValue(DataGridViewDataErrorContexts.Display);
                Assert.That(result, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void PrepareEditingControlForEdit_ClearsFilterAndShowsFullList()
        {
            using (var editor = new DataGridViewFilteredComboBoxEditingControl())
            {
                var items = new List<string> { "Alpha", "Beta", "Gamma" };
                editor.SetItems(items, "Beta");

                var txtFilter = editor.Controls.OfType<TextBox>().First();
                var combo = editor.Controls.OfType<ComboBox>().First();

                // Prepare for edit should clear filter, show full list, and preserve selection
                ((IDataGridViewEditingControl)editor).PrepareEditingControlForEdit(false);

                Assert.That(txtFilter.Text, Is.EqualTo(string.Empty));
                Assert.That(combo.Items.Count, Is.EqualTo(3));
                Assert.That(combo.SelectedItem?.ToString(), Is.EqualTo("Beta"));
            }
        }
    }
}
