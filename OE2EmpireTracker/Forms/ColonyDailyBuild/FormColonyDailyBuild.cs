using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.ColonyDailyBuild
{
    public partial class FormColonyDailyBuild : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private EmpireContext empireContext;
        private PlayerContext playerContext;

        public FormColonyDailyBuild()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.SelectedIndexChanged += cmbRoute_SelectedIndexChanged;

            txtRouteFilter.TextChanged += txtRouteFilter_TextChanged;

            PopulateRouteDropdown();

            flpBase.Layout += flpBase_Layout;
            flpSelectors.Layout += flpSelectors_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            pnlContent.Size = new Size(
                flpBase.Size.Width - flpSelectors.Size.Width - flpSelectors.Margin.Right - flpSelectors.Margin.Left - pnlContent.Margin.Left - pnlContent.Margin.Right,
                flpBase.Size.Height - pnlContent.Margin.Top - pnlContent.Margin.Bottom);
            flpSelectors.Size = new Size(
                flpSelectors.Size.Width,
                flpBase.Size.Height - flpSelectors.Margin.Top - flpSelectors.Margin.Bottom);
        }

        private void flpSelectors_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSelectors.Size.Width - 6;
            txtRouteFilter.Size = new Size(w, txtRouteFilter.Size.Height);
            cmbRoute.Size = new Size(w, cmbRoute.Size.Height);
        }

        // -----------------------------------------------------------------------
        // Route Selection
        // -----------------------------------------------------------------------

        private void txtRouteFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateRouteDropdown();
        }

        private void PopulateRouteDropdown()
        {
            _lastRouteUUID = RouteDropdownHelper.Populate(cmbRoute, playerContext.GetCurrentPlayerRoutes(), txtRouteFilter.Text ?? string.Empty, cmbRoute.SelectedValue as string, cmbRoute_SelectedIndexChanged);
        }

        private string _lastRouteUUID = string.Empty;

        private void cmbRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            string routeUUID = cmbRoute.SelectedValue as string ?? string.Empty;
            if (routeUUID == _lastRouteUUID) return;
            _lastRouteUUID = routeUUID;

            if (string.IsNullOrEmpty(routeUUID))
            {
                ClearContent();
                return;
            }

            BuildContent(routeUUID);
        }

        // -----------------------------------------------------------------------
        // Content Display
        // -----------------------------------------------------------------------

        private void ClearContent()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            pnlContent.Controls.Clear();
        }

        private void BuildContent(string routeUUID)
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var scrollPos = pnlContent.AutoScrollPosition;
            this.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();

            var route = playerContext.DeliveryRouteList.FirstOrDefault(r => r.UUID == routeUUID);
            if (route == null)
            {
                pnlContent.ResumeLayout();
                this.ResumeLayout();
                return;
            }

            foreach (var stop in route.Stops.OrderBy(s => s.Sequence))
            {
                var colony = playerContext.FindColony(stop.ColonyUUID);
                if (colony == null) continue;

                if (!ColonyBuildEligibility.IsEligible(colony, playerContext)) continue;

                var staged = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);
                if (staged == null) continue;

                var blueprint = playerContext.FindBlueprint(staged.FlatpackBlueprintUUID);
                string bpName = blueprint != null ? blueprint.ExtendedName : staged.FlatpackBlueprintUUID ?? "(unknown)";

                // Colony panel
                var pnlColony = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Margin = new Padding(3, 6, 3, 6),
                    Tag = colony.UUID
                };

                var lblColony = new Label
                {
                    Text = $"{colony.PlanetName} - {colony.ColonyName}",
                    Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                    AutoSize = true,
                    Margin = new Padding(3, 3, 3, 1)
                };

                pnlColony.Controls.Add(lblColony);

                var flpRow = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Margin = new Padding(3, 1, 3, 3)
                };

                var lblBlueprint = new Label
                {
                    Text = bpName,
                    AutoSize = true,
                    Margin = new Padding(10, 5, 3, 3)
                };

                flpRow.Controls.Add(lblBlueprint);

                var btnBuild = new Button
                {
                    Text = "Build",
                    AutoSize = true,
                    Margin = new Padding(6, 3, 3, 3),
                    Tag = new BuildTag { ColonyUUID = colony.UUID, StructureUUID = staged.UUID }
                };

                btnBuild.Click += btnBuild_Click;
                flpRow.Controls.Add(btnBuild);

                pnlColony.Controls.Add(flpRow);
                pnlContent.Controls.Add(pnlColony);
            }

            pnlContent.ResumeLayout();
            this.ResumeLayout();
            pnlContent.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
            sw.Stop();
            Log.Info("BuildContent PERF: total={0}ms", sw.ElapsedMilliseconds);
        }

        private class BuildTag
        {
            public string ColonyUUID { get; set; }
            public string StructureUUID { get; set; }
        }

        // -----------------------------------------------------------------------
        // Build Click
        // -----------------------------------------------------------------------

        private void btnBuild_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var tag = btn.Tag as BuildTag;
            if (tag == null) return;

            var colony = playerContext.FindColony(tag.ColonyUUID);
            if (colony == null) return;

            var structure = colony.Structures.FirstOrDefault(s => s.UUID == tag.StructureUUID);
            if (structure == null) return;

            // Look up Builder skill level
            int builderLevel = 0;
            if (!string.IsNullOrEmpty(colony.OwnerUUID))
            {
                var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == colony.OwnerUUID);
                if (owner != null)
                    builderLevel = owner.GetSkill(SkillName.Builder).Level;
            }

            long buildSeconds = BuildTimeCalculator.Calculate(builderLevel);

            // Transition from staged to building
            var vm = new ColonyStructureViewModel(structure, playerContext);
            vm.IsStaged = false;

            structure.BuildCompletionTime = new CountDownTime();
            structure.BuildCompletionTime.TimeRemaining = buildSeconds;

            playerContext.WriteContext();
            playerContext.OnColonyDataChanged(colony.UUID);

            Log.Info("Build started on {0} - {1}, structure {2}, {3}s",
                colony.PlanetName, colony.ColonyName, structure.UUID, buildSeconds);

            // Remove the colony panel from the display
            var panel = pnlContent.Controls.OfType<FlowLayoutPanel>()
                .FirstOrDefault(p => p.Tag as string == colony.UUID);
            if (panel != null)
            {
                pnlContent.Controls.Remove(panel);
                panel.Dispose();
            }
        }

        // -----------------------------------------------------------------------
        // Event Handlers
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            PopulateRouteDropdown();
            ClearContent();
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnColonyDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                BuildContent(routeUUID);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }
    }
}
