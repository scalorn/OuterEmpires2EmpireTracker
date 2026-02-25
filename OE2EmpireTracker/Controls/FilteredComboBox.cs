using OE2EmpireTracker.Baseline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OE2EmpireTracker.Controls
{
    public class FilteredComboBox : System.Windows.Forms.ComboBox
    {
        public BindingSource unfilteredList;
        private bool changingText = false;

        protected override void OnTextChanged(EventArgs e)
        {
            if (unfilteredList == null || changingText == true)
            {
                return;
            }
            string searchText = this.Text;
            BindingSource filteredItemsBindingList = unfilteredList;

            if (string.IsNullOrEmpty(searchText))
            {
                filteredItemsBindingList = unfilteredList;
            }
            else
            {
                BindingList<BlueprintType> blueprintTypes = (BindingList < BlueprintType > ) unfilteredList.DataSource;
                var filteredList = blueprintTypes
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                filteredItemsBindingList = new BindingSource();
                // Set the in-memory list as the DataSource for the BindingSource
                filteredItemsBindingList.DataSource = filteredList;
            }

            //this.SelectedIndex = -1;
            this.DataSource = filteredItemsBindingList;
            //this.SelectedIndex = -1;

            changingText = true;
            this.Text = searchText;
            this.SelectionStart = searchText.Length;
            //this.DroppedDown = true;

            if (string.IsNullOrEmpty(searchText))
            {
                this.Text = "";
            }
            changingText = false;

            base.OnTextChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            this.DroppedDown = true;
            base.OnGotFocus(e);
        }
    }
}
