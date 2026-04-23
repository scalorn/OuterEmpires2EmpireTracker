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

namespace OE2EmpireTracker.Forms.ColonyActivity
{
    public partial class FormColonyActivity : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private List<ActivityRow> allRows = new List<ActivityRow>();

        public FormColonyActivity()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            // Wire checkbox handlers
            chkBuilding.CheckedChanged += ChkFilter_CheckedChanged;
            chkManufacturing.CheckedChanged += ChkFilter_CheckedChanged;
            chkCommodityManufacturing.CheckedChanged += ChkFilter_CheckedChanged;
            chkCommodityRequest.CheckedChanged += ChkFilter_CheckedChanged;
            chkResearch.CheckedChanged += ChkFilter_CheckedChanged;
            chkMining.CheckedChanged += ChkFilter_CheckedChanged;
            chkRefining.CheckedChanged += ChkFilter_CheckedChanged;
            chkColonyImportStaleness.CheckedChanged += ChkFilter_CheckedChanged;
            chkShowInactive.CheckedChanged += ChkShowInactive_CheckedChanged;

            // Wire text filter
            txtFilter.TextChanged += TxtFilter_TextChanged;

            // Wire grid sort
            dgvActivities.SortCompare += DgvActivities_SortCompare;

            // Wire layout
            flpBase.Layout += FlpBase_Layout;

            // Wire timer
            timerRefresh.Tick += TimerRefresh_Tick;

            // Subscribe to player context events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;

            // Initial data load
            RefreshData();

