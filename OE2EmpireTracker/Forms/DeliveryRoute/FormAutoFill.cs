using NLog;
using OE2EmpireTracker.Services;
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

        public int TimeHorizonHours
        {
            get
            {
                int.TryParse(txtTimeHorizon.Text.Trim(), out int val);
                return val;
            }
        }

        public FormAutoFill()
        {
            InitializeComponent();
            // Load saved time horizon from preferences
            var prefs = PreferencesStore.GetInstance().Preferences;
            txtTimeHorizon.Text = prefs.FlatpackTimeHorizonHours.ToString();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Save time horizon to preferences
            if (DialogResult == DialogResult.OK)
            {
                var store = PreferencesStore.GetInstance();
                store.Preferences.FlatpackTimeHorizonHours = TimeHorizonHours;
                store.Save();
            }
            base.OnFormClosed(e);
        }
    }
}
