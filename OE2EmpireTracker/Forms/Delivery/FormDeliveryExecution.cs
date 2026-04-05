using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.Delivery
{
    public partial class FormDeliveryExecution : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private EmpireContext empireContext;
        private PlayerContext playerContext;

        public FormDeliveryExecution()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            flpBase.Layout += flpBase_Layout;
            flpSelectors.Layout += flpSelectors_Layout;
        }

        /// <summary>
        /// Constructor for launching from route builder with pre-selected route and plan.
        /// </summary>
        public FormDeliveryExecution(string routeUUID, string planUUID) : this()
        {
            // Will be wired up when we add functionality
        }

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            pnlExecution.Size = new Size(
                flpBase.Size.Width - flpSelectors.Size.Width - flpSelectors.Margin.Right - flpSelectors.Margin.Left - pnlExecution.Margin.Left - pnlExecution.Margin.Right,
                flpBase.Size.Height - pnlExecution.Margin.Top - pnlExecution.Margin.Bottom);
            flpSelectors.Size = new Size(
                flpSelectors.Size.Width,
                flpBase.Size.Height - flpSelectors.Margin.Top - flpSelectors.Margin.Bottom);
        }

        private void flpSelectors_Layout(object sender, LayoutEventArgs e)
        {
            cmbRoute.Size = new Size(
                flpSelectors.Size.Width - cmbRoute.Margin.Left - cmbRoute.Margin.Right,
                cmbRoute.Size.Height);
            cmbPlan.Size = new Size(
                flpSelectors.Size.Width - cmbPlan.Margin.Left - cmbPlan.Margin.Right,
                cmbPlan.Size.Height);
        }
    }
}
