// <copyright file="ManualRequestFormatter.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

namespace OE2EmpireTracker.Forms.GameApiStatus
{
    using Newtonsoft.Json;

    /// <summary>
    /// Formats API responses for display in the manual request panel.
    /// </summary>
    public static class ManualRequestFormatter
    {
        /// <summary>
        /// Formats an API response for display.
        /// </summary>
        /// <param name="success">Whether the response indicates success (HTTP 200).</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="responseBody">The raw response body text.</param>
        /// <returns>A formatted display string.</returns>
        public static string FormatResponse(bool success, int statusCode, string responseBody)
        {
            if (!success)
            {
                return "HTTP " + statusCode + " \u2014 " + responseBody;
            }

            string prettyJson = TryFormatJson(responseBody);
            return "HTTP 200 OK\r\n\r\n" + prettyJson;
        }

        private static string TryFormatJson(string body)
        {
            try
            {
                object parsed = JsonConvert.DeserializeObject(body);
                return JsonConvert.SerializeObject(parsed, Formatting.Indented);
            }
            catch (JsonException)
            {
                return body;
            }
        }
    }
}
