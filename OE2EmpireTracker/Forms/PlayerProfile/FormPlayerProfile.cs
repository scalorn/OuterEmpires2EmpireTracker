using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OE2EmpireTracker.Controls;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class FormPlayerProfile : Form, IProgrammaticUpdateSource
    {
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private PlayerProfileViewModel viewModel;

        private Dictionary<SkillGroupName, CheckBox> SkillGroups = new Dictionary<SkillGroupName, CheckBox>();
        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();

        public FormPlayerProfile()
        {
            InitializeComponent();

            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new PlayerProfileViewModel(new Data.PlayerProfile(), playerContext);

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

            PopulateListView();
            PopulateForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpPlayerData.Layout += flpPlayerData_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            lvwPlayerProfiles.Items.Clear();
            viewModel.Reset();
            PopulateListView();
            PopulateForm();
        }

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpPlayerData.Size = new System.Drawing.Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpPlayerData.Margin.Left - flpPlayerData.Margin.Right,
                flpBase.Size.Height - flpPlayerData.Margin.Top - flpPlayerData.Margin.Bottom);
            flpSearchList.Size = new System.Drawing.Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwPlayerProfiles.Size = new System.Drawing.Size(
                lvwPlayerProfiles.Size.Width,
                flpSearchList.Size.Height - flpBlueprintSearch.Size.Height - flpBlueprintSearch.Margin.Top - flpBlueprintSearch.Margin.Bottom - flpResource.Size.Height - flpResource.Margin.Top - flpResource.Margin.Bottom - lvwPlayerProfiles.Margin.Top - lvwPlayerProfiles.Margin.Bottom);
        }

        private void flpPlayerData_Layout(object sender, LayoutEventArgs e)
        {
            flpPlayerDetails.Size = new System.Drawing.Size(
                flpPlayerData.Size.Width - flpPlayerDetails.Margin.Left - flpPlayerDetails.Margin.Right,
                flpPlayerData.Size.Height - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom - flpPlayerDetails.Margin.Top - flpPlayerDetails.Margin.Bottom);
        }

        public void PopulateForm()
        {
            txtPlayerName.Text = viewModel.Name;
            cmbFaction.Text = viewModel.Faction;
            txtTotalCredits.Text = viewModel.TotalCredits.ToString();
            txtSkillPoints.Text = viewModel.SkillPoints.ToString();

            txtPublicRank.Text = viewModel.PublicRank.Rank.ToString();
            txtPublicRankCurXP.Text = viewModel.PublicRank.CurrentXP.ToString();
            txtPublicRankNextXP.Text = viewModel.PublicRank.NextXP.ToString();

            txtPrivateRank.Text = viewModel.PrivateRank.Rank.ToString();
            txtPrivateRankCurXP.Text = viewModel.PrivateRank.CurrentXP.ToString();
            txtPrivateRankNextXP.Text = viewModel.PrivateRank.NextXP.ToString();

            txtMilitaryRank.Text = viewModel.MilitaryRank.Rank.ToString();
            txtMilitaryRankCurXP.Text = viewModel.MilitaryRank.CurrentXP.ToString();
            txtMilitaryRankNextXP.Text = viewModel.MilitaryRank.NextXP.ToString();

            foreach (var entry in SkillGroups)
            {
                entry.Value.Checked = viewModel.GetSkillGroup(entry.Key);
            }

            UpdateSkillBlock(pskHumanResources, SkillName.HumanResources);
            UpdateSkillBlock(pskForeman, SkillName.Foreman);
            UpdateSkillBlock(pskFounder, SkillName.Founder);
            UpdateSkillBlock(pskEnergyEfficiency, SkillName.EnergyEfficiency);
            UpdateSkillBlock(pskBuilder, SkillName.Builder);
            UpdateSkillBlock(pskRefiningFocus, SkillName.RefiningFocus);
            UpdateSkillBlock(pskProductionFocus, SkillName.ProductionFocus);
            UpdateSkillBlock(pskExtractionFocus, SkillName.ExtractionFocus);
            UpdateSkillBlock(pskDamageControl, SkillName.DamageControl);
            UpdateSkillBlock(pskEngineeringCapacity, SkillName.EngineeringCapacity);
            UpdateSkillBlock(pskSoundAsAPound, SkillName.SoundsAsAPound);
            UpdateSkillBlock(pskSelfMadeMillionaire, SkillName.SelfMadeMillionaire);
            UpdateSkillBlock(pskAAAHealthcare, SkillName.AAAHealthcare);
            UpdateSkillBlock(pskJobOpportunities, SkillName.JobOpportunities);
            UpdateSkillBlock(pskContractManagement, SkillName.ContractManagement);
            UpdateSkillBlock(pskResearchReview, SkillName.ResearchReview);
            UpdateSkillBlock(pskResearchMethods, SkillName.ResearchMethods);
            UpdateSkillBlock(pskResearchFocus, SkillName.ResearchFocus);
            UpdateSkillBlock(pskSurveyingMethods, SkillName.SurveyingMethods);
            UpdateSkillBlock(pskScanningMethods, SkillName.ScanningMethods);
            UpdateSkillBlock(pskQuartermaster, SkillName.Quartermaster);
            UpdateSkillBlock(pskBroker, SkillName.Broker);

            bool isTraining = viewModel.IsAnySkillTraining();
            foreach (var skillBlockEntry in skillBlocks)
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

        private void UpdateSkillBlock(PlayerSkillBlock skillBlock, SkillName skill)
        {
            skillBlock.PlayerSkill = viewModel.GetSkill(skill);
            skillBlock.PopulateForm();
        }

        private void TrainingStatusChanged(object sender, EventArgs e)
        {
            PopulateForm();
        }

        private void chkColonyDirector_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.ColonyDirector, chkColonyDirector.Checked);
            PopulateForm();
        }

        private void chkColonyFounder_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.ColonyFounder, chkColonyFounder.Checked);
            PopulateForm();
        }

        private void chkColonyOperations_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.ColonyOperations, chkColonyOperations.Checked);
            PopulateForm();
        }

        private void chkCommander_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Commander, chkCommander.Checked);
            PopulateForm();
        }

        private void chkEngineer_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Engineer, chkEngineer.Checked);
            PopulateForm();
        }

        private void chkEntrepeneur_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Entrepeneur, chkEntrepeneur.Checked);
            PopulateForm();
        }

        private void chkJobManagement_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.JobManagement, chkJobManagement.Checked);
            PopulateForm();
        }

        private void chkResearcher_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Researcher, chkResearcher.Checked);
            PopulateForm();
        }

        private void chkSurveyor_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Surveyor, chkSurveyor.Checked);
            PopulateForm();
        }

        private void chkTrader_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Trader, chkTrader.Checked);
            PopulateForm();
        }

        private void PopulateListView(Data.PlayerProfile profileToSelect = null)
        {
            var profiles = viewModel.GetFilteredProfiles(txtNameFilter.Text);

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
            PopulateListView();
        }

        private void lvwPlayerProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && lvwPlayerProfiles.SelectedItems.Count == 1)
            {
                viewModel.SelectProfile(lvwPlayerProfiles.SelectedItems[0].Tag as Data.PlayerProfile);
                PopulateForm();
            }
        }

        private void txtPlayerName_TextChanged(object sender, EventArgs e)
        {
            string name = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                txtPlayerName.SetError("Name cannot be empty");
                return;
            }

            bool duplicate = playerContext.playerProfileList
                .Any(p => p.UUID != viewModel.Data.UUID &&
                     string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                txtPlayerName.SetError("Duplicate name");
            }
            else
            {
                txtPlayerName.ClearError();
            }
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            string newName = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(newName) || !txtPlayerName.IsValid)
            {
                return;
            }

            viewModel.Name = newName;
            viewModel.Faction = cmbFaction.Text;
            viewModel.TotalCredits = decimal.TryParse(txtTotalCredits.Text, out var credits) ? credits : 0;
            viewModel.SkillPoints = int.TryParse(txtSkillPoints.Text, out var sp) ? sp : 0;

            viewModel.PublicRank.Rank = int.TryParse(txtPublicRank.Text, out var pubRank) ? pubRank : 0;
            viewModel.PublicRank.CurrentXP = long.TryParse(txtPublicRankCurXP.Text, out var pubCur) ? pubCur : 0;
            viewModel.PublicRank.NextXP = long.TryParse(txtPublicRankNextXP.Text, out var pubNext) ? pubNext : 0;

            viewModel.PrivateRank.Rank = int.TryParse(txtPrivateRank.Text, out var priRank) ? priRank : 0;
            viewModel.PrivateRank.CurrentXP = long.TryParse(txtPrivateRankCurXP.Text, out var priCur) ? priCur : 0;
            viewModel.PrivateRank.NextXP = long.TryParse(txtPrivateRankNextXP.Text, out var priNext) ? priNext : 0;

            viewModel.MilitaryRank.Rank = int.TryParse(txtMilitaryRank.Text, out var milRank) ? milRank : 0;
            viewModel.MilitaryRank.CurrentXP = long.TryParse(txtMilitaryRankCurXP.Text, out var milCur) ? milCur : 0;
            viewModel.MilitaryRank.NextXP = long.TryParse(txtMilitaryRankNextXP.Text, out var milNext) ? milNext : 0;

            viewModel.Save();
            PopulateListView(viewModel.Data);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.Data.UUID)) return;

            var result = MessageBox.Show(
                $"Delete profile '{viewModel.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            viewModel.Delete();
            viewModel.Reset();
            PopulateListView();
            PopulateForm();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.Data.UUID))
            {
                viewModel.Reset();
            }
            PopulateForm();
        }
    }
}
