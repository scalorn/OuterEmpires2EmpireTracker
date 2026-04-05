using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.DeliveryRoute
{
    public partial class FormAutoFill : Form
    {
        public bool IncludeCommodities => chkCommodities.Checked;

        public FormAutoFill()
        {
            InitializeComponent();
        }
    }
}
