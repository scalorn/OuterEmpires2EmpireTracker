using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Contacts
{
    public partial class FormContacts : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        private PlayerContext playerContext;
        private Faction _selectedFaction;
        private ExternalCharacter _selectedCharacter;

        public FormContacts()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            // Factions ListView setup
            lvwFactions.View = View.Details;
            lvwFactions.Columns.Add("Name", 140);
            lvwFactions.Columns.Add("Description", 80);
            lvwFactions.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwFactions.FullRowSelect = true;
            lvwFactions.MultiSelect = false;
            lvwFactions.ItemSelectionChanged += LvwFactions_ItemSelectionChanged;

            // Characters ListView setup
            lvwCharacters.View = View.Details;
            lvwCharacters.Columns.Add("Name", 150);
            lvwCharacters.Columns.Add("Faction", 120);
            lvwCharacters.FullRowSelect = true;
            lvwCharacters.MultiSelect = false;
            lvwCharacters.ItemSelectionChanged += LvwCharacters_ItemSelectionChanged;

            // Factions tab events
            txtFactionFilter.TextChanged += TxtFactionFilter_TextChanged;
            txtFactionName.TextChanged += TxtFactionName_TextChanged;
            txtFactionDescription.TextChanged += TxtFactionDescription_TextChanged;
            cmdNewFaction.Click += CmdNewFaction_Click;
            cmdDeleteFaction.Click += CmdDeleteFaction_Click;
            cmdSaveFaction.Click += CmdSaveFaction_Click;

            // Characters tab events
            txtCharFilter.TextChanged += TxtCharFilter_TextChanged;
            txtCharName.TextChanged += TxtCharName_TextChanged;
            cmbCharFaction.SelectedIndexChanged += CmbCharFaction_SelectedIndexChanged;
            cmdNewChar.Click += CmdNewChar_Click;
            cmdDeleteChar.Click += CmdDeleteChar_Click;
            cmdSaveChar.Click += CmdSaveChar_Click;

            PopulateFactionList();
            ClearFactionForm();
            PopulateCharacterList();
            ClearCharacterForm();

            // Layout handlers
            flpFactionBase.Layout += FlpFactionBase_Layout;
            flpFactionSearchList.Layout += FlpFactionSearchList_Layout;
            flpCharBase.Layout += FlpCharBase_Layout;
            flpCharSearchList.Layout += FlpCharSearchList_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpFactionBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpFactionBase.ClientSize.Width;
            int h = flpFactionBase.ClientSize.Height;
            flpFactionSearchList.Size = new System.Drawing.Size(280, h - 6);
            flpFactionDetail.Size = new System.Drawing.Size(w - 292, h - 6);
        }

        private void FlpFactionSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpFactionSearchList.ClientSize.Width;
            int h = flpFactionSearchList.ClientSize.Height;
            int listHeight = h - flpFactionFilter.Height - flpFactionCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwFactions.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void FlpCharBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpCharBase.ClientSize.Width;
            int h = flpCharBase.ClientSize.Height;
            flpCharSearchList.Size = new System.Drawing.Size(280, h - 6);
            flpCharDetail.Size = new System.Drawing.Size(w - 292, h - 6);
        }

        private void FlpCharSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpCharSearchList.ClientSize.Width;
            int h = flpCharSearchList.ClientSize.Height;
            int listHeight = h - flpCharFilter.Height - flpCharCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwCharacters.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        // -----------------------------------------------------------------------
        // Faction List
        // -----------------------------------------------------------------------

        private void PopulateFactionList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedFaction?.UUID;
            lvwFactions.Items.Clear();

            var factions = playerContext.FactionList.ToList();
            string filter = txtFactionFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                factions = factions.Where(f => f.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            factions = CollectionSortHelper.OrderFactions(factions).ToList();

            var refCounter = new FactionReferenceCounter(
                playerContext.ExternalCharacterList,
                playerContext.PlayerProfileList,
                playerContext.MarketTransactionList);

            foreach (var faction in factions)
            {
                int refs = refCounter.CountReferences(faction.UUID);
                var item = new ListViewItem(faction.Name) { Tag = faction };
                item.SubItems.Add(faction.Description);
                item.SubItems.Add(refs.ToString());
                lvwFactions.Items.Add(item);
                if (faction.UUID == selectedUUID)
                    item.Selected = true;
            }

            sw.Stop();
            Log.Info(
                "PopulateFactionList PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds,
                factions.Count);
            sw.Stop();
            Log.Info("PERF PopulateFactionList: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtFactionFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateFactionList();
        }

        private void LvwFactions_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Faction faction)
            {
                _selectedFaction = faction;
                PopulateFactionForm();
            }
            else if (!e.IsSelected && lvwFactions.SelectedItems.Count == 0)
            {
                _selectedFaction = null;
                ClearFactionForm();
            }
        }

        // -----------------------------------------------------------------------
        // Faction Form Population
        // -----------------------------------------------------------------------

        private void PopulateFactionForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedFaction == null)
            {
                ClearFactionForm();
                return;
            }

            txtFactionName.Text = _selectedFaction.Name;
            txtFactionDescription.Text = _selectedFaction.Description;
            SetFactionDetailEnabled(true);
            sw.Stop();
            Log.Info("PopulateFactionForm PERF: total={0}ms", sw.ElapsedMilliseconds);
            sw.Stop();
            Log.Info("PERF PopulateFactionForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearFactionForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtFactionName.Text = string.Empty;
            txtFactionDescription.Text = string.Empty;
            SetFactionDetailEnabled(false);
        }

        private void SetFactionDetailEnabled(bool enabled)
        {
            txtFactionName.Enabled = enabled;
            txtFactionDescription.Enabled = enabled;
            cmdSaveFaction.Enabled = enabled;
        }

        // -----------------------------------------------------------------------
        // Faction CRUD
        // -----------------------------------------------------------------------

        private void CmdNewFaction_Click(object sender, EventArgs e)
        {
            var faction = new Faction
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Faction",
                Description = string.Empty
            };

            playerContext.AddFaction(faction);
            playerContext.WriteContext();
            _selectedFaction = faction;
            PopulateFactionList();
            PopulateFactionForm();
            PopulateCharFactionCombo();
        }

        private void CmdDeleteFaction_Click(object sender, EventArgs e)
        {
            if (_selectedFaction == null) return;

            var refCounter = new FactionReferenceCounter(
                playerContext.ExternalCharacterList,
                playerContext.PlayerProfileList,
                playerContext.MarketTransactionList);
            int refs = refCounter.CountReferences(_selectedFaction.UUID);

            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format("This faction is referenced by {0} item(s). Cannot delete.", refs),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete faction '{0}'?", _selectedFaction.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.RemoveFaction(_selectedFaction);
            playerContext.WriteContext();
            _selectedFaction = null;
            PopulateFactionList();
            ClearFactionForm();
            PopulateCharFactionCombo();
        }

        private void CmdSaveFaction_Click(object sender, EventArgs e)
        {
            if (_selectedFaction == null) return;

            string name = txtFactionName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Faction name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedFaction.Name = name;
            _selectedFaction.Description = txtFactionDescription.Text;
            playerContext.WriteContext();
            PopulateFactionList();
            PopulateCharFactionCombo();
            PopulateCharacterList();
            Log.Info("Saved faction '{0}'", _selectedFaction.Name);
        }

        // -----------------------------------------------------------------------
        // Faction Data Model Write-Through
        // -----------------------------------------------------------------------

        private void TxtFactionName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedFaction == null) return;
            _selectedFaction.Name = txtFactionName.Text;
        }

        private void TxtFactionDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedFaction == null) return;
            _selectedFaction.Description = txtFactionDescription.Text;
        }

        // -----------------------------------------------------------------------
        // Character List
        // -----------------------------------------------------------------------

        private void PopulateCharacterList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedCharacter?.UUID;
            lvwCharacters.Items.Clear();

            var characters = playerContext.ExternalCharacterList.ToList();
            string filter = txtCharFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                characters = characters.Where(c => c.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            characters = CollectionSortHelper.OrderExternalCharacters(characters).ToList();

            foreach (var character in characters)
            {
                string factionName = string.Empty;
                if (!string.IsNullOrEmpty(character.FactionUUID))
                {
                    var faction = playerContext.FactionList.FirstOrDefault(f => f.UUID == character.FactionUUID);
                    if (faction != null) factionName = faction.Name;
                }

                var item = new ListViewItem(character.Name) { Tag = character };
                item.SubItems.Add(factionName);
                lvwCharacters.Items.Add(item);
                if (character.UUID == selectedUUID)
                    item.Selected = true;
            }

            sw.Stop();
            Log.Info(
                "PopulateCharacterList PERF: total={0}ms items={1}",
                sw.ElapsedMilliseconds,
                characters.Count);
            sw.Stop();
            Log.Info("PERF PopulateCharacterList: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtCharFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateCharacterList();
        }

        private void LvwCharacters_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ExternalCharacter character)
            {
                _selectedCharacter = character;
                PopulateCharacterForm();
            }
            else if (!e.IsSelected && lvwCharacters.SelectedItems.Count == 0)
            {
                _selectedCharacter = null;
                ClearCharacterForm();
            }
        }

        // -----------------------------------------------------------------------
        // Character Form Population
        // -----------------------------------------------------------------------

        private void PopulateCharacterForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedCharacter == null)
            {
                ClearCharacterForm();
                return;
            }

            txtCharName.Text = _selectedCharacter.Name;
            PopulateCharFactionCombo();

            // Select the character's faction in the combo
            if (!string.IsNullOrEmpty(_selectedCharacter.FactionUUID))
            {
                for (int i = 0; i < cmbCharFaction.Items.Count; i++)
                {
                    if (cmbCharFaction.Items[i] is FactionComboItem fci && fci.UUID == _selectedCharacter.FactionUUID)
                    {
                        cmbCharFaction.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                cmbCharFaction.SelectedIndex = 0; // "(none)"
            }

            SetCharDetailEnabled(true);
            sw.Stop();
            Log.Info("PopulateCharacterForm PERF: total={0}ms", sw.ElapsedMilliseconds);
            sw.Stop();
            Log.Info("PERF PopulateCharacterForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearCharacterForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtCharName.Text = string.Empty;
            PopulateCharFactionCombo();
            cmbCharFaction.SelectedIndex = -1;
            SetCharDetailEnabled(false);
        }

        private void SetCharDetailEnabled(bool enabled)
        {
            txtCharName.Enabled = enabled;
            cmbCharFaction.Enabled = enabled;
            cmdSaveChar.Enabled = enabled;
        }

        private void PopulateCharFactionCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = null;
            if (cmbCharFaction.SelectedItem is FactionComboItem selected)
                selectedUUID = selected.UUID;

            cmbCharFaction.Items.Clear();
            cmbCharFaction.Items.Add(new FactionComboItem("(none)", string.Empty));

            foreach (var faction in CollectionSortHelper.OrderFactions(playerContext.FactionList))
            {
                cmbCharFaction.Items.Add(new FactionComboItem(faction.Name, faction.UUID));
            }

            // Restore selection
            if (selectedUUID != null)
            {
                for (int i = 0; i < cmbCharFaction.Items.Count; i++)
                {
                    if (cmbCharFaction.Items[i] is FactionComboItem fci && fci.UUID == selectedUUID)
                    {
                        cmbCharFaction.SelectedIndex = i;
                        return;
                    }
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateCharFactionCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Character CRUD
        // -----------------------------------------------------------------------

        private void CmdNewChar_Click(object sender, EventArgs e)
        {
            var character = new ExternalCharacter
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Character",
                FactionUUID = string.Empty
            };

            playerContext.AddExternalCharacter(character);
            playerContext.WriteContext();
            _selectedCharacter = character;
            PopulateCharacterList();
            PopulateCharacterForm();
        }

        private void CmdDeleteChar_Click(object sender, EventArgs e)
        {
            if (_selectedCharacter == null) return;

            var result = MessageBox.Show(
                string.Format("Delete character '{0}'?", _selectedCharacter.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.RemoveExternalCharacter(_selectedCharacter);
            playerContext.WriteContext();
            _selectedCharacter = null;
            PopulateCharacterList();
            ClearCharacterForm();
            // Refresh faction refs since an external character was removed
            PopulateFactionList();
        }

        private void CmdSaveChar_Click(object sender, EventArgs e)
        {
            if (_selectedCharacter == null) return;

            string name = txtCharName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Character name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedCharacter.Name = name;

            if (cmbCharFaction.SelectedItem is FactionComboItem fci)
                _selectedCharacter.FactionUUID = fci.UUID;
            else
                _selectedCharacter.FactionUUID = string.Empty;

            playerContext.WriteContext();
            PopulateCharacterList();
            PopulateFactionList(); // Refresh refs
            Log.Info("Saved character '{0}'", _selectedCharacter.Name);
        }

        // -----------------------------------------------------------------------
        // Character Data Model Write-Through
        // -----------------------------------------------------------------------

        private void TxtCharName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedCharacter == null) return;
            _selectedCharacter.Name = txtCharName.Text;
        }

        private void CmbCharFaction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedCharacter == null) return;
            if (cmbCharFaction.SelectedItem is FactionComboItem fci)
                _selectedCharacter.FactionUUID = fci.UUID;
        }

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

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

            _selectedFaction = null;
            _selectedCharacter = null;
            PopulateFactionList();
            ClearFactionForm();
            PopulateCharacterList();
            ClearCharacterForm();
        }

        // -----------------------------------------------------------------------
        // Helper class for faction combo items
        // -----------------------------------------------------------------------

        private class FactionComboItem
        {
            public FactionComboItem(string displayName, string uuid)
            {
                DisplayName = displayName;
                UUID = uuid;
            }

            public string DisplayName { get; }
            public string UUID { get; }

            public override string ToString() => DisplayName;
        }
    }
}