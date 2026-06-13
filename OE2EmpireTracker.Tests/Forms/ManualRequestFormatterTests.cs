// <copyright file="ManualRequestFormatterTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Unit tests for ManualRequestFormatter.FormatResponse.
    /// **Validates: Requirements 5.1, 5.2, 5.3**
    /// </summary>
    [TestFixture]
    public class ManualRequestFormatterTests
    {
        /// <summary>
        /// Success response starts with "HTTP 200 OK" header followed by a blank line
        /// and contains indented JSON.
        /// **Validates: Requirements 5.1, 5.2**
        /// </summary>
        [Test]
        public void FormatResponse_Success_HasStatusHeaderAndIndentedJson()
        {
            string body = "{\"id\":1,\"name\":\"test\"}";

            string result = ManualRequestFormatter.FormatResponse(true, 200, body);

            Assert.That(result, Does.StartWith("HTTP 200 OK\r\n\r\n"));
            Assert.That(result, Does.Contain("  \"id\": 1"));
            Assert.That(result, Does.Contain("  \"name\": \"test\""));
        }

        /// <summary>
        /// Error response shows "HTTP {code} — {body}" with em-dash (U+2014).
        /// **Validates: Requirement 5.3**
        /// </summary>
        [Test]
        public void FormatResponse_Error_ShowsStatusCodeAndBody()
        {
            string result = ManualRequestFormatter.FormatResponse(false, 404, "Not found");

            Assert.That(result, Is.EqualTo("HTTP 404 \u2014 Not found"));
        }

        /// <summary>
        /// When the success response body is not valid JSON, FormatResponse falls back
        /// to the raw body text without throwing.
        /// **Validates: Requirement 5.3**
        /// </summary>
        [Test]
        public void FormatResponse_Success_InvalidJson_FallsBackToRawBody()
        {
            string body = "not json at all";

            string result = ManualRequestFormatter.FormatResponse(true, 200, body);

            Assert.That(result, Does.StartWith("HTTP 200 OK\r\n\r\n"));
            Assert.That(result, Does.Contain("not json at all"));
        }
    }
}
