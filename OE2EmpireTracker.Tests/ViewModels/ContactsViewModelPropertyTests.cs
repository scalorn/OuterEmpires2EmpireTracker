using System;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for ContactsViewModel edit buffer.
    /// Feature: bl-121-contacts-readonly
    /// Validates: Requirements 4.1, 4.2, 4.3, 5.1, 5.2, 5.3, 6.1, 6.2, 6.3
    /// </summary>
    [TestFixture]
    public class ContactsViewModelPropertyTests
    {
        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<Faction> ValidFactionGen()
        {
            return from name in SafeStringGen()
                   from desc in SafeStringGen()
                   select new Faction
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       Description = desc,
                   };
        }

        private static Gen<ExternalCharacter> ValidCharacterGen()
        {
            return from name in SafeStringGen()
                   from factionUUID in SafeStringGen()
                   select new ExternalCharacter
                   {
                       UUID = Guid.NewGuid().ToString(),
                       Name = name,
                       FactionUUID = factionUUID,
                   };
        }

        /// <summary>
        /// Property 1: LoadFrom Round-Trip Preserves Faction Fields.
        /// Validates: Requirements 4.1, 4.2, 4.3
        /// </summary>
        [Test]
        public void LoadFrom_RoundTrip_PreservesFactionFields()
        {
            Prop.ForAll(ValidFactionGen().ToArbitrary(), faction =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyFaction(faction);
                vm.LoadFactionFrom(ro);

                return (vm.FactionUUID == faction.UUID
                    && vm.FactionName == faction.Name
                    && vm.FactionDescription == faction.Description
                    && vm.FactionOriginal == ro).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 2: LoadFrom Round-Trip Preserves Character Fields.
        /// Validates: Requirements 5.1, 5.2, 5.3
        /// </summary>
        [Test]
        public void LoadFrom_RoundTrip_PreservesCharacterFields()
        {
            Prop.ForAll(ValidCharacterGen().ToArbitrary(), character =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyExternalCharacter(character);
                vm.LoadCharacterFrom(ro);

                return (vm.CharacterUUID == character.UUID
                    && vm.CharacterName == character.Name
                    && vm.CharacterFactionUUID == character.FactionUUID
                    && vm.CharacterOriginal == ro).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 3: IsDirty False After LoadFrom (Faction).
        /// Validates: Requirements 6.1, 6.2
        /// </summary>
        [Test]
        public void IsDirty_FalseImmediatelyAfterLoadFrom_Faction()
        {
            Prop.ForAll(ValidFactionGen().ToArbitrary(), faction =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyFaction(faction);
                vm.LoadFactionFrom(ro);

                return (!vm.IsFactionDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property 4: IsDirty False After LoadFrom (Character).
        /// Validates: Requirements 6.1, 6.3
        /// </summary>
        [Test]
        public void IsDirty_FalseImmediatelyAfterLoadFrom_Character()
        {
            Prop.ForAll(ValidCharacterGen().ToArbitrary(), character =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyExternalCharacter(character);
                vm.LoadCharacterFrom(ro);

                return (!vm.IsCharacterDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property: IsDirty detects faction name change.
        /// Validates: Requirements 6.2
        /// </summary>
        [Test]
        public void IsFactionDirty_DetectsNameChange()
        {
            Prop.ForAll(ValidFactionGen().ToArbitrary(), Arb.From(SafeStringGen()), (faction, newName) =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyFaction(faction);
                vm.LoadFactionFrom(ro);
                vm.FactionName = newName;

                bool shouldBeDirty = newName != faction.Name;
                return (vm.IsFactionDirty == shouldBeDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }

        /// <summary>
        /// Property: IsDirty detects character name change.
        /// Validates: Requirements 6.3
        /// </summary>
        [Test]
        public void IsCharacterDirty_DetectsNameChange()
        {
            Prop.ForAll(ValidCharacterGen().ToArbitrary(), Arb.From(SafeStringGen()), (character, newName) =>
            {
                var vm = new ContactsViewModel();
                var ro = new ReadOnlyExternalCharacter(character);
                vm.LoadCharacterFrom(ro);
                vm.CharacterName = newName;

                bool shouldBeDirty = newName != character.Name;
                return (vm.IsCharacterDirty == shouldBeDirty).ToProperty();
            }).QuickCheckThrowOnFailure();
        }
    }
}
