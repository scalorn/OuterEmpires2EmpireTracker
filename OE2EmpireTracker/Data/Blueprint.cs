using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OE2EmpireTracker.Data
{
    public class Blueprint : Item
    {
        //[NotMapped]
        public string baseBlueprintUUID { get; set; }

        public string BluePrintType { get; set; }

        //[NotMapped]
        public int Evolution { get; set; }

        //[NotMapped]
        public string TechLevel { get; set; }

        //[NotMapped]
        //public int ManufactureRunTime { get; set; }

        //[NotMapped]
        public int Class { get; set; }

        public int CopyCost { get; set; }

        //[NotMapped]
        //public int MaxAllowedOnShip { get; set; }

        public PropertyBag Properties { get; set; }
        public Dictionary<string, string> Resources { get; set; }

        //public KeyValuePair<int, int> Mass { get; set; }
        //public KeyValuePair<int, int> CargoVolumeSize { get; set; }
        //public KeyValuePair<int, int> PowerRequired { get; set; }
        //public KeyValuePair<int, int> Health { get; set; }
        //public KeyValuePair<int, int> EngCapacityRequired { get; set; }
        //public KeyValuePair<double, double> WearAndTearRate { get; set; }
        //public KeyValuePair<double, double> MaximumDamageRepairPercentage { get; set; }

        // Hulls
        //public KeyValuePair<int, int> CargoCapacity {  get; set; }
        //public KeyValuePair<int, int> FuelCapacity { get; set; }
        //public KeyValuePair<int, int> LargeWeaponMounts { get; set; }
        //public KeyValuePair<int, int> MediumWeaponMounts { get; set; }
        //public KeyValuePair<int, int> SmallWeaponMounts { get; set; }
        //public KeyValuePair<int, int> EnergyDefense { get; set; }
        //public KeyValuePair<int, int> KineticDamageDefense { get; set; }
        //public KeyValuePair<int, int> MissileDamageDefense { get; set; }
        //public KeyValuePair<int, int> NumberOfCrewSupported { get; set; }

        // Cargo Pods
        //public KeyValuePair<int, int> AdditionalCargoCapacity { get; set; }

        // Fuel Pods
        //public KeyValuePair<int, int> AdditionalFuelCapacity { get; set; }

        // Thrusters
//        public KeyValuePair<double, double> RotationalThrust { get; set; }

        // Main Drives
        //public KeyValuePair<double, double> AccelerationRate { get; set; }

        // Reactor Cores
        //public KeyValuePair<double, double> PowerGenerated { get; set; }
        //public KeyValuePair<double, double> PowerRegenerationRate { get; set; }

        // Jump Drives
        //public KeyValuePair<double, double> FuelUsed { get; set; }
        //public KeyValuePair<double, double> MaxJumpDistance { get; set; }

        // For Navigation Computers.
        //public KeyValuePair<int, int> JumpChargeTime { get; set; }

        // Shields
        //public KeyValuePair<int, int> PowerDrawPerSecond { get; set; }
        //public KeyValuePair<int, int> EnergyDefence { get; set; }
        //public KeyValuePair<int, int> ShieldHitPoints { get; set; }
        //public KeyValuePair<int, int> ShieldRegenPerSecond { get; set; }

        // System Object Scanner
        //public KeyValuePair<int, int> SensorAbundanceFactor { get; set; }
        //public KeyValuePair<double, double> PurityModifier { get; set; }
        //public KeyValuePair<int, int> ScanLevel { get; set; }
        //public KeyValuePair<int, int> MaterialFocus { get; set; }
        //public KeyValuePair<int, int> MaterialFocusBonus { get; set; }

        // Universal Coupler
        //public KeyValuePair<int, int> FuelTransferRate { get; set; }

        [JsonIgnore]
        public override string ExtendedName {
            get {
                if (UUID == null)
                {
                    return string.Empty;
                }
                string extendedName = Name;
                if (Evolution > 0) {
                    extendedName += " Ev(" + Evolution + ")";
                }
                if (TechLevel != null)
                {
                    extendedName += " (" + TechLevel + ")";
                }
                if (!String.IsNullOrEmpty(NickName))
                {
                    extendedName += " [" + NickName + "]";
                }
                return extendedName;
            }
        }


        public Blueprint(string name /*, int quantity*/) : base(Data.ItemType.ItemTypeEnum.Blueprint, name /* , quantity */)
        {
            Properties = new PropertyBag();
            Resources = new Dictionary<string, string>();
        }

        public Blueprint() : base(Data.ItemType.ItemTypeEnum.Blueprint, "")
        {
            Properties = new PropertyBag();
            Resources = new Dictionary<string, string>();
        }

    }
}
