using System;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Feature: colony-import-timestamp, Property 1: Colony LastImportDateTime JSON round-trip
    /// </summary>
    [TestFixture]
    public class ColonyLastImportDateTimePropertyTests
    {
        /// <summary>
        /// Property 1: Colony LastImportDateTime JSON round-trip.
        /// For any Colony with a non-null LastImportDateTime string in ISO 8601 format,
        /// serializing to JSON and deserializing back should produce identical LastImportDateTime.
        /// **Validates: Requirements 1.1, 1.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property ColonyLastImportDateTimeJsonRoundTrip()
        {
            return Prop.ForAll(ValidIsoTimestampGen().ToArbitrary(), isoTimestamp =>
            {
                var colony = new Colony();
                colony.LastImportDateTime = isoTimestamp;

                var json = JsonConvert.SerializeObject(colony);
                var deserialized = JsonConvert.DeserializeObject<Colony>(json);

                return (deserialized.LastImportDateTime == isoTimestamp)
                    .Label($"Expected '{isoTimestamp}', got '{deserialized.LastImportDateTime}'");
            });
        }

        private static Gen<DateTime> ValidUtcDateTimeGen()
        {
            return from year in Gen.Choose(2020, 2035)
                   from month in Gen.Choose(1, 12)
                   from day in Gen.Choose(1, 28)
                   from hour in Gen.Choose(0, 23)
                   from minute in Gen.Choose(0, 59)
                   from second in Gen.Choose(0, 59)
                   select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        private static Gen<string> ValidIsoTimestampGen()
        {
            return ValidUtcDateTimeGen().Select(dt =>
                dt.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
