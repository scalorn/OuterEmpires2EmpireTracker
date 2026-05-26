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
using OE2EmpireTracker.Client;
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

        private EmpireContext empireContext;

        private PlayerContext playerContext;

        private PlayerProfileService _profileService;

        private PlayerProfileViewModel viewModel;

        private string _previousSelectedUUID;

        private Dictionary<SkillGroupName, CheckBox> _skillGroups = new Dictionary<SkillGroupName, CheckBox>();

        private Dictionary<string, PlayerSkillBlock> skillBlocks = new Dictionary<string, PlayerSkillBlock>();

        public FormPlayerProfile()
        {
            // Guard against WindowStateHelper.RestoreState setting control values
            // (called by MainWindow between constructor and Show). Cleared in Shown event.
            _isProgrammaticUpdate++;

            InitializeComponent();

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new PlayerProfileViewModel(playerContext);
            _profileService = new PlayerProfileService(playerContext);

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

            // Wire edit buffer handlers
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

            // Clear the programmatic guard set at constructor start.
            Shown += (s, ev) => _isProgrammaticUpdate--;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        public void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            txtPlayerName.Text = viewModel.Name;
            lblCharacterIdValue.Text = viewModel.CharacterId > 0
                ? viewModel.CharacterId.ToString()
                : "\u2014";
            lblFirstNameValue.Text = viewModel.FirstName ?? string.Empty;
            flpFirstName.Visible = !string.IsNullOrEmpty(viewModel.FirstName);
            lblLastNameValue.Text = viewModel.LastName ?? string.Empty;
            flpLastName.Visible = !string.IsNullOrEmpty(viewModel.LastName);
            cmbFaction.Text = viewModel.Faction;
            txtTotalCredits.Text = viewModel.TotalCredits.ToString();
            txtSkillPoints.Text = viewModel.SkillPoints.ToString();

            txtPublicRank.Text = viewModel.PublicRank.Rank.ToString();
            txtPublicRankCurXP.Text = viewModel.PublicRank.CurrentXp.ToString();
            txtPublicRankNextXP.Text = viewModel.PublicRank.XpToNextLevel.ToString();
            UpdateRankNameLabel(lblPublicRankName, viewModel.PublicRank.RankName);

            txtPrivateRank.Text = viewModel.PrivateRank.Rank.ToString();
            txtPrivateRankCurXP.Text = viewModel.PrivateRank.CurrentXp.ToString();
            txtPrivateRankNextXP.Text = viewModel.PrivateRank.XpToNextLevel.ToString();
            UpdateRankNameLabel(lblPrivateRankName, viewModel.PrivateRank.RankName);

            txtMilitaryRank.Text = viewModel.MilitaryRank.Rank.ToString();
            txtMilitaryRankCurXP.Text = viewModel.MilitaryRank.CurrentXp.ToString();
            txtMilitaryRankNextXP.Text = viewModel.MilitaryRank.XpToNextLevel.ToString();
            UpdateRankNameLabel(lblMilitaryRankName, viewModel.MilitaryRank.RankName);

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
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentProfile();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.PlayerProfileDataChanged -= OnPlayerProfileDataChanged;
            base.OnFormClosed(e);
        }

        private void OnPlayerProfileDataChanged(object sender, PlayerProfileDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnPlayerProfileDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            if (viewModel.UUID == e.PlayerUUID)
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
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

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

        private void ConfigureSkillBlockOnce(CheckBox skillGroup, PlayerSkillBlock skillBlock, SkillName skill)
        {
            skillBlock.SkillGroupCheckbox = skillGroup;
            skillBlock.SkillName = skill.ToDisplayName();
            skillBlocks[skill.ToDisplayName()] = skillBlock;
            skillBlock.TrainingStatusChanged += TrainingStatusChanged;
        }

        private void UpdateSkillBlock(PlayerSkillBlock skillBlock, SkillName skill)
        {
            skillBlock.SkillData = viewModel.GetSkill(skill);
            skillBlock.PopulateForm();
        }

        private void UpdateRankNameLabel(Label label, string rankName)
        {
            if (string.IsNullOrEmpty(rankName))
            {
                label.Visible = false;
            }
            else
            {
                label.Text = rankName;
                label.Visible = true;
            }
        }

        private void TrainingStatusChanged(object sender, EventArgs e)
        {
            PopulateForm();
            UpdateSaveButtonState();
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

        private void PopulateListView()
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
            }

            sw.Stop();
            Log.Info(
                "PopulateListView PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds,
                profiles.Count);
            sw.Stop();
            Log.Info("PERF PopulateListView: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtNameFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateListView();
        }

        private void LvwPlayerProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && lvwPlayerProfiles.SelectedItems.Count == 1)
            {
                var tag = lvwPlayerProfiles.SelectedItems[0].Tag;
                var roProfile = tag as ReadOnlyPlayerProfile;

                // Prompt for unsaved changes before switching
                if (viewModel.IsDirty)
                {
                    var result = PromptUnsavedChanges();
                    if (result == DialogResult.Yes)
                    {
                        SaveCurrentProfile();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Restore previous selection
                        lvwPlayerProfiles.ItemSelectionChanged -= LvwPlayerProfiles_ItemSelectionChanged;
                        lvwPlayerProfiles.SelectedItems.Clear();
                        if (!string.IsNullOrEmpty(_previousSelectedUUID))
                        {
                            foreach (ListViewItem item in lvwPlayerProfiles.Items)
                            {
                                if ((item.Tag as ReadOnlyPlayerProfile)?.UUID == _previousSelectedUUID)
                                {
                                    item.Selected = true;
                                    item.EnsureVisible();
                                    break;
                                }
                            }
                        }

                        lvwPlayerProfiles.ItemSelectionChanged += LvwPlayerProfiles_ItemSelectionChanged;
                        return;
                    }

                    // DialogResult.No - discard, fall through to load new
                }

                if (roProfile != null)
                {
                    viewModel.LoadFrom(roProfile);
                    _previousSelectedUUID = viewModel.UUID;
                }
                else
                {
                    viewModel.Reset();
                    _previousSelectedUUID = null;
                }

                PopulateForm();
                UpdateSaveButtonState();
            }
        }

        private void TxtPlayerName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            string name = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                txtPlayerName.SetError("Name cannot be empty");
                UpdateSaveButtonState();
                return;
            }

            bool duplicate = playerContext.GetReadOnlyPlayerProfileList()
                .Any(p => p.UUID != viewModel.UUID &&
                     string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                txtPlayerName.SetError("Duplicate name");
            }
            else
            {
                txtPlayerName.ClearError();
            }

            viewModel.Name = txtPlayerName.Text?.Trim() ?? string.Empty;
            UpdateSaveButtonState();
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
            viewModel.PublicRank.CurrentXp = long.TryParse(txtPublicRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtPublicRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PublicRank.XpToNextLevel = long.TryParse(txtPublicRankNextXP.Text, out var x) ? x : 0;
        }

        private void TxtPrivateRank_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.Rank = int.TryParse(txtPrivateRank.Text, out var r) ? r : 0;
        }

        private void TxtPrivateRankCurXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.CurrentXp = long.TryParse(txtPrivateRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtPrivateRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.PrivateRank.XpToNextLevel = long.TryParse(txtPrivateRankNextXP.Text, out var x) ? x : 0;
        }

        private void TxtMilitaryRank_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.Rank = int.TryParse(txtMilitaryRank.Text, out var r) ? r : 0;
        }

        private void TxtMilitaryRankCurXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.CurrentXp = long.TryParse(txtMilitaryRankCurXP.Text, out var x) ? x : 0;
        }

        private void TxtMilitaryRankNextXP_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.MilitaryRank.XpToNextLevel = long.TryParse(txtMilitaryRankNextXP.Text, out var x) ? x : 0;
        }

        private void CmbFaction_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            viewModel.Faction = cmbFaction.Text;
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            SaveCurrentProfile();
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.UUID)) return;

            var result = MessageBox.Show(
                string.Format("Delete profile '{0}'?", viewModel.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            _profileService.Delete(viewModel.UUID);
            viewModel.Reset();
            _previousSelectedUUID = null;
            PopulateListView();
            lvwPlayerProfiles.SelectedItems.Clear();
            PopulateForm();
            UpdateSaveButtonState();
        }

        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    SaveCurrentProfile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            viewModel.Reset();
            _previousSelectedUUID = null;
            PopulateForm();
            lvwPlayerProfiles.SelectedItems.Clear();
            UpdateSaveButtonState();
        }

        private void CmdImport_Click(object sender, EventArgs e)
        {
            try
            {
                if (!System.Windows.Forms.Clipboard.ContainsText(TextDataFormat.Html))
                {
                    MessageBox.Show(
                        "No profile data found on the clipboard.\n\nCopy the profile panel from the game first.",
                        "Import",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
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
                    MessageBox.Show(
                        string.Format("The clipboard contains {0}, not player profile data.\n\nCopy the profile panel from the game first.", found),
                        "Wrong Content",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Parse clipboard into a temp profile
                var tempProfile = new Models.PlayerProfile();
                Log.Info("Player profile clipboard data length: {0}", clipboardData.Length);

                var parser = new PlayerProfileParser();
                parser.ProcessHtml(tempProfile, htmlFragment);

                if (string.IsNullOrEmpty(tempProfile.Name))
                {
                    MessageBox.Show(
                        "Could not extract a player name from the clipboard data.",
                        "Import",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Import via service — merges or creates as needed
                var imported = _profileService.Import(tempProfile);
                viewModel.LoadFrom(imported);
                _previousSelectedUUID = viewModel.UUID;
                PopulateListView();
                SelectProfileInList(viewModel.UUID);
                PopulateForm();
                UpdateSaveButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Import failed: {0}", ex.Message),
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Triggers an immediate Game API sync for the currently-selected profile.
        /// </summary>
        private async void CmdSyncApi_Click(object sender, EventArgs e)
        {
            var gameApi = GameApiContext.Instance;
            if (gameApi == null)
            {
                MessageBox.Show(
                    "Game API is not configured or enabled.\nGo to File \u2192 Preferences \u2192 Game API to set it up.",
                    "Game API Not Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string playerUUID = viewModel.UUID;
            if (string.IsNullOrEmpty(playerUUID))
            {
                return;
            }

            if (!gameApi.CredentialManager.HasKey(playerUUID))
            {
                MessageBox.Show(
                    string.Format("No API secret configured for '{0}'.\nGo to File \u2192 Preferences \u2192 Game API to enter the secret for this character.", viewModel.Name),
                    "No Secret Configured",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            cmdSyncApi.Enabled = false;
            cmdSyncApi.Text = "Syncing...";

            try
            {
                bool success = await gameApi.SyncScheduler.SyncCharacterAsync(playerUUID).ConfigureAwait(true);
                if (success)
                {
                    PopulateForm();
                    MessageBox.Show(
                        string.Format("Profile synced successfully for '{0}'.", viewModel.Name),
                        "Sync Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        string.Format("Sync failed for '{0}'. Check the log for details.", viewModel.Name),
                        "Sync Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manual API sync failed for {0}", playerUUID);
                MessageBox.Show(
                    "Sync failed: " + ex.Message,
                    "Sync Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                cmdSyncApi.Enabled = true;
                cmdSyncApi.Text = "Sync API";
            }
        }

        /// <summary>
        /// Saves the current profile via the service. New profiles are created; existing profiles are updated.
        /// </summary>
        private void SaveCurrentProfile()
        {
            string newName = txtPlayerName.Text?.Trim();
            if (string.IsNullOrEmpty(newName) || !txtPlayerName.IsValid)
            {
                return;
            }

            ReadOnlyPlayerProfile saved;
            if (viewModel.IsNew)
            {
                saved = _profileService.Create(viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _profileService.Update(viewModel.UUID, viewModel.BuildUpdateRequest());
            }

            viewModel.LoadFrom(saved);
            _previousSelectedUUID = viewModel.UUID;
            PopulateListView();
            SelectProfileInList(viewModel.UUID);
            PopulateForm();
            UpdateSaveButtonState();
        }

        /// <summary>
        /// Prompts the user to save, discard, or cancel when there are unsaved changes.
        /// Returns Yes (save), No (discard), or Cancel.
        /// </summary>
        private DialogResult PromptUnsavedChanges()
        {
            return MessageBox.Show(
                string.Format("Save changes to '{0}'?", viewModel.Name),
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        /// <summary>
        /// Enables the Save button only when the ViewModel has unsaved changes.
        /// </summary>
        private void UpdateSaveButtonState()
        {
            cmdSave.Enabled = viewModel.IsDirty;
        }

        /// <summary>
        /// Selects the profile with the given UUID in the list view.
        /// </summary>
        private void SelectProfileInList(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;

            foreach (ListViewItem item in lvwPlayerProfiles.Items)
            {
                if ((item.Tag as ReadOnlyPlayerProfile)?.UUID == uuid)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    return;
                }
            }
        }
    }
}
