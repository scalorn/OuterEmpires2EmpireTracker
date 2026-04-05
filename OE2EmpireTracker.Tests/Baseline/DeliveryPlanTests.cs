using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Baseline
{
    [TestFixture]
    public class DeliveryPlanTests
    {
        // -----------------------------------------------------------------------
        // DeliveryPlan Default Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_StopsIsNotNull()
        {
            var plan = new DeliveryPlan();
            Assert.IsNotNull(plan.Stops);
        }

        [Test]
        public void DefaultConstructor_CompletedIsFalse()
        {
            var plan = new DeliveryPlan();
            Assert.IsFalse(plan.Completed);
        }

        [Test]
        public void DefaultConstructor_StringPropertiesAreEmpty()
        {
            var plan = new DeliveryPlan();
            Assert.AreEqual(string.Empty, plan.Name);
            Assert.AreEqual(string.Empty, plan.OwnerUUID);
            Assert.AreEqual(string.Empty, plan.RouteUUID);
        }

        // -----------------------------------------------------------------------
        // DeliveryPlanStop Default Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void PlanStop_DefaultConstructor_ListsAreNotNull()
        {
            var stop = new DeliveryPlanStop();
            Assert.IsNotNull(stop.DropOff);
            Assert.IsNotNull(stop.PickUp);
        }

        [Test]
        public void PlanStop_DefaultConstructor_StopCompletedIsFalse()
        {
            var stop = new DeliveryPlanStop();
            Assert.IsFalse(stop.StopCompleted);
        }

        // -----------------------------------------------------------------------
        // DeliveryItem Defaults and ExtendedName
        // -----------------------------------------------------------------------

        [Test]
        public void DeliveryItem_DefaultConstructor_AllDefaults()
        {
            var item = new DeliveryItem();
            Assert.AreEqual(ItemType.ItemTypeEnum.None, item.ItemType);
            Assert.AreEqual(string.Empty, item.BaseItemTypeID);
            Assert.AreEqual(string.Empty, item.Name);
            Assert.AreEqual(string.Empty, item.ResourcePurity);
            Assert.AreEqual(0, item.Quantity);
            Assert.IsFalse(item.Delivered);
        }

        [Test]
        public void DeliveryItem_ExtendedName_NoPurity_ReturnsName()
        {
            var item = new DeliveryItem { Name = "Iron" };
            Assert.AreEqual("Iron", item.ExtendedName);
        }

        [Test]
        public void DeliveryItem_ExtendedName_EmptyPurity_ReturnsName()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = "" };
            Assert.AreEqual("Iron", item.ExtendedName);
        }

        [Test]
        public void DeliveryItem_ExtendedName_WithPurity_AppendsPurity()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = "High" };
            Assert.AreEqual("Iron (High)", item.ExtendedName);
        }

        [Test]
        public void DeliveryItem_ExtendedName_CommodityNoPurity_ReturnsName()
        {
            var item = new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Name = "Steel Plates"
            };
            Assert.AreEqual("Steel Plates", item.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // JSON Round-Trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_EmptyPlan_Preserved()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Name = "Test Plan",
                OwnerUUID = "p1",
                RouteUUID = "r1",
                Completed = false
            };
            string json = JsonConvert.SerializeObject(plan);
            var restored = JsonConvert.DeserializeObject<DeliveryPlan>(json);
            Assert.AreEqual("plan-1", restored.UUID);
            Assert.AreEqual("Test Plan", restored.Name);
            Assert.AreEqual("p1", restored.OwnerUUID);
            Assert.AreEqual("r1", restored.RouteUUID);
            Assert.IsFalse(restored.Completed);
            Assert.AreEqual(0, restored.Stops.Count);
        }

        [Test]
        public void JsonRoundTrip_CompletedPlan_Preserved()
        {
            var plan = new DeliveryPlan { UUID = "plan-1", Completed = true };
            string json = JsonConvert.SerializeObject(plan);
            var restored = JsonConvert.DeserializeObject<DeliveryPlan>(json);
            Assert.IsTrue(restored.Completed);
        }

        [Test]
        public void JsonRoundTrip_WithItems_Preserved()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop
                    {
                        ColonyUUID = "c1",
                        Sequence = 0,
                        DropOff = new List<DeliveryItem>
                        {
                            new DeliveryItem
                            {
                                ItemType = ItemType.ItemTypeEnum.Resource,
                                BaseItemTypeID = "Iron",
                                Name = "Iron",
                                ResourcePurity = "High",
                                Quantity = 100,
                                Delivered = true
                            }
                        },
                        PickUp = new List<DeliveryItem>
                        {
                            new DeliveryItem
                            {
                                ItemType = ItemType.ItemTypeEnum.Commodity,
                                BaseItemTypeID = "SteelPlates",
                                Name = "Steel Plates",
                                Quantity = 50
                            }
                        }
                    }
                }
            };
            string json = JsonConvert.SerializeObject(plan);
            var restored = JsonConvert.DeserializeObject<DeliveryPlan>(json);
            Assert.AreEqual(1, restored.Stops.Count);
            var stop = restored.Stops[0];
            Assert.AreEqual(1, stop.DropOff.Count);
            Assert.AreEqual(1, stop.PickUp.Count);
            Assert.AreEqual("Iron", stop.DropOff[0].BaseItemTypeID);
            Assert.AreEqual("High", stop.DropOff[0].ResourcePurity);
            Assert.AreEqual(100, stop.DropOff[0].Quantity);
            Assert.IsTrue(stop.DropOff[0].Delivered);
            Assert.AreEqual(ItemType.ItemTypeEnum.Resource, stop.DropOff[0].ItemType);
            Assert.AreEqual("SteelPlates", stop.PickUp[0].BaseItemTypeID);
            Assert.AreEqual(50, stop.PickUp[0].Quantity);
            Assert.IsFalse(stop.PickUp[0].Delivered);
        }

        [Test]
        public void JsonRoundTrip_ItemTypeSerializedAsString()
        {
            var item = new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource };
            string json = JsonConvert.SerializeObject(item);
            Assert.IsTrue(json.Contains("\"Resource\""));
        }

        [Test]
        public void JsonRoundTrip_ExtendedNameNotSerialized()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = "High" };
            string json = JsonConvert.SerializeObject(item);
            Assert.IsFalse(json.Contains("ExtendedName"));
        }

        [Test]
        public void JsonRoundTrip_StopCompleted_Preserved()
        {
            var stop = new DeliveryPlanStop { ColonyUUID = "c1", StopCompleted = true };
            string json = JsonConvert.SerializeObject(stop);
            var restored = JsonConvert.DeserializeObject<DeliveryPlanStop>(json);
            Assert.IsTrue(restored.StopCompleted);
        }

        // -----------------------------------------------------------------------
        // CalculateLoadList
        // -----------------------------------------------------------------------

        [Test]
        public void CalculateLoadList_EmptyPlan_ReturnsEmptyList()
        {
            var plan = new DeliveryPlan();
            var result = plan.CalculateLoadList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CalculateLoadList_DropOffOnly_NeedsPreLoad()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 50 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Steel", result[0].BaseItemTypeID);
            Assert.AreEqual(50, result[0].Quantity);
        }

        [Test]
        public void CalculateLoadList_PickUpBeforeDropOff_NoPreLoad()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                PickUp = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 100 }
                }
            });
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c2",
                Sequence = 1,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 50 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CalculateLoadList_PartialPickUp_PreLoadsShortfall()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                PickUp = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 30 }
                }
            });
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c2",
                Sequence = 1,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 50 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(20, result[0].Quantity);
        }

        [Test]
        public void CalculateLoadList_MultipleStopsDropOff_Aggregates()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 30 }
                }
            });
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c2",
                Sequence = 1,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 20 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(50, result[0].Quantity);
        }

        [Test]
        public void CalculateLoadList_DifferentPurities_SeparateItems()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource, BaseItemTypeID = "Iron", Name = "Iron", ResourcePurity = "High", Quantity = 10 },
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource, BaseItemTypeID = "Iron", Name = "Iron", ResourcePurity = "Low", Quantity = 5 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(2, result.Count);
            var high = result.FirstOrDefault(i => i.ResourcePurity == "High");
            var low = result.FirstOrDefault(i => i.ResourcePurity == "Low");
            Assert.IsNotNull(high);
            Assert.IsNotNull(low);
            Assert.AreEqual(10, high.Quantity);
            Assert.AreEqual(5, low.Quantity);
        }

        [Test]
        public void CalculateLoadList_ResourcePurityPreserved()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource, BaseItemTypeID = "Iron", Name = "Iron", ResourcePurity = "Medium", Quantity = 25 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Medium", result[0].ResourcePurity);
            Assert.AreEqual("Iron (Medium)", result[0].ExtendedName);
        }

        [Test]
        public void CalculateLoadList_PickUpOnly_NoPreLoad()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                PickUp = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource, BaseItemTypeID = "Iron", Name = "Iron", Quantity = 100 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CalculateLoadList_PickUpAfterDropOff_StillNeedsPreLoad()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 50 }
                }
            });
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c2",
                Sequence = 1,
                PickUp = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 100 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(50, result[0].Quantity);
        }

        [Test]
        public void CalculateLoadList_StopsProcessedInSequenceOrder()
        {
            var plan = new DeliveryPlan();
            // Add stops out of order — sequence 1 first, then 0
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c2",
                Sequence = 1,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 50 }
                }
            });
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                PickUp = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 100 }
                }
            });
            // Even though stops are added out of order, sequence 0 (pick-up) should be processed first
            var result = plan.CalculateLoadList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CalculateLoadList_MixedItemTypes_AllTrackedSeparately()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource, BaseItemTypeID = "Iron", Name = "Iron", Quantity = 10 },
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Steel", Name = "Steel", Quantity = 20 },
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Flatpack, BaseItemTypeID = "fp-1", Name = "Reactor", Quantity = 1 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void CalculateLoadList_ResultSortedByName()
        {
            var plan = new DeliveryPlan();
            plan.Stops.Add(new DeliveryPlanStop
            {
                ColonyUUID = "c1",
                Sequence = 0,
                DropOff = new List<DeliveryItem>
                {
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Zinc", Name = "Zinc", Quantity = 5 },
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Alpha", Name = "Alpha", Quantity = 10 },
                    new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Commodity, BaseItemTypeID = "Mid", Name = "Mid", Quantity = 3 }
                }
            });
            var result = plan.CalculateLoadList();
            Assert.AreEqual("Alpha", result[0].Name);
            Assert.AreEqual("Mid", result[1].Name);
            Assert.AreEqual("Zinc", result[2].Name);
        }
    }
}
