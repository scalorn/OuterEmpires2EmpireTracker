// <copyright file="FormGameApiStatus.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
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
            this.btnRequest.Click += this.BtnRequest_Click;
            this.btnCopyResponse.Click += this.BtnCopyResponse_Click;
            this.cboEndpoint.SelectedIndexChanged += this.CboEndpoint_SelectedIndexChanged;

            this.PopulateEndpointDropdown();

            GameApiMetricsCollector.Instance.MetricsUpdated += this.OnMetricsUpdated;
            EmpireContext.PlayerContext.CurrentPlayerChanged += this.OnCurrentPlayerChanged;

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
        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            GameApiMetricsCollector.Instance.MetricsUpdated -= this.OnMetricsUpdated;
            EmpireContext.PlayerContext.CurrentPlayerChanged -= this.OnCurrentPlayerChanged;

            this._refreshTimer.Stop();
            this._countdownTimer.Stop();

            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);

            base.OnFormClosed(e);
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

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action(() => this.OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            // Player changed — metrics collector resets; refresh display
            this.UpdateDisplay(GameApiMetricsCollector.Instance.GetCurrentSnapshot());
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

            var sw = Stopwatch.StartNew();
            this._refreshPending = false;
            var snapshot = GameApiMetricsCollector.Instance.GetCurrentSnapshot();
            this.UpdateDisplay(snapshot);

            this._graphSamples = GameApiMetricsCollector.Instance.GetTpsSamples();
            this.pnlTpsGraph.Invalidate();

            this.PopulateHistoryGrid(GameApiMetricsCollector.Instance.GetHistory());
            sw.Stop();
            Log.Info("PERF RefreshTimer_Tick: {0}ms", sw.ElapsedMilliseconds);
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
            var sw = Stopwatch.StartNew();
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

            sw.Stop();
            Log.Info("PERF PopulateHistoryGrid: {0}ms rows={1}", sw.ElapsedMilliseconds, history.Count);
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

        private async void BtnRequest_Click(object sender, EventArgs e)
        {
            this.btnRequest.Enabled = false;
            this.txtResponse.Text = "Fetching...";

            try
            {
                var context = GameApiContext.Instance;
                if (context == null)
                {
                    this.txtResponse.Text = "Error: Game API is not initialized. Check Preferences.";
                    return;
                }

                string playerUUID = EmpireContext.PlayerContext.CurrentPlayerUUID;
                if (string.IsNullOrEmpty(playerUUID))
                {
                    this.txtResponse.Text = "Error: No player selected.";
                    return;
                }

                SecureString secureSecret = context.CredentialManager.GetKey(playerUUID);
                if (secureSecret == null)
                {
                    this.txtResponse.Text = "Error: No API key configured for current player.";
                    return;
                }

                int selectedIndex = this.cboEndpoint.SelectedIndex;
                if (selectedIndex < 0 || selectedIndex >= ManualRequestEndpoint.All.Count)
                {
                    this.txtResponse.Text = "Error: No endpoint selected.";
                    return;
                }

                var endpoint = ManualRequestEndpoint.All[selectedIndex];
                var validation = ManualRequestValidation.ValidateInputs(
                    endpoint,
                    this.txtId.Text.Trim(),
                    this.txtView.Text.Trim(),
                    this.txtMarketIds.Text.Trim());

                if (!validation.IsValid)
                {
                    this.txtResponse.Text = validation.ErrorMessage;
                    return;
                }

                var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
                string secret = SecureStringToPlainText(secureSecret);
                secureSecret.Dispose();

                var tokenResult = await context.Client.ExchangeTokenAsync(
                    settings.AppId,
                    settings.ClientId,
                    secret).ConfigureAwait(true);

                if (!tokenResult.Success)
                {
                    this.txtResponse.Text = "Token exchange failed: " + (tokenResult.ErrorMessage ?? "unknown");
                    return;
                }

                string appId = settings.AppId;
                string token = tokenResult.Token.AccessToken;

                var result = await this.RouteRequestAsync(endpoint, appId, token).ConfigureAwait(true);

                if (result.Success)
                {
                    this.txtResponse.Text = ManualRequestFormatter.FormatResponse(true, 200, result.Json);
                }
                else
                {
                    int statusCode = 0;
                    int.TryParse(result.Json, out statusCode);
                    if (statusCode > 0)
                    {
                        this.txtResponse.Text = ManualRequestFormatter.FormatResponse(false, statusCode, result.Json);
                    }
                    else
                    {
                        this.txtResponse.Text = ManualRequestFormatter.FormatResponse(false, 0, result.Json);
                    }
                }
            }
            catch (Exception ex)
            {
                this.txtResponse.Text = "Exception: " + ex.Message;
            }
            finally
            {
                this.btnRequest.Enabled = true;
            }
        }

        private async System.Threading.Tasks.Task<(bool Success, string Json)> RouteRequestAsync(
            ManualRequestEndpoint endpoint,
            string appId,
            string token)
        {
            var client = GameApiContext.Instance.Client;

            switch (endpoint.DisplayName)
            {
                case "Character":
                    return await client.GetCharacterAsync(appId, token).ConfigureAwait(true);
                case "Character Skills":
                    return await client.GetCharacterSkillsAsync(appId, token).ConfigureAwait(true);
                case "Colony List":
                    return await client.GetColonyListAsync(appId, token).ConfigureAwait(true);
                case "Colony Buildings":
                    return await client.GetColonyBuildingsAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Colony Warehouse":
                    return await client.GetColonyWarehouseAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Colony Workers":
                    return await client.GetColonyWorkersAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Colony Summary":
                    return await client.GetColonySummaryAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Banking Balance":
                    return await client.GetBankingBalanceAsync(appId, token).ConfigureAwait(true);
                case "Banking Transactions":
                    return await client.GetBankingTransactionsAsync(appId, token).ConfigureAwait(true);
                case "Accepted Jobs":
                    return await client.GetAcceptedJobsAsync(appId, token).ConfigureAwait(true);
                case "Asset Locations":
                    return await client.GetAssetLocationsAsync(appId, token).ConfigureAwait(true);
                case "Location Detail":
                    return await client.GetAssetLocationDetailAsync(
                        appId,
                        token,
                        int.Parse(this.txtId.Text.Trim()),
                        this.cboLocationType.SelectedItem?.ToString() ?? AssetTypeCodes.Colony).ConfigureAwait(true);
                case "Blueprint":
                    return await client.GetAssetBlueprintAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Survey":
                    return await client.GetAssetSurveyAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Crate":
                    return await client.GetAssetCrateAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Kill Mail List":
                    return await client.GetKillMailListAsync(appId, token).ConfigureAwait(true);
                case "Kill Mail Detail":
                    return await client.GetKillMailDetailAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Ship Configuration":
                    return await client.GetShipConfigurationAsync(appId, token).ConfigureAwait(true);
                case "Ship Cargo":
                    return await client.GetShipCargoAsync(appId, token).ConfigureAwait(true);
                case "Mail List":
                    return await client.GetMailListAsync(appId, token).ConfigureAwait(true);
                case "Mail Detail":
                    return await client.GetMailDetailAsync(appId, token, int.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Market Listings":
                    return await client.GetMarketListingsAsync(
                        appId,
                        token,
                        this.txtView.Text.Trim(),
                        this.ParseNullableInt(this.txtRange.Text),
                        this.NullIfEmpty(this.txtSearch.Text),
                        this.NullIfEmpty(this.txtType.Text),
                        this.NullIfEmpty(this.txtSubType.Text),
                        this.ParseNullableInt(this.txtEvolution.Text),
                        this.NullIfEmpty(this.txtOrderBy.Text)).ConfigureAwait(true);
                case "Market Prices":
                    return await client.GetMarketPricesAsync(
                        appId,
                        token,
                        this.txtType.Text.Trim(),
                        long.TryParse(this.txtId.Text.Trim(), out long priceTypeId) ? priceTypeId : 0).ConfigureAwait(true);
                case "Market Items":
                    return await client.GetMarketItemsAsync(
                        appId,
                        token,
                        this.txtType.Text.Trim(),
                        this.txtSearch.Text.Trim()).ConfigureAwait(true);
                case "Market Ship Components":
                    return await client.GetMarketShipComponentsAsync(
                        appId,
                        token,
                        long.Parse(this.txtId.Text.Trim())).ConfigureAwait(true);
                case "Market Buy Orders":
                    return await client.GetMarketBuyOrdersAsync(appId, token).ConfigureAwait(true);
                case "Market Sell Orders":
                    return await client.GetMarketSellOrdersAsync(appId, token).ConfigureAwait(true);
                case "Market Buy Competitors":
                    return await client.GetMarketBuyCompetitorsAsync(appId, token, this.txtMarketIds.Text.Trim()).ConfigureAwait(true);
                case "Market Sell Competitors":
                    return await client.GetMarketSellCompetitorsAsync(appId, token, this.txtMarketIds.Text.Trim()).ConfigureAwait(true);
                default:
                    return (false, "Unknown endpoint: " + endpoint.DisplayName);
            }
        }

        private int? ParseNullableInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return int.TryParse(text.Trim(), out int value) ? (int?)value : null;
        }

        private string NullIfEmpty(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private void CboEndpoint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this._isProgrammaticUpdate > 0)
            {
                return;
            }

            this.ApplyEndpointVisibility();
        }

        private void PopulateEndpointDropdown()
        {
            var sw = Stopwatch.StartNew();

            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                this.cboEndpoint.Items.Clear();
                foreach (var endpoint in ManualRequestEndpoint.All)
                {
                    this.cboEndpoint.Items.Add(endpoint.DisplayName);
                }

                int defaultIndex = 0;
                for (int i = 0; i < ManualRequestEndpoint.All.Count; i++)
                {
                    if (ManualRequestEndpoint.All[i].DisplayName == "Location Detail")
                    {
                        defaultIndex = i;
                        break;
                    }
                }

                this.cboEndpoint.SelectedIndex = defaultIndex;
                this.cboLocationType.SelectedIndex = 0;
            }

            this.ApplyEndpointVisibility();
            sw.Stop();
            Log.Info("PERF PopulateEndpointDropdown: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ApplyEndpointVisibility()
        {
            int selectedIndex = this.cboEndpoint.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= ManualRequestEndpoint.All.Count)
            {
                return;
            }

            var endpoint = ManualRequestEndpoint.All[selectedIndex];

            switch (endpoint.Category)
            {
                case EndpointCategory.Parameterless:
                    this.lblId.Visible = false;
                    this.txtId.Visible = false;
                    this.lblLocationType.Visible = false;
                    this.cboLocationType.Visible = false;
                    this.pnlMarketFields.Visible = false;
                    break;

                case EndpointCategory.SingleId:
                    this.lblId.Text = endpoint.IdLabel + ":";
                    this.lblId.Visible = true;
                    this.txtId.Visible = true;
                    this.lblLocationType.Visible = false;
                    this.cboLocationType.Visible = false;
                    this.pnlMarketFields.Visible = false;
                    break;

                case EndpointCategory.LocationDetail:
                    this.lblId.Text = "Location ID:";
                    this.lblId.Visible = true;
                    this.txtId.Visible = true;
                    this.lblLocationType.Visible = true;
                    this.cboLocationType.Visible = true;
                    this.pnlMarketFields.Visible = false;
                    break;

                case EndpointCategory.MarketView:
                    this.lblId.Visible = false;
                    this.txtId.Visible = false;
                    this.lblLocationType.Visible = false;
                    this.cboLocationType.Visible = false;
                    this.pnlMarketFields.Visible = true;
                    this.lblView.Visible = true;
                    this.txtView.Visible = true;
                    this.lblSearch.Visible = true;
                    this.txtSearch.Visible = true;
                    this.lblType.Visible = true;
                    this.txtType.Visible = true;
                    this.lblSubType.Visible = true;
                    this.txtSubType.Visible = true;
                    this.lblOrderBy.Visible = true;
                    this.txtOrderBy.Visible = true;
                    this.lblRange.Visible = true;
                    this.txtRange.Visible = true;
                    this.lblEvolution.Visible = true;
                    this.txtEvolution.Visible = true;
                    this.lblMarketIds.Visible = false;
                    this.txtMarketIds.Visible = false;
                    break;

                case EndpointCategory.MarketCompetitors:
                    this.lblId.Visible = false;
                    this.txtId.Visible = false;
                    this.lblLocationType.Visible = false;
                    this.cboLocationType.Visible = false;
                    this.pnlMarketFields.Visible = true;
                    this.lblView.Visible = false;
                    this.txtView.Visible = false;
                    this.lblSearch.Visible = false;
                    this.txtSearch.Visible = false;
                    this.lblType.Visible = false;
                    this.txtType.Visible = false;
                    this.lblSubType.Visible = false;
                    this.txtSubType.Visible = false;
                    this.lblOrderBy.Visible = false;
                    this.txtOrderBy.Visible = false;
                    this.lblRange.Visible = false;
                    this.txtRange.Visible = false;
                    this.lblEvolution.Visible = false;
                    this.txtEvolution.Visible = false;
                    this.lblMarketIds.Visible = true;
                    this.txtMarketIds.Visible = true;
                    break;
            }
        }

        private void BtnCopyResponse_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtResponse.Text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(this.txtResponse.Text);
                this.btnCopyResponse.Text = "Copied!";

                var revertTimer = new System.Windows.Forms.Timer();
                revertTimer.Interval = 2000;
                revertTimer.Tick += (s, args) =>
                {
                    revertTimer.Stop();
                    revertTimer.Dispose();
                    this.btnCopyResponse.Text = "Copy Response";
                };
                revertTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy response to clipboard.");
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

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateCircuitBreakerDisplay();
        }
    }
}
