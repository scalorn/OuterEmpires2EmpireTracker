using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Forms;
using OE2EmpireTracker.Forms.Colony;
using OE2EmpireTracker.Forms.PlayerProfile;
using OE2EmpireTracker.Forms.Survey;
using OE2EmpireTracker.Forms.ColonyActivity;
using OE2EmpireTracker.Forms.ColonyDailyBuild;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Controls;
using System.Diagnostics;

namespace OE2EmpireTracker
{
    public partial class MainWindow : Form, IProgrammaticUpdateSource
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        EmpireContext context = null;
        PlayerContext playerContext = null;
        private BackgroundProcessor _backgroundProcessor;
        private string _lastOpenedPath;
        private readonly Dictionary<string, int> _windowNumberCounters = new Dictionary<string, int>();

        // CPU utilization tracking
        private TimeSpan _lastCpuTime;
        private DateTime _lastCheckTime;

        public MainWindow()
        {
            context = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            InitializeComponent();
            WindowStateHelper.RestoreMainWindowState(this);
            PopulatePlayerDropdown();
            playerContext.PlayerProfilesChanged += OnPlayerProfilesChanged;

            _backgroundProcessor = new BackgroundProcessor(playerContext);
            _backgroundProcessor.Start();
            timerNextProcess.Tick += OnTimerNextProcessTick;
            timerNextProcess.Start();

            var proc = Process.GetCurrentProcess();
            _lastCpuTime = proc.TotalProcessorTime;
            _lastCheckTime = DateTime.UtcNow;

            TryAutoOpenLastFile();
        }

        private void PopulatePlayerDropdown()
        {
            cmbCurrentPlayer.Items.Clear();
            foreach (var profile in playerContext.playerProfileList)
            {
                cmbCurrentPlayer.Items.Add(profile.Name);
            }

            // Select the current player
            var current = playerContext.CurrentPlayer;
            if (current != null)
            {
                int idx = playerContext.playerProfileList.IndexOf(current);
                if (idx >= 0) cmbCurrentPlayer.SelectedIndex = idx;
            }
            else if (cmbCurrentPlayer.Items.Count > 0)
            {
                cmbCurrentPlayer.SelectedIndex = 0;
            }
        }

