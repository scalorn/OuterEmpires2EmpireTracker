using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class ContactsViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // --- Faction section ---
        private ReadOnlyFaction _factionOriginal;
        private string _factionUUID;
        private string _factionName = string.Empty;
        private string _factionDescription = string.Empty;

        // --- Character section ---
        private ReadOnlyExternalCharacter _characterOriginal;
        private string _characterUUID;
        private string _characterName = string.Empty;
        private string _characterFactionUUID = string.Empty;

        // --- Faction properties ---

        public string FactionUUID => _factionUUID;

        public ReadOnlyFaction FactionOriginal => _factionOriginal;

        public bool IsFactionNew => _factionOriginal == null;

        public bool IsFactionDirty
        {
            get
            {
                if (_factionOriginal == null)
                {
                    return !string.IsNullOrEmpty(_factionName) || !string.IsNullOrEmpty(_factionDescription);
                }

                if (_factionName != (_factionOriginal.Name ?? string.Empty))
                {
                    return true;
                }

                if (_factionDescription != (_factionOriginal.Description ?? string.Empty))
                {
                    return true;
                }

                return false;
            }
        }

        public string FactionName
        {
            get => _factionName;
            set => _factionName = value;
        }

        public string FactionDescription
        {
            get => _factionDescription;
            set => _factionDescription = value;
        }

        // --- Character properties ---

        public string CharacterUUID => _characterUUID;

        public ReadOnlyExternalCharacter CharacterOriginal => _characterOriginal;

        public bool IsCharacterNew => _characterOriginal == null;

        public bool IsCharacterDirty
        {
            get
            {
                if (_characterOriginal == null)
                {
                    return !string.IsNullOrEmpty(_characterName) || !string.IsNullOrEmpty(_characterFactionUUID);
                }

                if (_characterName != (_characterOriginal.Name ?? string.Empty))
                {
                    return true;
                }

                if (_characterFactionUUID != (_characterOriginal.FactionUUID ?? string.Empty))
                {
                    return true;
                }

                return false;
            }
        }

        public string CharacterName
        {
            get => _characterName;
            set => _characterName = value;
        }

        public string CharacterFactionUUID
        {
            get => _characterFactionUUID;
            set => _characterFactionUUID = value;
        }

        // --- Faction methods ---

        public void LoadFactionFrom(ReadOnlyFaction ro)
        {
            _factionOriginal = ro;
            _factionUUID = ro.UUID;
            _factionName = ro.Name ?? string.Empty;
            _factionDescription = ro.Description ?? string.Empty;
        }

        public void ResetFaction()
        {
            _factionOriginal = null;
            _factionUUID = null;
            _factionName = string.Empty;
            _factionDescription = string.Empty;
        }

        public FactionUpdateRequest BuildFactionUpdateRequest()
        {
            return new FactionUpdateRequest
            {
                Original = _factionOriginal,
                Name = _factionName,
                Description = _factionDescription,
            };
        }

        public FactionCreateRequest BuildFactionCreateRequest()
        {
            return new FactionCreateRequest
            {
                Name = _factionName,
                Description = _factionDescription,
            };
        }

        // --- Character methods ---

        public void LoadCharacterFrom(ReadOnlyExternalCharacter ro)
        {
            _characterOriginal = ro;
            _characterUUID = ro.UUID;
            _characterName = ro.Name ?? string.Empty;
            _characterFactionUUID = ro.FactionUUID ?? string.Empty;
        }

        public void ResetCharacter()
        {
            _characterOriginal = null;
            _characterUUID = null;
            _characterName = string.Empty;
            _characterFactionUUID = string.Empty;
        }

        public ExternalCharacterUpdateRequest BuildCharacterUpdateRequest()
        {
            return new ExternalCharacterUpdateRequest
            {
                Original = _characterOriginal,
                Name = _characterName,
                FactionUUID = _characterFactionUUID,
            };
        }

        public ExternalCharacterCreateRequest BuildCharacterCreateRequest()
        {
            return new ExternalCharacterCreateRequest
            {
                Name = _characterName,
                FactionUUID = _characterFactionUUID,
            };
        }
    }
}
