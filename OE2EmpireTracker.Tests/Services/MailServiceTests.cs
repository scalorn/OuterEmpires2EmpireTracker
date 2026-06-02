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
    }
}
