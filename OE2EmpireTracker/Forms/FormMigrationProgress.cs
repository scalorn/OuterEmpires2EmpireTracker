// <copyright file="FormMigrationProgress.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms
{
    /// <summary>
    /// Simple progress dialog shown during data migration.
    /// </summary>
    public partial class FormMigrationProgress : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate;

        private PlayerContext playerContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormMigrationProgress"/> class.
        /// </summary>
        public FormMigrationProgress()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        /// <inheritdoc/>
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        /// <inheritdoc/>
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

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

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            // No-op: migration progress is not player-specific.
        }
    }
}
