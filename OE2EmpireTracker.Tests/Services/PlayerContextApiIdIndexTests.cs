// <copyright file="PlayerContextApiIdIndexTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for PlayerContext API ID index consistency.
    /// Feature: queue-based-sync
    /// Validates: Requirements 15.1, 15.2, 15.3, 15.4, 15.5
    /// </summary>
    [TestFixture]
    public class PlayerContextApiIdIndexTests
    {
        /// <summary>
        /// Represents an operation on the blueprint index.
        /// </summary>
        private enum BpOp
        {
            /// <summary>Add a blueprint.</summary>
            Add,

            /// <summary>Remove a blueprint.</summary>
            Remove,

            /// <summary>Index an existing blueprint with a new API ID.</summary>
            Index,
        }

        /// <summary>
        /// Represents an operation on the survey index.
        /// </summary>
        private enum SurveyOp
        {
            /// <summary>Add a survey.</summary>
            Add,

            /// <summary>Remove a survey.</summary>
            Remove,

            /// <summary>Index an existing survey with a new API ID.</summary>
            Index,
        }

        private static Gen<BpOp> BpOpGen()
        {
            return Gen.Elements(BpOp.Add, BpOp.Remove, BpOp.Index);
        }

        private static Gen<SurveyOp> SurveyOpGen()
        {
            return Gen.Elements(SurveyOp.Add, SurveyOp.Remove, SurveyOp.Index);
        }

        private static Gen<int> ApiIdGen()
        {
            return Gen.Choose(1, 10000);
        }

        /// <summary>
        /// Property 5: Index Consistency (Blueprint).
        /// After any sequence of Add/Remove/Index operations, FindBlueprintByApiId
        /// returns the correct blueprint (or null) for all queried API IDs.
        /// Validates: Requirements 15.1, 15.3, 15.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BlueprintIndex_StaysConsistent_AfterRandomOperations()
        {
            var opsGen = Gen.ListOf(
                from op in BpOpGen()
                from apiId in ApiIdGen()
                select Tuple.Create(op, apiId));

            return Prop.ForAll(
                opsGen.ToArbitrary(),
                ApiIdGen().ArrayOf().ToArbitrary(),
                (ops, queryIds) =>
            {
                PlayerContext.FilePath = string.Empty;
                var ctx = new PlayerContext(new PlayerRoot());

                // Track expected state: apiId -> Blueprint
                var expected = new Dictionary<int, Blueprint>();
                var addedBlueprints = new List<Blueprint>();

                foreach (var pair in ops)
                {
                    var op = pair.Item1;
                    var apiId = pair.Item2;

                    switch (op)
                    {
                        case BpOp.Add:
                            var bp = new Blueprint("Test_" + Guid.NewGuid().ToString("N"))
                            {
                                UUID = Guid.NewGuid().ToString(),
                                GameApiBlueprintId = apiId,
                            };
                            ctx.AddBlueprint(bp);
                            addedBlueprints.Add(bp);
                            expected[apiId] = bp;
                            break;

                        case BpOp.Remove:
                            if (addedBlueprints.Count > 0)
                            {
                                var idx = Math.Abs(apiId) % addedBlueprints.Count;
                                var toRemove = addedBlueprints[idx];
                                ctx.RemoveBlueprint(toRemove);
                                addedBlueprints.RemoveAt(idx);
                                if (toRemove.GameApiBlueprintId.HasValue
                                    && expected.TryGetValue(toRemove.GameApiBlueprintId.Value, out var current)
                                    && ReferenceEquals(current, toRemove))
                                {
                                    expected.Remove(toRemove.GameApiBlueprintId.Value);
                                }
                            }

                            break;

                        case BpOp.Index:
                            if (addedBlueprints.Count > 0)
                            {
                                var idx2 = Math.Abs(apiId) % addedBlueprints.Count;
                                var toIndex = addedBlueprints[idx2];
                                // Remove old mapping if it was pointing to this blueprint
                                if (toIndex.GameApiBlueprintId.HasValue
                                    && expected.TryGetValue(toIndex.GameApiBlueprintId.Value, out var old)
                                    && ReferenceEquals(old, toIndex))
                                {
                                    expected.Remove(toIndex.GameApiBlueprintId.Value);
                                }

                                toIndex.GameApiBlueprintId = apiId;
                                ctx.IndexBlueprintByApiId(toIndex);
                                expected[apiId] = toIndex;
                            }

                            break;
                    }
                }

                // Verify: for each query ID, FindBlueprintByApiId matches expected
                foreach (var qid in queryIds)
                {
                    var actual = ctx.FindBlueprintByApiId(qid);
                    expected.TryGetValue(qid, out var exp);
                    if (!ReferenceEquals(actual, exp))
                    {
                        return false.ToProperty();
                    }
                }

                return true.ToProperty();
            });
        }

        /// <summary>
        /// Property 5: Index Consistency (Survey).
        /// After any sequence of Add/Remove/Index operations, FindSurveyByApiId
        /// returns the correct survey (or null) for all queried API IDs.
        /// Validates: Requirements 15.2, 15.4, 15.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SurveyIndex_StaysConsistent_AfterRandomOperations()
        {
            var opsGen = Gen.ListOf(
                from op in SurveyOpGen()
                from apiId in ApiIdGen()
                select Tuple.Create(op, apiId));

            return Prop.ForAll(
                opsGen.ToArbitrary(),
                ApiIdGen().ArrayOf().ToArbitrary(),
                (ops, queryIds) =>
            {
                PlayerContext.FilePath = string.Empty;
                var ctx = new PlayerContext(new PlayerRoot());

                // Track expected state: apiId -> Survey
                var expected = new Dictionary<int, Survey>();
                var addedSurveys = new List<Survey>();

                foreach (var pair in ops)
                {
                    var op = pair.Item1;
                    var apiId = pair.Item2;

                    switch (op)
                    {
                        case SurveyOp.Add:
                            var survey = new Survey("Survey_" + Guid.NewGuid().ToString("N"))
                            {
                                UUID = Guid.NewGuid().ToString(),
                                GameApiSurveyId = apiId,
                            };
                            ctx.AddSurvey(survey);
                            addedSurveys.Add(survey);
                            expected[apiId] = survey;
                            break;

                        case SurveyOp.Remove:
                            if (addedSurveys.Count > 0)
                            {
                                var idx = Math.Abs(apiId) % addedSurveys.Count;
                                var toRemove = addedSurveys[idx];
                                ctx.RemoveSurvey(toRemove);
                                addedSurveys.RemoveAt(idx);
                                if (toRemove.GameApiSurveyId.HasValue
                                    && expected.TryGetValue(toRemove.GameApiSurveyId.Value, out var current)
                                    && ReferenceEquals(current, toRemove))
                                {
                                    expected.Remove(toRemove.GameApiSurveyId.Value);
                                }
                            }

                            break;

                        case SurveyOp.Index:
                            if (addedSurveys.Count > 0)
                            {
                                var idx2 = Math.Abs(apiId) % addedSurveys.Count;
                                var toIndex = addedSurveys[idx2];
                                // Remove old mapping if it was pointing to this survey
                                if (toIndex.GameApiSurveyId.HasValue
                                    && expected.TryGetValue(toIndex.GameApiSurveyId.Value, out var old)
                                    && ReferenceEquals(old, toIndex))
                                {
                                    expected.Remove(toIndex.GameApiSurveyId.Value);
                                }

                                toIndex.GameApiSurveyId = apiId;
                                ctx.IndexSurveyByApiId(toIndex);
                                expected[apiId] = toIndex;
                            }

                            break;
                    }
                }

                // Verify: for each query ID, FindSurveyByApiId matches expected
                foreach (var qid in queryIds)
                {
                    var actual = ctx.FindSurveyByApiId(qid);
                    expected.TryGetValue(qid, out var exp);
                    if (!ReferenceEquals(actual, exp))
                    {
                        return false.ToProperty();
                    }
                }

                return true.ToProperty();
            });
        }
    }
}
