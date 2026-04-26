using System;
using System.Linq;
using System.Windows.Forms;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Market
{
    public class FormListingEdit : Form
    {
        private TextBox txtItemName;
        private ComboBox cmbItemType;
        private TextBox txtQuantity;
        private TextBox txtPricePerUnit;
        private TextBox txtCurrentHP;
        private TextBox txtMaxHP;
        private ComboBox cmbStation;
        private Button cmdOK;
        private Button cmdCancel;
        private MarketListing _listing;

        public FormListingEdit(MarketListing listing, PlayerContext playerContext)
        {
            _listing = listing;
            this.Text = "Edit Listing";
            this.Size = new System.Drawing.Size(380, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            int y = 10;
            AddLabel("Item Name:", 10, y);
            txtItemName = AddTextBox(120, y, 230);
            y += 30;
            AddLabel("Type:", 10, y);
            cmbItemType = new ComboBox { Left = 120, Top = y, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (ItemType.ItemTypeEnum t in Enum.GetValues(typeof(ItemType.ItemTypeEnum)))
                cmbItemType.Items.Add(t);
            this.Controls.Add(cmbItemType);
            y += 30;
            AddLabel("Station:", 10, y);
            cmbStation = new ComboBox { Left = 120, Top = y, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStation.Items.Add(new StationItem { Display = "(None)", UUID = string.Empty });
            foreach (var s in CollectionSortHelper.OrderStations(playerContext.GetCurrentPlayerStations()))
                cmbStation.Items.Add(new StationItem { Display = s.Name, UUID = s.UUID });
            this.Controls.Add(cmbStation);
            y += 30;
            AddLabel("Quantity:", 10, y);
            txtQuantity = AddTextBox(120, y, 100);
            y += 30;
            AddLabel("Price/Unit:", 10, y);
            txtPricePerUnit = AddTextBox(120, y, 100);
            y += 30;
            AddLabel("Current HP:", 10, y);
            txtCurrentHP = AddTextBox(120, y, 100);
            y += 30;
            AddLabel("Max HP:", 10, y);
            txtMaxHP = AddTextBox(120, y, 100);
            y += 30;

            cmdOK = new Button { Text = "OK", Left = 120, Top = y, Width = 75, DialogResult = DialogResult.OK };
            cmdCancel = new Button { Text = "Cancel", Left = 200, Top = y, Width = 75, DialogResult = DialogResult.Cancel };
            this.Controls.Add(cmdOK);
            this.Controls.Add(cmdCancel);
            this.AcceptButton = cmdOK;
            this.CancelButton = cmdCancel;

            // Populate
            txtItemName.Text = listing.ItemName;
            for (int i = 0; i < cmbItemType.Items.Count; i++)
            {
                if (cmbItemType.Items[i] is ItemType.ItemTypeEnum t2 && t2 == listing.ItemType)
                {
                    cmbItemType.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < cmbStation.Items.Count; i++)
            {
                if (cmbStation.Items[i] is StationItem si && si.UUID == listing.StationUUID)
                {
                    cmbStation.SelectedIndex = i;
                    break;
                }
            }

            if (cmbStation.SelectedIndex < 0) cmbStation.SelectedIndex = 0;
            txtQuantity.Text = listing.Quantity.ToString();
            txtPricePerUnit.Text = listing.PricePerUnit.ToString();
            txtCurrentHP.Text = listing.CurrentHP.ToString();
            txtMaxHP.Text = listing.MaxHP.ToString();

            cmdOK.Click += (s, ev) =>
            {
                listing.ItemName = txtItemName.Text.Trim();
                if (cmbItemType.SelectedItem is ItemType.ItemTypeEnum selType) listing.ItemType = selType;
                if (cmbStation.SelectedItem is StationItem selStation) listing.StationUUID = selStation.UUID;
                if (int.TryParse(txtQuantity.Text, out int qty)) listing.Quantity = qty;
                if (decimal.TryParse(txtPricePerUnit.Text, out decimal ppu)) listing.PricePerUnit = ppu;
                if (int.TryParse(txtCurrentHP.Text, out int chp)) listing.CurrentHP = chp;
                if (int.TryParse(txtMaxHP.Text, out int mhp)) listing.MaxHP = mhp;
            };
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Left = x, Top = y + 3, AutoSize = true };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int w)
        {
            var txt = new TextBox { Left = x, Top = y, Width = w };
            this.Controls.Add(txt);
            return txt;
        }

        private class StationItem
        {
            public string Display { get; set; }
            public string UUID { get; set; }
            public override string ToString() => Display;
        }
    }
}