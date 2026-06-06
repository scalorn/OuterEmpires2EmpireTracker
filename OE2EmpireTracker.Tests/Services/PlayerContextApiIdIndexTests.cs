// <copyright file="PlayerContextApiIdIndexTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

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

                // Mirror the index: apiId -> Blueprint (tracks what PlayerContext stores)
                var index = new Dictionary<int, Bp>();
                var addedBlueprints = new List<Bp>();

                foreach (var pair in ops)
                {
                    var op = pair.Item1;
                    var apiId = pair.Item2;

                    switch (op)
                    {
                        case BpOp.Add:
                            var bp = new Bp("Test_" + Guid.NewGuid().ToString("N"))
                            {
                                UUID = Guid.NewGuid().ToString(),
                                GameApiBlueprintId = apiId,
                            };
                            ctx.AddBlueprint(bp);
                            addedBlueprints.Add(bp);
                            index[apiId] = bp;
                            break;

                        case BpOp.Remove:
                            if (addedBlueprints.Count > 0)
                            {
                                var idx = Math.Abs(apiId) % addedBlueprints.Count;
                                var toRemove = addedBlueprints[idx];
                                ctx.RemoveBlueprint(toRemove);
                                addedBlueprints.RemoveAt(idx);

                                // Only remove from index if the entry points to this item
                                if (toRemove.GameApiBlueprintId.HasValue
                                    && index.TryGetValue(toRemove.GameApiBlueprintId.Value, out var current)
                                    && ReferenceEquals(current, toRemove))
                                {
                                    index.Remove(toRemove.GameApiBlueprintId.Value);
                                }
                            }

                            break;

                        case BpOp.Index:
                            if (addedBlueprints.Count > 0)
                            {
                                var idx2 = Math.Abs(apiId) % addedBlueprints.Count;
                                var toIndex = addedBlueprints[idx2];
                                toIndex.GameApiBlueprintId = apiId;
                                ctx.IndexBlueprintByApiId(toIndex);
                                index[apiId] = toIndex;
                            }

                            break;
                    }
                }

                // Verify: for each query ID, FindBlueprintByApiId matches index
                foreach (var qid in queryIds)
                {
                    var actual = ctx.FindBlueprintByApiId(qid);
                    index.TryGetValue(qid, out var exp);
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

                // Mirror the index: apiId -> Survey (tracks what PlayerContext stores)
                var index = new Dictionary<int, Survey>();
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
                            index[apiId] = survey;
                            break;

                        case SurveyOp.Remove:
                            if (addedSurveys.Count > 0)
                            {
                                var idx = Math.Abs(apiId) % addedSurveys.Count;
                                var toRemove = addedSurveys[idx];
                                ctx.RemoveSurvey(toRemove);
                                addedSurveys.RemoveAt(idx);

                                // Only remove from index if the entry points to this item
                                if (toRemove.GameApiSurveyId.HasValue
                                    && index.TryGetValue(toRemove.GameApiSurveyId.Value, out var current)
                                    && ReferenceEquals(current, toRemove))
                                {
                                    index.Remove(toRemove.GameApiSurveyId.Value);
                                }
                            }

                            break;

                        case SurveyOp.Index:
                            if (addedSurveys.Count > 0)
                            {
                                var idx2 = Math.Abs(apiId) % addedSurveys.Count;
                                var toIndex = addedSurveys[idx2];
                                toIndex.GameApiSurveyId = apiId;
                                ctx.IndexSurveyByApiId(toIndex);
                                index[apiId] = toIndex;
                            }

                            break;
                    }
                }

                // Verify: for each query ID, FindSurveyByApiId matches index
                foreach (var qid in queryIds)
                {
                    var actual = ctx.FindSurveyByApiId(qid);
                    index.TryGetValue(qid, out var exp);
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
