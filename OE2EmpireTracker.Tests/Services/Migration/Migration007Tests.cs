using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using System;

using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    [TestFixture]
    public class Migration007Tests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetAllFilePaths();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
        }

        [Test]
        public void RemovesClassFromPropertyBag()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();
            ec.DataVersion = 6;
            pc.DataVersion = 6;

            var bp = new BpModel("TestBP") { UUID = Guid.NewGuid().ToString(), Class = 3 };
            bp.Properties.setProperty("Class", "3");
            bp.Properties.setProperty("Health", "500");
            pc.BlueprintList.Add(bp);

            MigrationRunner.Run(ec, pc);

            Assert.That(bp.Properties.ContainsKey("Class"), Is.False,
                "Class should be removed from PropertyBag");
            Assert.That(bp.Properties.ContainsKey("Health"), Is.True,
                "Other properties should be untouched");
            Assert.That(bp.Class, Is.EqualTo(3),
                "Blueprint.Class should retain its value");
        }

        [Test]
        public void PreservesClassValueWhenBlueprintClassIsZero()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();
            ec.DataVersion = 6;
            pc.DataVersion = 6;

            var bp = new BpModel("TestBP") { UUID = Guid.NewGuid().ToString(), Class = 0 };
            bp.Properties.setProperty("Class", "7");
            pc.BlueprintList.Add(bp);

            MigrationRunner.Run(ec, pc);

            Assert.That(bp.Properties.ContainsKey("Class"), Is.False);
            Assert.That(bp.Class, Is.EqualTo(7),
                "Blueprint.Class should be set from PropertyBag value when it was 0");
        }

        [Test]
        public void IgnoresBlueprintWithNoClassProperty()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();
            ec.DataVersion = 6;
            pc.DataVersion = 6;

            var bp = new BpModel("TestBP") { UUID = Guid.NewGuid().ToString(), Class = 5 };
            bp.Properties.setProperty("Health", "500");
            pc.BlueprintList.Add(bp);

            MigrationRunner.Run(ec, pc);

            Assert.That(bp.Class, Is.EqualTo(5));
            Assert.That(bp.Properties.ContainsKey("Health"), Is.True);
        }

        [Test]
        public void BumpsVersionTo7()
        {
            var ec = EmpireContext.GetInstance();
            var pc = PlayerContext.GetInstance();
            ec.DataVersion = 6;
            pc.DataVersion = 6;

            MigrationRunner.Run(ec, pc);

            Assert.That(ec.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
            Assert.That(pc.DataVersion, Is.EqualTo(MigrationRunner.CurrentVersion));
        }
    }
}
