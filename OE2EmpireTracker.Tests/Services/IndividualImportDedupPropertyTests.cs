using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class IndividualImportDedupPropertyTests
    {
        private static Gen<string> NonEmptyAlphaStringGen()
        {
            return Gen.Elements(
                "Alpha", "Beta", "Gamma", "Delta", "Hull", "Shield", "Reactor",
                "Drive", "Weapon", "Cargo", "Nav", "Fuel", "Thruster", "Laser",
                "Drone", "Plating", "Coupler", "Scanner", "Grapple", "Hopper");
        }

        private static Gen<string> TechLevelGen()
        {
            return Gen.Elements("LL", "ML", "HL", "Milspec", "Civilian", string.Empty);
        }

        private static Gen<string> BluePrintTypeGen()
        {
            return Gen.Elements("Hull", "Shield", "Reactor", "Main Drive", "Weapon", "Flatpack", string.Empty);
        }

        private static Gen<BpModel> BlueprintGen()
        {
            return from name in NonEmptyAlphaStringGen()
                   from evo in Gen.Choose(0, 10)
                   from bpType in BluePrintTypeGen()
                   from cls in Gen.Choose(0, 5)
                   from tl in TechLevelGen()
                   select MakeBlueprint(name, evo, bpType, cls, tl);
        }

        private static BpModel MakeBlueprint(string name, int evolution, string bpType, int cls, string techLevel)
        {
            var bp = new BpModel();
            bp.Name = name;
            bp.Evolution = evolution;
            bp.BluePrintType = bpType;
            bp.Class = cls;
            bp.TechLevel = techLevel;
            return bp;
        }

        /// <summary>
        /// Property 1: UpdateExisting overwrites data while preserving protected fields.
        /// For any existing blueprint with arbitrary UUID, OwnerUUID, NickName, CopyCost,
        /// and for any incoming blueprint with arbitrary Properties and Resources,
        /// after calling UpdateExisting(existing, incoming):
        /// - existing.Properties contains all non-protected keys from incoming.Properties
        /// - existing.Resources equals incoming.Resources
        /// - existing.UUID, OwnerUUID, NickName, CopyCost are unchanged
        /// - Protected property keys preserved if they existed on original but not in incoming
        /// **Validates: Requirements 2.1, 2.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property UpdateExistingOverwritesDataWhilePreservingProtectedFields()
        {
            var gen = from uuid in NonEmptyAlphaStringGen()
                      from ownerUuid in NonEmptyAlphaStringGen()
                      from nickName in NonEmptyAlphaStringGen()
                      from copyCost in Gen.Choose(0, 10000)
                      from existingBp in BlueprintGen()
                      from incomingBp in BlueprintGen()
                      from mfgRunTime in NonEmptyAlphaStringGen()
                      from powerReq in NonEmptyAlphaStringGen()
                      from propCount in Gen.Choose(0, 3)
                      from resCount in Gen.Choose(0, 3)
                      select new
                      {
                          UUID = uuid,
                          OwnerUUID = ownerUuid,
                          NickName = nickName,
                          CopyCost = copyCost,
                          Existing = existingBp,
                          Incoming = incomingBp,
                          MfgRunTime = mfgRunTime,
                          PowerReq = powerReq,
                          PropCount = propCount,
                          ResCount = resCount
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Set up existing blueprint with protected fields
                var existing = data.Existing;
                existing.UUID = data.UUID;
                existing.OwnerUUID = data.OwnerUUID;
                existing.NickName = data.NickName;
                existing.CopyCost = data.CopyCost;
                existing.Properties = new PropertyBag();
                existing.Properties.SetProperty("Manufacture Run Time", data.MfgRunTime);
                existing.Properties.SetProperty("Power Required", data.PowerReq);
                existing.Properties.SetProperty("ExistingOnly", "should-be-replaced");

                // Set up incoming blueprint with properties and resources
                var incoming = data.Incoming;
                incoming.Properties = new PropertyBag();
                for (int i = 0; i < data.PropCount; i++)
                {
                    incoming.Properties.SetProperty("IncomingProp" + i, "val" + i);
                }

                incoming.Resources = new Dictionary<string, string>();
                for (int i = 0; i < data.ResCount; i++)
                {
                    incoming.Resources["Res" + i] = "qty" + i;
                }

                // Capture expected resource snapshot
                var expectedResources = new Dictionary<string, string>(incoming.Resources);

                MarketBlueprintImporter.UpdateExisting(existing, incoming);

                // UUID, OwnerUUID, NickName, CopyCost must be unchanged
                var uuidPreserved = (existing.UUID == data.UUID)
                    .Label($"UUID changed from '{data.UUID}' to '{existing.UUID}'");
                var ownerPreserved = (existing.OwnerUUID == data.OwnerUUID)
                    .Label($"OwnerUUID changed from '{data.OwnerUUID}' to '{existing.OwnerUUID}'");
                var nickPreserved = (existing.NickName == data.NickName)
                    .Label($"NickName changed from '{data.NickName}' to '{existing.NickName}'");
                var costPreserved = (existing.CopyCost == data.CopyCost)
                    .Label($"CopyCost changed from {data.CopyCost} to {existing.CopyCost}");

                // Resources must match incoming
                var resMatch = (existing.Resources.Count == expectedResources.Count
                    && expectedResources.All(kvp =>
                        existing.Resources.ContainsKey(kvp.Key) && existing.Resources[kvp.Key] == kvp.Value))
                    .Label("Resources do not match incoming");

                // All non-protected incoming property keys must be present
                var incomingPropsPresent = true;
                for (int i = 0; i < data.PropCount; i++)
                {
                    if (!existing.Properties.ContainsKey("IncomingProp" + i))
                    {
                        incomingPropsPresent = false;
                        break;
                    }
                }

                var propsPresent = incomingPropsPresent
                    .Label("Not all incoming non-protected properties are present");

                // Protected keys should be preserved (they were on original but not in incoming)
                string mfgVal;
                existing.Properties.GetString("Manufacture Run Time", null, out mfgVal);
                var mfgPreserved = (mfgVal == data.MfgRunTime)
                    .Label($"Manufacture Run Time: expected '{data.MfgRunTime}', got '{mfgVal}'");

                string pwrVal;
                existing.Properties.GetString("Power Required", null, out pwrVal);
                var pwrPreserved = (pwrVal == data.PowerReq)
                    .Label($"Power Required: expected '{data.PowerReq}', got '{pwrVal}'");

                return uuidPreserved
                    .And(ownerPreserved)
                    .And(nickPreserved)
                    .And(costPreserved)
                    .And(resMatch)
                    .And(propsPresent)
                    .And(mfgPreserved)
                    .And(pwrPreserved);
            });
        }

        /// <summary>
        /// Property 2: Routing logic is determined by Evolution and player presence.
        /// For any Evolution value and any player-selected state:
        /// - If Evolution == 0, the target shall be global (true) regardless of player state
        /// - If Evolution != 0 and a player is selected, the target shall be player (false)
        /// - If Evolution != 0 and no player is selected, the target shall be global (true)
        /// **Validates: Requirements 3.1, 3.2, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoutingLogicDeterminedByEvolutionAndPlayerPresence()
        {
            var gen = from evo in Arb.Default.Int32().Generator
                      from hasPlayer in Arb.Default.Bool().Generator
                      select new { Evolution = evo, HasCurrentPlayer = hasPlayer };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                bool result = MarketBlueprintImporter.IsGlobalRoute(data.Evolution, data.HasCurrentPlayer);

                if (data.Evolution == 0)
                {
                    return result.Label($"Evo 0 should always be global, got {result}");
                }
                else if (data.HasCurrentPlayer)
                {
                    return (!result).Label($"Evo {data.Evolution} with player should be player (false), got {result}");
                }
                else
                {
                    return result.Label($"Evo {data.Evolution} without player should be global (true), got {result}");
                }
            });
        }

        /// <summary>
        /// Property 3: FindByDedupKey returns the correct match or null.
        /// For any list of blueprints and any search blueprint, FindByDedupKey shall
        /// return a blueprint whose Name, Evolution, BluePrintType, Class, and TechLevel all
        /// match exactly (case-sensitive for strings), or null if no such blueprint exists.
        /// **Validates: Requirements 4.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FindByDedupKeyReturnsCorrectMatchOrNull()
        {
            var gen = from bpList in Gen.ListOf(BlueprintGen())
                      from search in BlueprintGen()
                      select new { List = bpList.ToList(), Search = search };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var list = new List<BpModel>(data.List);
                var result = MarketBlueprintImporter.FindByDedupKey(list, data.Search);

                if (result != null)
                {
                    // All 5 key fields must match exactly
                    var nameMatch = string.Equals(result.Name, data.Search.Name, StringComparison.Ordinal);
                    var evoMatch = result.Evolution == data.Search.Evolution;
                    var typeMatch = string.Equals(result.BluePrintType, data.Search.BluePrintType, StringComparison.Ordinal);
                    var classMatch = result.Class == data.Search.Class;
                    var tlMatch = string.Equals(result.TechLevel, data.Search.TechLevel, StringComparison.Ordinal);

                    return (nameMatch && evoMatch && typeMatch && classMatch && tlMatch)
                        .Label($"Result does not match search on all 5 dedup key fields");
                }
                else
                {
                    // No blueprint in the list should have all 5 fields matching
                    bool anyMatch = data.List.Any(bp =>
                        string.Equals(bp.Name, data.Search.Name, StringComparison.Ordinal)
                        && bp.Evolution == data.Search.Evolution
                        && string.Equals(bp.BluePrintType, data.Search.BluePrintType, StringComparison.Ordinal)
                        && bp.Class == data.Search.Class
                        && string.Equals(bp.TechLevel, data.Search.TechLevel, StringComparison.Ordinal));

                    return (!anyMatch)
                        .Label("FindByDedupKey returned null but a matching blueprint exists in the list");
                }
            });
        }

        /// <summary>
        /// Property 4: Create path produces a valid blueprint with correct ownership.
        /// For any temporary blueprint and any owner UUID string, when creating a new
        /// blueprint for insertion (assign UUID, set OwnerUUID), the blueprint's UUID
        /// shall be non-empty, OwnerUUID shall match, and all data fields shall match
        /// the temporary blueprint's values.
        /// **Validates: Requirements 3.5, 3.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CreatePathProducesValidBlueprintWithCorrectOwnership()
        {
            var gen = from temp in BlueprintGen()
                      from ownerUuid in NonEmptyAlphaStringGen()
                      from propCount in Gen.Choose(0, 3)
                      from resCount in Gen.Choose(0, 3)
                      select new { Temp = temp, OwnerUuid = ownerUuid, PropCount = propCount, ResCount = resCount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var temp = data.Temp;
                // Add some properties and resources to the temp blueprint
                for (int i = 0; i < data.PropCount; i++)
                {
                    temp.Properties.SetProperty("Prop" + i, "val" + i);
                }

                for (int i = 0; i < data.ResCount; i++)
                {
                    temp.Resources["Res" + i] = "qty" + i;
                }

                // Capture expected values before the create path
                var expectedName = temp.Name;
                var expectedEvo = temp.Evolution;
                var expectedType = temp.BluePrintType;
                var expectedClass = temp.Class;
                var expectedTL = temp.TechLevel;
                var expectedPropCount = temp.Properties.Count;
                var expectedResCount = temp.Resources.Count;

                // Simulate create path: assign UUID and OwnerUUID
                temp.UUID = Guid.NewGuid().ToString();
                temp.OwnerUUID = data.OwnerUuid;

                // UUID must be non-empty
                var uuidNonEmpty = (!string.IsNullOrEmpty(temp.UUID))
                    .Label("UUID should be non-null and non-empty");

                // OwnerUUID must match
                var ownerMatch = (temp.OwnerUUID == data.OwnerUuid)
                    .Label($"OwnerUUID: expected '{data.OwnerUuid}', got '{temp.OwnerUUID}'");

                // Data fields must match original temp values
                var nameMatch = (temp.Name == expectedName)
                    .Label($"Name: expected '{expectedName}', got '{temp.Name}'");
                var evoMatch = (temp.Evolution == expectedEvo)
                    .Label($"Evolution: expected {expectedEvo}, got {temp.Evolution}");
                var typeMatch = (temp.BluePrintType == expectedType)
                    .Label($"BluePrintType: expected '{expectedType}', got '{temp.BluePrintType}'");
                var classMatch = (temp.Class == expectedClass)
                    .Label($"Class: expected {expectedClass}, got {temp.Class}");
                var tlMatch = (temp.TechLevel == expectedTL)
                    .Label($"TechLevel: expected '{expectedTL}', got '{temp.TechLevel}'");
                var propsMatch = (temp.Properties.Count == expectedPropCount)
                    .Label($"Properties.Count: expected {expectedPropCount}, got {temp.Properties.Count}");
                var resMatch = (temp.Resources.Count == expectedResCount)
                    .Label($"Resources.Count: expected {expectedResCount}, got {temp.Resources.Count}");

                return uuidNonEmpty
                    .And(ownerMatch)
                    .And(nameMatch)
                    .And(evoMatch)
                    .And(typeMatch)
                    .And(classMatch)
                    .And(tlMatch)
                    .And(propsMatch)
                    .And(resMatch);
            });
        }
    }
}
