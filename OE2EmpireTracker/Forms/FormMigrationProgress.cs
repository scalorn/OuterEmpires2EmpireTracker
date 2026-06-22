// <copyright file="FormMigrationProgress.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Windows.Forms;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms
{
    /// <summary>
    /// Simple progress dialog shown during data migration.
    /// </summary>
    public partial class FormMigrationProgress : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormMigrationProgress"/> class.
        /// </summary>
        public FormMigrationProgress()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates the progress display from the UI thread.
        /// </summary>
        /// <param name="progress">The migration progress data.</param>
        public void UpdateProgress(MigrationProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(progress)));
                return;
            }

            lblPhase.Text = "Phase: " + (progress.Phase ?? string.Empty);
            lblEntityType.Text = "Current: " + (progress.CurrentEntityType ?? string.Empty);
            lblCount.Text = "Entities processed: " + progress.EntitiesProcessed;
        }
    }
}
