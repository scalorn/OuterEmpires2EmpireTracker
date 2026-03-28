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

        private Dictionary<SkillGroupName, CheckBox> SkillGroups = new Dictionary<SkillGroupName, CheckBox>();

        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();


        public FormPlayerProfile()
        {
            InitializeComponent();

            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            selectedProfile = new Data.PlayerProfile();

            SkillGroups[SkillGroupName.ColonyDirector]   = chkColonyDirector;
            SkillGroups[SkillGroupName.ColonyFounder]    = chkColonyFounder;
            SkillGroups[SkillGroupName.ColonyOperations] = chkColonyOperations;
            SkillGroups[SkillGroupName.Commander]        = chkCommander;
            SkillGroups[SkillGroupName.Engineer]         = chkEngineer;
            SkillGroups[SkillGroupName.Entrepeneur]      = chkEntrepeneur;
            SkillGroups[SkillGroupName.JobManagement]    = chkJobManagement;
            SkillGroups[SkillGroupName.Researcher]       = chkResearcher;
            SkillGroups[SkillGroupName.Surveyor]         = chkSurveyor;
            SkillGroups[SkillGroupName.Trader]           = chkTrader;

            configureSkillBlockOnce(chkColonyDirector, pskHumanResources, SkillName.HumanResources);
            configureSkillBlockOnce(chkColonyDirector, pskForeman, SkillName.Foreman);
            configureSkillBlockOnce(chkColonyFounder, pskFounder, SkillName.Founder);
            configureSkillBlockOnce(chkColonyFounder, pskEnergyEfficiency, SkillName.EnergyEfficiency);
            configureSkillBlockOnce(chkColonyFounder, pskBuilder, SkillName.Builder);
            configureSkillBlockOnce(chkColonyOperations, pskRefiningFocus, SkillName.RefiningFocus);
            configureSkillBlockOnce(chkColonyOperations, pskProductionFocus, SkillName.ProductionFocus);
            configureSkillBlockOnce(chkColonyOperations, pskExtractionFocus, SkillName.ExtractionFocus);
            configureSkillBlockOnce(chkCommander, pskDamageControl, SkillName.DamageControl);
            configureSkillBlockOnce(chkEngineer, pskEngineeringCapacity, SkillName.EngineeringCapacity);
            configureSkillBlockOnce(chkEntrepeneur, pskSoundAsAPound, SkillName.SoundsAsAPound);
            configureSkillBlockOnce(chkEntrepeneur, pskSelfMadeMillionaire, SkillName.SelfMadeMillionaire);
            configureSkillBlockOnce(chkEntrepeneur, pskAAAHealthcare, SkillName.AAAHealthcare);
            configureSkillBlockOnce(chkJobManagement, pskJobOpportunities, SkillName.JobOpportunities);
            configureSkillBlockOnce(chkJobManagement, pskContractManagement, SkillName.ContractManagement);
            configureSkillBlockOnce(chkResearcher, pskResearchReview, SkillName.ResearchReview);
            configureSkillBlockOnce(chkResearcher, pskResearchMethods, SkillName.ResearchMethods);
            configureSkillBlockOnce(chkResearcher, pskResearchFocus, SkillName.ResearchFocus);
            configureSkillBlockOnce(chkSurveyor, pskSurveyingMethods, SkillName.SurveyingMethods);
            configureSkillBlockOnce(chkSurveyor, pskScanningMethods, SkillName.ScanningMethods);
            configureSkillBlockOnce(chkSurveyor, pskQuartermaster, SkillName.Quartermaster);
            configureSkillBlockOnce(chkTrader, pskBroker, SkillName.Broker);

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

            updateSkillBlock(pskHumanResources, SkillName.HumanResources);
            updateSkillBlock(pskForeman, SkillName.Foreman);
            updateSkillBlock(pskFounder, SkillName.Founder);
            updateSkillBlock(pskEnergyEfficiency, SkillName.EnergyEfficiency);
            updateSkillBlock(pskBuilder, SkillName.Builder);
            updateSkillBlock(pskRefiningFocus, SkillName.RefiningFocus);
            updateSkillBlock(pskProductionFocus, SkillName.ProductionFocus);
            updateSkillBlock(pskExtractionFocus, SkillName.ExtractionFocus);
            updateSkillBlock(pskDamageControl, SkillName.DamageControl);
            updateSkillBlock(pskEngineeringCapacity, SkillName.EngineeringCapacity);
            updateSkillBlock(pskSoundAsAPound, SkillName.SoundsAsAPound);
            updateSkillBlock(pskSelfMadeMillionaire, SkillName.SelfMadeMillionaire);
            updateSkillBlock(pskAAAHealthcare, SkillName.AAAHealthcare);
            updateSkillBlock(pskJobOpportunities, SkillName.JobOpportunities);
            updateSkillBlock(pskContractManagement, SkillName.ContractManagement);
            updateSkillBlock(pskResearchReview, SkillName.ResearchReview);
            updateSkillBlock(pskResearchMethods, SkillName.ResearchMethods);
            updateSkillBlock(pskResearchFocus, SkillName.ResearchFocus);
            updateSkillBlock(pskSurveyingMethods, SkillName.SurveyingMethods);
            updateSkillBlock(pskScanningMethods, SkillName.ScanningMethods);
            updateSkillBlock(pskQuartermaster, SkillName.Quartermaster);
            updateSkillBlock(pskBroker, SkillName.Broker);

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

        private void configureSkillBlockOnce(CheckBox skillGroup, PlayerSkillBlock skillBlock, SkillName skill)
        {
            skillBlock.SkillGroupCheckbox = skillGroup;
            skillBlock.SkillName = skill.ToDisplayName();
            skillBlocks[skill.ToDisplayName()] = skillBlock;
            skillBlock.TrainingStatusChanged += TrainingStatusChanged;
        }

        private void updateSkillBlock(PlayerSkillBlock skillBlock, SkillName skill)
        {
            skillBlock.PlayerSkill = selectedProfile.GetSkill(skill);
            skillBlock.PopulateForm();
        }


        private void TrainingStatusChanged (object sender, EventArgs e)
        {
            PopulateForm();
        }

        private void chkColonyDirector_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.ColonyDirector, chkColonyDirector.Checked);
            PopulateForm();
        }

        private void chkColonyFounder_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.ColonyFounder, chkColonyFounder.Checked);
            PopulateForm();
        }

        private void chkColonyOperations_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.ColonyOperations, chkColonyOperations.Checked);
            PopulateForm();
        }

        private void chkCommander_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Commander, chkCommander.Checked);
            PopulateForm();
        }

        private void chkEngineer_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Engineer, chkEngineer.Checked);
            PopulateForm();
        }

        private void chkEntrepeneur_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Entrepeneur, chkEntrepeneur.Checked);
            PopulateForm();
        }

        private void chkJobManagement_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.JobManagement, chkJobManagement.Checked);
            PopulateForm();
        }

        private void chkResearcher_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Researcher, chkResearcher.Checked);
            PopulateForm();
        }

        private void chkSurveyor_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Surveyor, chkSurveyor.Checked);
            PopulateForm();
        }

        private void chkTrader_Click(object sender, EventArgs e)
        {
            selectedProfile.SetSkillGroup(SkillGroupName.Trader, chkTrader.Checked);
            PopulateForm();
        }

        private void populateListView(Data.PlayerProfile profileToSelect = null)
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

                if (profileToSelect != null && profile.UUID == profileToSelect.UUID)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                }
            }
        }

        private void txtNameFilter_TextChanged(object sender, EventArgs e)
        {
            populateListView();
        }

        private void lvwPlayerProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && lvwPlayerProfiles.SelectedItems.Count == 1)
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
            populateListView(selectedProfile);
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
