// <copyright file="FormGameApiStatusTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using NUnit.Framework;
using OE2EmpireTracker.Forms.GameApiStatus;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Unit tests for FormGameApiStatus helper methods: FormatBytes and FormatClipboardText.
    /// </summary>
    [TestFixture]
    public class FormGameApiStatusTests
    {
        // -----------------------------------------------------------------------
        // 15.1: FormatBytes unit tests
        // **Validates: Req 4, Criterion 3**
        // -----------------------------------------------------------------------

        /// <summary>
        /// FormatBytes(0) returns "0 B".
        /// </summary>
        [Test]
        public void FormatBytes_Zero_ReturnsZeroB()
        {
            string result = FormGameApiStatus.FormatBytes(0);
            Assert.That(result, Is.EqualTo("0 B"));
        }

        /// <summary>
        /// FormatBytes(512) returns "512 B".
        /// </summary>
        [Test]
        public void FormatBytes_512_Returns512B()
        {
            string result = FormGameApiStatus.FormatBytes(512);
            Assert.That(result, Is.EqualTo("512 B"));
        }

        /// <summary>
        /// FormatBytes(1023) returns "1023 B" (just below KB threshold).
        /// </summary>
        [Test]
        public void FormatBytes_1023_Returns1023B()
        {
            string result = FormGameApiStatus.FormatBytes(1023);
            Assert.That(result, Is.EqualTo("1023 B"));
        }

        /// <summary>
        /// FormatBytes(1024) returns "1.00 KB" (exact KB boundary).
        /// </summary>
        [Test]
        public void FormatBytes_1024_Returns1Point00KB()
        {
            string result = FormGameApiStatus.FormatBytes(1024);
            Assert.That(result, Is.EqualTo("1.00 KB"));
        }

        /// <summary>
        /// FormatBytes(4608) returns "4.50 KB".
        /// </summary>
        [Test]
        public void FormatBytes_4608_Returns4Point50KB()
        {
            string result = FormGameApiStatus.FormatBytes(4608);
            Assert.That(result, Is.EqualTo("4.50 KB"));
        }

        /// <summary>
        /// FormatBytes(1048576) returns "1.00 MB" (exact MB boundary).
        /// </summary>
        [Test]
        public void FormatBytes_1048576_Returns1Point00MB()
        {
            string result = FormGameApiStatus.FormatBytes(1048576);
            Assert.That(result, Is.EqualTo("1.00 MB"));
        }

        /// <summary>
        /// FormatBytes(12939264) returns "12.34 MB".
        /// </summary>
        [Test]
        public void FormatBytes_12939264_Returns12Point34MB()
        {
            string result = FormGameApiStatus.FormatBytes(12939264);
            Assert.That(result, Is.EqualTo("12.34 MB"));
        }

        // -----------------------------------------------------------------------
        // 15.2: FormatClipboardText unit tests
        // **Validates: Req 8, Criterion 3**
        // -----------------------------------------------------------------------

        /// <summary>
        /// FormatClipboardText produces output containing all expected labels with correct values.
        /// </summary>
        [Test]
        public void FormatClipboardText_ContainsAllLabelsAndValues()
        {
            var snapshot = new MetricsSnapshot(
                currentTps: 1.50m,
                totalRequests: 100,
                successCount: 85,
                clientErrorCount: 10,
                serverErrorCount: 3,
                exceptionCount: 2,
                rateLimitedCount: 5,
                bytesSent: 4608,
                bytesReceived: 12939264,
                outstandingRequests: 3,
                peakOutstanding: 7,
                resetTimestamp: null);

            string result = FormGameApiStatus.FormatClipboardText(snapshot);

            Assert.That(result, Does.Contain("Total Requests: 100"));
            Assert.That(result, Does.Contain("Successful (2xx): 85"));
            Assert.That(result, Does.Contain("Client Errors (4xx): 10"));
            Assert.That(result, Does.Contain("Server Errors (5xx): 3"));
            Assert.That(result, Does.Contain("Rate Limited (429): 5"));
            Assert.That(result, Does.Contain("Exceptions: 2"));
            Assert.That(result, Does.Contain("Current TPS: 1.50"));
            Assert.That(result, Does.Contain("Bytes Sent: 4.50 KB"));
            Assert.That(result, Does.Contain("Bytes Received: 12.34 MB"));
            Assert.That(result, Does.Contain("Outstanding: 3"));
            Assert.That(result, Does.Contain("Peak Outstanding: 7"));
            Assert.That(result, Does.Contain("Circuit Breaker: Closed"));
        }

        /// <summary>
        /// FormatClipboardText produces one metric per line.
        /// </summary>
        [Test]
        public void FormatClipboardText_OneMetricPerLine()
        {
            var snapshot = new MetricsSnapshot(
                currentTps: 0.75m,
                totalRequests: 50,
                successCount: 45,
                clientErrorCount: 3,
                serverErrorCount: 1,
                exceptionCount: 1,
                rateLimitedCount: 2,
                bytesSent: 512,
                bytesReceived: 1024,
                outstandingRequests: 0,
                peakOutstanding: 2,
                resetTimestamp: null);

            string result = FormGameApiStatus.FormatClipboardText(snapshot);
            string[] lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Each non-empty line should contain exactly one label-value pair
            // Verify key labels appear on separate lines
            bool foundTotal = false;
            bool foundSuccess = false;
            bool foundClientErrors = false;
            bool foundServerErrors = false;
            bool foundRateLimited = false;
            bool foundExceptions = false;
            bool foundTps = false;
            bool foundBytesSent = false;
            bool foundBytesReceived = false;
            bool foundOutstanding = false;
            bool foundPeakOutstanding = false;
            bool foundCircuitBreaker = false;

            foreach (string line in lines)
            {
                if (line.StartsWith("Total Requests:")) foundTotal = true;
                if (line.StartsWith("Successful (2xx):")) foundSuccess = true;
                if (line.StartsWith("Client Errors (4xx):")) foundClientErrors = true;
                if (line.StartsWith("Server Errors (5xx):")) foundServerErrors = true;
                if (line.StartsWith("Rate Limited (429):")) foundRateLimited = true;
                if (line.StartsWith("Exceptions:")) foundExceptions = true;
                if (line.StartsWith("Current TPS:")) foundTps = true;
                if (line.StartsWith("Bytes Sent:")) foundBytesSent = true;
                if (line.StartsWith("Bytes Received:")) foundBytesReceived = true;
                if (line.StartsWith("Outstanding:")) foundOutstanding = true;
                if (line.StartsWith("Peak Outstanding:")) foundPeakOutstanding = true;
                if (line.StartsWith("Circuit Breaker:")) foundCircuitBreaker = true;
            }

            Assert.That(foundTotal, Is.True, "Missing 'Total Requests:' on its own line");
            Assert.That(foundSuccess, Is.True, "Missing 'Successful (2xx):' on its own line");
            Assert.That(foundClientErrors, Is.True, "Missing 'Client Errors (4xx):' on its own line");
            Assert.That(foundServerErrors, Is.True, "Missing 'Server Errors (5xx):' on its own line");
            Assert.That(foundRateLimited, Is.True, "Missing 'Rate Limited (429):' on its own line");
            Assert.That(foundExceptions, Is.True, "Missing 'Exceptions:' on its own line");
            Assert.That(foundTps, Is.True, "Missing 'Current TPS:' on its own line");
            Assert.That(foundBytesSent, Is.True, "Missing 'Bytes Sent:' on its own line");
            Assert.That(foundBytesReceived, Is.True, "Missing 'Bytes Received:' on its own line");
            Assert.That(foundOutstanding, Is.True, "Missing 'Outstanding:' on its own line");
            Assert.That(foundPeakOutstanding, Is.True, "Missing 'Peak Outstanding:' on its own line");
            Assert.That(foundCircuitBreaker, Is.True, "Missing 'Circuit Breaker:' on its own line");
        }

        /// <summary>
        /// FormatClipboardText includes reset timestamp when present.
        /// </summary>
        [Test]
        public void FormatClipboardText_WithResetTimestamp_IncludesResetLine()
        {
            var resetTime = new DateTime(2024, 3, 15, 14, 32, 7, DateTimeKind.Utc);
            var snapshot = new MetricsSnapshot(
                currentTps: 0.00m,
                totalRequests: 0,
                successCount: 0,
                clientErrorCount: 0,
                serverErrorCount: 0,
                exceptionCount: 0,
                rateLimitedCount: 0,
                bytesSent: 0,
                bytesReceived: 0,
                outstandingRequests: 0,
                peakOutstanding: 0,
                resetTimestamp: resetTime);

            string result = FormGameApiStatus.FormatClipboardText(snapshot);

            Assert.That(result, Does.Contain("Reset:"));
        }
    }
}
