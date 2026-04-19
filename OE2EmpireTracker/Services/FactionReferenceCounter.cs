using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a Faction from ExternalCharacters and PlayerProfiles.
    /// Used to prevent deletion of factions that are still in use.
    /// </summary>
    public class FactionReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IEnumerable<ExternalCharacter> _externalCharacters;
        private readonly IEnumerable<PlayerProfile> _playerProfiles;
        private readonly IEnumerable<MarketTransaction> _marketTransactions;

        public FactionReferenceCounter(
            IEnumerable<ExternalCharacter> externalCharacters,
            IEnumerable<PlayerProfile> playerProfiles,
            IEnumerable<MarketTransaction> marketTransactions)
        {
            _externalCharacters = externalCharacters ?? Enumerable.Empty<ExternalCharacter>();
            _playerProfiles = playerProfiles ?? Enumerable.Empty<PlayerProfile>();
            _marketTransactions = marketTransactions ?? Enumerable.Empty<MarketTransaction>();
        }

        /// <summary>
        /// Returns the number of entities referencing the given faction UUID.
        /// Counts ExternalCharacter.FactionUUID and PlayerProfile.FactionUUID matches.
        /// </summary>
        public int CountReferences(string factionUUID)
        {
            if (string.IsNullOrEmpty(factionUUID))
                return 0;

            int charCount = _externalCharacters
                .Count(ec => ec.FactionUUID == factionUUID);

            int profileCount = _playerProfiles
                .Count(pp => pp.FactionUUID == factionUUID);

            return charCount + profileCount;
        }
    }
}