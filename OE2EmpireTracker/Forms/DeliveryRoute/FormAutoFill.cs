using NLog;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.DeliveryRoute
{
    public partial class FormAutoFill : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public bool IncludeCommodities => chkCommodities.Checked;
        public bool IncludeFlatpacks => chkFlatpacks.Checked;
        public bool IncludeResources => chkResources.Checked;
        public bool IncludeWorkers => chkWorkers.Checked;

        public FormAutoFill()
        {
            InitializeComponent();
        }
    }
}
