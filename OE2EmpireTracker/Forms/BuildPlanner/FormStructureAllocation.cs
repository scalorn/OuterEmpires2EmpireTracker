using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.BuildPlanner
{
    /// <summary>
    /// Modal dialog for allocating a build item to a colony structure.
    /// Shows eligible structures (Manufactories for Manufactory items,
    /// Commodity Factories for Commodity items) across all player colonies.
    /// </summary>
    public partial class FormStructureAllocation : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly BuildItem _buildItem;
        private readonly PlayerContext _playerContext;

        /// <summary>Colony UUID of the selected structure after OK.</summary>
        public string SelectedColonyUUID { get; private set; }

        /// <summary>Structure UUID of the selected structure after OK.</summary>
        public string SelectedStructureUUID { get; private set; }

        /// <summary>
        /// Initializes the allocation dialog for the given build item.
        /// </summary>
        /// <param name="buildItem">The build item to allocate.</param>
        public FormStructureAllocation(BuildItem buildItem)
        {
            InitializeComponent();
            _buildItem = buildItem ?? throw new ArgumentNullException(nameof(buildItem));
            _playerContext = EmpireContext.PlayerContext;

            lblItemInfo.Text = string.Format("Item: {0} ({1} runs)", _buildItem.ItemName, _buildItem.Quantity);

            txtFilter.TextChanged += (s, e) => PopulateGrid();
            chkIdleOnly.CheckedChanged += (s, e) => PopulateGrid();
            cmdAllocate.Click += cmdAllocate_Click;
            dgvStructures.CellDoubleClick += dgvStructures_CellDoubleClick;

            PopulateGrid();
        }

        /// <summary>
        /// Represents a row in the structures grid.
        /// </summary>
        private class StructureRow
        {
            public string ColonyUUID { get; set; }
            public string ColonyName { get; set; }
            public string StructureUUID { get; set; }
            public string StructureName { get; set; }
            public string TypeLabel { get; set; }
            public bool IsBusy { get; set; }
        }

        private void PopulateGrid()
        {
            dgvStructures.Rows.Clear();

            var colonies = _playerContext.GetCurrentPlayerColonies();
            string filter = txtFilter.Text.Trim();
            bool idleOnly = chkIdleOnly.Checked;

            var rows = new List<StructureRow>();

            foreach (var colony in colonies)
            {
                foreach (var structure in colony.Structures)
                {
                    if (!structure.IsBuiltAndOnline) continue;

                    Models.Blueprint bp = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                    if (bp == null) continue;

                    bool eligible = false;
                    string typeLabel = "";

                    if (_buildItem.ItemType == BuildItemType.Manufactory)
                    {
                        if (bp.BluePrintType == BlueprintTypes.Manufactory)
                        {
                            eligible = true;
                            typeLabel = "Manufactory";
                        }
                    }
                    else if (_buildItem.ItemType == BuildItemType.Commodity)
                    {
                        if (bp.BluePrintType.IsCommodityFactory())
                        {
                            eligible = true;
                            typeLabel = "Commodity";
                        }
                    }

                    if (!eligible) continue;

                    bool busy = !string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)
                             || !string.IsNullOrEmpty(structure.ManufacturingCommodityName);

                    if (idleOnly && busy) continue;

                    string structureName = bp.OutputItemName;
                    if (string.IsNullOrEmpty(structureName))
                        structureName = bp.Name;

                    // Apply text filter to colony name or structure name
                    if (!string.IsNullOrEmpty(filter))
                    {
                        bool matchColony = colony.ColonyName != null
                            && colony.ColonyName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool matchStructure = structureName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!matchColony && !matchStructure) continue;
                    }

                    rows.Add(new StructureRow
                    {
                        ColonyUUID = colony.UUID,
                        ColonyName = colony.ColonyName ?? colony.UUID,
                        StructureUUID = structure.UUID,
                        StructureName = structureName,
                        TypeLabel = typeLabel,
                        IsBusy = busy
                    });
                }
            }

            // Sort by colony name, then structure name
            rows = rows.OrderBy(r => r.ColonyName, StringComparer.OrdinalIgnoreCase)
                       .ThenBy(r => r.StructureName, StringComparer.OrdinalIgnoreCase)
                       .ToList();

            foreach (var row in rows)
            {
                int idx = dgvStructures.Rows.Add(
                    row.ColonyName,
                    row.StructureName,
                    row.TypeLabel,
                    row.IsBusy ? "Busy" : "Idle");
                dgvStructures.Rows[idx].Tag = row;
            }

            cmdAllocate.Enabled = dgvStructures.Rows.Count > 0;
            Log.Debug("PopulateGrid: {0} eligible structures for {1} item '{2}'",
                rows.Count, _buildItem.ItemType, _buildItem.ItemName);
        }

        private void cmdAllocate_Click(object sender, EventArgs e)
        {
            ApplySelection();
        }

        private void dgvStructures_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ApplySelection();
        }

        private void ApplySelection()
        {
            if (dgvStructures.CurrentRow == null || dgvStructures.CurrentRow.Tag == null)
            {
                MessageBox.Show("Select a structure to allocate.", "Allocate",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = (StructureRow)dgvStructures.CurrentRow.Tag;
            SelectedColonyUUID = row.ColonyUUID;
            SelectedStructureUUID = row.StructureUUID;

            Log.Info("Allocated item '{0}' to colony '{1}' structure '{2}'",
                _buildItem.ItemName, row.ColonyName, row.StructureName);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
