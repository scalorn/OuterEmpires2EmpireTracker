using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for DeliveryPlanViewModel LoadFrom and BuildUpdateRequest.
    /// Feature: bl-113-deliveryplan-readonly
    /// Validates: Requirements 4.1, 4.2, 6.1, 6.2
    /// </summary>
    [TestFixture]
    public class DeliveryPlanViewModelTests
    {
        // -----------------------------------------------------------------------
        // LoadFrom copies all scalar fields
        // -----------------------------------------------------------------------

        [Test]
        public void LoadFrom_CopiesAllScalarFields()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-uuid-1",
                Name = "Test Plan",
                OwnerUUID = "owner-1",
                RouteUUID = "route-1",
                ShipUUID = "ship-1",
                Completed = true,
            };
            var ro = new ReadOnlyDeliveryPlan(plan);
            var vm = new DeliveryPlanViewModel();

            vm.LoadFrom(ro);

            Assert.That(vm.UUID, Is.EqualTo("plan-uuid-1"));
            Assert.That(vm.Name, Is.EqualTo("Test Plan"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("owner-1"));
            Assert.That(vm.RouteUUID, Is.EqualTo("route-1"));
            Assert.That(vm.ShipUUID, Is.EqualTo("ship-1"));
            Assert.That(vm.Completed, Is.True);
        }

        [Test]
        public void LoadFrom_NullFields_DefaultToEmpty()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-uuid-2",
                Name = null,
                OwnerUUID = null,
                RouteUUID = null,
                ShipUUID = null,
                Completed = false,
            };
            var ro = new ReadOnlyDeliveryPlan(plan);
            var vm = new DeliveryPlanViewModel();

            vm.LoadFrom(ro);

            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.OwnerUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.RouteUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.ShipUUID, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // LoadFrom deep-copies Stops
        // -----------------------------------------------------------------------

        [Test]
        public void LoadFrom_DeepCopiesStops_NotSameReference()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-deep",
                Name = "Deep Copy Test",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop
                    {
                        ColonyUUID = "c1",
                        Sequence = 0,
                        StopCompleted = false,
                        DestinationType = DestinationType.Colony,
                        DestinationUUID = "dest-1",
                        DropOff = new List<DeliveryItem>
                        {
                            new DeliveryItem
                            {
                                ItemType = ItemType.ItemTypeEnum.Commodity,
                                BaseItemTypeID = "Steel",
                                Name = "Steel",
                                Quantity = 10,
                                Delivered = false,
                            },
                        },
                        PickUp = new List<DeliveryItem>
                        {
                            new DeliveryItem
                            {
                                ItemType = ItemType.ItemTypeEnum.Resource,
                                BaseItemTypeID = "Iron",
                                Name = "Iron",
                                Quantity = 5,
                                ResourcePurity = "High",
                                Delivered = true,
                            },
                        },
                    },
                },
            };
            var ro = new ReadOnlyDeliveryPlan(plan);
            var vm = new DeliveryPlanViewModel();

            vm.LoadFrom(ro);

            // Verify values copied correctly
            Assert.That(vm.Stops.Count, Is.EqualTo(1));
            Assert.That(vm.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(vm.Stops[0].DropOff.Count, Is.EqualTo(1));
            Assert.That(vm.Stops[0].DropOff[0].Name, Is.EqualTo("Steel"));
            Assert.That(vm.Stops[0].PickUp.Count, Is.EqualTo(1));
            Assert.That(vm.Stops[0].PickUp[0].Name, Is.EqualTo("Iron"));
            Assert.That(vm.Stops[0].PickUp[0].Delivered, Is.True);

            // Verify deep copy: modifying VM stops does not affect original entity
            vm.Stops[0].DropOff[0].Quantity = 999;
            Assert.That(plan.Stops[0].DropOff[0].Quantity, Is.EqualTo(10));
        }

        // -----------------------------------------------------------------------
        // BuildUpdateRequest copies Name and Stops
        // -----------------------------------------------------------------------

        [Test]
        public void BuildUpdateRequest_CopiesNameAndStops()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-build",
                Name = "Original Name",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop
                    {
                        ColonyUUID = "c1",
                        Sequence = 0,
                        StopCompleted = true,
                        DestinationType = DestinationType.Station,
                        DestinationUUID = "station-1",
                        DropOff = new List<DeliveryItem>
                        {
                            new DeliveryItem
                            {
                                ItemType = ItemType.ItemTypeEnum.Flatpack,
                                BaseItemTypeID = "bp-1",
                                Name = "Habitat",
                                Quantity = 2,
                            },
                        },
                        PickUp = new List<DeliveryItem>(),
                    },
                },
            };
            var ro = new ReadOnlyDeliveryPlan(plan);
            var vm = new DeliveryPlanViewModel();
            vm.LoadFrom(ro);

            // Modify the VM name
            vm.Name = "Updated Name";

            var request = vm.BuildUpdateRequest();

            Assert.That(request.Name, Is.EqualTo("Updated Name"));
            Assert.That(request.Stops.Count, Is.EqualTo(1));
            Assert.That(request.Stops[0].ColonyUUID, Is.EqualTo("c1"));
            Assert.That(request.Stops[0].StopCompleted, Is.True);
            Assert.That(request.Stops[0].DestinationType, Is.EqualTo(DestinationType.Station));
            Assert.That(request.Stops[0].DropOff.Count, Is.EqualTo(1));
            Assert.That(request.Stops[0].DropOff[0].Name, Is.EqualTo("Habitat"));
            Assert.That(request.Stops[0].DropOff[0].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void BuildUpdateRequest_DeepCopiesStops()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-deep-req",
                Name = "Deep Req",
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
                                ItemType = ItemType.ItemTypeEnum.Commodity,
                                BaseItemTypeID = "A",
                                Name = "A",
                                Quantity = 5,
                            },
                        },
                        PickUp = new List<DeliveryItem>(),
                    },
                },
            };
            var ro = new ReadOnlyDeliveryPlan(plan);
            var vm = new DeliveryPlanViewModel();
            vm.LoadFrom(ro);

            var request = vm.BuildUpdateRequest();

            // Modifying request should not affect VM
            request.Stops[0].DropOff[0].Quantity = 999;
            Assert.That(vm.Stops[0].DropOff[0].Quantity, Is.EqualTo(5));
        }
    }
}