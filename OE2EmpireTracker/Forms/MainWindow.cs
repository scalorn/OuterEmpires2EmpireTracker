using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
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
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker
{
    public partial class MainWindow : Form, IProgrammaticUpdateSource
    {
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        EmpireContext context = null;
        PlayerContext playerContext = null;

        public MainWindow()
        {
            context = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            InitializeComponent();
            PopulatePlayerDropdown();
            playerContext.PlayerProfilesChanged += OnPlayerProfilesChanged;
        }

        private void PopulatePlayerDropdown()
        {
            cmbCurrentPlayer.Items.Clear();
            foreach (var profile in playerContext.playerProfileList)
            {
                cmbCurrentPlayer.Items.Add(profile.Name);
            }

            // Select the current player
            var current = playerContext.CurrentPlayer;
            if (current != null)
            {
                int idx = playerContext.playerProfileList.IndexOf(current);
                if (idx >= 0) cmbCurrentPlayer.SelectedIndex = idx;
            }
            else if (cmbCurrentPlayer.Items.Count > 0)
            {
                cmbCurrentPlayer.SelectedIndex = 0;
            }
        }

        private void cmbCurrentPlayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCurrentPlayer.SelectedIndex;
            if (idx >= 0 && idx < playerContext.playerProfileList.Count)
            {
                var selected = playerContext.playerProfileList[idx];
                if (selected.UUID != playerContext.CurrentPlayerUUID)
                {
                    playerContext.CurrentPlayerUUID = selected.UUID;
                }
            }
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

        private void deliveryRoutesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form routes = new Forms.DeliveryRoute.FormDeliveryRoute();
            routes.MdiParent = this;
            routes.Show();
        }

        private void deliveryExecutionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form execution = new Forms.DeliveryExecution.FormDeliveryExecution();
            execution.MdiParent = this;
            execution.Show();
        }

        private void OnPlayerProfilesChanged(object sender, EventArgs e)
        {
            PopulatePlayerDropdown();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.PlayerProfilesChanged -= OnPlayerProfilesChanged;
            base.OnFormClosed(e);
        }
    }
}
