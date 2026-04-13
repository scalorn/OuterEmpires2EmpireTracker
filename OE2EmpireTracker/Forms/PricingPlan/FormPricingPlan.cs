using System;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.PricingPlan
{
    public partial class FormPricingPlan : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private Models.PricingPlan _selectedPlan;

        public FormPricingPlan()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 200);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += lvwPlans_ItemSelectionChanged;

            txtPlanFilter.TextChanged += txtPlanFilter_TextChanged;
            txtPlanName.TextChanged += txtPlanName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            txtFixedCost.TextChanged += txtFixedCost_TextChanged;
            txtHourlyCost.TextChanged += txtHourlyCost_TextChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            dgvResourcePrices.CellValueChanged += dgvResourcePrices_CellValueChanged;
            dgvResourcePrices.CellValidating += dgvResourcePrices_CellValidating;

            PopulatePlanList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpPlanFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwPlans.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int gridHeight = h - flpPlanName.Height - flpDescription.Height - flpCosts.Height - cmdSave.Height - 30;
            if (gridHeight < 50) gridHeight = 50;
            dgvResourcePrices.Size = new System.Drawing.Size(w - 6, gridHeight);
        }

        // -----------------------------------------------------------------------
        // Plan List
        // -----------------------------------------------------------------------

        private void PopulatePlanList()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedPlan?.UUID;
            lvwPlans.Items.Clear();

            var plans = playerContext.GetCurrentPlayerPricingPlans();
            string filter = txtPlanFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                plans = plans.Where(p => p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            plans = plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var plan in plans)
            {
                var item = new ListViewItem(plan.Name) { Tag = plan };
                lvwPlans.Items.Add(item);
                if (plan.UUID == selectedUUID)
                    item.Selected = true;
            }
        }

        private void txtPlanFilter_TextChanged(object sender, EventArgs e)
        {
            PopulatePlanList();
        }

        private void lvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Models.PricingPlan plan)
            {
                _selectedPlan = plan;
                PopulateForm();
            }
            else if (!e.IsSelected && lvwPlans.SelectedItems.Count == 0)
            {
                _selectedPlan = null;
                ClearForm();
            }
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedPlan == null) { ClearForm(); return; }

            txtPlanName.Text = _selectedPlan.Name;
            txtDescription.Text = _selectedPlan.Description;
            txtFixedCost.Text = _selectedPlan.FixedCostPerItem == 0m ? "" : _selectedPlan.FixedCostPerItem.ToString();
            txtHourlyCost.Text = _selectedPlan.HourlyCostRate == 0m ? "" : _selectedPlan.HourlyCostRate.ToString();

            PopulateResourceGrid();
            SetDetailEnabled(true);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanName.Text = "";
            txtDescription.Text = "";
            txtFixedCost.Text = "";
            txtHourlyCost.Text = "";
            dgvResourcePrices.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtPlanName.Enabled = enabled;
            txtDescription.Enabled = enabled;
            txtFixedCost.Enabled = enabled;
            txtHourlyCost.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvResourcePrices.Enabled = enabled;
        }

        private void PopulateResourceGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvResourcePrices.CellValidating -= dgvResourcePrices_CellValidating;
            dgvResourcePrices.EndEdit();
            dgvResourcePrices.Rows.Clear();
            dgvResourcePrices.CellValidating += dgvResourcePrices_CellValidating;

            if (_selectedPlan == null) return;

            foreach (var resource in Resource.Resources.OrderBy(r => r.Name))
            {
                string purity = PriceCalculator.DeterminePurity(resource.Name);
                string key = PriceCalculator.MakeResourceKey(resource.Name, purity);
                string priceText = "";
                decimal price;
                if (_selectedPlan.ResourcePrices.TryGetValue(key, out price))
                {
                    priceText = price.ToString();
                }
                int rowIdx = dgvResourcePrices.Rows.Add(resource.Name, purity, priceText);
                dgvResourcePrices.Rows[rowIdx].Tag = key;
            }
        }

        // -----------------------------------------------------------------------
        // CRUD Operations
        // -----------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            var plan = new Models.PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Plan",
                OwnerUUID = playerContext.CurrentPlayerUUID
            };
            playerContext.PricingPlanList.Add(plan);
            playerContext.WriteContext();
            playerContext.OnPricingDataChanged();
            _selectedPlan = plan;
            PopulatePlanList();
            PopulateForm();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            var result = MessageBox.Show(
                $"Delete pricing plan '{_selectedPlan.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.PricingPlanList.Remove(_selectedPlan);
            playerContext.WriteContext();
            playerContext.OnPricingDataChanged();
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            string name = txtPlanName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Plan name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal fixedCost;
            if (!string.IsNullOrEmpty(txtFixedCost.Text) && !decimal.TryParse(txtFixedCost.Text, out fixedCost))
            {
                MessageBox.Show("Fixed Cost must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (string.IsNullOrEmpty(txtFixedCost.Text))
            {
                fixedCost = 0m;
            }
            else
            {
                fixedCost = decimal.Parse(txtFixedCost.Text);
            }

            if (fixedCost < 0m)
            {
                MessageBox.Show("Fixed Cost cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal hourlyCost;
            if (!string.IsNullOrEmpty(txtHourlyCost.Text) && !decimal.TryParse(txtHourlyCost.Text, out hourlyCost))
            {
                MessageBox.Show("Hourly Rate must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (string.IsNullOrEmpty(txtHourlyCost.Text))
            {
                hourlyCost = 0m;
            }
            else
            {
                hourlyCost = decimal.Parse(txtHourlyCost.Text);
            }

            if (hourlyCost < 0m)
            {
                MessageBox.Show("Hourly Rate cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedPlan.Name = name;
            _selectedPlan.Description = txtDescription.Text;
            _selectedPlan.FixedCostPerItem = fixedCost;
            _selectedPlan.HourlyCostRate = hourlyCost;

            playerContext.WriteContext();
            playerContext.OnPricingDataChanged();
            PopulatePlanList();
            Log.Info("Saved pricing plan '{0}'", _selectedPlan.Name);
        }

        // -----------------------------------------------------------------------
        // Data Model Write-Through
        // -----------------------------------------------------------------------

        private void txtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Name = txtPlanName.Text;
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Description = txtDescription.Text;
        }

        private void txtFixedCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            decimal val;
            if (decimal.TryParse(txtFixedCost.Text, out val) && val >= 0m)
                _selectedPlan.FixedCostPerItem = val;
        }

        private void txtHourlyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            decimal val;
            if (decimal.TryParse(txtHourlyCost.Text, out val) && val >= 0m)
                _selectedPlan.HourlyCostRate = val;
        }

        // -----------------------------------------------------------------------
        // Resource Price Grid
        // -----------------------------------------------------------------------

        private void dgvResourcePrices_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.ColumnIndex != colPrice.Index) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrWhiteSpace(value)) return; // blank is valid (clears entry)

            decimal parsed;
            if (!decimal.TryParse(value, out parsed))
            {
                e.Cancel = true;
                dgvResourcePrices.Rows[e.RowIndex].ErrorText = "Price must be a valid number.";
                return;
            }
            if (parsed < 0m)
            {
                e.Cancel = true;
                dgvResourcePrices.Rows[e.RowIndex].ErrorText = "Price cannot be negative.";
                return;
            }
            dgvResourcePrices.Rows[e.RowIndex].ErrorText = "";
        }

        private void dgvResourcePrices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0 || e.ColumnIndex != colPrice.Index) return;
            if (_selectedPlan == null) return;

            var row = dgvResourcePrices.Rows[e.RowIndex];
            string key = row.Tag as string;
            if (key == null) return;

            string value = row.Cells[colPrice.Index].Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                // Clear entry — resource becomes unpriced
                _selectedPlan.ResourcePrices.Remove(key);
            }
            else
            {
                decimal parsed;
                if (decimal.TryParse(value, out parsed) && parsed >= 0m)
                {
                    _selectedPlan.ResourcePrices[key] = parsed;
                }
            }

            playerContext.WriteContext();
            playerContext.OnPricingDataChanged();
        }

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }
    }
}
