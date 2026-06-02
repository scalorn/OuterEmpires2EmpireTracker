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
    /// Property-based tests for in-game mail functionality.
    /// Validates correctness properties from the design document.
    /// </summary>
    [TestFixture]
    public class MailServiceTests
    {
        // -------------------------------------------------------------------
        // Shared generators
        // -------------------------------------------------------------------

        private static Gen<MailMessage> MailMessageGen(Gen<int> mailIdGen)
        {
            return from mailId in mailIdGen
                   from fromName in Gen.Elements("System", "PlayerA", "PlayerB", "Admin")
                   from subject in Gen.Elements("Hello", "Colony Report", "Research Done", "Trade Offer")
                   select new MailMessage
                   {
                       MailId = mailId,
                       FromName = fromName,
                       Subject = subject,
                       MailContent = "Body for mail " + mailId,
                       CharacterIdFrom = 100 + mailId,
                       CharacterIdTo = 200,
                       ToName = "TestPlayer",
                       SentTime = "2025-01-15T10:00:00",
                       MailRead = false,
                       LocalRead = false,
                   };
        }

        // -------------------------------------------------------------------
        // Property 1: Deduplication Invariant
        // Generate random sequences of AddMailMessage calls with repeated MailIds.
        // Assert that MailMessageList never contains duplicate MailIds.
        // **Validates: Req 2, Criteria 2.3, 2.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DeduplicationInvariant_NoDuplicateMailIdsAfterRepeatedAdds()
        {
            // Generate a list of mail IDs where some are intentionally repeated.
            // We pick from a small pool of IDs to guarantee collisions.
            var mailIdGen = Gen.Choose(1, 20);

            var messageListGen = from count in Gen.Choose(5, 50)
                                 from messages in Gen.ListOf(count, MailMessageGen(mailIdGen))
                                 select messages.ToList();

            return Prop.ForAll(
                Arb.From(messageListGen),
                messages =>
                {
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());

                    foreach (var msg in messages)
                    {
                        ctx.AddMailMessage(msg);
                    }

                    var storedIds = ctx.MailMessageList.Select(m => m.MailId).ToList();
                    int distinctCount = storedIds.Distinct().Count();
                    bool noDuplicates = distinctCount == storedIds.Count;

                    return noDuplicates.Label(
                        "Expected all stored MailIds to be distinct. "
                        + "Total stored=" + storedIds.Count
                        + ", distinct=" + distinctCount);
                });
        }

        // -------------------------------------------------------------------
        // Property 2: High-Water Mark Monotonicity
        // Generate sequences of AddMailMessage calls.
        // Assert GetHighWaterMark never decreases after additions.
        // **Validates: Req 3, Criteria 3.3, 3.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property HighWaterMarkMonotonicity_NeverDecreasesAfterAdditions()
        {
            var mailIdGen = Gen.Choose(1, 500);

            var messageListGen = from count in Gen.Choose(1, 40)
                                 from messages in Gen.ListOf(count, MailMessageGen(mailIdGen))
                                 select messages.ToList();

            return Prop.ForAll(
                Arb.From(messageListGen),
                messages =>
                {
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());

                    int previousHwm = MailService.GetHighWaterMark(ctx);
                    bool neverDecreased = true;

                    foreach (var msg in messages)
                    {
                        ctx.AddMailMessage(msg);
                        int currentHwm = MailService.GetHighWaterMark(ctx);

                        if (currentHwm < previousHwm)
                        {
                            neverDecreased = false;
                            break;
                        }

                        previousHwm = currentHwm;
                    }

                    return neverDecreased.Label(
                        "High-water mark must never decrease after additions. "
                        + "Messages added=" + messages.Count);
                });
        }

        // -------------------------------------------------------------------
        // Property 3: Filter Completeness
        // Generate random mail sets and random filter combinations.
        // Assert GetFilteredMessages result equals manual predicate evaluation.
        // **Validates: Requirements 5, Criterion 5.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FilterCompleteness_ResultMatchesManualPredicateEvaluation()
        {
            var mailTypeGen = Gen.Elements<string>(null, "C", "R", "S");
            var localReadGen = Gen.Elements(true, false);
            var fromNameGen = Gen.Elements("System", "PlayerA", "PlayerB", "Admin", "Trader");
            var subjectGen = Gen.Elements("Hello", "Colony Report", "Research Done", "Trade Offer", "Skill Up");
            var contentGen = Gen.Elements("Body text", "Colony built", "Research complete", "Offer details", "Training finished");

            var filterMessageGen = from mailId in Gen.Choose(1, 500)
                                   from mailType in mailTypeGen
                                   from localRead in localReadGen
                                   from fromName in fromNameGen
                                   from subject in subjectGen
                                   from content in contentGen
                                   select new MailMessage
                                   {
                                       MailId = mailId,
                                       MailType = mailType,
                                       LocalRead = localRead,
                                       FromName = fromName,
                                       Subject = subject,
                                       MailContent = content,
                                       CharacterIdFrom = 100,
                                       CharacterIdTo = 200,
                                       ToName = "TestPlayer",
                                       SentTime = "2025-01-15T10:00:00",
                                       MailRead = false,
                                   };

            var readFilterGen = Gen.Elements("All", "Unread", "Read");
            var typeFilterGen = Gen.Elements("All", "Player/System", "Colony", "Research", "Skill");
            var searchTextGen = Gen.Elements(string.Empty, "system", "Player", "Colony", "Trade", "body", "XYZ");

            var testCaseGen = from msgCount in Gen.Choose(0, 30)
                              from messages in Gen.ListOf(msgCount, filterMessageGen)
                              from readFilter in readFilterGen
                              from typeFilter in typeFilterGen
                              from searchText in searchTextGen
                              select new { Messages = messages.ToList(), ReadFilter = readFilter, TypeFilter = typeFilter, SearchText = searchText };

            return Prop.ForAll(
                Arb.From(testCaseGen),
                tc =>
                {
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());

                    foreach (var msg in tc.Messages)
                    {
                        ctx.AddMailMessage(msg);
                    }

                    var serviceResult = MailService.GetFilteredMessages(ctx, tc.ReadFilter, tc.TypeFilter, tc.SearchText);

                    // Manual predicate evaluation on deduplicated messages
                    var allMessages = ctx.MailMessageList;
                    var expected = new List<MailMessage>();
                    for (int i = 0; i < allMessages.Count; i++)
                    {
                        var msg = allMessages[i];
                        if (!ManualPassesReadFilter(msg, tc.ReadFilter))
                        {
                            continue;
                        }

                        if (!ManualPassesTypeFilter(msg, tc.TypeFilter))
                        {
                            continue;
                        }

                        if (!ManualPassesSearchFilter(msg, tc.SearchText))
                        {
                            continue;
                        }

                        expected.Add(msg);
                    }

                    var serviceIds = serviceResult.Select(m => m.MailId).OrderBy(id => id).ToList();
                    var expectedIds = expected.Select(m => m.MailId).OrderBy(id => id).ToList();

                    bool sameCount = serviceIds.Count == expectedIds.Count;
                    bool sameItems = serviceIds.SequenceEqual(expectedIds);

                    return (sameCount && sameItems).Label(
                        "Filter mismatch. ReadFilter=" + tc.ReadFilter
                        + " TypeFilter=" + tc.TypeFilter
                        + " SearchText='" + tc.SearchText + "'"
                        + " Expected=" + expectedIds.Count
                        + " Got=" + serviceIds.Count);
                });
        }

        // -------------------------------------------------------------------
        // Property 4: LocalRead Independence
        // Generate mail messages with random MailRead values, call MarkAsRead.
        // Assert MailRead field is never modified when LocalRead changes,
        // and LocalRead is true after MarkAsRead.
        // **Validates: Req 6, Criteria 6.1, 6.2**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property LocalReadIndependence_MarkAsReadNeverModifiesMailRead()
        {
            var mailIdGen = Gen.Choose(1, 1000);
            var mailReadGen = Arb.Generate<bool>();

            var messageGen = from mailId in mailIdGen
                             from mailRead in mailReadGen
                             from fromName in Gen.Elements("System", "PlayerA", "PlayerB")
                             from subject in Gen.Elements("Hello", "Report", "Trade")
                             select new MailMessage
                             {
                                 MailId = mailId,
                                 FromName = fromName,
                                 Subject = subject,
                                 MailContent = "Body " + mailId,
                                 CharacterIdFrom = 100 + mailId,
                                 CharacterIdTo = 200,
                                 ToName = "TestPlayer",
                                 SentTime = "2025-01-15T10:00:00",
                                 MailRead = mailRead,
                                 LocalRead = false,
                             };

            var messageListGen = from count in Gen.Choose(1, 20)
                                 from messages in Gen.ListOf(count, messageGen)
                                 select messages.ToList();

            return Prop.ForAll(
                Arb.From(messageListGen),
                messages =>
                {
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());

                    // Deduplicate input by MailId so we have a known set
                    var unique = new Dictionary<int, MailMessage>();
                    foreach (var msg in messages)
                    {
                        if (!unique.ContainsKey(msg.MailId))
                        {
                            unique[msg.MailId] = msg;
                            ctx.AddMailMessage(msg);
                        }
                    }

                    // Record original MailRead values
                    var originalMailRead = new Dictionary<int, bool>();
                    foreach (var kvp in unique)
                    {
                        originalMailRead[kvp.Key] = kvp.Value.MailRead;
                    }

                    // Call MarkAsRead for each message
                    foreach (var kvp in unique)
                    {
                        MailService.MarkAsRead(kvp.Key, ctx);
                    }

                    // Assert MailRead unchanged and LocalRead is true
                    bool allMailReadUnchanged = true;
                    bool allLocalReadTrue = true;
                    foreach (var kvp in unique)
                    {
                        var stored = ctx.FindMailMessage(kvp.Key);
                        if (stored.MailRead != originalMailRead[kvp.Key])
                        {
                            allMailReadUnchanged = false;
                        }

                        if (!stored.LocalRead)
                        {
                            allLocalReadTrue = false;
                        }
                    }

                    return (allMailReadUnchanged && allLocalReadTrue).Label(
                        "MailRead must remain unchanged after MarkAsRead; "
                        + "LocalRead must be true for all messages");
                });
        }

        // -------------------------------------------------------------------
        // Property 5: Sync Idempotency
        // Add a set of messages, record state, add same messages again.
        // Assert list state is identical after second pass (no duplicates, no losses).
        // **Validates: Req 3, Criteria 3.3, 3.4, 3.5**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SyncIdempotency_AddingSameMessagesTwiceProducesIdenticalState()
        {
            // Generate messages with unique MailIds (no duplicates in the input set)
            var uniqueMailIdGen = from count in Gen.Choose(1, 30)
                                  from ids in Gen.ListOf(count, Gen.Choose(1, 10000))
                                  select ids.Distinct().ToList();

            var testCaseGen = from ids in uniqueMailIdGen
                              from messages in Gen.Sequence(
                                  ids.Select(id => MailMessageGen(Gen.Constant(id))))
                              select messages.ToList();

            return Prop.ForAll(
                Arb.From(testCaseGen),
                messages =>
                {
                    PlayerContext.FilePath = string.Empty;
                    var ctx = new PlayerContext(new PlayerRoot());

                    // First pass: add all messages
                    foreach (var msg in messages)
                    {
                        ctx.AddMailMessage(msg);
                    }

                    // Record state after first pass
                    var idsAfterFirstPass = ctx.MailMessageList
                        .Select(m => m.MailId).OrderBy(id => id).ToList();
                    int countAfterFirstPass = ctx.MailMessageList.Count;

                    // Second pass: add the same messages again
                    foreach (var msg in messages)
                    {
                        ctx.AddMailMessage(msg);
                    }

                    // Record state after second pass
                    var idsAfterSecondPass = ctx.MailMessageList
                        .Select(m => m.MailId).OrderBy(id => id).ToList();
                    int countAfterSecondPass = ctx.MailMessageList.Count;

                    bool sameCount = countAfterFirstPass == countAfterSecondPass;
                    bool sameIds = idsAfterFirstPass.SequenceEqual(idsAfterSecondPass);
                    bool noDuplicates = idsAfterSecondPass.Distinct().Count()
                        == idsAfterSecondPass.Count;

                    return (sameCount && sameIds && noDuplicates).Label(
                        "State must be identical after second pass. "
                        + "CountFirst=" + countAfterFirstPass
                        + " CountSecond=" + countAfterSecondPass
                        + " IdsMatch=" + sameIds
                        + " NoDuplicates=" + noDuplicates);
                });
        }

        // -------------------------------------------------------------------
        // Helper methods for manual predicate evaluation (Property 3)
        // -------------------------------------------------------------------

        private static bool ManualPassesReadFilter(MailMessage msg, string readFilter)
        {
            if (string.IsNullOrEmpty(readFilter) || readFilter == "All")
            {
                return true;
            }

            if (readFilter == "Unread")
            {
                return !msg.LocalRead;
            }

            if (readFilter == "Read")
            {
                return msg.LocalRead;
            }

            return true;
        }

        private static bool ManualPassesTypeFilter(MailMessage msg, string typeFilter)
        {
            if (string.IsNullOrEmpty(typeFilter) || typeFilter == "All")
            {
                return true;
            }

            switch (typeFilter)
            {
                case "Player/System":
                    return msg.MailType == null;
                case "Colony":
                    return msg.MailType == "C";
                case "Research":
                    return msg.MailType == "R";
                case "Skill":
                    return msg.MailType == "S";
                default:
                    return true;
            }
        }

        private static bool ManualPassesSearchFilter(MailMessage msg, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            if (msg.FromName != null &&
                msg.FromName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (msg.Subject != null &&
                msg.Subject.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (msg.MailContent != null &&
                msg.MailContent.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
