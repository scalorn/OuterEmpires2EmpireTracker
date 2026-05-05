using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    [TestFixture]
    public class ContactsViewModelTests
    {
        [Test]
        public void IsNew_TrueAfterReset_Faction()
        {
            var vm = new ContactsViewModel();
            vm.LoadFactionFrom(new ReadOnlyFaction(new Faction { UUID = "f1", Name = "A", Description = "B" }));
            vm.ResetFaction();
            Assert.That(vm.IsFactionNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom_Faction()
        {
            var vm = new ContactsViewModel();
            vm.LoadFactionFrom(new ReadOnlyFaction(new Faction { UUID = "f1", Name = "A", Description = "B" }));
            Assert.That(vm.IsFactionNew, Is.False);
        }

        [Test]
        public void IsNew_TrueAfterReset_Character()
        {
            var vm = new ContactsViewModel();
            vm.LoadCharacterFrom(new ReadOnlyExternalCharacter(new ExternalCharacter { UUID = "c1", Name = "X", FactionUUID = "f1" }));
            vm.ResetCharacter();
            Assert.That(vm.IsCharacterNew, Is.True);
        }

        [Test]
        public void IsNew_FalseAfterLoadFrom_Character()
        {
            var vm = new ContactsViewModel();
            vm.LoadCharacterFrom(new ReadOnlyExternalCharacter(new ExternalCharacter { UUID = "c1", Name = "X", FactionUUID = "f1" }));
            Assert.That(vm.IsCharacterNew, Is.False);
        }

        [Test]
        public void BuildFactionUpdateRequest_CopiesAllFields()
        {
            var vm = new ContactsViewModel();
            var ro = new ReadOnlyFaction(new Faction { UUID = "f1", Name = "A", Description = "B" });
            vm.LoadFactionFrom(ro);
            vm.FactionName = "C";
            vm.FactionDescription = "D";
            var req = vm.BuildFactionUpdateRequest();
            Assert.That(req.Original, Is.EqualTo(ro));
            Assert.That(req.Name, Is.EqualTo("C"));
            Assert.That(req.Description, Is.EqualTo("D"));
        }

        [Test]
        public void BuildFactionCreateRequest_CopiesAllFields()
        {
            var vm = new ContactsViewModel();
            vm.FactionName = "NewFaction";
            vm.FactionDescription = "Desc";
            var req = vm.BuildFactionCreateRequest();
            Assert.That(req.Name, Is.EqualTo("NewFaction"));
            Assert.That(req.Description, Is.EqualTo("Desc"));
        }

        [Test]
        public void BuildCharacterUpdateRequest_CopiesAllFields()
        {
            var vm = new ContactsViewModel();
            var ro = new ReadOnlyExternalCharacter(new ExternalCharacter { UUID = "c1", Name = "X", FactionUUID = "f1" });
            vm.LoadCharacterFrom(ro);
            vm.CharacterName = "Y";
            vm.CharacterFactionUUID = "f2";
            var req = vm.BuildCharacterUpdateRequest();
            Assert.That(req.Original, Is.EqualTo(ro));
            Assert.That(req.Name, Is.EqualTo("Y"));
            Assert.That(req.FactionUUID, Is.EqualTo("f2"));
        }

        [Test]
        public void BuildCharacterCreateRequest_CopiesAllFields()
        {
            var vm = new ContactsViewModel();
            vm.CharacterName = "NewChar";
            vm.CharacterFactionUUID = "f3";
            var req = vm.BuildCharacterCreateRequest();
            Assert.That(req.Name, Is.EqualTo("NewChar"));
            Assert.That(req.FactionUUID, Is.EqualTo("f3"));
        }

        [Test]
        public void Reset_ClearsAllFactionFields()
        {
            var vm = new ContactsViewModel();
            vm.LoadFactionFrom(new ReadOnlyFaction(new Faction { UUID = "f1", Name = "A", Description = "B" }));
            vm.ResetFaction();
            Assert.That(vm.FactionUUID, Is.Null);
            Assert.That(vm.FactionName, Is.EqualTo(string.Empty));
            Assert.That(vm.FactionDescription, Is.EqualTo(string.Empty));
            Assert.That(vm.FactionOriginal, Is.Null);
        }

        [Test]
        public void Reset_ClearsAllCharacterFields()
        {
            var vm = new ContactsViewModel();
            vm.LoadCharacterFrom(new ReadOnlyExternalCharacter(new ExternalCharacter { UUID = "c1", Name = "X", FactionUUID = "f1" }));
            vm.ResetCharacter();
            Assert.That(vm.CharacterUUID, Is.Null);
            Assert.That(vm.CharacterName, Is.EqualTo(string.Empty));
            Assert.That(vm.CharacterFactionUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.CharacterOriginal, Is.Null);
        }
    }
}
