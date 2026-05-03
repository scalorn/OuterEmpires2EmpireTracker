using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for PricingPlanViewModel edit buffer.
    /// Feature: bl-123-pricingplan-readonly
    /// Validates: Requirements 4.1, 4.2, 7.1, 7.5
    /// </summary>
    [TestFixture]
    public class PricingPlanViewModelTests
    {
        // -----------------------------------------------------------------------
        // Reset
        // -----------------------------------------------------------------------

        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new PricingPlanViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.OwnerUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.Name, Is.EqualTo(string.Empty));
            Assert.That(vm.Description, Is.EqualTo(string.Empty));
            Assert.That(vm.FixedCostPerItem, Is.EqualTo(0m));
            Assert.That(vm.HourlyCostRate, Is.EqualTo(0m));
            Assert.That(vm.ResourcePrices, Is.Empty);
            Assert.That(vm.Original, Is.Null);
        }

        // -----------------------------------------------------------------------
        // IsNew
        // -----------------------------------------------------------------------

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new PricingPlanViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new PricingPlanViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            Assert.That(vm.IsNew, Is.False);
        }

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        [Test]
        public void IsDirty_ReturnsTrue_ForNewPlanWithNonDefaultName()
        {
            var vm = new PricingPlanViewModel();
            vm.Reset();
            vm.Name = "Test Plan";

            Assert.That(vm.IsDirty, Is.True);
        }

        // -----------------------------------------------------------------------
        // BuildUpdateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildUpdateRequest_CopiesAllFields()
        {
            var vm = new PricingPlanViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Name = "Updated Name";
            vm.Description = "Updated Desc";
            vm.FixedCostPerItem = 99.99m;
            vm.HourlyCostRate = 12.50m;
            vm.ResourcePrices["Iron|Refined"] = 42.00m;

            var request = vm.BuildUpdateRequest();

            Assert.That(request.Original, Is.SameAs(vm.Original));
            Assert.That(request.Name, Is.EqualTo("Updated Name"));
            Assert.That(request.Description, Is.EqualTo("Updated Desc"));
            Assert.That(request.FixedCostPerItem, Is.EqualTo(99.99m));
            Assert.That(request.HourlyCostRate, Is.EqualTo(12.50m));
            Assert.That(request.ResourcePrices.ContainsKey("Iron|Refined"), Is.True);
            Assert.That(request.ResourcePrices["Iron|Refined"], Is.EqualTo(42.00m));
        }

        [Test]
        public void BuildUpdateRequest_ResourcePricesIsDeepCopy()
        {
            var vm = new PricingPlanViewModel();
            vm.LoadFrom(CreateSampleReadOnly());
            vm.ResourcePrices["Copper|High"] = 5.00m;

            var request = vm.BuildUpdateRequest();
            request.ResourcePrices["Copper|High"] = 999.00m;

            Assert.That(vm.ResourcePrices["Copper|High"], Is.EqualTo(5.00m));
        }

        // -----------------------------------------------------------------------
        // BuildCreateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCreateRequest_CopiesAllFields()
        {
            var vm = new PricingPlanViewModel();
            vm.Reset();
            vm.Name = "New Plan";
            vm.Description = "New Desc";
            vm.FixedCostPerItem = 10.00m;
            vm.HourlyCostRate = 5.00m;
            vm.ResourcePrices["Alkali Metals|Refined"] = 25.00m;

            var request = vm.BuildCreateRequest();

            Assert.That(request.Name, Is.EqualTo("New Plan"));
            Assert.That(request.Description, Is.EqualTo("New Desc"));
            Assert.That(request.FixedCostPerItem, Is.EqualTo(10.00m));
            Assert.That(request.HourlyCostRate, Is.EqualTo(5.00m));
            Assert.That(request.ResourcePrices.ContainsKey("Alkali Metals|Refined"), Is.True);
            Assert.That(request.ResourcePrices["Alkali Metals|Refined"], Is.EqualTo(25.00m));
        }

        [Test]
        public void BuildCreateRequest_ResourcePricesIsDeepCopy()
        {
            var vm = new PricingPlanViewModel();
            vm.Reset();
            vm.ResourcePrices["Iron|Refined"] = 10.00m;

            var request = vm.BuildCreateRequest();
            request.ResourcePrices["Iron|Refined"] = 999.00m;

            Assert.That(vm.ResourcePrices["Iron|Refined"], Is.EqualTo(10.00m));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static ReadOnlyPricingPlan CreateSampleReadOnly()
        {
            var plan = new PricingPlan
            {
                UUID = "plan-001",
                Name = "Standard Plan",
                OwnerUUID = "owner-001",
                Description = "A standard pricing plan",
                FixedCostPerItem = 1.50m,
                HourlyCostRate = 3.25m,
            };

            plan.ResourcePrices["Iron|Refined"] = 10.00m;
            plan.ResourcePrices["Copper|High"] = 7.50m;

            return new ReadOnlyPricingPlan(plan);
        }
    }
}
