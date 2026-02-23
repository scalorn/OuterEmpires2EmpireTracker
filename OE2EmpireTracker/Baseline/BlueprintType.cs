using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Baseline.BlueprintField;

namespace OE2EmpireTracker.Baseline
{
    public class BlueprintType
    {
        public enum BluePrintTypeEnum
        {
            None = 0,
            CargoPod,
            FuelTank,
            GERTYDroneRack,
            Hull,
            HullReinforcement,
            HullSealantInjectionUnit,
            JumpDrive,
            MainDrive,
            NavComp,
            Reactor,
            Shield,
            SystemObjectScanner,
            Thruster,
            UniversalCoupler,
            BeamerSmall,
            BeamerMedium,
            BeamerLarge,
            CoilGunSmall,
            CoilGunMedium,
            CoilGunLarge,
            MissileLauncherSmall,
            MissileLauncherMedium,
            MissileLauncherLarge,
            RailgunSmall,
            RailgunMedium,
            RailgunLarge,
            TorpedoLauncherSmall,
            TorpedoLauncherMedium,
            TorpedoLauncherLarge
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public List<BlueprintFieldEnum> Fields { get; set; }
    }
}
