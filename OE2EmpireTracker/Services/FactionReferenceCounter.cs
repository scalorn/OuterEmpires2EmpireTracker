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
        private readonly Dictionary<string, int> _characterMap;
        private readonly Dictionary<string, int> _profileMap;

        public FactionReferenceCounter(
            IEnumerable<ExternalCharacter> externalCharacters,
            IEnumerable<PlayerProfile> playerProfiles,
            IEnumerable<MarketTransaction> marketTransactions)
        {
            var charList = externalCharacters ?? Enumerable.Empty<ExternalCharacter>();
            var profileList = playerProfiles ?? Enumerable.Empty<PlayerProfile>();

            _characterMap = new Dictionary<string, int>();
            foreach (var ec in charList)
            {
                if (!string.IsNullOrEmpty(ec.FactionUUID))
                {
                    _characterMap.TryGetValue(ec.FactionUUID, out int c);
                    _characterMap[ec.FactionUUID] = c + 1;
                }
            }

            _profileMap = new Dictionary<string, int>();
            foreach (var pp in profileList)
            {
                if (!string.IsNullOrEmpty(pp.FactionUUID))
                {
                    _profileMap.TryGetValue(pp.FactionUUID, out int c);
                    _profileMap[pp.FactionUUID] = c + 1;
                }
            }
        }

        /// <summary>
        /// Returns the number of entities referencing the given faction UUID.
        /// Counts ExternalCharacter.FactionUUID and PlayerProfile.FactionUUID matches.
        /// </summary>
        public int CountReferences(string factionUUID)
        {
            if (string.IsNullOrEmpty(factionUUID))
                return 0;

            _characterMap.TryGetValue(factionUUID, out int charCount);
            _profileMap.TryGetValue(factionUUID, out int profileCount);

            return charCount + profileCount;
        }
    }
}
