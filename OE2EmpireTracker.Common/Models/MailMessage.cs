using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents a single mail message received by the player from the game API.
    /// </summary>
    public class MailMessage
    {
        /// <summary>
        /// Gets or sets the unique mail identifier from the game API.
        /// </summary>
        [JsonProperty("mailId")]
        public int MailId { get; set; }

        /// <summary>
        /// Gets or sets the character ID of the sender.
        /// </summary>
        [JsonProperty("characterIdFrom")]
        public int CharacterIdFrom { get; set; }

        /// <summary>
        /// Gets or sets the sender's display name.
        /// </summary>
        [JsonProperty("fromName")]
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the character ID of the recipient.
        /// </summary>
        [JsonProperty("characterIdTo")]
        public int CharacterIdTo { get; set; }

        /// <summary>
        /// Gets or sets the recipient's display name.
        /// </summary>
        [JsonProperty("toName")]
        public string ToName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sent timestamp as an ISO 8601 string from the API.
        /// </summary>
        [JsonProperty("sentTime")]
        public string SentTime { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message subject line.
        /// </summary>
        [JsonProperty("subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the message has been read according to the game API.
        /// </summary>
        [JsonProperty("mailRead")]
        public bool MailRead { get; set; }

        /// <summary>
        /// Gets or sets the mail type classifier: null (player/system), "C" (colony),
        /// "R" (research), "S" (skill).
        /// </summary>
        [JsonProperty("mailType")]
        public string MailType { get; set; }

        /// <summary>
        /// Gets or sets the full message body content.
        /// </summary>
        [JsonProperty("mailContent")]
        public string MailContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the user has read this message locally in the tracker.
        /// Independent of the API's MailRead field.
        /// </summary>
        [JsonProperty("localRead")]
        public bool LocalRead { get; set; }
    }
}
