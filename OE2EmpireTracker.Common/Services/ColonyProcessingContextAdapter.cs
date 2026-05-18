using System.Linq;
using NLog;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Adapts PlayerContext/EmpireContext singletons to the IColonyProcessingContext
    /// interface used by Colony.ProcessColony().
    /// </summary>
    public class ColonyProcessingContextAdapter : IColonyProcessingContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;

        public ColonyProcessingContextAdapter(PlayerContext playerContext, EmpireContext empireContext)
        {
            _playerContext = playerContext;
            _empireContext = empireContext;
        }

        public ReadOnlyBlueprint FindBlueprint(string uuid) => _playerContext.FindBlueprint(uuid);

        public Survey FindSurvey(string uuid) => _playerContext.FindSurvey(uuid);

        public PlayerProfile FindPlayerProfile(string ownerUUID) =>
            _playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == ownerUUID);

        public BlueprintType FindBlueprintType(string typeName) => _empireContext.FindBlueprintType(typeName);

        public void AddBlueprint(Blueprint blueprint) => _playerContext.AddBlueprint(blueprint);
    }
}
