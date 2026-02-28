using OE2EmpireTracker.Baseline;
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
    }
}
