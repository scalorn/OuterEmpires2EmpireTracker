// <copyright file="FormGameApiStatus.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.GameApiStatus
{
    /// <summary>
    /// MDI child form displaying real-time and historical Game API metrics.
    /// </summary>
    public partial class FormGameApiStatus : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly System.Windows.Forms.Timer _countdownTimer;
        private int _isProgrammaticUpdate;
        private bool _refreshPending;
        private decimal[] _graphSamples;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormGameApiStatus"/> class.
        /// </summary>
        public FormGameApiStatus()
        {
            this.InitializeComponent();

            this.pnlTpsGraph.Paint += this.PnlTpsGraph_Paint;

            this._refreshTimer = new System.Windows.Forms.Timer();
            this._refreshTimer.Interval = 1000;
            this._refreshTimer.Tick += this.RefreshTimer_Tick;

            this._countdownTimer = new System.Windows.Forms.Timer();
            this._countdownTimer.Interval = 1000;
            this._countdownTimer.Tick += this.CountdownTimer_Tick;

            this.btnReset.Click += this.BtnReset_Click;
            this.btnCopyToClipboard.Click += this.BtnCopyToClipboard_Click;
            this.btnTestFetch.Click += this.BtnTestFetch_Click;
            this.cboTestLocationType.SelectedIndex = 0;

            GameApiMetricsCollector.Instance.MetricsUpdated += this.OnMetricsUpdated;

            this.UpdateDisplay(GameApiMetricsCollector.Instance.GetCurrentSnapshot());

            this._refreshTimer.Start();
            this._countdownTimer.Start();
        }

        /// <inheritdoc/>
        public void BeginProgrammaticUpdate()
        {
            Interlocked.Increment(ref this._isProgrammaticUpdate);
        }

        /// <inheritdoc/>
        public void EndProgrammaticUpdate()
        {
            Interlocked.Decrement(ref this._isProgrammaticUpdate);
        }

        /// <summary>
        /// Formats a byte count into a human-readable string (B, KB, or MB).
        /// </summary>
        /// <param name="bytes">The byte count to format.</param>
        /// <returns>A formatted string with appropriate unit suffix.</returns>
        internal static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes.ToString() + " B";
            }

            if (bytes < 1048576)
            {
                return (bytes / 1024.0).ToString("F2") + " KB";
            }

            return (bytes / 1048576.0).ToString("F2") + " MB";
        }

        /// <summary>
        /// Formats a metrics snapshot as plain text for clipboard copy.
        /// One metric per line with label and value.
        /// </summary>
        /// <param name="snapshot">The metrics snapshot to format.</param>
        /// <returns>A multi-line plain text summary of all metrics.</returns>
        internal static string FormatClipboardText(MetricsSnapshot snapshot)
        {
            var sb = new System.Text.StringBuilder();

            if (snapshot.ResetTimestamp.HasValue)
            {
                sb.AppendLine("Reset: " + snapshot.ResetTimestamp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            }

            sb.AppendLine("Total Requests: " + snapshot.TotalRequests.ToString());
            sb.AppendLine("Successful (2xx): " + snapshot.SuccessCount.ToString());
            sb.AppendLine("Client Errors (4xx): " + snapshot.ClientErrorCount.ToString());
            sb.AppendLine("Server Errors (5xx): " + snapshot.ServerErrorCount.ToString());
            sb.AppendLine("Rate Limited (429): " + snapshot.RateLimitedCount.ToString());
            sb.AppendLine("Exceptions: " + snapshot.ExceptionCount.ToString());
            sb.AppendLine("Current TPS: " + snapshot.CurrentTps.ToString("F2"));
            sb.AppendLine("Bytes Sent: " + FormatBytes(snapshot.BytesSent));
            sb.AppendLine("Bytes Received: " + FormatBytes(snapshot.BytesReceived));
            sb.AppendLine("Outstanding: " + snapshot.OutstandingRequests.ToString());
            sb.AppendLine("Peak Outstanding: " + snapshot.PeakOutstanding.ToString());
            sb.Append("Circuit Breaker: Closed");

            return sb.ToString();
        }

        /// <summary>
        /// Converts a <see cref="SecureString"/> to a plain-text string for API calls.
        /// </summary>
        /// <param name="secureString">The secure string to convert.</param>
        /// <returns>The plain-text value, or null if the input is null.</returns>
        private static string SecureStringToPlainText(SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            GameApiMetricsCollector.Instance.MetricsUpdated -= this.OnMetricsUpdated;

            this._refreshTimer.Stop();
            this._countdownTimer.Stop();

            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);

            base.OnFormClosed(e);
        }

        private void OnMetricsUpdated(object sender, MetricsSnapshot snapshot)
        {
            this._refreshPending = true;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (!this._refreshPending)
            {
                return;
            }

            this._refreshPending = false;
            var snapshot = GameApiMetricsCollector.Instance.GetCurrentSnapshot();
            this.UpdateDisplay(snapshot);

            this._graphSamples = GameApiMetricsCollector.Instance.GetTpsSamples();
            this.pnlTpsGraph.Invalidate();

            this.PopulateHistoryGrid(GameApiMetricsCollector.Instance.GetHistory());
        }

        private void UpdateDisplay(MetricsSnapshot snapshot)
        {
            this.lblTps.Text = "TPS: " + snapshot.CurrentTps.ToString("F2");
            this.lblTotalRequests.Text = "Total: " + snapshot.TotalRequests.ToString();
            this.lblOutstanding.Text = "Outstanding: " + snapshot.OutstandingRequests.ToString();
            this.lblPeakOutstanding.Text = "Peak: " + snapshot.PeakOutstanding.ToString();
            this.lblSuccess.Text = "2xx: " + snapshot.SuccessCount.ToString();
            this.lblClientErrors.Text = "4xx: " + snapshot.ClientErrorCount.ToString();
            this.lblServerErrors.Text = "5xx: " + snapshot.ServerErrorCount.ToString();
            this.lblRateLimited.Text = "429: " + snapshot.RateLimitedCount.ToString();
            this.lblExceptions.Text = "Exceptions: " + snapshot.ExceptionCount.ToString();
            this.lblBytesSent.Text = "Sent: " + FormatBytes(snapshot.BytesSent);
            this.lblBytesReceived.Text = "Received: " + FormatBytes(snapshot.BytesReceived);
        }

        private void PnlTpsGraph_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int width = this.pnlTpsGraph.ClientSize.Width;
            int height = this.pnlTpsGraph.ClientSize.Height;
            int margin = 4;

            // Determine Y-axis scale (minimum 1.00)
            decimal maxTps = 1.00m;
            decimal[] samples = this._graphSamples;
            if (samples != null)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (samples[i] > maxTps)
                    {
                        maxTps = samples[i];
                    }
                }
            }

            // Draw grid lines
            using (var gridPen = new Pen(Color.FromArgb(40, 128, 128, 128)))
            {
                for (int i = 1; i <= 4; i++)
                {
                    int y = margin + (int)((height - (2 * margin)) * i / 5.0);
                    g.DrawLine(gridPen, margin, y, width - margin, y);
                }
            }

            // Draw TPS line
            if (samples != null && samples.Length > 1)
            {
                using (var linePen = new Pen(Color.FromArgb(0, 120, 215), 1.5f))
                {
                    float xStep = (float)(width - (2 * margin)) / (samples.Length - 1);
                    var points = new PointF[samples.Length];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        float x = margin + (i * xStep);
                        float y = height - margin - (float)((double)(samples[i] / maxTps) * (height - (2 * margin)));
                        points[i] = new PointF(x, y);
                    }

                    g.DrawLines(linePen, points);
                }
            }
        }

        private void PopulateHistoryGrid(IReadOnlyList<RequestRecord> history)
        {
            this.dgvHistory.Rows.Clear();

            for (int i = history.Count - 1; i >= 0; i--)
            {
                var record = history[i];
                this.dgvHistory.Rows.Add(
                    record.Timestamp.ToString("HH:mm:ss.fff"),
                    record.HttpMethod,
                    record.Endpoint,
                    record.StatusCode.ToString(),
                    record.DurationMs.ToString(),
                    record.BytesReceived.ToString());
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            GameApiMetricsCollector.Instance.ResetCounters();
            this.lblResetTimestamp.Text = "Reset: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void BtnCopyToClipboard_Click(object sender, EventArgs e)
        {
            var snapshot = GameApiMetricsCollector.Instance.GetCurrentSnapshot();
            string text = FormatClipboardText(snapshot);

            try
            {
                Clipboard.SetText(text);
                this.btnCopyToClipboard.Text = "Copied!";

                var revertTimer = new System.Windows.Forms.Timer();
                revertTimer.Interval = 2000;
                revertTimer.Tick += (s, args) =>
                {
                    revertTimer.Stop();
                    revertTimer.Dispose();
                    this.btnCopyToClipboard.Text = "Copy to Clipboard";
                };
                revertTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy metrics to clipboard.");
                this.btnCopyToClipboard.Text = "Copy failed";

                var revertTimer = new System.Windows.Forms.Timer();
                revertTimer.Interval = 2000;
                revertTimer.Tick += (s, args) =>
                {
                    revertTimer.Stop();
                    revertTimer.Dispose();
                    this.btnCopyToClipboard.Text = "Copy to Clipboard";
                };
                revertTimer.Start();
            }
        }

        private async void BtnTestFetch_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(this.txtTestLocationId.Text.Trim(), out int locationId))
            {
                this.txtTestResult.Text = "Error: Location ID must be a number";
                return;
            }

            string locationType = this.cboTestLocationType.SelectedItem?.ToString() ?? "Co";
            this.btnTestFetch.Enabled = false;
            this.txtTestResult.Text = "Fetching...";

            try
            {
                var context = GameApiContext.Instance;
                if (context == null)
                {
                    this.txtTestResult.Text = "Error: Game API is not initialized. Check Preferences.";
                    return;
                }

                var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
                string playerUUID = EmpireContext.PlayerContext.CurrentPlayerUUID;
                if (string.IsNullOrEmpty(playerUUID))
                {
                    this.txtTestResult.Text = "Error: No player selected.";
                    return;
                }

                SecureString secureSecret = context.CredentialManager.GetKey(playerUUID);
                if (secureSecret == null)
                {
                    this.txtTestResult.Text = "Error: No API key configured for current player.";
                    return;
                }

                string secret = SecureStringToPlainText(secureSecret);
                secureSecret.Dispose();

                var tokenResult = await context.Client.ExchangeTokenAsync(
                    settings.AppId,
                    settings.ClientId,
                    secret).ConfigureAwait(true);

                if (!tokenResult.Success)
                {
                    this.txtTestResult.Text = "Token exchange failed: " + (tokenResult.ErrorMessage ?? "unknown");
                    return;
                }

                var result = await context.Client.GetAssetLocationDetailAsync(
                    settings.AppId,
                    tokenResult.Token.AccessToken,
                    locationId,
                    locationType).ConfigureAwait(true);

                if (result.Success)
                {
                    try
                    {
                        var obj = Newtonsoft.Json.Linq.JToken.Parse(result.Json);
                        this.txtTestResult.Text = obj.ToString(Newtonsoft.Json.Formatting.Indented);
                    }
                    catch
                    {
                        this.txtTestResult.Text = result.Json;
                    }
                }
                else
                {
                    this.txtTestResult.Text = "Failed: " + (result.Json ?? "null");
                }
            }
            catch (Exception ex)
            {
                this.txtTestResult.Text = "Exception: " + ex.Message;
            }
            finally
            {
                this.btnTestFetch.Enabled = true;
            }
        }

        private void UpdateCircuitBreakerDisplay()
        {
            // Placeholder: actual CB state integration depends on GameApiClient exposing state.
            // For now, show "Closed" as default. When Open, show countdown in "Xm Ys" format.
            string state = "Closed";
            TimeSpan? remaining = null;

            if (remaining.HasValue && state == "Open")
            {
                int minutes = (int)remaining.Value.TotalMinutes;
                int seconds = remaining.Value.Seconds;
                this.lblCircuitBreaker.Text = "Circuit Breaker: Open (" + minutes.ToString() + "m " + seconds.ToString() + "s)";
            }
            else
            {
                this.lblCircuitBreaker.Text = "Circuit Breaker: " + state;
            }
        }

        private void UpdateRateLimiterDisplay()
        {
            // Placeholder: actual integration depends on GameApiClient exposing RateLimitState.
            // For now, show default placeholder values.
            this.lblRateLimit.Text = "Rate Limit: --/min";
            this.lblRateLimitState.Text = "Rate Limiter: Active";
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateCircuitBreakerDisplay();
        }
    }
}
