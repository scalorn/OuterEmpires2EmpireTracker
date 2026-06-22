// <copyright file="GenHelpers.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Reusable FsCheck generator combinators for entity property tests.
    /// Reduces per-entity generator boilerplate from ~50 lines to ~15-20 lines.
    /// Satisfies: Req 7, Criteria 4-6 (enables proper edge case generation).
    /// </summary>
    public static class GenHelpers
    {
        /// <summary>
        /// Generates a UUID string (standard Guid format).
        /// </summary>
        public static Gen<string> GenUUID =>
            from g in Arb.Generate<Guid>()
            select g.ToString();

        /// <summary>
        /// Generates a non-empty string suitable for entity names and identifiers.
        /// </summary>
        public static Gen<string> GenNonEmptyString =>
            Arb.Generate<NonEmptyString>().Select(s => s.Get);

        /// <summary>
        /// Generates an optional string (null or non-empty).
        /// </summary>
        public static Gen<string> GenOptionalString =>
            Gen.OneOf(
                Gen.Constant((string)null),
                Arb.Generate<NonEmptyString>().Select(s => s.Get));

        /// <summary>
        /// Generates a decimal value across a wide range with up to 8 decimal places.
        /// Covers negative, zero, and positive values.
        /// </summary>
        public static Gen<decimal> GenDecimal =>
            from mantissa in Gen.Choose(-999999999, 999999999)
            from scale in Gen.Choose(0, 8)
            select (decimal)mantissa / (decimal)Math.Pow(10, scale);

        /// <summary>
        /// Generates a positive decimal value (greater than zero).
        /// Suitable for prices, quantities, and rates.
        /// </summary>
        public static Gen<decimal> GenPositiveDecimal =>
            from mantissa in Gen.Choose(1, 999999999)
            from scale in Gen.Choose(0, 8)
            select (decimal)mantissa / (decimal)Math.Pow(10, scale);

        /// <summary>
        /// Generates a DateTime value with second precision.
        /// Constrained to years 2000-2099 to avoid edge cases with min/max values.
        /// </summary>
        public static Gen<DateTime> GenDateTime =>
            from year in Gen.Choose(2000, 2099)
            from month in Gen.Choose(1, 12)
            from day in Gen.Choose(1, 28)
            from hour in Gen.Choose(0, 23)
            from minute in Gen.Choose(0, 59)
            from second in Gen.Choose(0, 59)
            select new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        /// <summary>
        /// Generates an optional list of items (null or a list of 0-5 items).
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="itemGen">Generator for individual items.</param>
        /// <returns>A generator producing null or a list of T.</returns>
        public static Gen<List<T>> GenOptionalList<T>(Gen<T> itemGen)
        {
            return Gen.OneOf(
                Gen.Constant((List<T>)null),
                from count in Gen.Choose(0, 5)
                from items in Gen.ListOf(count, itemGen)
                select items.ToList());
        }

        /// <summary>
        /// Generates a nullable value (null or a generated value).
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="valueGen">Generator for the underlying value.</param>
        /// <returns>A generator producing null or a value of T.</returns>
        public static Gen<T?> GenNullable<T>(Gen<T> valueGen)
            where T : struct
        {
            return Gen.OneOf(
                Gen.Constant((T?)null),
                valueGen.Select(v => (T?)v));
        }
    }
}
