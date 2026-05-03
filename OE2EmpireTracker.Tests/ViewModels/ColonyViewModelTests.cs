using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for ColonyViewModel edit buffer.
    /// Feature: bl-109-colony-readonly
    /// Validates: Requirements 4.1, 4.2, 7.1, 7.4, 7.5
    /// </summary>
    [TestFixture]
    public class ColonyViewModelTests
    {
        // -----------------------------------------------------------------------
        // Reset
        // -----------------------------------------------------------------------

        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new ColonyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.OwnerUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.PlanetName, Is.EqualTo(string.Empty));
            Assert.That(vm.ColonyName, Is.EqualTo(string.Empty));
            Assert.That(vm.SystemName, Is.EqualTo(string.Empty));
            Assert.That(vm.Original, Is.Null);
        }

        // -----------------------------------------------------------------------
        // IsNew
        // -----------------------------------------------------------------------

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new ColonyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new ColonyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            Assert.That(vm.IsNew, Is.False);
        }

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        [Test]
        public void IsDirty_ReturnsTrue_ForNewColonyWithNonDefaultPlanetName()
        {
            var vm = new ColonyViewModel();
            vm.Reset();
            vm.PlanetName = "Test Planet";

            Assert.That(vm.IsDirty, Is.True);
        }

        // -----------------------------------------------------------------------
        // BuildUpdateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildUpdateRequest_CopiesAllScalarFields()
        {
            var vm = new ColonyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.PlanetName = "Updated Planet";
            vm.ColonyName = "Updated Colony";
            vm.SystemName = "Updated System";

            var request = vm.BuildUpdateRequest();

            Assert.That(request.Original, Is.SameAs(vm.Original));
            Assert.That(request.PlanetName, Is.EqualTo("Updated Planet"));
            Assert.That(request.ColonyName, Is.EqualTo("Updated Colony"));
            Assert.That(request.SystemName, Is.EqualTo("Updated System"));
        }

        // -----------------------------------------------------------------------
        // BuildCreateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCreateRequest_CopiesAllScalarFields()
        {
            var vm = new ColonyViewModel();
            vm.Reset();
            vm.PlanetName = "New Planet";
            vm.ColonyName = "New Colony";
            vm.SystemName = "New System";

            var request = vm.BuildCreateRequest();

            Assert.That(request.PlanetName, Is.EqualTo("New Planet"));
            Assert.That(request.ColonyName, Is.EqualTo("New Colony"));
            Assert.That(request.SystemName, Is.EqualTo("New System"));
        }

        // -----------------------------------------------------------------------
        // UUID and OwnerUUID preserved from LoadFrom
        // -----------------------------------------------------------------------

        [Test]
        public void UUID_And_OwnerUUID_ArePreservedFromLoadFrom()
        {
            var vm = new ColonyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            Assert.That(vm.UUID, Is.EqualTo("colony-001"));
            Assert.That(vm.OwnerUUID, Is.EqualTo("owner-001"));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static ReadOnlyColony CreateSampleReadOnly()
        {
            var colony = new Colony
            {
                UUID = "colony-001",
                OwnerUUID = "owner-001",
                PlanetName = "Zeh Vazoran II",
                ColonyName = "Main Colony",
                SystemName = "Zeh Vazoran",
            };

            return new ReadOnlyColony(colony);
        }
    }
}
