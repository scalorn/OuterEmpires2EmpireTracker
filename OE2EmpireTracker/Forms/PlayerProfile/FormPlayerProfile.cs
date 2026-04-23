using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.PlayerProfile
{
    public partial class FormPlayerProfile : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private PlayerProfileViewModel viewModel;

        private Dictionary<SkillGroupName, CheckBox> _skillGroups = new Dictionary<SkillGroupName, CheckBox>();
        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();

        public FormPlayerProfile()
        {
            InitializeComponent();

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new PlayerProfileViewModel(new Models.PlayerProfile(), playerContext);

            _skillGroups[SkillGroupName.ColonyDirector]   = chkColonyDirector;
            _skillGroups[SkillGroupName.ColonyFounder]    = chkColonyFounder;
            _skillGroups[SkillGroupName.ColonyOperations] = chkColonyOperations;
            _skillGroups[SkillGroupName.Commander]        = chkCommander;
            _skillGroups[SkillGroupName.Engineer]         = chkEngineer;
            _skillGroups[SkillGroupName.Entrepeneur]      = chkEntrepeneur;
            _skillGroups[SkillGroupName.JobManagement]    = chkJobManagement;
            _skillGroups[SkillGroupName.Researcher]       = chkResearcher;
            _skillGroups[SkillGroupName.Surveyor]         = chkSurveyor;
            _skillGroups[SkillGroupName.Trader]           = chkTrader;

            ConfigureSkillBlockOnce(chkColonyDirector, pskHumanResources, SkillName.HumanResources);
            ConfigureSkillBlockOnce(chkColonyDirector, pskForeman, SkillName.Foreman);
            ConfigureSkillBlockOnce(chkColonyFounder, pskFounder, SkillName.Founder);
            ConfigureSkillBlockOnce(chkColonyFounder, pskEnergyEfficiency, SkillName.EnergyEfficiency);
            ConfigureSkillBlockOnce(chkColonyFounder, pskBuilder, SkillName.Builder);
            ConfigureSkillBlockOnce(chkColonyOperations, pskRefiningFocus, SkillName.RefiningFocus);
            ConfigureSkillBlockOnce(chkColonyOperations, pskProductionFocus, SkillName.ProductionFocus);
            ConfigureSkillBlockOnce(chkColonyOperations, pskExtractionFocus, SkillName.ExtractionFocus);
            ConfigureSkillBlockOnce(chkCommander, pskDamageControl, SkillName.DamageControl);
            ConfigureSkillBlockOnce(chkEngineer, pskEngineeringCapacity, SkillName.EngineeringCapacity);
            ConfigureSkillBlockOnce(chkEntrepeneur, pskSoundAsAPound, SkillName.SoundsAsAPound);
            ConfigureSkillBlockOnce(chkEntrepeneur, pskSelfMadeMillionaire, SkillName.SelfMadeMillionaire);
            ConfigureSkillBlockOnce(chkEntrepeneur, pskAAAHealthcare, SkillName.AAAHealthcare);
            ConfigureSkillBlockOnce(chkJobManagement, pskJobOpportunities, SkillName.JobOpportunities);
            ConfigureSkillBlockOnce(chkJobManagement, pskContractManagement, SkillName.ContractManagement);
            ConfigureSkillBlockOnce(chkResearcher, pskResearchReview, SkillName.ResearchReview);
            ConfigureSkillBlockOnce(chkResearcher, pskResearchMethods, SkillName.ResearchMethods);
            ConfigureSkillBlockOnce(chkResearcher, pskResearchFocus, SkillName.ResearchFocus);
            ConfigureSkillBlockOnce(chkSurveyor, pskSurveyingMethods, SkillName.SurveyingMethods);
            ConfigureSkillBlockOnce(chkSurveyor, pskScanningMethods, SkillName.ScanningMethods);
            ConfigureSkillBlockOnce(chkSurveyor, pskQuartermaster, SkillName.Quartermaster);
            ConfigureSkillBlockOnce(chkTrader, pskBroker, SkillName.Broker);

            txtNameFilter.TextChanged += TxtNameFilter_TextChanged;
            lvwPlayerProfiles.ItemSelectionChanged += LvwPlayerProfiles_ItemSelectionChanged;

            lvwPlayerProfiles.View = View.Details;
            lvwPlayerProfiles.Columns.Clear();
            lvwPlayerProfiles.Columns.Add("Name", 200);
            lvwPlayerProfiles.Columns.Add("Faction", 200);

            PopulateListView();
            PopulateForm();

            // Wire write-through handlers
            txtTotalCredits.TextChanged += TxtTotalCredits_TextChanged;
            txtSkillPoints.TextChanged += TxtSkillPoints_TextChanged;
            txtPublicRank.TextChanged += TxtPublicRank_TextChanged;
            txtPublicRankCurXP.TextChanged += TxtPublicRankCurXP_TextChanged;
            txtPublicRankNextXP.TextChanged += TxtPublicRankNextXP_TextChanged;
            txtPrivateRank.TextChanged += TxtPrivateRank_TextChanged;
            txtPrivateRankCurXP.TextChanged += TxtPrivateRankCurXP_TextChanged;
            txtPrivateRankNextXP.TextChanged += TxtPrivateRankNextXP_TextChanged;
            txtMilitaryRank.TextChanged += TxtMilitaryRank_TextChanged;
            txtMilitaryRankCurXP.TextChanged += TxtMilitaryRankCurXP_TextChanged;
            txtMilitaryRankNextXP.TextChanged += TxtMilitaryRankNextXP_TextChanged;
            cmbFaction.TextChanged += CmbFaction_TextChanged;

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpPlayerData.Layout += FlpPlayerData_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.PlayerProfileDataChanged += OnPlayerProfileDataChanged;
        }

        private void OnPlayerProfileDataChanged(object sender, PlayerProfileDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnPlayerProfileDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            if (viewModel.Data.UUID == e.PlayerUUID)
            {
                PopulateForm();
            }

            PopulateListView();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            lvwPlayerProfiles.Items.Clear();
            viewModel.Reset();
            PopulateListView();
            PopulateForm();
        }

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpPlayerData.Size = new System.Drawing.Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpPlayerData.Margin.Left - flpPlayerData.Margin.Right,
                flpBase.Size.Height - flpPlayerData.Margin.Top - flpPlayerData.Margin.Bottom);
            flpSearchList.Size = new System.Drawing.Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwPlayerProfiles.Size = new System.Drawing.Size(
                lvwPlayerProfiles.Size.Width,
                flpSearchList.Size.Height - flpBlueprintSearch.Size.Height - flpBlueprintSearch.Margin.Top - flpBlueprintSearch.Margin.Bottom - flpResource.Size.Height - flpResource.Margin.Top - flpResource.Margin.Bottom - lvwPlayerProfiles.Margin.Top - lvwPlayerProfiles.Margin.Bottom);
        }

        private void FlpPlayerData_Layout(object sender, LayoutEventArgs e)
        {
            flpPlayerDetails.Size = new System.Drawing.Size(
                flpPlayerData.Size.Width - flpPlayerDetails.Margin.Left - flpPlayerDetails.Margin.Right,
                flpPlayerData.Size.Height - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom - flpPlayerDetails.Margin.Top - flpPlayerDetails.Margin.Bottom);
        }

        public void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
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

            foreach (var entry in _skillGroups)
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

            sw.Stop();
            Log.Info("PopulateForm PERF: total={0}ms", sw.ElapsedMilliseconds);
            sw.Stop(); Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ConfigureSkillBlockOnce(CheckBox skillGroup, PlayerSkillBlock skillBlock, SkillName skill)
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

        private void ChkColonyDirector_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.ColonyDirector, chkColonyDirector.Checked);
            PopulateForm();
        }

        private void ChkColonyFounder_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.ColonyFounder, chkColonyFounder.Checked);
            PopulateForm();
        }

        private void ChkCommander_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Commander, chkCommander.Checked);
            PopulateForm();
        }

        private void ChkEngineer_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Engineer, chkEngineer.Checked);
            PopulateForm();
        }

        private void ChkEntrepeneur_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Entrepeneur, chkEntrepeneur.Checked);
            PopulateForm();
        }

        private void ChkJobManagement_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.JobManagement, chkJobManagement.Checked);
            PopulateForm();
        }

        private void ChkResearcher_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Researcher, chkResearcher.Checked);
            PopulateForm();
        }

        private void ChkSurveyor_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Surveyor, chkSurveyor.Checked);
            PopulateForm();
        }

        private void ChkTrader_Click(object sender, EventArgs e)
        {
            viewModel.SetSkillGroup(SkillGroupName.Trader, chkTrader.Checked);
            PopulateForm();
        }

        private void PopulateListView(Models.PlayerProfile profileToSelect = null)
        {
            var sw = Stopwatch.StartNew();
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

            sw.Stop();
            Log.Info("PopulateListView PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds, profiles.Count);
            sw.Stop(); Log.Info("PERF PopulateListView: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtNameFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateListView();
        }

        private void LvwPlayerProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && lvwPlayerProfiles.SelectedItems.Count == 1)
            {
                viewModel.SelectProfile(lvwPlayerProfiles.SelectedItems[0].Tag as Models.PlayerProfile);
                PopulateForm();
            }
        }

        private void TxtPlayerName_TextChanged(object sender, EventArgs e)
        {
            string name = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                txtPlayerName.SetError("Name cannot be empty");
                return;
            }

            bool duplicate = playerContext.PlayerProfileList
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

            if (_isProgrammaticUpdate > 0) return;
            viewModel.Name = txtPlayerName.Text?.Trim() ?? string.Empty;
        }

        private void TxtTotalCredits_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.TotalCredits = decimal.TryParse(txtTotalCredits.Text, out var c) ? c : 0;
        }

        private void TxtSkillPoints_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.SkillPoints = int.TryParse(txtSkillPoints.Text, out var sp) ? sp : 0;
        }

        private void TxtPublicRank_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PublicRank.Rank = int.TryParse(txtPublicRank.Text, out var r) ? r : 0;
        }

        private void TxtPublicRankCurXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PublicRank.CurrentXP = long.TryParse(txtPublicRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtPublicRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PublicRank.NextXP = long.TryParse(txtPublicRankNextXP.Text, out var x) ? x : 0;
        }

        private void TxtPrivateRank_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.Rank = int.TryParse(txtPrivateRank.Text, out var r) ? r : 0;
        }

        private void TxtPrivateRankCurXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.CurrentXP = long.TryParse(txtPrivateRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtPrivateRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.NextXP = long.TryParse(txtPrivateRankNextXP.Text, out var x) ? x : 0;
        }

        private void TxtMilitaryRank_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.Rank = int.TryParse(txtMilitaryRank.Text, out var r) ? r : 0;
        }

        private void TxtMilitaryRankCurXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.CurrentXP = long.TryParse(txtMilitaryRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtMilitaryRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.NextXP = long.TryParse(txtMilitaryRankNextXP.Text, out var x) ? x : 0;
        }

        private void CmbFaction_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Faction = cmbFaction.Text;
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            string newName = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(newName) || !txtPlayerName.IsValid)
            {
                return;
            }

            viewModel.Save();
            PopulateListView(viewModel.Data);
        }

        private void CmdDelete_Click(object sender, EventArgs e)
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
            lvwPlayerProfiles.SelectedItems.Clear();
            PopulateForm();
        }

        private void CmdNew_Click(object sender, EventArgs e)
        {
            viewModel.Reset();
            PopulateForm();
            lvwPlayerProfiles.SelectedItems.Clear();
        }

        private void CmdImport_Click(object sender, EventArgs e)
        {
            try
            {
                if (!System.Windows.Forms.Clipboard.ContainsText(TextDataFormat.Html))
                {
                    MessageBox.Show("No profile data found on the clipboard.\n\nCopy the profile panel from the game first.",
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Validate clipboard contains player profile data
                string clipboardData = System.Windows.Forms.Clipboard.GetText(TextDataFormat.Html);
                string htmlFragment = Parsers.ClipboardHelper.ExtractHtmlFragment(clipboardData);
                var detected = Parsers.ClipboardContentDetector.Detect(htmlFragment);
                if (detected != Parsers.ClipboardContentDetector.ContentType.PlayerProfile &&
                    detected != Parsers.ClipboardContentDetector.ContentType.Unknown)
                {
                    string found = Parsers.ClipboardContentDetector.GetDescription(detected);
                    MessageBox.Show($"The clipboard contains {found}, not player profile data.\n\nCopy the profile panel from the game first.",
                        "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Parse clipboard into a temp profile
                var tempProfile = new Models.PlayerProfile();
                var parser = new PlayerProfileParser();
                parser.ProcessClipboard(tempProfile);

                if (string.IsNullOrEmpty(tempProfile.Name))
                {
                    MessageBox.Show("Could not extract a player name from the clipboard data.",
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Find existing profile by name (case-insensitive)
                var existing = playerContext.PlayerProfileList
                    .FirstOrDefault(p => string.Equals(p.Name, tempProfile.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Update existing profile -- preserve UUID
                    MergeProfile(existing, tempProfile);
                    viewModel.SelectProfile(existing);
                }
                else
                {
                    // Create new profile with generated UUID
                    tempProfile.UUID = Guid.NewGuid().ToString();
                    playerContext.AddPlayerProfile(tempProfile);
                    viewModel.SelectProfile(tempProfile);
                }

                playerContext.WriteContext();
                playerContext.OnPlayerProfilesChanged();
                playerContext.OnPlayerProfileDataChanged(viewModel.Data.UUID);
                PopulateListView(viewModel.Data);
                PopulateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Merges parsed profile data into an existing profile, preserving UUID.
        /// </summary>
        internal static void MergeProfile(Models.PlayerProfile existing, Models.PlayerProfile parsed)
        {
            existing.Name = parsed.Name;
            existing.Faction = parsed.Faction;
            existing.TotalCredits = parsed.TotalCredits;
            existing.SkillPoints = parsed.SkillPoints;
            existing.CitizenId = parsed.CitizenId;
            existing.RegistrationDate = parsed.RegistrationDate;
            existing.ActiveTime = parsed.ActiveTime;

            // Merge ranks
            MergeRank(existing.Public, parsed.Public);
            MergeRank(existing.Private, parsed.Private);
            MergeRank(existing.Military, parsed.Military);

            // Merge skill groups and skills
            foreach (SkillGroupName group in Enum.GetValues(typeof(SkillGroupName)))
            {
                existing.SetSkillGroup(group, parsed.GetSkillGroup(group));
            }

            foreach (var skillEntry in parsed.Skills)
            {
                var existingSkill = existing.GetSkill(skillEntry.Key);
                existingSkill.Level = skillEntry.Value.Level;
                existingSkill.TrainingStarted = skillEntry.Value.TrainingStarted;
                existingSkill.CompletionTime = skillEntry.Value.CompletionTime;
            }
        }

        internal static void MergeRank(PlayerRank existing, PlayerRank parsed)
        {
            existing.Rank = parsed.Rank;
            existing.Title = parsed.Title;
            existing.CurrentXP = parsed.CurrentXP;
            existing.NextXP = parsed.NextXP;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.PlayerProfileDataChanged -= OnPlayerProfileDataChanged;
            base.OnFormClosed(e);
        }
    }
}
