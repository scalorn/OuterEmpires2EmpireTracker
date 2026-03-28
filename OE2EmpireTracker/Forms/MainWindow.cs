using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Forms.Colony;
using OE2EmpireTracker.Forms.PlayerProfile;
using OE2EmpireTracker.Forms.Survey;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker
{
    public partial class MainWindow : Form
    {
        EmpireContext context = null;

        public MainWindow()
        {
            context = EmpireContext.getInstance();
            InitializeComponent();

            SurveyParser parser = new SurveyParser();
            parser.parseIt();
        }

        private void addBlueprintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form blueprint = new FormBlueprint();
            blueprint.MdiParent = this;
            blueprint.Show();
        }

        private void addColonyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form colony = new FormColony();
            colony.MdiParent = this;
            colony.Show();
        }

        private void addSurveyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form survey = new FormSurvey();
            survey.MdiParent = this;
            survey.Show();
        }

        private void managePlayerProfiles_Click(object sender, EventArgs e)
        {
            Form playerProfile = new FormPlayerProfile();
            playerProfile.MdiParent = this;
            playerProfile.Show();
        }
    }
}
