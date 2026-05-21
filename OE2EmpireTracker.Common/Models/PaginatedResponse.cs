using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Generic paginated response wrapper for collection endpoints.
    /// </summary>
    /// <typeparam name="T">The entity type contained in the page.</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>Gets or sets the items in the current page.</summary>
        [JsonProperty("items")]
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>Gets or sets the total number of items in the full collection.</summary>
        [JsonProperty("total")]
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>Gets or sets the maximum number of items per page.</summary>
        [JsonProperty("limit")]
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        /// <summary>Gets or sets the zero-based offset into the full collection.</summary>
        [JsonProperty("offset")]
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }
}
