using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Common.Client;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides sync, filter, and read-status logic for mail messages.
    /// </summary>
    public static class MailService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Fired when mail data changes (sync completes with new messages, or mark-as-read).
        /// </summary>
        public static event EventHandler MailDataChanged;

        /// <summary>
        /// Returns the highest MailId in the local store, or 0 if empty.
        /// </summary>
        /// <param name="playerContext">The player context containing mail messages.</param>
        /// <returns>The maximum MailId, or 0 if no messages exist.</returns>
        public static int GetHighWaterMark(PlayerContext playerContext)
        {
            var messages = playerContext.MailMessageList;
            if (messages.Count == 0)
            {
                return 0;
            }

            int max = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].MailId > max)
                {
                    max = messages[i].MailId;
                }
            }

            return max;
        }

        /// <summary>
        /// Performs incremental sync from the game API.
        /// Fetches pages of mail headers, stopping when all IDs
        /// on a page already exist locally. For each new mailId, fetches the
        /// detail endpoint for full content.
        /// </summary>
        /// <param name="typedClient">The typed game API client.</param>
        /// <param name="playerContext">The player context for persistence.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The number of new messages synced, -1 on auth failure, or -2 on rate limit.</returns>
        public static async Task<int> SyncMailAsync(
            IGameApiTypedClient typedClient,
            PlayerContext playerContext,
            CancellationToken ct = default)
        {
            int offset = 0;
            int pageSize = 50;
            int newCount = 0;

            try
            {
                while (true)
                {
                    var mailList = await typedClient.GetMailListAsync(offset, pageSize, ct: ct).ConfigureAwait(false);

                    if (mailList.Mail == null || mailList.Mail.Count == 0)
                    {
                        break;
                    }

                    bool allExist = true;

                    foreach (var header in mailList.Mail)
                    {
                        if (header.MailId <= 0)
                        {
                            continue;
                        }

                        if (playerContext.FindMailMessage(header.MailId) != null)
                        {
                            continue;
                        }

                        allExist = false;

                        MailMessage message = await FetchMailDetailAsync(typedClient, header, ct).ConfigureAwait(false);

                        if (message != null)
                        {
                            message.LocalRead = false;
                            if (string.IsNullOrEmpty(message.UUID))
                            {
                                message.UUID = Guid.NewGuid().ToString();
                            }

                            playerContext.AddMailMessage(message);
                            playerContext.MarkDirty<MailMessage>(message.UUID);
                            newCount++;
                        }
                    }

                    if (allExist)
                    {
                        break;
                    }

                    offset += pageSize;
                }
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
            {
                Log.Error("Authentication failed for mail sync (HTTP {0})", ex.StatusCode);
                return -1;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 429)
            {
                Log.Warn("Rate limited during mail sync (HTTP 429)");
                return -2;
            }

            if (newCount > 0)
            {
                playerContext.WriteContext();
                OnMailDataChanged();
            }

            return newCount;
        }

        /// <summary>
        /// Returns the count of messages where LocalRead is false.
        /// </summary>
        /// <param name="playerContext">The player context containing mail messages.</param>
        /// <returns>The number of unread messages.</returns>
        public static int GetUnreadCount(PlayerContext playerContext)
        {
            int count = 0;
            var messages = playerContext.MailMessageList;
            for (int i = 0; i < messages.Count; i++)
            {
                if (!messages[i].LocalRead)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Marks a message as locally read and persists the change.
        /// Only sets LocalRead; never modifies MailRead.
        /// </summary>
        /// <param name="mailId">The unique mail identifier to mark as read.</param>
        /// <param name="playerContext">The player context containing mail messages.</param>
        public static void MarkAsRead(int mailId, PlayerContext playerContext)
        {
            var msg = playerContext.FindMailMessage(mailId);
            if (msg == null)
            {
                return;
            }

            if (msg.LocalRead)
            {
                return;
            }

            msg.LocalRead = true;
            playerContext.MarkDirty<MailMessage>(msg.UUID);
            playerContext.WriteContext();
            OnMailDataChanged();
        }

        /// <summary>
        /// Returns messages matching all filter criteria (AND logic), sorted by SentTime descending.
        /// </summary>
        /// <param name="playerContext">The player context containing mail messages.</param>
        /// <param name="readFilter">Read status filter: "All", "Unread", or "Read".</param>
        /// <param name="typeFilter">Mail type filter: "All", "Player/System", "Colony", "Research", or "Skill".</param>
        /// <param name="searchText">Case-insensitive substring to match against FromName, Subject, or MailContent.</param>
        /// <returns>A read-only list of matching messages sorted by SentTime descending.</returns>
        public static IReadOnlyList<MailMessage> GetFilteredMessages(
            PlayerContext playerContext,
            string readFilter,
            string typeFilter,
            string searchText)
        {
            var source = playerContext.MailMessageList;
            var filtered = new List<MailMessage>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                var msg = source[i];

                if (!PassesReadFilter(msg, readFilter))
                {
                    continue;
                }

                if (!PassesTypeFilter(msg, typeFilter))
                {
                    continue;
                }

                if (!PassesSearchFilter(msg, searchText))
                {
                    continue;
                }

                filtered.Add(msg);
            }

            filtered.Sort(CompareBySentTimeDescending);
            return filtered;
        }

        /// <summary>
        /// Fires the MailDataChanged event to notify subscribers that mail data has been modified.
        /// </summary>
        public static void OnMailDataChanged()
        {
            MailDataChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Fetches a mail detail and converts the DTO to a local MailMessage.
        /// Falls back to constructing the message from the header if the detail fetch fails.
        /// </summary>
        /// <param name="typedClient">The typed game API client.</param>
        /// <param name="header">The mail header from the list response.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A MailMessage constructed from the detail or header, or null on failure.</returns>
        private static async Task<MailMessage> FetchMailDetailAsync(
            IGameApiTypedClient typedClient,
            MailHeader header,
            CancellationToken ct)
        {
            try
            {
                var body = await typedClient.GetMailBodyAsync(header.MailId, ct).ConfigureAwait(false);
                return BuildMessageFromBody(body);
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
            {
                throw;
            }
            catch (ApiHttpException ex) when (ex.StatusCode == 429)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Mail detail fetch failed for mailId={0}, storing with empty content", header.MailId);
                return BuildMessageFromHeader(header);
            }
        }

        private static MailMessage BuildMessageFromBody(MailBody body)
        {
            return new MailMessage
            {
                MailId = body.MailId,
                CharacterIdFrom = body.CharacterIdFrom,
                FromName = (body.FromName ?? string.Empty).Trim(),
                CharacterIdTo = body.CharacterIdTo,
                ToName = (body.ToName ?? string.Empty).Trim(),
                SentTime = body.SentTime.ToString("o", CultureInfo.InvariantCulture),
                Subject = body.Subject ?? string.Empty,
                MailRead = body.MailRead,
                MailType = body.MailType,
                MailContent = body.MailContent ?? string.Empty,
            };
        }

        private static MailMessage BuildMessageFromHeader(MailHeader header)
        {
            return new MailMessage
            {
                MailId = header.MailId,
                CharacterIdFrom = header.CharacterIdFrom,
                FromName = (header.FromName ?? string.Empty).Trim(),
                CharacterIdTo = header.CharacterIdTo,
                ToName = (header.ToName ?? string.Empty).Trim(),
                SentTime = header.SentTime.ToString("o", CultureInfo.InvariantCulture),
                Subject = header.Subject ?? string.Empty,
                MailRead = header.MailRead,
                MailType = header.MailType,
                MailContent = string.Empty,
            };
        }

        private static bool PassesReadFilter(MailMessage msg, string readFilter)
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

        private static bool PassesTypeFilter(MailMessage msg, string typeFilter)
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
                    return msg.MailType == AssetTypeCodes.Commodity;
                case "Research":
                    return msg.MailType == AssetTypeCodes.Resource;
                case "Skill":
                    return msg.MailType == AssetTypeCodes.ShipPart;
                default:
                    return true;
            }
        }

        private static bool PassesSearchFilter(MailMessage msg, string searchText)
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

        private static int CompareBySentTimeDescending(MailMessage a, MailMessage b)
        {
            DateTime dtA = ParseSentTime(a.SentTime);
            DateTime dtB = ParseSentTime(b.SentTime);
            return dtB.CompareTo(dtA);
        }

        private static DateTime ParseSentTime(string sentTime)
        {
            if (string.IsNullOrEmpty(sentTime))
            {
                return DateTime.MinValue;
            }

            DateTime result;
            if (DateTime.TryParse(sentTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return DateTime.MinValue;
        }
    }
}
