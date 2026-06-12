using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Client;
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
        /// Fetches pages of mail IDs starting at offset 0, stopping when all IDs
        /// on a page already exist locally. For each new mailId, fetches the
        /// detail endpoint for full content.
        /// </summary>
        /// <param name="client">The game API client instance.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="tokenAccessor">A function that returns the current access token (supports mid-sync refresh).</param>
        /// <param name="playerContext">The player context for persistence.</param>
        /// <returns>The number of new messages synced, or -1 on error.</returns>
        public static async Task<int> SyncMailAsync(
            GameApiClient client,
            string appId,
            Func<string> tokenAccessor,
            PlayerContext playerContext)
        {
            int offset = 0;
            int pageSize = 50;
            int newCount = 0;

            while (true)
            {
                string token = tokenAccessor();
                var apiResult = await client.GetMailListAsync(appId, token, offset, pageSize).ConfigureAwait(false);
                if (!apiResult.Success)
                {
                    if (apiResult.Json == "401" || apiResult.Json == "403")
                    {
                        Log.Error("Authentication failed for mail sync");
                        return -1;
                    }

                    if (apiResult.Json == "429")
                    {
                        Log.Warn("Rate limited during mail sync");
                        return -1;
                    }

                    Log.Error("Mail sync failed: network error");
                    return -1;
                }

                if (string.IsNullOrEmpty(apiResult.Json))
                {
                    Log.Error("Mail sync failed: empty response at offset {0}", offset);
                    return -1;
                }

                JArray mails;
                try
                {
                    var envelope = JObject.Parse(apiResult.Json);
                    mails = envelope["data"]?["mail"] as JArray;
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Mail sync failed: JSON parse error at offset {0}", offset);
                    return -1;
                }

                if (mails == null || mails.Count == 0)
                {
                    break;
                }

                bool allExist = true;

                foreach (var item in mails)
                {
                    int mailId = item.Value<int>("mailId");
                    if (mailId <= 0)
                    {
                        continue;
                    }

                    if (playerContext.FindMailMessage(mailId) != null)
                    {
                        continue;
                    }

                    allExist = false;

                    var detailResult = await FetchMailDetailWithRetryAsync(client, appId, tokenAccessor, mailId).ConfigureAwait(false);
                    MailMessage message;

                    if (detailResult.Success && !string.IsNullOrEmpty(detailResult.Json))
                    {
                        message = ParseMailFromDetail(detailResult.Json);
                    }
                    else
                    {
                        Log.Error("Mail detail fetch failed for mailId={0}, storing with empty content", mailId);
                        message = ParseMailFromListItem(item);
                    }

                    if (message != null)
                    {
                        message.LocalRead = false;
                        playerContext.AddMailMessage(message);
                        newCount++;
                    }
                }

                if (allExist)
                {
                    break;
                }

                offset += pageSize;
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
        /// Fetches a mail detail with a single retry on 401 (token expired).
        /// Re-reads the token accessor to pick up a potentially-refreshed token.
        /// </summary>
        /// <param name="client">The game API client instance.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="tokenAccessor">A function that returns the current access token.</param>
        /// <param name="mailId">The mail identifier to fetch.</param>
        /// <returns>The API result tuple.</returns>
        private static async Task<(bool Success, string Json)> FetchMailDetailWithRetryAsync(
            GameApiClient client,
            string appId,
            Func<string> tokenAccessor,
            int mailId)
        {
            string token = tokenAccessor();
            var result = await client.GetMailDetailAsync(appId, token, mailId).ConfigureAwait(false);

            if (!result.Success && result.Json == "401")
            {
                Log.Warn("Mail detail 401 for mailId={0}, re-reading token and retrying once.", mailId);
                string refreshedToken = tokenAccessor();
                if (refreshedToken != token)
                {
                    result = await client.GetMailDetailAsync(appId, refreshedToken, mailId).ConfigureAwait(false);
                }
            }

            return result;
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

        private static MailMessage ParseMailFromDetail(string json)
        {
            try
            {
                var envelope = JObject.Parse(json);
                var data = envelope["data"];
                if (data == null)
                {
                    return null;
                }

                var message = new MailMessage
                {
                    MailId = data.Value<int>("mailId"),
                    CharacterIdFrom = data.Value<int>("characterIdFrom"),
                    FromName = (data.Value<string>("fromName") ?? string.Empty).Trim(),
                    CharacterIdTo = data.Value<int>("characterIdTo"),
                    ToName = (data.Value<string>("toName") ?? string.Empty).Trim(),
                    SentTime = data.Value<string>("sentTime") ?? string.Empty,
                    Subject = data.Value<string>("subject") ?? string.Empty,
                    MailRead = data.Value<bool>("mailRead"),
                    MailType = data.Value<string>("mailType"),
                    MailContent = data.Value<string>("mailContent") ?? string.Empty,
                };

                return message;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Failed to parse mail detail JSON");
                return null;
            }
        }

        private static MailMessage ParseMailFromListItem(JToken item)
        {
            try
            {
                var message = new MailMessage
                {
                    MailId = item.Value<int>("mailId"),
                    CharacterIdFrom = item.Value<int>("characterIdFrom"),
                    FromName = (item.Value<string>("fromName") ?? string.Empty).Trim(),
                    CharacterIdTo = item.Value<int>("characterIdTo"),
                    ToName = (item.Value<string>("toName") ?? string.Empty).Trim(),
                    SentTime = item.Value<string>("sentTime") ?? string.Empty,
                    Subject = item.Value<string>("subject") ?? string.Empty,
                    MailRead = item.Value<bool>("mailRead"),
                    MailType = item.Value<string>("mailType"),
                    MailContent = string.Empty,
                };

                return message;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Failed to parse mail list item JSON");
                return null;
            }
        }
    }
}