        private void cmbCurrentPlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCurrentPlayer.SelectedIndex;
            if (idx >= 0 && idx < playerContext.playerProfileList.Count)
            {
                var selected = playerContext.playerProfileList[idx];
                if (selected.UUID != playerContext.CurrentPlayerUUID)
                {
                    playerContext.CurrentPlayerUUID = selected.UUID;
                }
            }
        }

        private T OpenMdiChild<T>() where T : Form, new()
        {
            string formTypeKey = typeof(T).Name;
            if (!_windowNumberCounters.ContainsKey(formTypeKey))
                _windowNumberCounters[formTypeKey] = 0;
            _windowNumberCounters[formTypeKey]++;
            int windowNumber = _windowNumberCounters[formTypeKey];

            T form = new T();
            form.MdiParent = this;
            form.Tag = windowNumber;
            form.Text = "#" + windowNumber + " - " + form.Text;
            WindowStateHelper.RestoreState(form, formTypeKey, windowNumber);
            form.Show();
            return form;
        }

        private void addBlueprintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormBlueprint>();
        }

        private void addColonyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormColony>();
        }

        private void addSurveyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormSurvey>();
        }

        private void managePlayerProfiles_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormPlayerProfile>();
        }

        private void deliveryRoutesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<Forms.DeliveryRoute.FormDeliveryRoute>();
        }

        private void deliveryExecutionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<Forms.DeliveryExecution.FormDeliveryExecution>();
        }

        private void colonyDailyBuildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormColonyDailyBuild>();
        }

        private void colonyActivityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenMdiChild<FormColonyActivity>();
        }

        private void OnPlayerProfilesChanged(object sender, EventArgs e)
        {
            PopulatePlayerDropdown();
        }

        private void OnTimerNextProcessTick(object sender, EventArgs e)
        {
            if (_backgroundProcessor == null)
            {
                toolStripNextProcess.Text = "Next Process: --";
                return;
            }

            DateTime next = _backgroundProcessor.NextProcessTime;
            if (next == default(DateTime))
            {
                toolStripNextProcess.Text = "Next Process: --";
                return;
            }

            TimeSpan remaining = next - DateTime.Now;
            if (remaining.TotalSeconds < 0)
                remaining = TimeSpan.Zero;

            if (remaining.Days > 0)
                toolStripNextProcess.Text = string.Format("Next Process: {0}d {1}h {2}m {3}s",
                    remaining.Days, remaining.Hours, remaining.Minutes, remaining.Seconds);
            else if (remaining.Hours > 0)
                toolStripNextProcess.Text = string.Format("Next Process: {0}h {1}m {2}s",
                    remaining.Hours, remaining.Minutes, remaining.Seconds);
            else if (remaining.Minutes > 0)
                toolStripNextProcess.Text = string.Format("Next Process: {0}m {1}s",
                    remaining.Minutes, remaining.Seconds);
            else
                toolStripNextProcess.Text = string.Format("Next Process: {0}s",
                    remaining.Seconds);

            if (_backgroundProcessor.LastCycleHadError)
                toolStripNextProcess.ForeColor = Color.Red;
            else
                toolStripNextProcess.ForeColor = SystemColors.ControlText;

            // Update performance label
            var proc = Process.GetCurrentProcess();
            double memMB = proc.WorkingSet64 / (1024.0 * 1024.0);

            var now = DateTime.UtcNow;
            double cpuUsedMs = (proc.TotalProcessorTime - _lastCpuTime).TotalMilliseconds;
            double elapsedMs = (now - _lastCheckTime).TotalMilliseconds;
            double cpuPercent = elapsedMs > 0
                ? (cpuUsedMs / (Environment.ProcessorCount * elapsedMs)) * 100.0
                : 0;
            _lastCpuTime = proc.TotalProcessorTime;
            _lastCheckTime = now;

            toolStripPerformance.Text = string.Format("Mem: {0:F0} MB | CPU: {1:F1}%", memMB, cpuPercent);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            WindowStateHelper.SaveMainWindowState(this);
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timerNextProcess.Stop();
            timerNextProcess.Tick -= OnTimerNextProcessTick;

            if (_backgroundProcessor != null)
            {
                _backgroundProcessor.Stop();
                _backgroundProcessor.Dispose();
                _backgroundProcessor = null;
            }

            playerContext.PlayerProfilesChanged -= OnPlayerProfilesChanged;
            base.OnFormClosed(e);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CloseAllMdiChildren();

                if (_backgroundProcessor != null)
                {
                    _backgroundProcessor.Stop();
                    _backgroundProcessor.Dispose();
                    _backgroundProcessor = null;
                }

                EmpireContext.Reset();
                PlayerContext.FilePath = @"..\..\PlayerData.json";
                context = EmpireContext.getInstance();
                playerContext = EmpireContext.PlayerContext;

                _backgroundProcessor = new BackgroundProcessor(playerContext);
                _backgroundProcessor.Start();

                PopulatePlayerDropdown();
                SetLastOpenedPath(string.Empty);

                Log.Info("File → New completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during File → New");
                MessageBox.Show("An error occurred while creating a new file: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dlg.DefaultExt = "json";

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ReloadContextFromFile(dlg.FileName);
                    SetLastOpenedPath(dlg.FileName);
                    Log.Info("File → Open completed: {0}", dlg.FileName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error opening file {0}", dlg.FileName);
                    MessageBox.Show("Failed to open file: " + ex.Message,
                        "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastOpenedPath))
            {
                try
                {
                    PlayerContext.FilePath = _lastOpenedPath;
                    playerContext.writeContext();
                    Log.Info("File → Save completed: {0}", _lastOpenedPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving file {0}", _lastOpenedPath);
                    MessageBox.Show("Failed to save file: " + ex.Message,
                        "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                PerformSaveAs();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformSaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?",
                "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                this.Close();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FormAbout().ShowDialog(this);
        }

        // -----------------------------------------------------------------------
        // Helper Methods
        // -----------------------------------------------------------------------

        private void CloseAllMdiChildren()
        {
            foreach (Form child in MdiChildren)
            {
                child.Close();
            }
        }

        private void cascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void tileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void tileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void UpdateTitleBar()
        {
            if (!string.IsNullOrEmpty(_lastOpenedPath))
            {
                this.Text = "OE2 Empire Tracker - " + Path.GetFileName(_lastOpenedPath);
            }
            else
            {
                this.Text = "OE2 Empire Tracker";
            }
        }

        private void SetLastOpenedPath(string path)
        {
            _lastOpenedPath = path;
            Properties.Settings.Default.LastOpenedPath = path ?? string.Empty;
            Properties.Settings.Default.Save();
            UpdateTitleBar();
        }

        private void ReloadContextFromFile(string filePath)
        {
            if (_backgroundProcessor != null)
            {
                _backgroundProcessor.Stop();
                _backgroundProcessor.Dispose();
                _backgroundProcessor = null;
            }

            CloseAllMdiChildren();

            EmpireContext.Reset();
            PlayerContext.FilePath = filePath;
            context = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            _backgroundProcessor = new BackgroundProcessor(playerContext);
            _backgroundProcessor.Start();

            PopulatePlayerDropdown();
        }

        private bool PerformSaveAs()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dlg.DefaultExt = "json";

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                try
                {
                    PlayerContext.FilePath = dlg.FileName;
                    playerContext.writeContext();
                    SetLastOpenedPath(dlg.FileName);
                    Log.Info("File → Save As completed: {0}", dlg.FileName);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving file {0}", dlg.FileName);
                    MessageBox.Show("Failed to save file: " + ex.Message,
                        "Save As Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private void TryAutoOpenLastFile()
        {
            try
            {
                string lastPath = Properties.Settings.Default.LastOpenedPath;
                if (string.IsNullOrEmpty(lastPath))
                    return;

                if (File.Exists(lastPath))
                {
                    ReloadContextFromFile(lastPath);
                    _lastOpenedPath = lastPath;
                    UpdateTitleBar();
                    Log.Info("Auto-opened last file: {0}", lastPath);
                }
                else
                {
                    Log.Warn("Last opened file not found: {0}, clearing setting", lastPath);
                    Properties.Settings.Default.LastOpenedPath = string.Empty;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to auto-open last file, clearing setting");
                Properties.Settings.Default.LastOpenedPath = string.Empty;
                Properties.Settings.Default.Save();
            }
        }
    }
}
