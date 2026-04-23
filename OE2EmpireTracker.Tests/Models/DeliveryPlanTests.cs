using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(plan.Stops, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_CompletedIsFalse()
        {
            var plan = new DeliveryPlan();
            Assert.That(plan.Completed, Is.False);
        }

        [Test]
        public void DefaultConstructor_StringPropertiesAreEmpty()
        {
            var plan = new DeliveryPlan();
            Assert.That(plan.Name, Is.EqualTo(string.Empty));
            Assert.That(plan.OwnerUUID, Is.EqualTo(string.Empty));
            Assert.That(plan.RouteUUID, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // DeliveryPlanStop Default Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void PlanStop_DefaultConstructor_ListsAreNotNull()
        {
            var stop = new DeliveryPlanStop();
            Assert.That(stop.DropOff, Is.Not.Null);
            Assert.That(stop.PickUp, Is.Not.Null);
        }

        [Test]
        public void PlanStop_DefaultConstructor_StopCompletedIsFalse()
        {
            var stop = new DeliveryPlanStop();
            Assert.That(stop.StopCompleted, Is.False);
        }

        // -----------------------------------------------------------------------
        // DeliveryItem Defaults and ExtendedName
        // -----------------------------------------------------------------------

        [Test]
        public void DeliveryItem_DefaultConstructor_AllDefaults()
        {
            var item = new DeliveryItem();
            Assert.That(item.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.None));
            Assert.That(item.BaseItemTypeID, Is.EqualTo(string.Empty));
            Assert.That(item.Name, Is.EqualTo(string.Empty));
            Assert.That(item.ResourcePurity, Is.EqualTo(string.Empty));
            Assert.That(item.Quantity, Is.EqualTo(0));
            Assert.That(item.Delivered, Is.False);
        }

        [Test]
        public void DeliveryItem_ExtendedName_NoPurity_ReturnsName()
        {
            var item = new DeliveryItem { Name = "Iron" };
            Assert.That(item.ExtendedName, Is.EqualTo("Iron"));
        }

        [Test]
        public void DeliveryItem_ExtendedName_EmptyPurity_ReturnsName()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = string.Empty };
            Assert.That(item.ExtendedName, Is.EqualTo("Iron"));
        }

        [Test]
        public void DeliveryItem_ExtendedName_WithPurity_AppendsPurity()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = "High" };
            Assert.That(item.ExtendedName, Is.EqualTo("Iron (High)"));
        }

        [Test]
        public void DeliveryItem_ExtendedName_CommodityNoPurity_ReturnsName()
        {
            var item = new DeliveryItem
            {
                ItemType = ItemType.ItemTypeEnum.Commodity,
                Name = "Steel Plates"
            };

            Assert.That(item.ExtendedName, Is.EqualTo("Steel Plates"));
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
            Assert.That(restored.UUID, Is.EqualTo("plan-1"));
            Assert.That(restored.Name, Is.EqualTo("Test Plan"));
            Assert.That(restored.OwnerUUID, Is.EqualTo("p1"));
            Assert.That(restored.RouteUUID, Is.EqualTo("r1"));
            Assert.That(restored.Completed, Is.False);
            Assert.That(restored.Stops.Count, Is.EqualTo(0));
        }

        [Test]
        public void JsonRoundTrip_CompletedPlan_Preserved()
        {
            var plan = new DeliveryPlan { UUID = "plan-1", Completed = true };
            string json = JsonConvert.SerializeObject(plan);
            var restored = JsonConvert.DeserializeObject<DeliveryPlan>(json);
            Assert.That(restored.Completed, Is.True);
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
            Assert.That(restored.Stops.Count, Is.EqualTo(1));
            var stop = restored.Stops[0];
            Assert.That(stop.DropOff.Count, Is.EqualTo(1));
            Assert.That(stop.PickUp.Count, Is.EqualTo(1));
            Assert.That(stop.DropOff[0].BaseItemTypeID, Is.EqualTo("Iron"));
            Assert.That(stop.DropOff[0].ResourcePurity, Is.EqualTo("High"));
            Assert.That(stop.DropOff[0].Quantity, Is.EqualTo(100));
            Assert.That(stop.DropOff[0].Delivered, Is.True);
            Assert.That(stop.DropOff[0].ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Resource));
            Assert.That(stop.PickUp[0].BaseItemTypeID, Is.EqualTo("SteelPlates"));
            Assert.That(stop.PickUp[0].Quantity, Is.EqualTo(50));
            Assert.That(stop.PickUp[0].Delivered, Is.False);
        }

        [Test]
        public void JsonRoundTrip_ItemTypeSerializedAsString()
        {
            var item = new DeliveryItem { ItemType = ItemType.ItemTypeEnum.Resource };
            string json = JsonConvert.SerializeObject(item);
            Assert.That(json.Contains("\"Resource\""), Is.True);
        }

        [Test]
        public void JsonRoundTrip_ExtendedNameNotSerialized()
        {
            var item = new DeliveryItem { Name = "Iron", ResourcePurity = "High" };
            string json = JsonConvert.SerializeObject(item);
            Assert.That(json.Contains("ExtendedName"), Is.False);
        }

        [Test]
        public void JsonRoundTrip_StopCompleted_Preserved()
        {
            var stop = new DeliveryPlanStop { ColonyUUID = "c1", StopCompleted = true };
            string json = JsonConvert.SerializeObject(stop);
            var restored = JsonConvert.DeserializeObject<DeliveryPlanStop>(json);
            Assert.That(restored.StopCompleted, Is.True);
        }

        // -----------------------------------------------------------------------
        // CalculateLoadList
        // -----------------------------------------------------------------------

        [Test]
        public void CalculateLoadList_EmptyPlan_ReturnsEmptyList()
        {
            var plan = new DeliveryPlan();
            var result = plan.CalculateLoadList();
            Assert.That(result.Count, Is.EqualTo(0));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].BaseItemTypeID, Is.EqualTo("Steel"));
            Assert.That(result[0].Quantity, Is.EqualTo(50));
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
            Assert.That(result.Count, Is.EqualTo(0));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Quantity, Is.EqualTo(20));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Quantity, Is.EqualTo(50));
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
            Assert.That(result.Count, Is.EqualTo(2));
            var high = result.FirstOrDefault(i => i.ResourcePurity == "High");
            var low = result.FirstOrDefault(i => i.ResourcePurity == "Low");
            Assert.That(high, Is.Not.Null);
            Assert.That(low, Is.Not.Null);
            Assert.That(high.Quantity, Is.EqualTo(10));
            Assert.That(low.Quantity, Is.EqualTo(5));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].ResourcePurity, Is.EqualTo("Medium"));
            Assert.That(result[0].ExtendedName, Is.EqualTo("Iron (Medium)"));
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
            Assert.That(result.Count, Is.EqualTo(0));
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
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Quantity, Is.EqualTo(50));
        }

        [Test]
        public void CalculateLoadList_StopsProcessedInSequenceOrder()
        {
            var plan = new DeliveryPlan();
            // Add stops out of order -- sequence 1 first, then 0
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
            Assert.That(result.Count, Is.EqualTo(0));
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
            Assert.That(result.Count, Is.EqualTo(3));
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
            Assert.That(result[0].Name, Is.EqualTo("Alpha"));
            Assert.That(result[1].Name, Is.EqualTo("Mid"));
            Assert.That(result[2].Name, Is.EqualTo("Zinc"));
        }
    }
}
