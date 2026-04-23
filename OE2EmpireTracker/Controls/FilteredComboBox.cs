using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OE2EmpireTracker.Controls
{
    public class FilteredComboBox : System.Windows.Forms.ComboBox
    {
        private bool changingText = false;

        public BindingSource UnfilteredList { get; set; }

        protected override void OnTextChanged(EventArgs e)
        {
            if (UnfilteredList == null || changingText == true)
            {
                return;
            }

            string searchText = this.Text;
            BindingSource filteredSource = UnfilteredList;

            if (string.IsNullOrEmpty(searchText))
            {
                filteredSource = UnfilteredList;
            }
            else
            {
                List<BlueprintType> blueprintTypes = (List<BlueprintType>) UnfilteredList.DataSource;
                var filteredList = blueprintTypes
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                filteredSource = new BindingSource();
                // Set the in-memory list as the DataSource for the BindingSource
                filteredSource.DataSource = filteredList;
            }

            // this.SelectedIndex = -1;
            this.DataSource = filteredSource;
            // this.SelectedIndex = -1;

            changingText = true;
            this.Text = searchText;
            this.SelectionStart = searchText.Length;
            // this.DroppedDown = true;

            if (string.IsNullOrEmpty(searchText))
            {
                this.Text = string.Empty;
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
