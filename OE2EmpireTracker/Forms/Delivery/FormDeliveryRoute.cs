using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.Delivery
{
    public partial class FormDeliveryRoute : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private DeliveryRouteViewModel viewModel;

        public FormDeliveryRoute()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new DeliveryRouteViewModel(new DeliveryRoute(), playerContext);

            lvwRoutes.View = View.Details;
            lvwRoutes.Columns.Add("Name", 200);
            lvwRoutes.FullRowSelect = true;
            lvwRoutes.MultiSelect = false;
            lvwRoutes.ItemSelectionChanged += lvwRoutes_ItemSelectionChanged;

            txtRouteFilter.TextChanged += txtRouteFilter_TextChanged;

            cmbColony.DisplayMember = "Display";
            cmbColony.ValueMember = "UUID";
            PopulateColonyPicker();

            cmdAddStop.Click += cmdAddStop_Click;
            cmdUp.Click += cmdUp_Click;
            cmdDown.Click += cmdDown_Click;
            cmdRemoveStop.Click += cmdRemoveStop_Click;
            chkPreventDuplicates.CheckedChanged += (s, ev) => PopulateColonyPicker();
            cmdNew.Click += cmdNew_Click;
            cmdSave.Click += cmdSave_Click;
            cmdDelete.Click += cmdDelete_Click;

            PopulateRouteList();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpRouteData.Layout += flpRouteData_Layout;
            tabStops.Layout += tabStops_Layout;
            flpPlanContent.Layout += flpPlanContent_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpRouteData.Size = new Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpRouteData.Margin.Left - flpRouteData.Margin.Right,
                flpBase.Size.Height - flpRouteData.Margin.Top - flpRouteData.Margin.Bottom);
            flpSearchList.Size = new Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwRoutes.Size = new Size(
                lvwRoutes.Size.Width,
                flpSearchList.Size.Height - flpRouteFilter.Size.Height - flpRouteFilter.Margin.Top - flpRouteFilter.Margin.Bottom - lvwRoutes.Margin.Top - lvwRoutes.Margin.Bottom);
        }

        private void flpRouteData_Layout(object sender, LayoutEventArgs e)
        {
            int tabHeight = flpRouteData.Size.Height
                - flpRouteName.Size.Height - flpRouteName.Margin.Top - flpRouteName.Margin.Bottom
                - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom
                - tabRouteDetail.Margin.Top - tabRouteDetail.Margin.Bottom;
            if (tabHeight < 100) tabHeight = 100;
            tabRouteDetail.Size = new Size(
                flpRouteData.Size.Width - tabRouteDetail.Margin.Left - tabRouteDetail.Margin.Right,
                tabHeight);
        }

        private void tabStops_Layout(object sender, LayoutEventArgs e)
        {
            int gridHeight = tabStops.ClientSize.Height
                - flpAddStop.Size.Height - flpAddStop.Margin.Top - flpAddStop.Margin.Bottom
                - dgvStops.Margin.Top - dgvStops.Margin.Bottom;
            if (gridHeight < 50) gridHeight = 50;
            dgvStops.Size = new Size(
                tabStops.ClientSize.Width - dgvStops.Margin.Left - dgvStops.Margin.Right,
                gridHeight);
            flpAddStop.Size = new Size(
                tabStops.ClientSize.Width - flpAddStop.Margin.Left - flpAddStop.Margin.Right,
                flpAddStop.Size.Height);
        }

        private void flpPlanContent_Layout(object sender, LayoutEventArgs e)
        {
            int availWidth = flpPlanContent.ClientSize.Width;
            int availHeight = flpPlanContent.ClientSize.Height;

            // Fixed heights: labels, add rows
            int fixedHeight = lblPlanStop.Size.Height + lblPlanStop.Margin.Top + lblPlanStop.Margin.Bottom
                + lblDropOff.Size.Height + lblDropOff.Margin.Top + lblDropOff.Margin.Bottom
                + flpDropOffAdd.Size.Height + flpDropOffAdd.Margin.Top + flpDropOffAdd.Margin.Bottom
                + lblPickUp.Size.Height + lblPickUp.Margin.Top + lblPickUp.Margin.Bottom
                + flpPickUpAdd.Size.Height + flpPickUpAdd.Margin.Top + flpPickUpAdd.Margin.Bottom
                + dgvDropOff.Margin.Top + dgvDropOff.Margin.Bottom
                + dgvPickUp.Margin.Top + dgvPickUp.Margin.Bottom;

            int gridSpace = availHeight - fixedHeight;
            int perGrid = Math.Max(50, gridSpace / 2);

            dgvDropOff.Size = new Size(availWidth - dgvDropOff.Margin.Left - dgvDropOff.Margin.Right, perGrid);
            dgvPickUp.Size = new Size(availWidth - dgvPickUp.Margin.Left - dgvPickUp.Margin.Right, perGrid);
        }

        // -----------------------------------------------------------------------
        // Route List
        // -----------------------------------------------------------------------

        private void PopulateRouteList()
        {
            lvwRoutes.Items.Clear();
            var routes = viewModel.GetFilteredRoutes(txtRouteFilter.Text);
            foreach (var route in routes)
            {
                var item = new ListViewItem(route.Name);
                item.Tag = route;
                lvwRoutes.Items.Add(item);
            }
        }

        private void txtRouteFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateRouteList();
        }

        private void lvwRoutes_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvwRoutes.SelectedItems.Count == 1)
            {
                var route = lvwRoutes.SelectedItems[0].Tag as DeliveryRoute;
                viewModel.SelectRoute(route);
                PopulateForm();
            }
        }

        // -----------------------------------------------------------------------
        // Colony Picker
        // -----------------------------------------------------------------------

        private void PopulateColonyPicker()
        {
            var colonies = playerContext.GetCurrentPlayerColonies()
                .OrderBy(c => c.PlanetName)
                .ToList();

            // Filter out colonies already in the route when "No Duplicates" is checked
            if (chkPreventDuplicates.Checked)
            {
                var existingUUIDs = new HashSet<string>(viewModel.Stops.Select(s => s.ColonyUUID));
                colonies = colonies.Where(c => !existingUUIDs.Contains(c.UUID)).ToList();
            }

            var items = new List<ColonyPickerItem>();
            items.Add(new ColonyPickerItem { UUID = "", Display = "" });
            foreach (var colony in colonies)
            {
                string display = $"{colony.PlanetName} - {colony.ColonyName}";
                if (!string.IsNullOrEmpty(colony.SystemName))
                    display += $" ({colony.SystemName})";
                items.Add(new ColonyPickerItem { UUID = colony.UUID, Display = display });
            }

            cmbColony.DataSource = null;
            cmbColony.DisplayMember = "Display";
            cmbColony.ValueMember = "UUID";
            cmbColony.DataSource = items;
        }

        private class ColonyPickerItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            txtRouteName.Text = viewModel.Name ?? "";
            PopulateStopsGrid();
        }

        private void PopulateStopsGrid()
        {
            dgvStops.Rows.Clear();
            foreach (var stop in viewModel.Stops)
            {
                var colony = playerContext.FindColony(stop.ColonyUUID);
                int rowIndex = dgvStops.Rows.Add(
                    stop.Sequence + 1,
                    colony?.ColonyName ?? "(unknown)",
                    colony?.PlanetName ?? "",
                    colony?.SystemName ?? "");
                dgvStops.Rows[rowIndex].Tag = stop;
            }
        }

        private void ClearForm()
        {
            viewModel.Reset();
            txtRouteName.Text = "";
            dgvStops.Rows.Clear();
            PopulateColonyPicker();
        }

        // -----------------------------------------------------------------------
        // Stop Management
        // -----------------------------------------------------------------------

        private void cmdAddStop_Click(object sender, EventArgs e)
        {
            string colonyUUID = cmbColony.SelectedValue as string;
            if (string.IsNullOrEmpty(colonyUUID)) return;

            viewModel.AddStop(colonyUUID);
            PopulateStopsGrid();
            if (chkPreventDuplicates.Checked) PopulateColonyPicker();
        }

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            var newIndices = viewModel.MoveStopsUp(indices);
            PopulateStopsGrid();
            dgvStops.ClearSelection();
            foreach (int i in newIndices)
                if (i >= 0 && i < dgvStops.Rows.Count)
                    dgvStops.Rows[i].Selected = true;
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            var newIndices = viewModel.MoveStopsDown(indices);
            PopulateStopsGrid();
            dgvStops.ClearSelection();
            foreach (int i in newIndices)
                if (i >= 0 && i < dgvStops.Rows.Count)
                    dgvStops.Rows[i].Selected = true;
        }

        private void cmdRemoveStop_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            viewModel.RemoveStops(indices);
            PopulateStopsGrid();
            if (chkPreventDuplicates.Checked) PopulateColonyPicker();
        }

        // -----------------------------------------------------------------------
        // Commands
        // -----------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwRoutes.SelectedItems.Clear();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            viewModel.Name = txtRouteName.Text;
            viewModel.Save();
            PopulateRouteList();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.UUID)) return;

            var result = MessageBox.Show(
                $"Delete route '{viewModel.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            viewModel.Delete();
            ClearForm();
            PopulateRouteList();
        }

        // -----------------------------------------------------------------------
        // Player Change
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            ClearForm();
            PopulateRouteList();
            PopulateColonyPicker();
        }
    }
}
