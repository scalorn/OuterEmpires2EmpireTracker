using System;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for ColonyViewModel edit buffer.
    /// Feature: bl-109-colony-readonly
    /// Validates: LoadFrom round-trip, IsDirty false after LoadFrom, IsDirty detects changes.
    /// </summary>
    [TestFixture]
    public class ColonyViewModelPropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random Colony with scalar fields populated
        // -----------------------------------------------------------------------

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<Colony> ValidColonyGen()
        {
            return from planetName in SafeStringGen()
                   from colonyName in SafeStringGen()
                   from systemName in SafeStringGen()
                   select BuildColony(planetName, colonyName, systemName);
        }

        private static Colony BuildColony(
            string planetName,
            string colonyName,
            string systemName)
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = Guid.NewGuid().ToString(),
                PlanetName = planetName,
                ColonyName = colonyName,
                SystemName = systemName,
            };

            return colony;
        }

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all scalar fields
        // Feature: bl-109-colony-readonly, Property 1
        // **Validates: Requirements 4.1, 4.2, 4.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllScalarFields()
        {
            return Prop.ForAll(ValidColonyGen().ToArbitrary(), colony =>
            {
                var ro = new ReadOnlyColony(colony);
                var vm = new ColonyViewModel();
                vm.LoadFrom(ro);

                bool planetMatch = vm.PlanetName == (ro.PlanetName ?? string.Empty);
                bool colonyMatch = vm.ColonyName == (ro.ColonyName ?? string.Empty);
                bool systemMatch = vm.SystemName == (ro.SystemName ?? string.Empty);

                return (planetMatch && colonyMatch && systemMatch)
                    .Label(
                        "planet=" + planetMatch
                        + ", colony=" + colonyMatch
                        + ", system=" + systemMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: IsDirty false immediately after LoadFrom
        // Feature: bl-109-colony-readonly, Property 2
        // **Validates: Requirements 7.1, 7.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidColonyGen().ToArbitrary(), colony =>
            {
                var ro = new ReadOnlyColony(colony);
                var vm = new ColonyViewModel();
                vm.LoadFrom(ro);

                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects any single scalar field change
        // Feature: bl-109-colony-readonly, Property 3
        // **Validates: Requirements 7.1, 7.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsAnySingleScalarFieldChange()
        {
            // 3 scalar fields: PlanetName, ColonyName, SystemName
            var fieldIndexGen = Gen.Choose(0, 2);

            return Prop.ForAll(
                ValidColonyGen().ToArbitrary(),
                Arb.From(fieldIndexGen),
                (colony, fieldIndex) =>
                {
                    var ro = new ReadOnlyColony(colony);
                    var vm = new ColonyViewModel();
                    vm.LoadFrom(ro);

                    string changedField = MutateSingleField(vm, ro, fieldIndex);

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing " + changedField);
                });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string MutateSingleField(
            ColonyViewModel vm,
            ReadOnlyColony ro,
            int fieldIndex)
        {
            switch (fieldIndex % 3)
            {
                case 0:
                    vm.PlanetName = (ro.PlanetName ?? string.Empty) + "X";
                    return "PlanetName";
                case 1:
                    vm.ColonyName = (ro.ColonyName ?? string.Empty) + "X";
                    return "ColonyName";
                case 2:
                    vm.SystemName = (ro.SystemName ?? string.Empty) + "X";
                    return "SystemName";
                default:
                    vm.PlanetName = (ro.PlanetName ?? string.Empty) + "X";
                    return "PlanetName(default)";
            }
        }
    }
}