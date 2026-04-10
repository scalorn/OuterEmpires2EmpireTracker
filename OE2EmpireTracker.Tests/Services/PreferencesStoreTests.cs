using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class PreferencesStoreTests
    {
        [SetUp]
        public void SetUp()
        {
            PreferencesStore.Reset();
        }

        [Test]
        public void GetWindowState_CreatesNewEntry_WhenNoneExists()
        {
            var store = PreferencesStore.GetInstance();

            var state = store.GetWindowState("FormColony", 1);

            Assert.That(state, Is.Not.Null);
            Assert.That(state, Is.InstanceOf<WindowState>());
            Assert.That(store.Preferences.Forms, Contains.Key("FormColony"));
            Assert.That(store.Preferences.Forms["FormColony"], Contains.Key("1"));
        }

        [Test]
        public void GetWindowState_ReturnsSameInstance_WhenCalledAgainWithSameKey()
        {
            var store = PreferencesStore.GetInstance();

            var first = store.GetWindowState("FormBlueprint", 2);
            var second = store.GetWindowState("FormBlueprint", 2);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void GetWindowState_PreservesExistingEntries_WhenAddingNewFormType()
        {
            var store = PreferencesStore.GetInstance();

            var colonyState = store.GetWindowState("FormColony", 1);
            colonyState.Position = new WindowPosition { Left = 100, Top = 200, Width = 800, Height = 600 };

            // Add a different form type
            var blueprintState = store.GetWindowState("FormBlueprint", 1);

            // Original entry should still be intact
            var retrieved = store.GetWindowState("FormColony", 1);
            Assert.That(retrieved.Position, Is.Not.Null);
            Assert.That(retrieved.Position.Left, Is.EqualTo(100));
            Assert.That(retrieved.Position.Top, Is.EqualTo(200));
            Assert.That(store.Preferences.Forms.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void GetWindowState_PreservesExistingEntries_WhenAddingNewWindowNumber()
        {
            var store = PreferencesStore.GetInstance();

            var state1 = store.GetWindowState("FormSurvey", 1);
            state1.Position = new WindowPosition { Left = 50, Top = 50, Width = 400, Height = 300 };

            // Add a second window of the same type
            var state2 = store.GetWindowState("FormSurvey", 2);

            // First window state should be unchanged
            var retrieved = store.GetWindowState("FormSurvey", 1);
            Assert.That(retrieved.Position.Left, Is.EqualTo(50));
            Assert.That(store.Preferences.Forms["FormSurvey"].Count, Is.EqualTo(2));
        }

        [Test]
        public void Reset_CausesGetInstance_ToCreateFreshInstance()
        {
            var store1 = PreferencesStore.GetInstance();
            store1.GetWindowState("FormColony", 1);

            PreferencesStore.Reset();

            var store2 = PreferencesStore.GetInstance();
            Assert.That(store2, Is.Not.SameAs(store1));
        }
    }
}
