using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using NLog;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker
{
    /// <summary>
    /// FormBlueprint - Main form for managing blueprint data in the OE2 Empire Tracker.
    /// </summary>
    /// <remarks>
    /// This form allows users to:
    /// - View all recorded blueprints in a list view
    /// - Create new blueprint entries with various properties
    /// - Edit existing blueprint entries
    /// - Delete blueprint records
    /// 
    /// Blueprints represent discovered technology including system object scanners,
    /// ship classes, and resource-related items. Each blueprint contains type information,
    /// evolution level, properties, resources, and copy costs.
    /// </remarks>
    public partial class FormBlueprint : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        /// <summary>
        /// Gets or sets the empire context instance for accessing empire-wide data.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// Gets or sets the player context instance for accessing player-specific data.
        /// </summary>
        private PlayerContext playerContext;

        /// <summary>
        /// Gets or sets the currently selected blueprint in the list view.
        /// Used when editing an existing blueprint.
        /// </summary>
        private BlueprintViewModel viewModel;

        // ListView sorting state
        private int _sortColumn = 0;
        private SortOrder _sortOrder = SortOrder.Ascending;

        // Evolution Graph tab controls
        private TabPage tabPEvolutionGraph;
        private Chart chartEvolution;
        private FlowLayoutPanel pnlPropertyCheckboxes;
        private Label lblNoChanges;

        // Filter panel controls (created in code-behind)
        private FlowLayoutPanel flpFilterPanel;
        private ComboBox cmbFilterType;
        private ComboBox cmbFilterClass;
        private ComboBox cmbFilterTechLevel;
        private ComboBox cmbFilterEvolution;
        private CheckBox chkEvolutionAndAbove;
        private Button btnClearFilters;

        /// <summary>
        /// Extended 16-color Wong palette (8 base + 8 lighter tints) for colorblind-friendly chart lines.
        /// </summary>
        internal static readonly Color[] WongPalette = new Color[]
        {
            // Base Wong palette (8 colors)
            ColorTranslator.FromHtml("#000000"), // black
            ColorTranslator.FromHtml("#E69F00"), // orange
            ColorTranslator.FromHtml("#56B4E9"), // sky blue
            ColorTranslator.FromHtml("#009E73"), // bluish green
            ColorTranslator.FromHtml("#B8860B"), // dark goldenrod (replaces yellow for contrast)
            ColorTranslator.FromHtml("#0072B2"), // blue
            ColorTranslator.FromHtml("#D55E00"), // vermillion
            ColorTranslator.FromHtml("#CC79A7"), // reddish purple
            // 50% lighter tints for properties 9-16
            ColorTranslator.FromHtml("#808080"), // light black (grey)
            ColorTranslator.FromHtml("#F2CF80"), // light orange
            ColorTranslator.FromHtml("#ABD9F4"), // light sky blue
            ColorTranslator.FromHtml("#80CEB9"), // light bluish green
            ColorTranslator.FromHtml("#DAA520"), // goldenrod (replaces light yellow for contrast)
            ColorTranslator.FromHtml("#80B8D8"), // light blue
            ColorTranslator.FromHtml("#EAAF80"), // light vermillion
            ColorTranslator.FromHtml("#E5BCD3"), // light reddish purple
        };

        /// <summary>
        /// Initializes a new instance of the FormBlueprint class.
        /// </summary>
        /// <remarks>
        /// Performs the following initialization tasks:
        /// - Obtains empire and player context instances
        /// - Configures blueprint type combo box with available types
        /// - Configures ship class combo box for non-universal blueprints
        /// - Configures tech level combo box for applicable blueprints
        /// - Sets up evolution dropdown with Evolution 0 selected by default
        /// - Configures resource data grid with resource types
        /// - Configures base blueprint combo box showing all available blueprints
        /// - Sets up the blueprint list view with appropriate columns (UUID, Type, Name, Tech Level, Evolution, Nick Name)
        /// - Populates the list view with existing blueprints from player context
        /// </remarks>
        public FormBlueprint()
        {
            InitializeComponent();
            InitFilterPanel();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new BlueprintViewModel(new Blueprint(), playerContext);

            // Configure blueprint type combo box
            cmbBlueprintType.DisplayMember = "Name";
            cmbBlueprintType.ValueMember = "Id";
            cmbBlueprintType.DataSource = empireContext.BindingSourceBlueprintType;
            cmbBlueprintType.SelectedIndex = -1;

            // Configure ship class combo box (used for non-universal blueprints)
            cmbShipClass.DisplayMember = "Name";
            cmbShipClass.ValueMember = "Id";
            cmbShipClass.DataSource = empireContext.BindingSourceShipClass;
            cmbShipClass.SelectedIndex = -1;

            // Configure tech level combo box
            cmbTechLevel.DisplayMember = "Name";
            cmbTechLevel.ValueMember = "Name";
            cmbTechLevel.DataSource = empireContext.BindingSourceTechLevel;
            cmbTechLevel.SelectedIndex = -1;

            // Set focus for statistics grid to previous control (tab navigation)
            dgvStatistics.previousControl = tabDetailedData;

            // Configure evolution dropdown with default value
            cmbEvolution.DisplayMember = "Name";
            cmbEvolution.ValueMember = "Name";
            cmbEvolution.DataSource = empireContext.BindingSourceEvolution;
            cmbEvolution.SelectedIndex = 0;

            // Configure resource data grid
            DataGridViewComboBoxColumn cmbResource = (DataGridViewComboBoxColumn)dgvResources.Columns["Resource"];
            cmbResource.DisplayMember = "Name";
            cmbResource.ValueMember = "Name";
            cmbResource.DataSource = empireContext.BindingSourceResource;

            // Configure base blueprint combo box
            cmbBaseBlueprint.DisplayMember = "ExtendedName";
            cmbBaseBlueprint.ValueMember = "UUID";
            cmbBaseBlueprint.DataSource = playerContext.BindingSourceBlueprint;
            cmbBaseBlueprint.SelectedIndex = -1;

            // Set up blueprint list view with columns
            lvwBlueprints.View = View.Details;
            lvwBlueprints.Columns.Add("UUID", 0);
            lvwBlueprints.Columns.Add("Type", 50);
            lvwBlueprints.Columns.Add("Name", 100);
            lvwBlueprints.Columns.Add("Tech Level", 60);
            lvwBlueprints.Columns.Add("Evolution", 30);
            lvwBlueprints.Columns.Add("Nick Name", 100);
            lvwBlueprints.Columns.Add("Refs", 40);
            lvwBlueprints.ColumnClick += lvwBlueprints_ColumnClick;
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(viewModel.GetFilteredBlueprints(null));

            // Wire write-through handlers
            txtName.TextChanged += txtName_TextChanged;
            txtNickName.TextChanged += txtNickName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            txtCopyCost.TextChanged += txtCopyCost_TextChanged;
            cmbShipClass.SelectedIndexChanged += cmbShipClass_SelectedIndexChanged;
            cmbTechLevel.SelectedIndexChanged += cmbTechLevel_SelectedIndexChanged;
            cmbEvolution.SelectedIndexChanged += cmbEvolution_SelectedIndexChanged;
            cmbBaseBlueprint.SelectedIndexChanged += cmbBaseBlueprint_SelectedIndexChanged;
            chkGlobalBlueprint.CheckedChanged += chkGlobalBlueprint_CheckedChanged;

            // Configure pricing plan combo
            cmbPricingPlan.DisplayMember = "Name";
            cmbPricingPlan.ValueMember = "UUID";
            PopulatePricingPlanCombo();
            cmbPricingPlan.SelectedIndexChanged += cmbPricingPlan_SelectedIndexChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged += OnBlueprintDataChanged;
            playerContext.PricingDataChanged += OnPricingDataChanged;

            InitEvolutionGraphTab();
            UpdateTitleBarCounts();
        }

        /// <summary>
        /// Creates the filter panel with four labeled ComboBoxes and a Clear Filters button.
        /// Uses local data copies (not shared BindingSources) so filter selections don't
        /// affect the detail-panel ComboBoxes. Each ComboBox has a blank first entry
        /// representing "no filter". Layout uses three rows: Type+Class, TechLevel+Evolution,
        /// and Clear Filters.
        /// </summary>
        private void InitFilterPanel()
        {
            empireContext = EmpireContext.GetInstance();

            // Create the outer container (vertical stack of rows)
            flpFilterPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(2)
            };

            // --- Row 1: Type + Class ---
            var row1 = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var lblType = new Label
            {
                Text = "Type:",
                Size = new Size(40, 21),
                TextAlign = ContentAlignment.MiddleRight
            };
            cmbFilterType = new ComboBox
            {
                Name = "cmbFilterType",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(150, 21)
            };
            // Local copy with blank first entry â€” avoids cross-talk with detail panel
            cmbFilterType.Items.Add("");
            foreach (BlueprintType bt in empireContext.BlueprintTypeList)
                cmbFilterType.Items.Add(bt.Name);
            cmbFilterType.SelectedIndex = 0;

            var lblClass = new Label
            {
                Text = "Class:",
                Size = new Size(40, 21),
                TextAlign = ContentAlignment.MiddleRight
            };
            cmbFilterClass = new ComboBox
            {
                Name = "cmbFilterClass",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(150, 21)
            };
            cmbFilterClass.Items.Add("");
            foreach (ShipClass sc in empireContext.ShipClassList)
                cmbFilterClass.Items.Add(sc.Name);
            cmbFilterClass.SelectedIndex = 0;

            row1.Controls.AddRange(new Control[] { lblType, cmbFilterType, lblClass, cmbFilterClass });

            // --- Row 2: Tech Level + Evolution ---
            var row2 = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var lblTech = new Label
            {
                Text = "Tech Level:",
                Size = new Size(65, 21),
                TextAlign = ContentAlignment.MiddleRight
            };
            cmbFilterTechLevel = new ComboBox
            {
                Name = "cmbFilterTechLevel",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(125, 21)
            };
            cmbFilterTechLevel.Items.Add("");
            foreach (TechLevel tl in empireContext.TechLevelList)
                cmbFilterTechLevel.Items.Add(tl.Name);
            cmbFilterTechLevel.SelectedIndex = 0;

            var lblEvo = new Label
            {
                Text = "Evolution:",
                Size = new Size(60, 21),
                TextAlign = ContentAlignment.MiddleRight
            };
            cmbFilterEvolution = new ComboBox
            {
                Name = "cmbFilterEvolution",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(60, 21)
            };
            cmbFilterEvolution.Items.Add("");
            foreach (string evo in empireContext.EvolutionList)
                cmbFilterEvolution.Items.Add(evo);
            cmbFilterEvolution.SelectedIndex = 0;

            chkEvolutionAndAbove = new CheckBox
            {
                Name = "chkEvolutionAndAbove",
                Text = "And Above",
                Size = new Size(80, 21),
                Checked = false
            };

            row2.Controls.AddRange(new Control[] { lblTech, cmbFilterTechLevel, lblEvo, cmbFilterEvolution, chkEvolutionAndAbove });

            // --- Row 3: Clear Filters ---
            var row3 = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            btnClearFilters = new Button
            {
                Text = "Clear Filters",
                Size = new Size(85, 23),
                UseVisualStyleBackColor = true
            };
            row3.Controls.Add(btnClearFilters);

            flpFilterPanel.Controls.AddRange(new Control[] { row1, row2, row3 });

            // Insert filter panel into flpSearchList between the text filter (index 0) and ListView (index 1)
            flpSearchList.Controls.Add(flpFilterPanel);
            flpSearchList.Controls.SetChildIndex(flpFilterPanel, 1);

            // Wire filter events
            cmbFilterType.SelectedIndexChanged += (s, e) => RefreshBlueprintList();
            cmbFilterClass.SelectedIndexChanged += (s, e) => RefreshBlueprintList();
            cmbFilterTechLevel.SelectedIndexChanged += (s, e) => RefreshBlueprintList();
            cmbFilterEvolution.SelectedIndexChanged += (s, e) => RefreshBlueprintList();
            chkEvolutionAndAbove.CheckedChanged += (s, e) => RefreshBlueprintList();
            btnClearFilters.Click += (s, e) =>
            {
                cmbFilterType.SelectedIndex = 0;
                cmbFilterClass.SelectedIndex = 0;
                cmbFilterTechLevel.SelectedIndex = 0;
                cmbFilterEvolution.SelectedIndex = 0;
                chkEvolutionAndAbove.Checked = false;
                RefreshBlueprintList();
            };
        }

        /// <summary>
        /// Formats the title bar text with global and player blueprint counts.
        /// Extracted as a public static method for independent testability.
        /// </summary>
        public static string FormatTitleBar(int globalCount, int playerCount)
        {
            return $"Blueprints - Global: {globalCount} Player: {playerCount}";
        }

        /// <summary>
        /// Updates the form's title bar with current global and player blueprint counts.
        /// </summary>
        private void UpdateTitleBarCounts()
        {
            int globalCount = empireContext.GlobalBlueprintList?.Count ?? 0;
            int playerCount = 0;
            if (!string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                playerCount = playerContext.GetCurrentPlayerBlueprints().Count;
            }
            this.Text = FormatTitleBar(globalCount, playerCount);
        }

        /// <summary>
        /// Creates the Evolution Graph tab, chart control, checkbox panel, and no-changes label.
        /// Called from the constructor after InitializeComponent() and existing setup.
        /// </summary>
        private void InitEvolutionGraphTab()
        {
            // Create the tab page
            tabPEvolutionGraph = new TabPage
            {
                Text = "Evolution Graph",
                UseVisualStyleBackColor = true
            };

            // Create the chart control
            chartEvolution = new Chart
            {
                Dock = DockStyle.Fill
            };

            var chartArea = new ChartArea("EvolutionArea");

            // X-axis: Evolution Level, range 0-15, interval 1
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = 15;
            chartArea.AxisX.Interval = 1;
            chartArea.AxisX.Title = "Evolution Level";

            // Y-axis: percentage change, range 50â€“150 with 100% baseline center, gridline interval 10%
            chartArea.AxisY.Title = "% Change from Evolution 0 Value";
            chartArea.AxisY.Minimum = 50;
            chartArea.AxisY.Maximum = 150;
            chartArea.AxisY.MajorGrid.Interval = 10;

            chartEvolution.ChartAreas.Add(chartArea);

            // Create the property checkbox panel
            pnlPropertyCheckboxes = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 180
            };

            // Create the no-changes label
            lblNoChanges = new Label
            {
                Text = "No property changes found across the evolution chain.",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Add controls to the tab page (order matters for Dock layout)
            tabPEvolutionGraph.Controls.Add(chartEvolution);
            tabPEvolutionGraph.Controls.Add(pnlPropertyCheckboxes);
            tabPEvolutionGraph.Controls.Add(lblNoChanges);

            // Add the tab after existing tabs
            tabDetailedData.TabPages.Add(tabPEvolutionGraph);
        }

        private void OnBlueprintDataChanged(object sender, BlueprintDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnBlueprintDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            if (viewModel.Data.UUID == e.BlueprintUUID)
            {
                PopulateForm();
            }
            RefreshEvolutionGraph();
            RefreshBlueprintList();
            UpdateTitleBarCounts();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            lvwBlueprints.Items.Clear();
            viewModel.Reset();
            ClearForm();
            PopulatePricingPlanCombo();
            RefreshBlueprintList();
            UpdateTitleBarCounts();
        }

        /// <summary>
        /// Handles text changes in the copy target rich text box.
        /// </summary>
        /// <param name="sender">The RichTextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void rtbCopyTarget_TextChanged(object sender, EventArgs e)
        {
            // Log.Debug(e.ToString());
        }

        /// <summary>
        /// Processes HTML content by parsing with SgmlReader and debugging child nodes.
        /// </summary>
        /// <param name="inputText">The HTML string to parse.</param>
        /// <remarks>
        /// Currently used for debugging - prints inner text of each node to Debug window.
        /// Uses SgmlReader for HTML parsing with whitespace handling preserved.
        /// </remarks>
        private void ProcessHTML(string inputText)
        {
            StringReader reader = new StringReader(inputText);

            // Setup SgmlReader with HTML document type and settings
            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };

            // Create document with whitespace preservation
            XmlDocument doc = new XmlDocument()
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            doc.Load(sgmlReader);

            // Debug: Print inner text of each node
            foreach (XmlNode item in doc)
            {
                Log.Debug("T = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children(0, item.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Recursively processes child nodes and prints their inner text to debug output.
        /// </summary>
        /// <param name="depth">Current recursion depth for indentation.</param>
        /// <param name="nodes">List of child nodes to process.</param>
        /// <remarks>
        /// Used by ProcessHTML() to traverse and debug HTML node structure.
        /// Increments depth parameter for recursive calls to show nesting level.
        /// </remarks>
        private void children(int depth, XmlNodeList nodes)
        {
            foreach (XmlNode item in nodes)
            {
                Log.Debug("C" + depth + " = " + item.InnerText);
                if (item.HasChildNodes)
                {
                    children((depth + 1), item.ChildNodes);
                }
            }
        }

        /// <summary>
        /// Handles changes in the blueprint type combo box selection.
        /// </summary>
        /// <param name="sender">The ComboBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Triggers property grid update to reflect properties of selected blueprint type.
        /// Universal blueprint types hide ship class and tech level options.
        /// </remarks>
        private void cmbBlueprintType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bt = cmbBlueprintType.SelectedItem as BlueprintType;
            if (bt != null) viewModel.BluePrintType = bt.Id;
            UpdatePropertyGrid();
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Name = txtName.Text;
        }

        private void txtNickName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.NickName = txtNickName.Text;
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Description = txtDescription.Text;
        }

        private void txtCopyCost_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            int copyCost = 0;
            int.TryParse(txtCopyCost.Text, out copyCost);
            viewModel.CopyCost = copyCost;
        }

        private void cmbShipClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var sc = cmbShipClass.SelectedItem as ShipClass;
            viewModel.Class = sc != null ? sc.Id : 0;
        }

        private void cmbTechLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var tl = cmbTechLevel.SelectedItem as TechLevel;
            viewModel.TechLevel = tl?.Name;
        }

        private void cmbEvolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            string evo = cmbEvolution.SelectedItem as string ?? cmbEvolution.Text ?? "0";
            int.TryParse(evo, out int ev);
            viewModel.Evolution = ev;
        }

        private void cmbBaseBlueprint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            var bp = cmbBaseBlueprint.SelectedItem as Blueprint;
            viewModel.BaseBlueprintUUID = bp?.UUID ?? "";
        }

        private void chkGlobalBlueprint_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Global flag is read at save time â€” no viewModel field to write.
        }

        /// <summary>
        /// Updates the base blueprint list with filtered available blueprints based on current filters.
        /// </summary>
        /// <remarks>
        /// Applies multiple filter conditions:
        /// - Filters by blueprint type if a type is selected
        /// - Applies text filter from txtFilterBaseBlueprint for name-based search
        /// - Filters by ship class if a class is selected (for non-universal types)
        /// - Excludes the currently selected blueprint to allow selecting different base blueprints
        /// 
        /// An empty Blueprint entry is inserted at the beginning to allow deselecting a base blueprint.
        /// </remarks>
        public void UpdateBlueprintTypeListBase()
        {
            string searchText = txtFilterBlueprintType.Text;
            List<BlueprintType> filteredList = new List<BlueprintType>(empireContext.BlueprintTypeList);

            // Apply text filter if specified
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            
            filteredList.Insert(0, new BlueprintType());
            
            var filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;

            cmbBlueprintType.DataSource = filteredItemsBindingList;
        }

        /// <summary>
        /// Updates the property grid based on the selected blueprint type.
        /// </summary>
        /// <remarks>
        /// For universal blueprints (Universal == true):
        /// - Hides the ship class dropdown panel
        /// - Resets ship class selection
        /// - Hides the tech level dropdown panel
        /// - Resets tech level selection
        /// 
        /// For non-universal blueprints:
        /// - Shows both ship class and tech level panels
        /// 
        /// Then populates the statistics grid (dgvStatistics) with blueprint type properties:
        /// - Clears existing rows if no properties defined
        /// - Adds rows for each property defined by the blueprint type
        /// - Sets property name in "Property" column
        /// - Stores property tag for later value retrieval
        /// - Removes excess rows if fewer than defined properties
        /// </remarks>
        public void UpdatePropertyGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            BlueprintType bt = cmbBlueprintType.SelectedItem as BlueprintType;
            
            // Handle universal vs non-universal blueprints
            if (bt != null && bt.Universal == true)
            {
                flpClass.Visible = false;
                cmbShipClass.SelectedIndex = -1;
                flpTechLevel.Visible = false;
                cmbTechLevel.SelectedIndex = -1;
            }
            else
            {
                flpClass.Visible = true;
                flpTechLevel.Visible = true;
            }

            // Clear grid when no blueprint type is selected
            if (bt == null || bt.Properties == null || bt.Properties.Length == 0)
            {
                dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
                try { dgvStatistics.EndEdit(); } catch { }
                dgvStatistics.Rows.Clear();
                dgvStatistics.CellValidating += dgvStatistics_CellValidating;
                return;
            }

            // Populate property grid rows
            {
                int row = 0;
                foreach (string property in bt.Properties)
                {
                    int rowIndex = row;
                    // Add row if needed
                    if (dgvStatistics.Rows.Count <= row)
                    {
                        rowIndex = dgvStatistics.Rows.Add();
                    }
                    
                    DataGridViewRow newRow = dgvStatistics.Rows[rowIndex];
                    newRow.Cells["Property"].Value = property;
                    newRow.Cells["Property"].Tag = property;

                    // Swap the CurrentValue cell based on property type
                    var propType = BlueprintPropertyValidation.GetPropertyType(property);
                    if (propType == PropertyValueType.ComboBox)
                    {
                        var comboCell = new DataGridViewComboBoxCell();
                        comboCell.DataSource = BlueprintPropertyValidation.GetComboBoxDataSource(property);
                        comboCell.FlatStyle = FlatStyle.Flat;
                        newRow.Cells["CurrentValue"] = comboCell;
                    }
                    else if (propType == PropertyValueType.CheckBox)
                    {
                        var checkCell = new DataGridViewCheckBoxCell();
                        checkCell.Value = false;
                        newRow.Cells["CurrentValue"] = checkCell;
                    }
                    else
                    {
                        // Ensure it's a text cell (may have been swapped previously)
                        if (!(newRow.Cells["CurrentValue"] is DataGridViewTextBoxCell))
                        {
                            newRow.Cells["CurrentValue"] = new DataGridViewTextBoxCell();
                        }
                    }

                    row++;
                }

                // Remove excess rows if there are more than defined properties
                while (dgvStatistics.Rows.Count > bt.Properties.Length)
                {
                    dgvStatistics.Rows.RemoveAt(dgvStatistics.Rows.Count - 1);
                }
            }
        }

        /// <summary>
        /// Handles selection changes in the statistics data grid.
        /// </summary>
        /// <param name="sender">The DataGridView object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void dgvStatistics_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            Log.Debug("dgvStatistics_SelectionChanged Sender = " + sender + " Event Args " + e);
        }

        /// <summary>
        /// Updates the base blueprint list with filtered results.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtFilterBaseBlueprint_TextChanged(object sender, EventArgs e)
        {
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.DroppedDown = true;
        }

        /// <summary>
        /// Reads all filter controls, builds a BlueprintFilterCriteria, calls GetFilteredBlueprints,
        /// and populates the list view with the results.
        /// </summary>
        private void RefreshBlueprintList()
        {
            string nameFilter = txtBlueprintListFilter.Text;

            var criteria = new BlueprintFilterCriteria();

            // Filter ComboBoxes use local string items with blank at index 0.
            // Index 0 (blank) means no filter; any other selection is a display name
            // that we map back to the actual filter value.
            if (cmbFilterType != null && cmbFilterType.SelectedIndex > 0)
            {
                string typeName = (string)cmbFilterType.SelectedItem;
                var bt = empireContext.BlueprintTypeList.FirstOrDefault(b => b.Name == typeName);
                if (bt != null) criteria.BlueprintTypeId = bt.Id;
            }

            if (cmbFilterClass != null && cmbFilterClass.SelectedIndex > 0)
            {
                string className = (string)cmbFilterClass.SelectedItem;
                var sc = empireContext.ShipClassList.FirstOrDefault(s => s.Name == className);
                if (sc != null) criteria.ShipClassId = sc.Id;
            }

            if (cmbFilterTechLevel != null && cmbFilterTechLevel.SelectedIndex > 0)
            {
                criteria.TechLevelName = (string)cmbFilterTechLevel.SelectedItem;
            }

            if (cmbFilterEvolution != null && cmbFilterEvolution.SelectedIndex > 0)
            {
                string evoStr = (string)cmbFilterEvolution.SelectedItem;
                if (int.TryParse(evoStr, out int evo))
                    criteria.Evolution = evo;
                if (chkEvolutionAndAbove != null && chkEvolutionAndAbove.Checked)
                    criteria.EvolutionAndAbove = true;
            }

            var results = viewModel.GetFilteredBlueprints(nameFilter, criteria);
            PopulateListView(results);
        }

        /// <summary>
        /// Updates the blueprint list view with filtered blueprints based on current search filters.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtBlueprintListFilter_TextChanged(object sender, EventArgs e)
        {
            RefreshBlueprintList();
        }

        /// <summary>
        /// Updates the base blueprint list with filtered results based on current blueprint data.
        /// </summary>
        /// <remarks>
        /// Delegates filtering to BlueprintViewModel.GetBaseBlueprintCandidates which matches on:
        /// - Same BluePrintType, Name, Class, TechLevel as the current blueprint
        /// - Only lower evolutions (0 to current-1)
        /// - Excludes the current blueprint itself
        /// - Applies text filter from txtFilterBaseBlueprint
        /// Results are ordered by Evolution descending (best match first).
        /// An empty Blueprint entry is inserted at the beginning to allow deselecting a base blueprint.
        /// </remarks>
        private void UpdateBaseBlueprintList()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            string searchText = txtFilterBaseBlueprint.Text;
            List<Blueprint> filteredList = new List<Blueprint>(viewModel.GetBaseBlueprintCandidates(searchText));

            // Add empty entry at top to allow selecting no base blueprint
            filteredList.Insert(0, new Blueprint());

            BindingSource filteredItemsBindingList = new BindingSource();
            filteredItemsBindingList.DataSource = filteredList;

            cmbBaseBlueprint.DataSource = filteredItemsBindingList;
        }

        /// <summary>
        /// Populates the blueprint list view with the provided list of blueprints.
        /// </summary>
        /// <param name="blueprints">The list of blueprints to populate the view with.</param>
        /// <remarks>
        /// Clears the existing ListView and creates a dictionary keyed by UUID for efficient 
        /// lookup of existing items to avoid duplicates. For each blueprint in the input list:
        /// - Creates a new ListView item if not already present
        /// - Updates existing item's subitems with current blueprint data
        /// - Maintains reference to Blueprint object in Tag property for editing and deletion operations
        /// 
        /// Displays the following columns:
        /// - UUID (first column, uses full width remaining)
        /// - Type (blueprint type ID/name)
        /// - Name
        /// - Tech Level
        /// - Evolution level (as string)
        /// - Nick Name
        /// 
        /// After processing all items, removes any remaining ListView items whose 
        /// corresponding blueprints were deleted or filtered out.
        /// </remarks>
        /// <summary>
        /// Creates a <see cref="BlueprintReferenceCounter"/> from the current
        /// <see cref="PlayerContext"/> and <see cref="EmpireContext"/> data.
        /// </summary>
        private static BlueprintReferenceCounter CreateReferenceCounter()
        {
            var pc = PlayerContext.GetInstance();
            var ec = EmpireContext.GetInstance();

            var colonies = pc?.ColonyList as IEnumerable<Colony> ?? Enumerable.Empty<Colony>();
            var allBlueprints = new List<Blueprint>();
            if (pc?.BlueprintList != null) allBlueprints.AddRange(pc.BlueprintList);
            if (ec?.GlobalBlueprintList != null) allBlueprints.AddRange(ec.GlobalBlueprintList);
            var surveys = pc?.SurveyList as IEnumerable<Survey> ?? Enumerable.Empty<Survey>();

            return new BlueprintReferenceCounter(colonies, allBlueprints, surveys);
        }

        void PopulateListView(IReadOnlyList<Blueprint> blueprints)
        {
            if (blueprints == null)
            {
                return;
            }

            var counter = CreateReferenceCounter();

            // Clear and rebuild the list view
            lvwBlueprints.Items.Clear();

            // Dictionary for efficient duplicate detection by UUID
            Dictionary<string, ListViewItem> viewableBlueprints = new Dictionary<string, ListViewItem>();

            // Index currently viewable items to detect duplicates
            foreach (ListViewItem item in lvwBlueprints.Items)
            {
                viewableBlueprints[(item.Tag as Blueprint).UUID] = item;
            }

            // Process each blueprint - add or update ListView items
            foreach (Blueprint blueprint in blueprints)
            {
                ListViewItem item;
                bool found = viewableBlueprints.TryGetValue(blueprint.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(blueprint.UUID); // Main item text (first column - UUID)
                }
                item.Tag = blueprint;
                item.SubItems[0].Tag = blueprint;
                item.SubItems.Add(blueprint.BluePrintType); // Type
                item.SubItems.Add(blueprint.Name); // Name
                item.SubItems.Add(blueprint.TechLevel); // Tech Level
                item.SubItems.Add("" + blueprint.Evolution); // Evolution
                item.SubItems.Add(blueprint.NickName); // Nick Name
                item.SubItems.Add(counter.CountReferences(blueprint.UUID).TotalCount.ToString()); // Refs

                if (!found)
                {
                    lvwBlueprints.Items.Add(item); // Add the item to the ListView
                }
                else
                {
                    viewableBlueprints.Remove(blueprint.UUID);
                }
            }

            // Remove items that no longer have corresponding blueprints (deleted/filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableBlueprint in viewableBlueprints)
            {
                lvwBlueprints.Items.Remove(viewableBlueprint.Value);
            }
        }

        /// <summary>
        /// Handles changes in the blueprint list view item selection.
        /// </summary>
        /// <param name="sender">The ListView object that triggered the event.</param>
        /// <param name="e">Event data containing selection change information.</param>
        /// <remarks>
        /// When a user selects an item in the blueprint list, this method:
        /// - Validates that exactly one item is selected
        /// - Retrieves the Blueprint object from the ListView item's Tag property
        /// - Populates all form fields with the selected blueprint's data
        /// 
        /// Uses NLog for logging selection changes.
        /// </remarks>
        private void lvwBlueprints_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Log.Debug("lvwBlueprints.SelectedItems.Count = " + lvwBlueprints.SelectedItems.Count);

            if (lvwBlueprints.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Text);
                Log.Debug("Selected item = " + lvwBlueprints.SelectedItems[0].SubItems[0].Tag);

                // Get the Blueprint object from the ListView item tag
                var blueprint = lvwBlueprints.SelectedItems[0].SubItems[0].Tag as Blueprint;
                viewModel.SelectBlueprint(blueprint);

                // Compute reference report and update delete button state
                var counter = CreateReferenceCounter();
                var report = counter.CountReferences(blueprint?.UUID);
                var (enabled, text) = GetDeleteButtonState(report);
                cmdDelete.Enabled = enabled;
                cmdDelete.Text = text;

                // Populate form fields with selected blueprint data
                PopulateForm();
                RefreshEvolutionGraph();
            }
            else
            {
                // No blueprint selected â€” disable delete button
                var (enabled, text) = GetDeleteButtonState(null);
                cmdDelete.Enabled = enabled;
                cmdDelete.Text = text;
                ClearEvolutionGraph();
            }
        }

        /// <summary>
        /// Handles text changes in the blueprint list filter text box.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        private void txtFilterBlueprintType_TextChanged(object sender, EventArgs e)
        {
            UpdateBlueprintTypeListBase();
            cmbBlueprintType.DroppedDown = true;
        }

        /// <summary>
        /// Handles entering text in the blueprint type filter text box.
        /// </summary>
        /// <param name="sender">The TextBox object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Automatically drops down the combo box when user clicks in filter text box.
        /// </remarks>
        private void txtFilterBlueprintType_Enter(object sender, EventArgs e)
        {
            cmbBlueprintType.DroppedDown = true;
        }

        /// <summary>
        /// Handles the click event for the Delete button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Deletes the currently selected blueprint:
        /// - Delegates removal to viewModel.Delete()
        /// - Resets viewModel state
        /// - Updates the list view with remaining blueprints
        /// - Clears all form controls
        /// </remarks>
        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (!cmdDelete.Enabled) return;
            if (viewModel.Data.UUID == null) return;
            var result = MessageBox.Show(
                $"Delete blueprint '{viewModel.Data.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            viewModel.Delete();
            viewModel.Reset();
            RefreshBlueprintList();
            lvwBlueprints.SelectedItems.Clear();
            ClearForm();
        }

        /// <summary>
        /// Handles the click event for the New button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Clears all form controls to prepare for entering a new blueprint.
        /// Called from both New button and Save button after successful save.
        /// </remarks>
        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwBlueprints.SelectedItems.Clear();
        }

        /// <summary>
        /// Handles the click event for the Save button.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Saves a new blueprint or updates an existing one based on whether selectedBlueprint is null.
        /// 
        /// For new blueprints:
        /// - Generates a unique UUID using Guid.NewGuid()
        /// - Initializes with default empty values
        /// 
        /// For existing blueprints:
        /// - Uses the currently selected Blueprint object
        /// 
        /// Populates the blueprint with:
        /// - Blueprint type ID from combo box selection
        /// - Ship class ID (for non-universal types)
        /// - Tech level name (if applicable)
        /// - Evolution level as integer
        /// - Base blueprint UUID from combo box selection
        /// - Name, nickname, and description from text inputs
        /// - Copy cost parsed from text input
        /// - Properties from statistics grid rows
        /// - Resources from resources grid
        /// 
        /// After saving:
        /// - Adds to player context or updates existing blueprint
        /// - Persists changes via WriteContext()
        /// - Refreshes list view with updated data
        /// - Clears form controls
        /// - Restores focus to blueprint list filter
        /// </remarks>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // Temporarily suppress cell validation so we can exit edit mode
            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try
            {
                dgvStatistics.EndEdit();
                dgvResources.EndEdit();
            }
            catch { }
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            dgvResources.CellValidating += dgvResources_CellValidating;

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            // Map properties from grid via viewModel
            viewModel.ClearProperties();
            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string propName = row.Cells[0].Tag as string;
                object cellValue = row.Cells[2].Value;
                string strValue = cellValue is bool ? cellValue.ToString() : cellValue as string;
                viewModel.SetProperty(propName, strValue);
            }

            // Map resources from grid via viewModel
            viewModel.ClearResources();
            foreach (DataGridViewRow row in dgvResources.Rows)
            {
                string resourceName = row.Cells[0].Value as string;
                string resourceAmount = row.Cells[1].Value as string;
                if (resourceName != null)
                {
                    viewModel.SetResource(resourceName, resourceAmount);
                }
            }

            // Persist via viewModel
            viewModel.Save(chkGlobalBlueprint.Checked);

            // Preserve the blueprint type filter across the list refresh â€”
            // PopulateForm (triggered by selection change) clears it otherwise.
            string savedTypeFilter = txtFilterBlueprintType.Text;

            // Refresh list view
            RefreshBlueprintList();

            // Restore the blueprint type filter
            txtFilterBlueprintType.Text = savedTypeFilter;
            using (new ProgrammaticUpdateGuard(this))
            {
                UpdateBlueprintTypeListBase();
                cmbBlueprintType.SelectedItem = empireContext.FindBlueprintType(viewModel.Data.BluePrintType);
            }
            
            // Restore focus
            txtBlueprintListFilter.Focus();
        }

        /// <summary>
        /// Populates the form fields with data from the currently selected blueprint.
        /// </summary>
        /// <remarks>
        /// If no blueprint is selected (selectedBlueprint is null), this method returns immediately.
        /// Otherwise, it:
        /// - Clears and regenerates blueprint type list
        /// - Loads the blueprint type into the combo box
        /// - Updates property grid based on blueprint type
        /// - Loads ship class, tech level, and evolution dropdown selections
        /// - Clears and regenerates base blueprint list
        /// - Loads the base blueprint (if any) into the combo box
        /// - Populates all text input fields from the blueprint's properties and sub-properties
        /// - Populates the statistics grid with property values
        /// - Populates the resources grid with the blueprint's resource data
        /// </remarks>
        private void PopulateForm()
        {
            if (viewModel.Data.UUID == null)
            {
                return;
            }

            using var guard = new ProgrammaticUpdateGuard(this);

            // Clear and regenerate blueprint type list
            txtFilterBlueprintType.Text = "";
            UpdateBlueprintTypeListBase();

            // Load blueprint type selection
            cmbBlueprintType.SelectedItem = empireContext.FindBlueprintType(viewModel.Data.BluePrintType);

            // Update property grid based on selected type
            UpdatePropertyGrid();

            // Load dependent dropdowns
            cmbShipClass.SelectedItem = empireContext.FindShipClass(viewModel.Data.Class);
            cmbTechLevel.SelectedItem = empireContext.FindTechLevel(viewModel.Data.TechLevel);
            cmbEvolution.SelectedItem = empireContext.FindEvolution(viewModel.Data.Evolution);

            // Clear and regenerate base blueprint list
            txtFilterBaseBlueprint.Text = "";
            UpdateBaseBlueprintList();

            // Load base blueprint selection (if this is not the base blueprint)
            if (!string.IsNullOrEmpty(viewModel.Data.BaseBlueprintUUID))
                cmbBaseBlueprint.SelectedValue = viewModel.Data.BaseBlueprintUUID;
            else
                cmbBaseBlueprint.SelectedIndex = 0;

            // Populate text input fields
            txtName.Text = viewModel.Data.Name;
            txtNickName.Text = viewModel.Data.NickName;
            txtDescription.Text = viewModel.Data.Description;
            txtCopyCost.Text = "" + viewModel.Data.CopyCost;

            // Populate statistics grid with property values
            foreach (DataGridViewRow row in dgvStatistics.Rows)
            {
                string property = row.Cells["Property"].Tag as string;
                string value = "";
                bool found = viewModel.GetProperty(property, "", out value);
                if (!found || value == null)
                {
                    value = "";
                }

                var propType = BlueprintPropertyValidation.GetPropertyType(property);
                if (propType == PropertyValueType.CheckBox)
                {
                    bool boolVal = false;
                    bool.TryParse(value, out boolVal);
                    row.Cells["CurrentValue"].Value = boolVal;
                }
                else
                {
                    row.Cells["CurrentValue"].Value = value;
                }
            }
            PopulateResources();

            chkGlobalBlueprint.Checked = viewModel.IsGlobal;

            UpdateCalculatedPrice();
        }
        private void PopulateResources()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            // Populate resources grid with blueprint's resource data
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvResources.EndEdit(); } catch { }
            dgvResources.Rows.Clear();
            dgvResources.CellValidating += dgvResources_CellValidating;
            foreach (KeyValuePair<string, string> resource in viewModel.GetResources())
            {
                dgvResources.Rows.Add();
                DataGridViewRow row = dgvResources.Rows[dgvResources.RowCount - 2];
                row.Cells[0].Value = resource.Key;
                row.Cells[1].Value = resource.Value;
            }
        }

        /// <summary>
        /// Clears all form controls to prepare for a new blueprint entry.
        /// </summary>
        /// <remarks>
        /// Resets all controls to their default/empty states:
        /// - Clears the selected blueprint reference
        /// - Resets all text inputs (name, nickname, description, copy cost)
        /// - Resets all combo boxes and dropdowns
        /// - Regenerates filtered lists for type and base blueprint selectors
        /// - Clears both statistics and resources grids
        /// </remarks>
        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            viewModel.Reset();

            // Clear filters
            txtFilterBlueprintType.Text = "";
            txtFilterBaseBlueprint.Text = "";

            // Regenerate type list with empty selection
            UpdateBlueprintTypeListBase();
            cmbBlueprintType.SelectedItem = null;
            cmbBlueprintType.Text = "";
            
            // Reset ship class and tech level dropdowns
            cmbShipClass.SelectedItem = null;
            cmbShipClass.Text = "";
            cmbTechLevel.SelectedItem = null;
            cmbTechLevel.Text = "";
            
            // Reset evolution dropdown
            cmbEvolution.SelectedItem = null;
            cmbEvolution.Text = "";

            // Regenerate base blueprint list with empty selection
            UpdateBaseBlueprintList();
            cmbBaseBlueprint.SelectedItem = null;

            // Clear text inputs
            txtName.Text = "";
            txtNickName.Text = "";
            txtDescription.Text = "";
            txtCopyCost.Text = "";

            // Clear grids
            dgvStatistics.CellValidating -= dgvStatistics_CellValidating;
            dgvResources.CellValidating -= dgvResources_CellValidating;
            try { dgvStatistics.EndEdit(); } catch { }
            try { dgvResources.EndEdit(); } catch { }
            dgvStatistics.Rows.Clear();
            dgvResources.Rows.Clear();
            dgvStatistics.CellValidating += dgvStatistics_CellValidating;
            dgvResources.CellValidating += dgvResources_CellValidating;

            chkGlobalBlueprint.Checked = false;

            txtCalculatedPrice.Text = "";

            ClearEvolutionGraph();
        }

        /// <summary>
        /// Handles size changes to the search list flow panel.
        /// </summary>
        /// <param name="sender">The FlowLayoutPanel object that triggered the event.</param>
        /// <param name="e">Event data containing event information.</param>
        /// <remarks>
        /// Previously used to adjust ListView height based on parent panel size.
        /// Commented out - may be re-enabled when needed.
        /// </remarks>
        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            // Account for child margins in the FlowLayoutPanel
            int totalMarginH = flpSearchList.Margin.Horizontal + flowLayoutPanel1.Margin.Horizontal;
            int totalMarginV = flpSearchList.Margin.Vertical;
            int availableWidth = flpBase.ClientSize.Width - totalMarginH;
            int height = flpBase.ClientSize.Height - totalMarginV;

            // Split: ~38% for search list, remainder for detail panel
            int searchWidth = (int)(availableWidth * 0.38);
            int detailWidth = availableWidth - searchWidth;

            flpSearchList.Size = new System.Drawing.Size(searchWidth, height);
            flowLayoutPanel1.Size = new System.Drawing.Size(detailWidth, height);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            // ListView fills remaining height after the search bar and filter panel
            int usedHeight = flpBlueprintSearch.Height + flpBlueprintSearch.Margin.Top + flpBlueprintSearch.Margin.Bottom;
            if (flpFilterPanel != null)
            {
                usedHeight += flpFilterPanel.Height + flpFilterPanel.Margin.Top + flpFilterPanel.Margin.Bottom;
            }
            lvwBlueprints.Size = new System.Drawing.Size(
                flpSearchList.ClientSize.Width - lvwBlueprints.Margin.Left - lvwBlueprints.Margin.Right,
                flpSearchList.ClientSize.Height - usedHeight - lvwBlueprints.Margin.Top - lvwBlueprints.Margin.Bottom);
        }

        private void flowLayoutPanel1_Layout(object sender, LayoutEventArgs e)
        {
            // TabControl fills remaining height after base details and commands
            int tabHeight = flowLayoutPanel1.ClientSize.Height
                - flpBaseDetails.Height - flpBaseDetails.Margin.Top - flpBaseDetails.Margin.Bottom
                - flpCommands.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom
                - tabDetailedData.Margin.Top - tabDetailedData.Margin.Bottom;
            int tabWidth = flowLayoutPanel1.ClientSize.Width - tabDetailedData.Margin.Left - tabDetailedData.Margin.Right;

            tabDetailedData.Size = new System.Drawing.Size(tabWidth, Math.Max(100, tabHeight));
        }

        private void cmdImportMarket_Click(object sender, EventArgs e)
        {
            // Read clipboard HTML
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No market HTML found on clipboard.",
                    "Import Market", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string clipboardData = Clipboard.GetText(TextDataFormat.Html);
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(clipboardData);
            if (string.IsNullOrEmpty(html) || html.StartsWith("ERROR:"))
            {
                MessageBox.Show("No market HTML found on clipboard.",
                    "Import Market", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Validate clipboard contains market listing data
            var detected = ClipboardContentDetector.Detect(html);
            if (detected != ClipboardContentDetector.ContentType.MarketListing &&
                detected != ClipboardContentDetector.ContentType.Unknown)
            {
                string found = ClipboardContentDetector.GetDescription(detected);
                MessageBox.Show($"The clipboard contains {found}, not market listing data.\n\nCopy the market page from the game browser first.",
                    "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Parse market HTML
            var scanner = new BlueprintScanner();
            var parsed = scanner.ProcessMarketHtml(html);
            if (parsed == null || parsed.Count == 0)
            {
                MessageBox.Show("No blueprint listings found in clipboard data.",
                    "Import Market", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Import
            var result = MarketBlueprintImporter.Import(parsed, playerContext, empireContext);

            // Build summary
            var sb = new StringBuilder();
            sb.AppendLine($"Created: {result.CreatedCount}  Updated: {result.UpdatedCount}  Skipped: {result.SkippedCount}");
            sb.AppendLine();
            foreach (var entry in result.Entries)
            {
                string key = $"{entry.Name} Ev{entry.Evolution} {entry.BluePrintType} C{entry.Class}";
                if (entry.Action == ImportAction.Skipped)
                    sb.AppendLine($"  [{entry.Storage ?? "?"}] SKIP  {key} â€” {entry.SkipReason}");
                else
                    sb.AppendLine($"  [{entry.Storage}] {entry.Action}  {key}");
            }

            MessageBox.Show(sb.ToString(), "Import Market Results",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Notify if player blueprints changed
            bool playerChanged = result.Entries.Any(e2 =>
                e2.Storage == "Player" && (e2.Action == ImportAction.Created || e2.Action == ImportAction.Updated));
            if (playerChanged)
            {
                playerContext.OnBlueprintDataChanged(null);
            }

            // Refresh the blueprint list
            RefreshBlueprintList();
        }

        private void cmdImport_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML found on clipboard. Copy the blueprint page from the game first.",
                    "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Validate clipboard contains blueprint data (individual or resources-only)
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string htmlFragment = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                var detected = Parsers.ClipboardContentDetector.Detect(htmlFragment);
                if (detected != Parsers.ClipboardContentDetector.ContentType.Blueprint &&
                    detected != Parsers.ClipboardContentDetector.ContentType.Survey &&
                    detected != Parsers.ClipboardContentDetector.ContentType.Unknown)
                {
                    string found = Parsers.ClipboardContentDetector.GetDescription(detected);
                    MessageBox.Show($"The clipboard contains {found}, not blueprint data.\n\nCopy the blueprint page from the game browser first.",
                        "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var scanner = new BlueprintScanner();
                var tempBP = scanner.ParseClipboardToTemp();

                if (tempBP == null)
                    return;

                // ── BL-062: Resources-only import (e.g. resources tab copied from game) ──
                if (MarketBlueprintImporter.IsResourcesOnlyImport(tempBP))
                {
                    if (string.IsNullOrEmpty(viewModel.Data.UUID))
                    {
                        MessageBox.Show(
                            "Please select or import a blueprint first, then import the resources tab.",
                            "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    MarketBlueprintImporter.MergeResourcesOnly(viewModel.Data, tempBP);

                    // Persist to the correct list
                    bool resGlobal = empireContext.GlobalBlueprintList.Any(b => b.UUID == viewModel.Data.UUID);
                    if (resGlobal)
                        empireContext.WriteContext();
                    else
                        playerContext.WriteContext();

                    // Notify, refresh, re-select
                    playerContext.OnBlueprintDataChanged(viewModel.Data.UUID);
                    RefreshBlueprintList();

                    foreach (ListViewItem item in lvwBlueprints.Items)
                    {
                        if ((item.Tag as Blueprint)?.UUID == viewModel.Data.UUID)
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                            break;
                        }
                    }

                    PopulateForm();
                    Log.Info("Resources-only import merged into selected blueprint: {0} UUID={1}",
                        viewModel.Data.Name, viewModel.Data.UUID);
                    return;
                }

                // Fallback: if no name was parsed, use current behavior
                if (string.IsNullOrEmpty(tempBP.Name))
                {
                    scanner.ProcessClipboard(viewModel.Data);
                    if (string.IsNullOrEmpty(viewModel.Data.UUID))
                    {
                        bool fallbackGlobal = viewModel.Data.Evolution == 0
                            && string.IsNullOrEmpty(viewModel.Data.OwnerUUID);
                        viewModel.Data.UUID = fallbackGlobal
                            ? DeterministicUUID.Generate(viewModel.Data)
                            : Guid.NewGuid().ToString();
                    }
                    PopulateForm();
                    Log.Info("Blueprint imported from clipboard (fallback, no name parsed)");
                    return;
                }

                Blueprint importedBP;
                bool globalChanged = false;
                bool playerChanged = false;

                // Check if the selected blueprint matches the dedup key
                if (!string.IsNullOrEmpty(viewModel.Data.UUID) &&
                    MarketBlueprintImporter.FindByDedupKey(
                        new BindingList<Blueprint>(new[] { viewModel.Data }),
                        tempBP) != null)
                {
                    // Selected blueprint matches â€” update in place
                    MarketBlueprintImporter.UpdateExisting(viewModel.Data, tempBP);
                    importedBP = viewModel.Data;

                    // Determine which list it belongs to for persistence
                    if (empireContext.GlobalBlueprintList.Any(b => b.UUID == importedBP.UUID))
                        globalChanged = true;
                    else
                        playerChanged = true;

                    Log.Info("Blueprint updated via dedup (selected match): {0} Ev{1} {2}",
                        importedBP.Name, importedBP.Evolution, importedBP.BluePrintType);
                }
                else
                {
                    // No match with selected â€” route via market logic
                    bool hasCurrentPlayer = !string.IsNullOrEmpty(playerContext.CurrentPlayerUUID);
                    bool isGlobal = MarketBlueprintImporter.IsGlobalRoute(tempBP.Evolution, hasCurrentPlayer);

                    var targetList = isGlobal
                        ? empireContext.GlobalBlueprintList
                        : playerContext.BlueprintList;

                    var existing = MarketBlueprintImporter.FindByDedupKey(targetList, tempBP);

                    if (existing != null)
                    {
                        MarketBlueprintImporter.UpdateExisting(existing, tempBP);
                        importedBP = existing;
                        Log.Info("Blueprint updated via dedup (list match): {0} Ev{1} {2}",
                            importedBP.Name, importedBP.Evolution, importedBP.BluePrintType);
                    }
                    else
                    {
                        tempBP.UUID = isGlobal
                            ? DeterministicUUID.Generate(tempBP)
                            : Guid.NewGuid().ToString();
                        if (!isGlobal)
                            tempBP.OwnerUUID = playerContext.CurrentPlayerUUID;
                        targetList.Add(tempBP);
                        importedBP = tempBP;
                        Log.Info("New blueprint created via dedup: {0} Ev{1} {2} â†’ {3}",
                            importedBP.Name, importedBP.Evolution, importedBP.BluePrintType,
                            isGlobal ? "Global" : "Player");
                    }

                    if (isGlobal) globalChanged = true;
                    else playerChanged = true;
                }

                // Persist
                if (globalChanged) empireContext.WriteContext();
                if (playerChanged) playerContext.WriteContext();

                // Notify
                playerContext.OnBlueprintDataChanged(importedBP.UUID);

                // Refresh UI, select imported blueprint
                RefreshBlueprintList();

                foreach (ListViewItem item in lvwBlueprints.Items)
                {
                    if ((item.Tag as Blueprint)?.UUID == importedBP.UUID)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }

                viewModel.SelectBlueprint(importedBP);
                PopulateForm();

                // Auto-select best base blueprint match
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    if (string.IsNullOrEmpty(importedBP.BaseBlueprintUUID) && cmbBaseBlueprint.Items.Count > 1)
                    {
                        cmbBaseBlueprint.SelectedIndex = 1;
                        var bp = cmbBaseBlueprint.SelectedItem as Blueprint;
                        viewModel.BaseBlueprintUUID = bp?.UUID ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing blueprint from clipboard");
                MessageBox.Show("Failed to import blueprint: " + ex.Message,
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvResources_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the Amount column (index 1)
            if (e.ColumnIndex != 1) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!int.TryParse(value, out _))
            {
                e.Cancel = true;
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                dgvResources.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
            }
            else
            {
                dgvResources.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvResources.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void dgvStatistics_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            // Only validate the CurrentValue column (index 2)
            if (e.ColumnIndex != 2) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(value)) return; // Allow empty

            // Get the property name from the Property cell's Tag
            string propertyName = dgvStatistics.Rows[e.RowIndex].Cells[0].Tag as string;
            if (string.IsNullOrEmpty(propertyName)) return;

            string pattern = Constants.BlueprintPropertyValidation.GetValidationPattern(propertyName);
            if (pattern == null)
            {
                // Unknown property ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â log and allow free-form
                Log.Warn("Unknown blueprint property for validation: {0}", propertyName);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
            {
                e.Cancel = true;
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                var propType = Constants.BlueprintPropertyValidation.GetPropertyType(propertyName);
                dgvStatistics.Rows[e.RowIndex].ErrorText = $"{propertyName} must be a valid {propType}";
            }
            else
            {
                dgvStatistics.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvStatistics.Rows[e.RowIndex].ErrorText = "";
            }
        }

        // -----------------------------------------------------------------------
        // Pricing Plan
        // -----------------------------------------------------------------------

        private void PopulatePricingPlanCombo()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = cmbPricingPlan.SelectedValue as string;
            cmbPricingPlan.DataSource = null;

            var plans = playerContext.GetCurrentPlayerPricingPlans();
            var items = new List<object>();
            items.Add(new { Name = "(none)", UUID = "" });
            foreach (var p in plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                items.Add(new { Name = p.Name, UUID = p.UUID });

            cmbPricingPlan.DisplayMember = "Name";
            cmbPricingPlan.ValueMember = "UUID";
            cmbPricingPlan.DataSource = items;

            if (!string.IsNullOrEmpty(selectedUUID) && items.Any(i => ((dynamic)i).UUID == selectedUUID))
                cmbPricingPlan.SelectedValue = selectedUUID;
            else
                cmbPricingPlan.SelectedIndex = 0;
        }

        private void cmbPricingPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            UpdateCalculatedPrice();
        }

        private void OnPricingDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnPricingDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            PopulatePricingPlanCombo();
            UpdateCalculatedPrice();
        }

        private void UpdateCalculatedPrice()
        {
            if (viewModel.Data.UUID == null || viewModel.Data.Resources == null || viewModel.Data.Resources.Count == 0)
            {
                txtCalculatedPrice.Text = "";
                return;
            }

            string planUUID = cmbPricingPlan.SelectedValue as string;
            if (string.IsNullOrEmpty(planUUID))
            {
                txtCalculatedPrice.Text = "";
                return;
            }

            var plan = playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (plan == null)
            {
                txtCalculatedPrice.Text = "";
                return;
            }

            // Parse manufacturing hours from blueprint properties
            decimal mfgHours = 0m;
            string mfgTimeStr;
            if (viewModel.Data.Properties != null)
            {
                viewModel.Data.Properties.getString("Manufacture Run Time", null, out mfgTimeStr);
                if (!string.IsNullOrEmpty(mfgTimeStr))
                {
                    decimal seconds = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
                    mfgHours = seconds / 3600m;
                }
            }

            var result = PriceCalculator.ComputeBlueprintPrice(plan, viewModel.Data, mfgHours);
            string priceText = result.Price.ToString("N2");
            if (!result.IsComplete)
                priceText += " *";
            txtCalculatedPrice.Text = priceText;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BlueprintDataChanged -= OnBlueprintDataChanged;
            playerContext.PricingDataChanged -= OnPricingDataChanged;
            base.OnFormClosed(e);
        }

        private void lvwBlueprints_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }
            lvwBlueprints.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            lvwBlueprints.Sort();
        }

        /// <summary>
        /// Refreshes the evolution graph chart and checkbox panel for the currently selected blueprint.
        /// Resolves the evolution chain, builds graph data, and renders series with solid/dashed segments.
        /// </summary>
        private void RefreshEvolutionGraph()
        {
            if (viewModel == null)
            {
                ClearEvolutionGraph();
                return;
            }

            var blueprint = viewModel.Data;
            if (blueprint == null || string.IsNullOrEmpty(blueprint.UUID))
            {
                ClearEvolutionGraph();
                return;
            }

            // Resolve the evolution chain
            var chain = EvolutionChainService.ResolveChain(
                blueprint,
                uuid => playerContext.FindBlueprint(uuid) ?? EmpireContext.GetInstance()?.FindGlobalBlueprint(uuid));

            // Look up BlueprintType to get Properties array
            var blueprintType = empireContext.BlueprintTypeList?
                .FirstOrDefault(bt => bt.Id == blueprint.BluePrintType);
            string[] blueprintTypeProperties = blueprintType?.Properties ?? new string[0];

            // Build graph data
            var graphData = EvolutionChainService.BuildGraphData(chain, blueprintTypeProperties);

            if (graphData.NoChanges)
            {
                lblNoChanges.Visible = true;
                chartEvolution.Visible = false;
                pnlPropertyCheckboxes.Visible = false;
                return;
            }

            // Show chart and checkbox panel, hide no-changes label
            lblNoChanges.Visible = false;
            chartEvolution.Visible = true;
            pnlPropertyCheckboxes.Visible = true;

            // Clear existing series and checkboxes
            chartEvolution.Series.Clear();
            pnlPropertyCheckboxes.Controls.Clear();

            int colorIndex = 0;
            foreach (var kvp in graphData.Series)
            {
                string propertyName = kvp.Key;
                var points = kvp.Value;
                Color lineColor = WongPalette[colorIndex % WongPalette.Length];

                // Sort points by evolution level
                points.Sort((a, b) => a.Evolution.CompareTo(b.Evolution));

                // Build segments: solid for consecutive evolution levels, dashed for gaps
                int segmentIndex = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    if (i == 0 && points.Count == 1)
                    {
                        // Single point â€” create a series with just one data point
                        var singleSeries = new Series($"{propertyName}_{segmentIndex}")
                        {
                            ChartType = SeriesChartType.Line,
                            Color = lineColor,
                            BorderWidth = 2,
                            MarkerStyle = MarkerStyle.Circle,
                            MarkerSize = 6
                        };
                        singleSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                        chartEvolution.Series.Add(singleSeries);
                        segmentIndex++;
                        continue;
                    }

                    if (i == 0) continue; // Start processing pairs from index 1

                    int evDiff = points[i].Evolution - points[i - 1].Evolution;
                    bool isGap = evDiff > 1;
                    ChartDashStyle dashStyle = isGap ? ChartDashStyle.Dash : ChartDashStyle.Solid;

                    // Check if we can extend the previous segment (same dash style and connects)
                    bool extendPrevious = false;
                    if (segmentIndex > 0)
                    {
                        string prevSeriesName = $"{propertyName}_{segmentIndex - 1}";
                        var prevSeries = chartEvolution.Series.FindByName(prevSeriesName);
                        if (prevSeries != null && prevSeries.BorderDashStyle == dashStyle)
                        {
                            // Extend the previous segment
                            prevSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                            extendPrevious = true;
                        }
                    }

                    if (!extendPrevious)
                    {
                        // Create a new segment series
                        var segmentSeries = new Series($"{propertyName}_{segmentIndex}")
                        {
                            ChartType = SeriesChartType.Line,
                            Color = lineColor,
                            BorderWidth = 2,
                            BorderDashStyle = dashStyle,
                            MarkerStyle = MarkerStyle.Circle,
                            MarkerSize = 6
                        };
                        // Add the connecting point from the previous data point
                        segmentSeries.Points.AddXY(points[i - 1].Evolution, points[i - 1].Percent);
                        segmentSeries.Points.AddXY(points[i].Evolution, points[i].Percent);
                        chartEvolution.Series.Add(segmentSeries);
                        segmentIndex++;
                    }
                }

                // Add checkbox for this property
                var checkbox = new CheckBox
                {
                    Text = propertyName,
                    Checked = true,
                    ForeColor = lineColor,
                    AutoSize = true,
                    Tag = propertyName
                };
                checkbox.CheckedChanged += (s, ev) =>
                {
                    string propTag = (string)((CheckBox)s).Tag;
                    bool visible = ((CheckBox)s).Checked;
                    foreach (var series in chartEvolution.Series)
                    {
                        if (series.Name.StartsWith(propTag + "_"))
                        {
                            series.Enabled = visible;
                        }
                    }
                };
                pnlPropertyCheckboxes.Controls.Add(checkbox);

                colorIndex++;
            }
        }

        /// <summary>
        /// Clears the evolution graph chart series, checkbox panel, and hides the no-changes label.
        /// </summary>
        private void ClearEvolutionGraph()
        {
            chartEvolution.Series.Clear();
            pnlPropertyCheckboxes.Controls.Clear();
            lblNoChanges.Visible = false;
        }

        /// <summary>
        /// Determines the Delete button enabled state and display text based on a
        /// <see cref="ReferenceReport"/>. Pure logic extracted for testability.
        /// </summary>
        /// <param name="report">
        /// The reference report for the selected blueprint, or <c>null</c> when no
        /// blueprint is selected.
        /// </param>
        /// <returns>
        /// A tuple where <c>enabled</c> indicates whether the button should be
        /// clickable and <c>text</c> is the label to display on the button.
        /// </returns>
        internal static (bool enabled, string text) GetDeleteButtonState(ReferenceReport report)
        {
            if (report == null)
                return (false, "Delete");

            if (report.TotalCount > 0)
                return (false, $"In Use ({report.TotalCount})");

            return (true, "Delete");
        }
    }
}