using System;
using System.Collections.Generic;
using System.Globalization;
using NLog;
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
                    return msg.MailType == "C";
                case "Research":
                    return msg.MailType == "R";
                case "Skill":
                    return msg.MailType == "S";
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
