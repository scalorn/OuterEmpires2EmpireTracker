using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class FormPlayerProfile : Form
    {
        /// <summary>
        /// Gets or sets the empire context instance for accessing empire-wide data.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// Gets or sets the player context instance for accessing player-specific data.
        /// </summary>
        private PlayerContext playerContext;

        private Data.PlayerProfile selectedProfile;

        private Dictionary<string, CheckBox> SkillGroups = new Dictionary<string, CheckBox>();

        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();


        public FormPlayerProfile()
        {
            InitializeComponent();

            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            selectedProfile = new Data.PlayerProfile();

            SkillGroups["Colony Director"] = chkColonyDirector;
            SkillGroups["Colony Founder"] = chkColonyFounder;
            SkillGroups["Colony Operations"] = chkColonyOperations;
            SkillGroups["Commander"] = chkCommander;
            SkillGroups["Engineer"] = chkEngineer;
            SkillGroups["Entrepeneur"] = chkEntrepeneur;
            SkillGroups["Job Management"] = chkJobManagement;
            SkillGroups["Researcher"] = chkResearcher;
            SkillGroups["Surveyor"] = chkSurveyor;
            SkillGroups["Trader"] = chkTrader;

            configureSkillBlockOnce(chkColonyDirector, pskHumanResources, "Human Resources");
            configureSkillBlockOnce(chkColonyDirector, pskForeman, "Foreman");
            configureSkillBlockOnce(chkColonyFounder, pskFounder, "Founder");
            configureSkillBlockOnce(chkColonyFounder, pskEnergyEfficiency, "Energy Efficiency");
            configureSkillBlockOnce(chkColonyFounder, pskBuilder, "Builder");
            configureSkillBlockOnce(chkColonyOperations, pskRefiningFocus, "Refining Focus");
            configureSkillBlockOnce(chkColonyOperations, pskProductionFocus, "Production Focus");
            configureSkillBlockOnce(chkColonyOperations, pskExtractionFocus, "Extraction Focus");
            configureSkillBlockOnce(chkCommander, pskDamageControl, "Damage Control");
            configureSkillBlockOnce(chkEngineer, pskEngineeringCapacity, "Engineering Capacity");
            configureSkillBlockOnce(chkEntrepeneur, pskSoundAsAPound, "Sounds As A Pound");
            configureSkillBlockOnce(chkEntrepeneur, pskSelfMadeMillionaire, "Self Made Millionaire");
            configureSkillBlockOnce(chkEntrepeneur, pskAAAHealthcare, "AAA Healthcare");
            configureSkillBlockOnce(chkJobManagement, pskJobOpportunities, "Job Opportunities");
            configureSkillBlockOnce(chkJobManagement, pskContractManagement, "Contract Management");
            configureSkillBlockOnce(chkResearcher, pskResearchReview, "Research Review");
            configureSkillBlockOnce(chkResearcher, pskResearchMethods, "Research Methods");
            configureSkillBlockOnce(chkResearcher, pskResearchFocus, "Research Focus");
            configureSkillBlockOnce(chkSurveyor, pskSurveyingMethods, "Surveying Methods");
            configureSkillBlockOnce(chkSurveyor, pskScanningMethods, "Scanning Methods");
            configureSkillBlockOnce(chkSurveyor, pskQuartermaster, "Quartermaster");
            configureSkillBlockOnce(chkTrader, pskBroker, "Broker");

            txtNameFilter.TextChanged += txtNameFilter_TextChanged;
            lvwPlayerProfiles.ItemSelectionChanged += lvwPlayerProfiles_ItemSelectionChanged;

            lvwPlayerProfiles.View = View.Details;
            lvwPlayerProfiles.Columns.Clear();
            lvwPlayerProfiles.Columns.Add("Name", 200);
            lvwPlayerProfiles.Columns.Add("Faction", 200);

            populateListView();
            PopulateForm();
        }

        public void PopulateForm()
        {
            txtPlayerName.Text = selectedProfile.Name;
            cmbFaction.Text = selectedProfile.Faction;
            txtTotalCredits.Text = selectedProfile.TotalCredits.ToString();
            txtSkillPoints.Text = selectedProfile.SkillPoints.ToString();

            txtPublicRank.Text = selectedProfile.Public.Rank.ToString();
            txtPublicRankCurXP.Text = selectedProfile.Public.CurrentXP.ToString();
            txtPublicRankNextXP.Text = selectedProfile.Public.NextXP.ToString();

            txtPrivateRank.Text = selectedProfile.Private.Rank.ToString();
            txtPrivateRankCurXP.Text = selectedProfile.Private.CurrentXP.ToString();
            txtPrivateRankNextXP.Text = selectedProfile.Private.NextXP.ToString();

            txtMilitaryRank.Text = selectedProfile.Military.Rank.ToString();
            txtMilitaryRankCurXP.Text = selectedProfile.Military.CurrentXP.ToString();
            txtMilitaryRankNextXP.Text = selectedProfile.Military.NextXP.ToString();

            // Restore skill group checkbox states from the profile
            foreach (var entry in SkillGroups)
            {
                entry.Value.Checked = selectedProfile.GetSkillGroup(entry.Key);
            }

            updateSkillBlock(pskHumanResources, "Human Resources");
            updateSkillBlock(pskForeman, "Foreman");
            updateSkillBlock(pskFounder, "Founder");
            updateSkillBlock(pskEnergyEfficiency, "Energy Efficiency");
            updateSkillBlock(pskBuilder, "Builder");
            updateSkillBlock(pskRefiningFocus, "Refining Focus");
            updateSkillBlock(pskProductionFocus, "Production Focus");
            updateSkillBlock(pskExtractionFocus, "Extraction Focus");
            updateSkillBlock(pskDamageControl, "Damage Control");
            updateSkillBlock(pskEngineeringCapacity, "Engineering Capacity");
            updateSkillBlock(pskSoundAsAPound, "Sounds As A Pound");
            updateSkillBlock(pskSelfMadeMillionaire, "Self Made Millionaire");
            updateSkillBlock(pskAAAHealthcare, "AAA Healthcare");
            updateSkillBlock(pskJobOpportunities, "Job Opportunities");
            updateSkillBlock(pskContractManagement, "Contract Management");
            updateSkillBlock(pskResearchReview, "Research Review");
            updateSkillBlock(pskResearchMethods, "Research Methods");
            updateSkillBlock(pskResearchFocus, "Research Focus");
            updateSkillBlock(pskSurveyingMethods, "Surveying Methods");
            updateSkillBlock(pskScanningMethods, "Scanning Methods");
            updateSkillBlock(pskQuartermaster, "Quartermaster");
            updateSkillBlock(pskBroker, "Broker");

            bool isTraining = false;
            foreach (KeyValuePair<string, PlayerSkill> skillEntry in selectedProfile.Skills)
            {
                if (skillEntry.Value.TrainingStarted)
                {
                    isTraining = true;
                    break;
                }
            }
            foreach (KeyValuePair<string, PlayerSkillBlock> skillBlockEntry in skillBlocks)
            {
                skillBlockEntry.Value.CanStartTraining = !isTraining;
            }
        }

        private void configureSkillBlockOnce(CheckBox skillGroup, PlayerSkillBlock skillBlock, string skillName)
        {
            skillBlock.SkillGroupCheckbox = skillGroup;
            skillBlock.SkillName = skillName;
            skillBlocks[skillName] = skillBlock;
            skillBlock.TrainingStatusChanged += TrainingStatusChanged;
        }

        private void updateSkillBlock(PlayerSkillBlock skillBlock, string skillName)
        {
            skillBlock.PlayerSkill = selectedProfile.GetSkill(skillName);
            skillBlock.PopulateForm();
        }


        private void TrainingStatusChanged (object sender, EventArgs e)
        {
            PopulateForm();
        }

        private void chkColonyDirector_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Colony Director", chkColonyDirector.Checked);
            PopulateForm();
        }

        private void chkColonyFounder_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Colony Founder", chkColonyFounder.Checked);
            PopulateForm();
        }

        private void chkColonyOperations_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Colony Operations", chkColonyOperations.Checked);
            PopulateForm();
        }

        private void chkCommander_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Commander", chkCommander.Checked);
            PopulateForm();
        }

        private void chkEngineer_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Engineer", chkEngineer.Checked);
            PopulateForm();
        }

        private void chkEntrepeneur_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Entrepeneur", chkEntrepeneur.Checked);
            PopulateForm();
        }

        private void chkJobManagement_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Job Management", chkJobManagement.Checked);
            PopulateForm();
        }

        private void chkResearcher_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Researcher", chkResearcher.Checked);
            PopulateForm();
        }

        private void chkSurveyor_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Surveyor", chkSurveyor.Checked);
            PopulateForm();
        }

        private void chkTrader_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup("Trader", chkTrader.Checked);
            PopulateForm();
        }

        private void populateListView()
        {
            string filter = txtNameFilter.Text;
            var profiles = playerContext.playerProfileList
                .Where(p => string.IsNullOrEmpty(filter) ||
                            p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            lvwPlayerProfiles.Items.Clear();
            foreach (var profile in profiles)
            {
                var item = new ListViewItem(profile.Name);
                item.SubItems.Add(profile.Faction);
                item.Tag = profile;
                lvwPlayerProfiles.Items.Add(item);
            }
        }

        private void txtNameFilter_TextChanged(object sender, EventArgs e)
        {
            populateListView();
        }

        private void lvwPlayerProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvwPlayerProfiles.SelectedItems.Count == 1)
            {
                selectedProfile = lvwPlayerProfiles.SelectedItems[0].Tag as Data.PlayerProfile;
                PopulateForm();
            }
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            // Populate fields first so the profile is complete before adding to the list
            selectedProfile.Name = txtPlayerName.Text;
            selectedProfile.Faction = cmbFaction.Text;
            selectedProfile.TotalCredits = decimal.TryParse(txtTotalCredits.Text, out var credits) ? credits : 0;
            selectedProfile.SkillPoints = int.TryParse(txtSkillPoints.Text, out var sp) ? sp : 0;

            selectedProfile.Public.Rank = int.TryParse(txtPublicRank.Text, out var pubRank) ? pubRank : 0;
            selectedProfile.Public.CurrentXP = long.TryParse(txtPublicRankCurXP.Text, out var pubCur) ? pubCur : 0;
            selectedProfile.Public.NextXP = long.TryParse(txtPublicRankNextXP.Text, out var pubNext) ? pubNext : 0;

            selectedProfile.Private.Rank = int.TryParse(txtPrivateRank.Text, out var priRank) ? priRank : 0;
            selectedProfile.Private.CurrentXP = long.TryParse(txtPrivateRankCurXP.Text, out var priCur) ? priCur : 0;
            selectedProfile.Private.NextXP = long.TryParse(txtPrivateRankNextXP.Text, out var priNext) ? priNext : 0;

            selectedProfile.Military.Rank = int.TryParse(txtMilitaryRank.Text, out var milRank) ? milRank : 0;
            selectedProfile.Military.CurrentXP = long.TryParse(txtMilitaryRankCurXP.Text, out var milCur) ? milCur : 0;
            selectedProfile.Military.NextXP = long.TryParse(txtMilitaryRankNextXP.Text, out var milNext) ? milNext : 0;

            // Add to list only if this is a new profile
            if (string.IsNullOrEmpty(selectedProfile.UUID))
            {
                selectedProfile.UUID = Guid.NewGuid().ToString();
                playerContext.playerProfileList.Add(selectedProfile);
            }

            playerContext.writeContext();
            populateListView();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedProfile.UUID)) return;

            playerContext.playerProfileList.Remove(selectedProfile);
            playerContext.writeContext();

            selectedProfile = new Data.PlayerProfile();
            populateListView();
            PopulateForm();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            // Discard changes by re-selecting the saved profile, or reset to blank if new
            if (string.IsNullOrEmpty(selectedProfile.UUID))
            {
                selectedProfile = new Data.PlayerProfile();
            }
            PopulateForm();
        }
    }
}
