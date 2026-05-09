using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.PricingPlan
{
    public partial class FormPricingPlan : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;
        private PricingPlanViewModel _viewModel = new PricingPlanViewModel();
        private PricingPlanService _pricingPlanService;

        // Tracks the previously selected plan UUID for unsaved-changes cancel/restore
        private string _previousSelectedUUID;

        public FormPricingPlan()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _pricingPlanService = new PricingPlanService(playerContext);

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 200);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += LvwPlans_ItemSelectionChanged;

            txtPlanFilter.TextChanged += TxtPlanFilter_TextChanged;
            txtPlanName.TextChanged += TxtPlanName_TextChanged;
            txtDescription.TextChanged += TxtDescription_TextChanged;
            txtFixedCost.TextChanged += TxtFixedCost_TextChanged;
            txtHourlyCost.TextChanged += TxtHourlyCost_TextChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;

            dgvResourcePrices.CellValueChanged += DgvResourcePrices_CellValueChanged;
            dgvResourcePrices.CellValidating += DgvResourcePrices_CellValidating;
            dgvResourcePrices.CellMouseClick += DgvResourcePrices_CellMouseClick;
            cmsResourcePrices.Opening += CmsResourcePrices_Opening;
            tsmiClearPrice.Click += TsmiClearPrice_Click;

            PopulatePlanList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentPlan();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int availHeight = flpSearchList.ClientSize.Height
                - flpPlanFilter.Height - flpPlanFilter.Margin.Top - flpPlanFilter.Margin.Bottom
                - lvwPlans.Margin.Top - lvwPlans.Margin.Bottom;
            if (availHeight < 50) availHeight = 50;
            lvwPlans.Size = new System.Drawing.Size(
                flpSearchList.ClientSize.Width - lvwPlans.Margin.Left - lvwPlans.Margin.Right,
                availHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int gridHeight = h - flpPlanName.Height - flpDescription.Height - flpCosts.Height - flpCommands.Height - 30;
            if (gridHeight < 50) gridHeight = 50;
            dgvResourcePrices.Size = new System.Drawing.Size(w - 6, gridHeight);
        }

        // -----------------------------------------------------------------------
        // Plan List
        // -----------------------------------------------------------------------

        private void PopulatePlanList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = GetSelectedReadOnlyPlan()?.UUID;
            lvwPlans.Items.Clear();

            var plans = playerContext.GetCurrentPlayerReadOnlyPricingPlans();
            string filter = txtPlanFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                plans = plans.Where(p => p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            var sorted = CollectionSortHelper.OrderReadOnlyPricingPlans(plans);

            foreach (var plan in sorted)
            {
                var item = new ListViewItem(plan.Name) { Tag = plan };
                lvwPlans.Items.Add(item);
                if (plan.UUID == selectedUUID)
                {
                    item.Selected = true;
                }
            }

            sw.Stop();
            Log.Info(
                "PopulatePlanList PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds,
                sorted.Count);
            sw.Stop();
            Log.Info("PERF PopulatePlanList: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtPlanFilter_TextChanged(object sender, EventArgs e)
        {
            PopulatePlanList();
        }

        private void LvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlyPricingPlan plan)
            {
                // Prompt for unsaved changes before switching
                if (_viewModel.IsDirty)
                {
                    var result = PromptUnsavedChanges();
                    if (result == DialogResult.Yes)
                    {
                        SaveCurrentPlan();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Restore previous selection
                        lvwPlans.ItemSelectionChanged -= LvwPlans_ItemSelectionChanged;
                        lvwPlans.SelectedItems.Clear();
                        if (!string.IsNullOrEmpty(_previousSelectedUUID))
                        {
                            foreach (ListViewItem item in lvwPlans.Items)
                            {
                                if ((item.Tag as ReadOnlyPricingPlan)?.UUID == _previousSelectedUUID)
                                {
                                    item.Selected = true;
                                    item.EnsureVisible();
                                    break;
                                }
                            }
                        }

                        lvwPlans.ItemSelectionChanged += LvwPlans_ItemSelectionChanged;
                        return;
                    }

                    // DialogResult.No - discard, fall through to load new
                }

                _viewModel.LoadFrom(plan);
                _previousSelectedUUID = plan.UUID;
                PopulateForm();
                UpdateSaveButtonState();
            }
            else if (!e.IsSelected && lvwPlans.SelectedItems.Count == 0)
            {
                ClearForm();
            }
        }

        /// <summary>
        /// Returns the ReadOnlyPricingPlan from the currently selected list view item,
        /// or null if nothing is selected.
        /// </summary>
        private ReadOnlyPricingPlan GetSelectedReadOnlyPlan()
        {
            if (lvwPlans.SelectedItems.Count == 0)
            {
                return null;
            }

            return lvwPlans.SelectedItems[0].Tag as ReadOnlyPricingPlan;
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var selected = GetSelectedReadOnlyPlan();
            if (selected == null)
            {
                ClearForm();
                return;
            }

            txtPlanName.Text = _viewModel.Name;
            txtDescription.Text = _viewModel.Description;
            txtFixedCost.Text = _viewModel.FixedCostPerItem == 0m ? string.Empty : _viewModel.FixedCostPerItem.ToString();
            txtHourlyCost.Text = _viewModel.HourlyCostRate == 0m ? string.Empty : _viewModel.HourlyCostRate.ToString();

            PopulateResourceGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PopulateForm PERF: total={0}ms", sw.ElapsedMilliseconds);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
            UpdateSaveButtonState();
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            _viewModel.Reset();
            txtPlanName.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtFixedCost.Text = string.Empty;
            txtHourlyCost.Text = string.Empty;
            dgvResourcePrices.Rows.Clear();
            SetDetailEnabled(false);
            UpdateSaveButtonState();
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtPlanName.Enabled = enabled;
            txtDescription.Enabled = enabled;
            txtFixedCost.Enabled = enabled;
            txtHourlyCost.Enabled = enabled;
            dgvResourcePrices.Enabled = enabled;
        }

        private void PopulateResourceGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvResourcePrices.CellValidating -= DgvResourcePrices_CellValidating;
            dgvResourcePrices.EndEdit();
            dgvResourcePrices.Rows.Clear();
            dgvResourcePrices.CellValidating += DgvResourcePrices_CellValidating;

            var selected = GetSelectedReadOnlyPlan();
            if (selected == null)
            {
                sw.Stop();
                return;
            }

            foreach (var resource in Resource.Resources.OrderBy(r => r.Name))
            {
                string purity = PriceCalculator.DeterminePurity(resource.Name);
                string key = PriceCalculator.MakeResourceKey(resource.Name, purity);
                string priceText = string.Empty;
                decimal price;
                if (_viewModel.ResourcePrices.TryGetValue(key, out price))
                {
                    priceText = price.ToString();
                }

                int rowIdx = dgvResourcePrices.Rows.Add(resource.Name, purity, priceText);
                dgvResourcePrices.Rows[rowIdx].Tag = key;
            }

            sw.Stop();
            Log.Info("PERF PopulateResourceGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Dirty Tracking / Unsaved Changes
        // -----------------------------------------------------------------------

        /// <summary>
        /// Enables the Save button only when the ViewModel has unsaved changes.
        /// </summary>
        private void UpdateSaveButtonState()
        {
            cmdSave.Enabled = _viewModel.IsDirty;
        }

        /// <summary>
        /// Prompts the user to save, discard, or cancel when there are unsaved changes.
        /// Returns Yes (save), No (discard), or Cancel.
        /// </summary>
        private DialogResult PromptUnsavedChanges()
        {
            return MessageBox.Show(
                string.Format("Save changes to '{0}'?", _viewModel.Name),
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        /// <summary>
        /// Saves the current plan via the service (Create or Update) and reloads the ViewModel.
        /// </summary>
        private void SaveCurrentPlan()
        {
            ReadOnlyPricingPlan saved;
            if (_viewModel.IsNew)
            {
                saved = _pricingPlanService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _pricingPlanService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _viewModel.LoadFrom(saved);
            _previousSelectedUUID = saved.UUID;
            PopulatePlanList();
            SelectPlanInList(saved.UUID);
            PopulateForm();
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Selects the plan with the given UUID in the list view and scrolls it into view.
        /// </summary>
        private void SelectPlanInList(string uuid)
        {
            foreach (ListViewItem item in lvwPlans.Items)
            {
                if ((item.Tag as ReadOnlyPricingPlan)?.UUID == uuid)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        // -----------------------------------------------------------------------
        // CRUD Operations
        // -----------------------------------------------------------------------

        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentPlan();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            _viewModel.Reset();
            _previousSelectedUUID = null;
            lvwPlans.SelectedItems.Clear();
            ClearForm();
            SetDetailEnabled(true);
            UpdateSaveButtonState();
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedReadOnlyPlan();
            if (selected == null)
            {
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete pricing plan '{0}'?", selected.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            _pricingPlanService.Delete(selected.UUID);
            _previousSelectedUUID = null;
            PopulatePlanList();
            ClearForm();
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            string name = _viewModel.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Plan name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_viewModel.FixedCostPerItem < 0m)
            {
                MessageBox.Show("Fixed Cost cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_viewModel.HourlyCostRate < 0m)
            {
                MessageBox.Show("Hourly Rate cannot be negative.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ReadOnlyPricingPlan saved;
            if (_viewModel.IsNew)
            {
                saved = _pricingPlanService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _pricingPlanService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _viewModel.LoadFrom(saved);
            _previousSelectedUUID = saved.UUID;
            PopulatePlanList();
            SelectPlanInList(saved.UUID);
            PopulateForm();
            Log.Info("Saved pricing plan '{0}'", saved.Name);
        }

        // -----------------------------------------------------------------------
        // Data Model Write-Through
        // -----------------------------------------------------------------------

        private void TxtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.Name = txtPlanName.Text;
            UpdateSaveButtonState();
        }

        private void TxtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.Description = txtDescription.Text;
            UpdateSaveButtonState();
        }

        private void TxtFixedCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (decimal.TryParse(txtFixedCost.Text, out decimal val))
            {
                _viewModel.FixedCostPerItem = val;
            }
            else if (string.IsNullOrEmpty(txtFixedCost.Text))
            {
                _viewModel.FixedCostPerItem = 0m;
            }

            UpdateSaveButtonState();
        }

        private void TxtHourlyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (decimal.TryParse(txtHourlyCost.Text, out decimal val))
            {
                _viewModel.HourlyCostRate = val;
            }
            else if (string.IsNullOrEmpty(txtHourlyCost.Text))
            {
                _viewModel.HourlyCostRate = 0m;
            }

            UpdateSaveButtonState();
        }

        // -----------------------------------------------------------------------
        // Resource Price Grid
        // -----------------------------------------------------------------------

        private void DgvResourcePrices_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            if (e.ColumnIndex != colPrice.Index)
            {
                return;
            }

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return; // blank is valid (clears entry)
            }

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

            dgvResourcePrices.Rows[e.RowIndex].ErrorText = string.Empty;
        }

        private void DgvResourcePrices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex != colPrice.Index)
            {
                return;
            }

            string key = dgvResourcePrices.Rows[e.RowIndex].Tag as string;
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            string cellValue = dgvResourcePrices.Rows[e.RowIndex].Cells[colPrice.Index].Value?.ToString();
            if (string.IsNullOrWhiteSpace(cellValue))
            {
                _viewModel.ResourcePrices.Remove(key);
            }
            else if (decimal.TryParse(cellValue, out decimal price) && price >= 0m)
            {
                _viewModel.ResourcePrices[key] = price;
            }

            UpdateSaveButtonState();
        }

        private void DgvResourcePrices_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvResourcePrices.ClearSelection();
                dgvResourcePrices.Rows[e.RowIndex].Selected = true;
                dgvResourcePrices.CurrentCell = dgvResourcePrices.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvResourcePrices.ClearSelection();
            }
        }

        private void CmsResourcePrices_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvResourcePrices.CurrentRow != null;
            tsmiClearPrice.Enabled = hasSelection;
        }

        private void TsmiClearPrice_Click(object sender, EventArgs e)
        {
            if (dgvResourcePrices.CurrentRow == null) return;

            int rowIndex = dgvResourcePrices.CurrentRow.Index;
            string key = dgvResourcePrices.Rows[rowIndex].Tag as string;
            if (string.IsNullOrEmpty(key)) return;

            dgvResourcePrices.Rows[rowIndex].Cells[colPrice.Index].Value = string.Empty;
            _viewModel.ResourcePrices.Remove(key);
            UpdateSaveButtonState();
        }

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            PopulatePlanList();
            ClearForm();
        }
    }
}