            // Start timer
            int intervalMs = (int)(PreferencesStore.GetInstance().Preferences.Thresholds.CountdownRefreshRateSeconds * 1000);
            timerRefresh.Interval = Math.Max(intervalMs, 1000);
            timerRefresh.Start();
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int gridWidth = flpBase.ClientSize.Width - dgvActivities.Margin.Left - dgvActivities.Margin.Right;
            int gridHeight = flpBase.ClientSize.Height - flpFilters.Height - flpFilters.Margin.Top - flpFilters.Margin.Bottom - dgvActivities.Margin.Top - dgvActivities.Margin.Bottom;
            dgvActivities.Size = new Size(
                Math.Max(100, gridWidth),
                Math.Max(100, gridHeight));
        }

        // -----------------------------------------------------------------------
        // Data Refresh
        // -----------------------------------------------------------------------

        private void RefreshData()
        {
            var sw = Stopwatch.StartNew();
            var colonies = playerContext.GetCurrentPlayerColonies();

            if (chkShowInactive.Checked)
                allRows = ColonyInactivityCollector.CollectInactivities(colonies, playerContext);
            else
                allRows = ColonyActivityCollector.CollectActivities(colonies, playerContext);

            // Update form title to reflect current mode
            string prefix = Tag != null ? "#" + Tag + " - " : string.Empty;
            Text = prefix + (chkShowInactive.Checked ? "Colony Inactivity" : "Colony Activity");

            long t1 = sw.ElapsedMilliseconds;
            ApplyFiltersAndPopulate();
            sw.Stop();
            Log.Info("RefreshData PERF: total={0}ms collect={1}ms populate={2}ms rows={3}",
                sw.ElapsedMilliseconds, t1, sw.ElapsedMilliseconds - t1, allRows?.Count ?? 0);
            sw.Stop(); Log.Info("PERF RefreshData: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Filtering
        // -----------------------------------------------------------------------

        private HashSet<ActivityType> GetSelectedActivityTypes()
        {
            var types = new HashSet<ActivityType>();
            if (chkBuilding.Checked) types.Add(ActivityType.Building);
            if (chkManufacturing.Checked) types.Add(ActivityType.Manufacturing);
            if (chkCommodityManufacturing.Checked) types.Add(ActivityType.CommodityManufacturing);
            if (!chkShowInactive.Checked && chkCommodityRequest.Checked)
                types.Add(ActivityType.CommodityRequest);
            if (chkResearch.Checked) types.Add(ActivityType.Research);
            if (chkMining.Checked) types.Add(ActivityType.Mining);
            if (chkRefining.Checked) types.Add(ActivityType.Refining);
            if (chkShowInactive.Checked && chkColonyImportStaleness.Checked)
                types.Add(ActivityType.ColonyImportStaleness);
            return types;
        }

        private void ApplyFiltersAndPopulate()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvActivities.Rows.Clear();

            bool inactivityMode = chkShowInactive.Checked;

            // Hide CommodityRequest checkbox and CountDown column in Inactivity Mode
            chkCommodityRequest.Visible = !inactivityMode;
            chkColonyImportStaleness.Visible = inactivityMode;
            colCountDown.Visible = !inactivityMode;

            var selectedTypes = GetSelectedActivityTypes();
            string textFilter = txtFilter.Text ?? string.Empty;

            var filtered = allRows
                .Where(r => selectedTypes.Contains(r.Type))
                .Where(r => PassesTextFilter(r, textFilter))
                .OrderBy(r => r.GetSecondsRemaining())
                .ToList();

            foreach (var row in filtered)
            {
                long seconds = row.GetSecondsRemaining();
                dgvActivities.Rows.Add(
                    row.GetTimeRemainingString(),
                    row.SystemName,
                    row.ColonyName,
                    row.Type.ToString(),
                    row.SourceName,
                    row.ProcessDetails,
                    seconds);
                dgvActivities.Rows[dgvActivities.Rows.Count - 1].Tag = row;
            }
        }

        private static bool PassesTextFilter(ActivityRow row, string textFilter)
        {
            if (string.IsNullOrEmpty(textFilter)) return true;

            return (row.GetTimeRemainingString() ?? string.Empty).IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (row.SystemName ?? string.Empty).IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (row.ColonyName ?? string.Empty).IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || row.Type.ToString().IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (row.SourceName ?? string.Empty).IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
                || (row.ProcessDetails ?? string.Empty).IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // -----------------------------------------------------------------------
        // Event Handlers
        // -----------------------------------------------------------------------

        private void ChkFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ApplyFiltersAndPopulate();
        }

        private void ChkShowInactive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            RefreshData();
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            ApplyFiltersAndPopulate();
        }

        private void TimerRefresh_Tick(object sender, EventArgs e)
        {
            if (chkShowInactive.Checked) return;

            using var guard = new ProgrammaticUpdateGuard(this);
            foreach (DataGridViewRow gridRow in dgvActivities.Rows)
            {
                var activityRow = gridRow.Tag as ActivityRow;
                if (activityRow == null) continue;

                long seconds = activityRow.GetSecondsRemaining();
                gridRow.Cells[colCountDown.Index].Value = activityRow.GetTimeRemainingString();
                gridRow.Cells[colSecondsRemaining.Index].Value = seconds;
            }
        }

        private void DgvActivities_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column == colCountDown)
            {
                long sec1 = Convert.ToInt64(dgvActivities.Rows[e.RowIndex1].Cells[colSecondsRemaining.Index].Value ?? 0);
                long sec2 = Convert.ToInt64(dgvActivities.Rows[e.RowIndex2].Cells[colSecondsRemaining.Index].Value ?? 0);
                e.SortResult = sec1.CompareTo(sec2);
                e.Handled = true;
            }
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

            RefreshData();
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs args)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnColonyDataChanged(sender, args))); }
                catch (ObjectDisposedException) { }
                return;
            }

            try
            {
                Log.Info("FormColonyActivity.OnColonyDataChanged: RefreshData starting for colony {0}", args.ColonyUUID);
                RefreshData();
                Log.Info("FormColonyActivity.OnColonyDataChanged: RefreshData completed, row count = {0}", allRows.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in OnColonyDataChanged RefreshData");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            timerRefresh.Stop();
            timerRefresh.Dispose();
            base.OnFormClosed(e);
        }
    }
}
