using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: colony-admin-summary, Property 11: Commodity request rows contain name and quantity
    /// </summary>
    [TestFixture]
    public class ColonyAdminReportBuilderCommodityPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.SetEmpireFilePath();
            EmpireContext.Reset();
            EmpireContext.GetInstance();
            PlayerContext.Reset();
            PlayerContext.FilePath = "nonexistent_player_data.json";
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            EmpireContext.Reset();
        }

        /// <summary>
        /// Feature: colony-admin-summary, Property 11: Commodity request rows contain name and quantity.
        /// For any unfulfilled commodity request, the Commodity Requests section SHALL contain
        /// a row displaying the commodity name and requested quantity.
        /// **Validates: Requirements 2a.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property CommodityRequestRows_ContainNameAndQuantity()
        {
            var qtyGen = Gen.Choose(1, 1000);

            return Prop.ForAll(qtyGen.ToArbitrary(), qty =>
            {
                var pc = PlayerContext.GetInstance();

                string commodityName = "TestCommodity" + qty;
                var colony = new Colony
                {
                    UUID = Guid.NewGuid().ToString(),
                    SystemName = "TestSystem",
                    ColonyName = "TestColony",
                    LastImportDateTime = SurveyDateTimeParser.ToIsoString(DateTime.UtcNow),
                    Commodities = new List<CommodityRequested>
                    {
                        new CommodityRequested
                        {
                            Name = commodityName,
                            Requested = qty,
                            Fulfilled = false,
                            NeedBy = DateTime.MinValue
                        }
                    }
                };

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, pc);

                if (!rtf.Contains(commodityName))
                    return false.Label($"Missing commodity name '{commodityName}'");

                if (!rtf.Contains($"x{qty}"))
                    return false.Label($"Missing quantity 'x{qty}'");

                return true.Label("Commodity name and quantity present");
            });
        }
    }
}
