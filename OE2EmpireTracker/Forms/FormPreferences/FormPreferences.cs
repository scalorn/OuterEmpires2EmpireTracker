using System;
using System.Windows.Forms;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms
{
    public partial class FormPreferences : Form
    {
        public FormPreferences()
        {
            InitializeComponent();

            btnOK.Click += btnOK_Click;
            btnResetDefaults.Click += btnResetDefaults_Click;

            LoadPreferences();
        }

        private void LoadPreferences()
        {
            var thresholds = PreferencesStore.GetInstance().Preferences.Thresholds;
            PopulateFields(thresholds);
        }

        private void PopulateFields(ThresholdPreferences thresholds)
        {
            // Structure count fields -- plain integer display
            txtStructureYellow.Text = thresholds.StructureCountYellow.ToString();
            txtStructureRed.Text = thresholds.StructureCountRed.ToString();

            // Time-based fields -- countdown format display
            txtWorkerYellow.Text = ActivityRow.FormatSeconds(thresholds.WorkerRequestYellowSeconds);
            txtWorkerRed.Text = ActivityRow.FormatSeconds(thresholds.WorkerRequestRedSeconds);
            txtColonyImportYellow.Text = ActivityRow.FormatSeconds(thresholds.ColonyImportStalenessYellowSeconds);
            txtColonyImportRed.Text = ActivityRow.FormatSeconds(thresholds.ColonyImportStalenessRedSeconds);
            txtBackgroundInterval.Text = ActivityRow.FormatSeconds(thresholds.BackgroundProcessingIntervalSeconds);
            txtAdminRefresh.Text = ActivityRow.FormatSeconds(thresholds.AdminRefreshIntervalSeconds);
            txtCountdownRefresh.Text = ActivityRow.FormatSeconds(thresholds.CountdownRefreshRateSeconds);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Parse structure count fields as integers
            if (!int.TryParse(txtStructureYellow.Text.Trim(), out int structureYellow) || structureYellow <= 0)
            {
                MessageBox.Show("Structure Count Yellow threshold must be a positive integer.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtStructureRed.Text.Trim(), out int structureRed) || structureRed <= 0)
            {
                MessageBox.Show("Structure Count Red threshold must be a positive integer.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse time-based fields via CountdownFormatParser
            if (!CountdownFormatParser.TryParse(txtWorkerYellow.Text.Trim(), out long workerYellow))
            {
                MessageBox.Show("Worker Request Yellow threshold must be a valid countdown format (e.g. \"2d 0h 0m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtWorkerRed.Text.Trim(), out long workerRed))
            {
                MessageBox.Show("Worker Request Red threshold must be a valid countdown format (e.g. \"1d 0h 0m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtColonyImportYellow.Text.Trim(), out long colonyImportYellow))
            {
                MessageBox.Show("Colony Import Staleness Yellow threshold must be a valid countdown format (e.g. \"5d 0h 0m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtColonyImportRed.Text.Trim(), out long colonyImportRed))
            {
                MessageBox.Show("Colony Import Staleness Red threshold must be a valid countdown format (e.g. \"6d 0h 0m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtBackgroundInterval.Text.Trim(), out long backgroundInterval))
            {
                MessageBox.Show("Background Processing Interval must be a valid countdown format (e.g. \"1m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtAdminRefresh.Text.Trim(), out long adminRefresh))
            {
                MessageBox.Show("Admin Refresh Interval must be a valid countdown format (e.g. \"1m 0s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(txtCountdownRefresh.Text.Trim(), out long countdownRefresh))
            {
                MessageBox.Show("Countdown Refresh Rate must be a valid countdown format (e.g. \"1s\").",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build ThresholdPreferences from parsed values
            var prefs = new ThresholdPreferences
            {
                StructureCountYellow = structureYellow,
                StructureCountRed = structureRed,
                WorkerRequestYellowSeconds = workerYellow,
                WorkerRequestRedSeconds = workerRed,
                ColonyImportStalenessYellowSeconds = colonyImportYellow,
                ColonyImportStalenessRedSeconds = colonyImportRed,
                BackgroundProcessingIntervalSeconds = backgroundInterval,
                AdminRefreshIntervalSeconds = adminRefresh,
                CountdownRefreshRateSeconds = countdownRefresh
            };

            // Validate using ThresholdPreferences.Validate()
            if (!ThresholdPreferences.Validate(prefs, out string error))
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Write to PreferencesStore and save
            PreferencesStore.GetInstance().Preferences.Thresholds = prefs;
            PreferencesStore.GetInstance().Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnResetDefaults_Click(object sender, EventArgs e)
        {
            PopulateFields(new ThresholdPreferences());
        }
    }
}
