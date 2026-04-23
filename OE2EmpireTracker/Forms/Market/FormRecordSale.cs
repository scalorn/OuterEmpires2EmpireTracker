using System;
using System.Windows.Forms;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Forms.Market
{
    public class FormRecordSale : Form
    {
        private TextBox txtQuantity;

        private TextBox txtPricePerUnit;

        private TextBox txtCounterparty;

        private TextBox txtCounterpartyFaction;

        private Button cmdOK;

        private Button cmdCancel;

        public FormRecordSale(MarketListing listing)
        {
            this.Text = string.Format("Record Sale - {0}", listing.ItemName);
            this.Size = new System.Drawing.Size(360, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            int y = 10;
            AddLabel(string.Format("Available: {0}", listing.Quantity), 10, y);
            y += 25;
            string condStr = listing.MaxHP > 0
                ? string.Format("Condition: {0:F0}%", listing.CurrentHP * 100.0 / listing.MaxHP)
                : "Condition: N/A";
            AddLabel(condStr, 10, y);
            y += 25;
            AddLabel("Quantity:", 10, y);
            txtQuantity = AddTextBox(130, y, 100);
            y += 30;
            AddLabel("Price/Unit:", 10, y);
            txtPricePerUnit = AddTextBox(130, y, 100);
            txtPricePerUnit.Text = listing.PricePerUnit.ToString();
            y += 30;
            AddLabel("Counterparty:", 10, y);
            txtCounterparty = AddTextBox(130, y, 180);
            y += 30;
            AddLabel("Counterparty Faction:", 10, y);
            txtCounterpartyFaction = AddTextBox(130, y, 180);
            y += 35;

            cmdOK = new Button { Text = "Record Sale", Left = 80, Top = y, Width = 90, DialogResult = DialogResult.OK };
            cmdCancel = new Button { Text = "Cancel", Left = 180, Top = y, Width = 75, DialogResult = DialogResult.Cancel };
            this.Controls.Add(cmdOK);
            this.Controls.Add(cmdCancel);
            this.AcceptButton = cmdOK;
            this.CancelButton = cmdCancel;

            cmdOK.Click += (s, ev) =>
            {
                if (!int.TryParse(txtQuantity.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Enter a valid quantity.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (!decimal.TryParse(txtPricePerUnit.Text, out decimal ppu))
                {
                    MessageBox.Show("Enter a valid price.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                SaleQuantity = qty;
                SalePricePerUnit = ppu;
                Counterparty = txtCounterparty.Text.Trim();
                CounterpartyFaction = txtCounterpartyFaction.Text.Trim();
            };
        }

        public int SaleQuantity { get; private set; }

        public decimal SalePricePerUnit { get; private set; }

        public string Counterparty { get; private set; }

        public string CounterpartyFaction { get; private set; }

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
    }
}