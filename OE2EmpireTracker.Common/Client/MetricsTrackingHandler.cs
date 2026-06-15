// <copyright file="MetricsTrackingHandler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// A delegating handler that measures bytes sent/received and HTTP round-trip duration
    /// for each request. Reports metrics directly to <see cref="GameApiMetricsCollector"/>
    /// using the actual request URL path and HTTP method.
    /// </summary>
    internal class MetricsTrackingHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsTrackingHandler"/> class
        /// with a default <see cref="HttpClientHandler"/> as the inner handler.
        /// </summary>
        public MetricsTrackingHandler()
            : base(new HttpClientHandler())
        {
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string endpoint = request.RequestUri?.PathAndQuery ?? "unknown";
            string httpMethod = request.Method?.Method ?? "GET";
            long bytesSent = request.Content?.Headers?.ContentLength ?? 0;

            GameApiMetricsCollector.Instance?.OnRequestStarted(endpoint, httpMethod);
            var sw = Stopwatch.StartNew();

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                sw.Stop();
                GameApiMetricsCollector.Instance?.OnRequestFailed(endpoint, httpMethod, "NetworkError", "SendAsync threw");
                throw;
            }

            sw.Stop();

            long bytesReceived = response.Content?.Headers?.ContentLength ?? 0;

            if (bytesReceived == 0 && response.Content != null)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                bytesReceived = bytes.Length;

                // Replace the content so downstream consumers can still read it.
                var replacement = new ByteArrayContent(bytes);
                foreach (var header in response.Content.Headers)
                {
                    replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                response.Content = replacement;
            }

            int statusCode = (int)response.StatusCode;
            GameApiMetricsCollector.Instance?.OnRequestCompleted(
                endpoint, httpMethod, statusCode, sw.ElapsedMilliseconds, bytesSent, bytesReceived);

            return response;
        }
    }
}
