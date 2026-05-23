using System;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Sharing
{
    /// <summary>
    /// Form for managing sharing/visibility rules.
    /// </summary>
    public partial class FormSharing : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate;
        private PlayerContext playerContext;

        public FormSharing()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            dgvRules.DataError += DgvRules_DataError;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate()
        {
            _isProgrammaticUpdate++;
        }

        public void EndProgrammaticUpdate()
        {
            _isProgrammaticUpdate--;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

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

            Log.Info("Player changed, clearing sharing rules grid");
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvRules.Rows.Clear();
        }

        private void DgvRules_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn(
                "dgvRules DataError at [{0},{1}]: {2}",
                e.RowIndex,
                e.ColumnIndex,
                e.Exception?.Message);
            e.ThrowException = false;
        }
    }
}
