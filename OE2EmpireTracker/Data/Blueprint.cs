using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class Blueprint : Item
    {
        public enum BluePrintTypeEnum
        {
            None,
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

        [Key]
        [Required]
        public override int ID
        {
            get
            {
                return base.ID;
            }
            set
            {
                base.ID = value;
            }
        }

        [NotMapped]
        public int Parentid { get; set; }

        [NotMapped]
        public BluePrintTypeEnum BluePrintType { get; set; }

        [NotMapped]
        public int Evolution { get; set; }

        [NotMapped]
        public int Quality { get; set; }

        [NotMapped]
        public int ManufactureRunTime { get; set; }

        [NotMapped]
        public int ClassID { get; set; }

        [NotMapped]
        public int MaxAllowedOnShip { get; set; }

        public KeyValuePair<int, int> Mass { get; set; }
        public KeyValuePair<int, int> CargoVolumeSize { get; set; }
        public KeyValuePair<int, int> PowerRequired { get; set; }
        public KeyValuePair<int, int> Health { get; set; }
        public KeyValuePair<int, int> EngCapacityRequired { get; set; }
        public KeyValuePair<double, double> WearAndTearRate { get; set; }
        public KeyValuePair<double, double> MaximumDamageRepairPercentage { get; set; }

        // Hulls
        public KeyValuePair<int, int> CargoCapacity {  get; set; }
        public KeyValuePair<int, int> FuelCapacity { get; set; }
        public KeyValuePair<int, int> LargeWeaponMounts { get; set; }
        public KeyValuePair<int, int> MediumWeaponMounts { get; set; }
        public KeyValuePair<int, int> SmallWeaponMounts { get; set; }
        public KeyValuePair<int, int> EnergyDefense { get; set; }
        public KeyValuePair<int, int> KineticDamageDefense { get; set; }
        public KeyValuePair<int, int> MissileDamageDefense { get; set; }
        public KeyValuePair<int, int> NumberOfCrewSupported { get; set; }

        // Cargo Pods
        public KeyValuePair<int, int> AdditionalCargoCapacity { get; set; }

        // Fuel Pods
        public KeyValuePair<int, int> AdditionalFuelCapacity { get; set; }

        // Thrusters
        public KeyValuePair<double, double> RotationalThrust { get; set; }

        // Main Drives
        public KeyValuePair<double, double> AccelerationRate { get; set; }

        // Reactor Cores
        public KeyValuePair<double, double> PowerGenerated { get; set; }
        public KeyValuePair<double, double> PowerRegenerationRate { get; set; }

        // Jump Drives
        public KeyValuePair<double, double> FuelUsed { get; set; }
        public KeyValuePair<double, double> MaxJumpDistance { get; set; }

        // For Navigation Computers.
        public KeyValuePair<int, int> JumpChargeTime { get; set; }

        // Shields
        public KeyValuePair<int, int> PowerDrawPerSecond { get; set; }
        public KeyValuePair<int, int> EnergyDefence { get; set; }
        public KeyValuePair<int, int> ShieldHitPoints { get; set; }
        public KeyValuePair<int, int> ShieldRegenPerSecond { get; set; }

        // System Object Scanner
        public KeyValuePair<int, int> SensorAbundanceFactor { get; set; }
        public KeyValuePair<double, double> PurityModifier { get; set; }
        public KeyValuePair<int, int> ScanLevel { get; set; }
        public KeyValuePair<int, int> MaterialFocus { get; set; }
        public KeyValuePair<int, int> MaterialFocusBonus { get; set; }

        // Universal Coupler
        public KeyValuePair<int, int> FuelTransferRate { get; set; }

        public Blueprint(string name, int quantity) : base(ItemTypeEnum.Blueprint, name, quantity)
        {
        }

        public Blueprint() : base()
        {
        }

    }
}
