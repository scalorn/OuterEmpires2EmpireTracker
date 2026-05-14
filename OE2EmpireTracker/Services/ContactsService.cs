using System;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Faction and ExternalCharacter mutation.
    /// The form and ViewModel never touch the entities directly.
    /// </summary>
    public class ContactsService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public ContactsService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>Applies changes from the update request to an existing faction.</summary>
        public ReadOnlyFaction UpdateFaction(string uuid, FactionUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var faction = _playerContext.FindMutableFaction(uuid);
            if (faction == null)
            {
                throw new InvalidOperationException("Faction not found: " + uuid);
            }

            Log.Info("ContactsService.UpdateFaction: UUID={0} name='{1}' -> '{2}'", uuid, faction.Name, request.Name);

            faction.Name = request.Name;
            faction.Description = request.Description;

            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(uuid);
            return new ReadOnlyFaction(faction);
        }

        /// <summary>Creates a new faction with a generated UUID.</summary>
        public ReadOnlyFaction CreateFaction(FactionCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var faction = new Faction();
            faction.UUID = Guid.NewGuid().ToString();
            faction.Name = string.IsNullOrWhiteSpace(request.Name) ? "New Faction" : request.Name;
            faction.Description = request.Description ?? string.Empty;

            Log.Info("ContactsService.CreateFaction: name='{0}' UUID={1}", faction.Name, faction.UUID);

            _playerContext.AddFaction(faction);
            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(faction.UUID);
            return new ReadOnlyFaction(faction);
        }

        /// <summary>Removes a faction. No-op if UUID is empty or not found.</summary>
        public void DeleteFaction(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var faction = _playerContext.FindMutableFaction(uuid);
            if (faction == null)
            {
                return;
            }

            Log.Info("ContactsService.DeleteFaction: UUID={0} name='{1}'", uuid, faction.Name);

            _playerContext.RemoveFaction(faction);
            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(uuid);
        }

        /// <summary>Applies changes from the update request to an existing external character.</summary>
        public ReadOnlyExternalCharacter UpdateCharacter(string uuid, ExternalCharacterUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var character = _playerContext.FindMutableExternalCharacter(uuid);
            if (character == null)
            {
                throw new InvalidOperationException("ExternalCharacter not found: " + uuid);
            }

            Log.Info("ContactsService.UpdateCharacter: UUID={0} name='{1}' -> '{2}'", uuid, character.Name, request.Name);

            string previousFactionUUID = character.FactionUUID ?? string.Empty;
            character.Name = request.Name;
            character.FactionUUID = request.FactionUUID;

            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(uuid);

            // Prompt sharing preferences when character first joins a faction
            PromptSharingPreferencesIfJoinedFaction(previousFactionUUID, request.FactionUUID);

            return new ReadOnlyExternalCharacter(character);
        }

        /// <summary>Creates a new external character with a generated UUID.</summary>
        public ReadOnlyExternalCharacter CreateCharacter(ExternalCharacterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var character = new ExternalCharacter();
            character.UUID = Guid.NewGuid().ToString();
            character.Name = string.IsNullOrWhiteSpace(request.Name) ? "New Character" : request.Name;
            character.FactionUUID = request.FactionUUID ?? string.Empty;

            Log.Info("ContactsService.CreateCharacter: name='{0}' UUID={1}", character.Name, character.UUID);

            _playerContext.AddExternalCharacter(character);
            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(character.UUID);
            return new ReadOnlyExternalCharacter(character);
        }

        /// <summary>Removes an external character. No-op if UUID is empty or not found.</summary>
        public void DeleteCharacter(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var character = _playerContext.FindMutableExternalCharacter(uuid);
            if (character == null)
            {
                return;
            }

            Log.Info("ContactsService.DeleteCharacter: UUID={0} name='{1}'", uuid, character.Name);

            _playerContext.RemoveExternalCharacter(character);
            _playerContext.WriteContext();
            _playerContext.OnContactDataChanged(uuid);
        }

        /// <summary>
        /// Prompts the user to configure sharing preferences when a character first joins a faction.
        /// Triggered when FactionUUID changes from empty to non-empty.
        /// </summary>
        private static void PromptSharingPreferencesIfJoinedFaction(string previousFactionUUID, string newFactionUUID)
        {
            bool wasInFaction = !string.IsNullOrEmpty(previousFactionUUID);
            bool isNowInFaction = !string.IsNullOrEmpty(newFactionUUID);

            if (wasInFaction || !isNowInFaction)
            {
                return;
            }

            // Character just joined a faction — prompt sharing preferences
            if (PlayerContext.SuppressUI)
            {
                return;
            }

            Log.Info("Character joined faction {0} — prompting sharing preferences", newFactionUUID);

            var result = System.Windows.Forms.MessageBox.Show(
                "You've joined a faction. Would you like to configure what data to share with faction members?",
                "Sharing Preferences",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Sharing configuration will be available in a future update.\n\n" +
                    "By default, your data is private. You can configure sharing later in Preferences.",
                    "Sharing Preferences",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
        }
    }
}
