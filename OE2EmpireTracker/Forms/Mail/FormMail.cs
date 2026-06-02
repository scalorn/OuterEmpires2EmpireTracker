// <copyright file="FormMail.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Mail
{
    /// <summary>
    /// MDI child form for viewing and managing in-game mail messages.
    /// </summary>
    public partial class FormMail : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate;

        private PlayerContext playerContext;

        private System.Threading.Timer _syncTimer;

        private bool _isSyncing;

        private DateTime? _lastSyncTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormMail"/> class.
        /// </summary>
        public FormMail()
        {
            this.InitializeComponent();
            this.playerContext = EmpireContext.PlayerContext;

            // Populate filter dropdowns.
            this.cboReadFilter.Items.AddRange(new object[] { "All", "Unread", "Read" });
            this.cboReadFilter.SelectedIndex = 0;

            this.cboTypeFilter.Items.AddRange(new object[] { "All", "Player/System", "Colony", "Research", "Skill" });
            this.cboTypeFilter.SelectedIndex = 0;

            // Subscribe to PlayerContext events.
            this.playerContext.MailDataChanged += this.OnMailDataChanged;
            this.playerContext.CurrentPlayerChanged += this.OnCurrentPlayerChanged;

            // Wire filter change handlers.
            this.cboReadFilter.SelectedIndexChanged += this.OnFilterChanged;
            this.cboTypeFilter.SelectedIndexChanged += this.OnFilterChanged;
            this.txtSearch.TextChanged += this.OnFilterChanged;

            // Wire list selection handler.
            this.lvwMail.SelectedIndexChanged += this.OnMailSelected;

            // Start background sync timer (one-shot mode, immediate first sync).
            this._syncTimer = new System.Threading.Timer(
                this.OnSyncTimerTick,
                null,
                Timeout.Infinite,
                Timeout.Infinite);
            this.lblSyncStatus.Text = "Last sync: Never";
            Task.Run(() => this.ExecuteSyncAsync());
        }

        /// <inheritdoc/>
        public void BeginProgrammaticUpdate()
        {
            this._isProgrammaticUpdate++;
        }

        /// <inheritdoc/>
        public void EndProgrammaticUpdate()
        {
            this._isProgrammaticUpdate--;
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this._syncTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            this._syncTimer?.Dispose();
            this._syncTimer = null;

            this.playerContext.MailDataChanged -= this.OnMailDataChanged;
            this.playerContext.CurrentPlayerChanged -= this.OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        private void OnMailDataChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => this.OnMailDataChanged(sender, e)));
                return;
            }

            this.RefreshMailList();
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => this.OnCurrentPlayerChanged(sender, e)));
                return;
            }

            this.RefreshMailList();
        }

        private void OnSyncTimerTick(object state)
        {
            if (this._isSyncing)
            {
                return;
            }

            Task.Run(() => this.ExecuteSyncAsync());
        }

        private async Task ExecuteSyncAsync()
        {
            if (this._isSyncing)
            {
                return;
            }

            this._isSyncing = true;
            try
            {
                this.UpdateSyncStatus("Syncing...");

                var gameApi = GameApiContext.Instance;
                if (gameApi == null)
                {
                    return;
                }

                var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
                if (settings == null)
                {
                    return;
                }

                string appId = settings.AppId;
                string clientId = settings.ClientId;
                string ownerUUID = this.playerContext.CurrentPlayerUUID;

                if (string.IsNullOrEmpty(ownerUUID))
                {
                    return;
                }

                SecureString secureSecret = gameApi.CredentialManager.GetKey(ownerUUID);
                if (secureSecret == null)
                {
                    return;
                }

                string secret = CredentialStore.SecureStringToString(secureSecret);
                secureSecret.Dispose();

                var tokenResult = await gameApi.Client.ExchangeTokenAsync(appId, clientId, secret).ConfigureAwait(false);
                if (!tokenResult.Success)
                {
                    Log.Error("Mail sync: token exchange failed: {0}", tokenResult.ErrorMessage);
                    this.UpdateSyncStatus("Sync failed: auth error");
                    int retryMs = PreferencesStore.GetMailSyncIntervalMs();
                    this._syncTimer?.Change(retryMs, Timeout.Infinite);
                    return;
                }

                string accessToken = tokenResult.Token.AccessToken;
                int result = await MailService.SyncMailAsync(
                    gameApi.Client,
                    appId,
                    accessToken,
                    this.playerContext).ConfigureAwait(false);

                if (result < 0)
                {
                    this.UpdateSyncStatus("Sync failed: rate limited");
                }
                else
                {
                    this._lastSyncTime = DateTime.Now;
                    string lastSyncDisplay = this._lastSyncTime.Value.ToString(
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture);

                    if (result > 0)
                    {
                        this.UpdateSyncStatus("Synced: " + result + " new messages | Last sync: " + lastSyncDisplay);
                    }
                    else
                    {
                        this.UpdateSyncStatus("Sync complete: up to date | Last sync: " + lastSyncDisplay);
                    }
                }

                Log.Info("Mail sync completed: {0} new messages", result);

                // Reschedule timer for next sync.
                int intervalMs = PreferencesStore.GetMailSyncIntervalMs();
                this._syncTimer?.Change(intervalMs, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Mail sync failed unexpectedly");
                this.UpdateSyncStatus("Sync failed: network error");
                int intervalMs = PreferencesStore.GetMailSyncIntervalMs();
                this._syncTimer?.Change(intervalMs, Timeout.Infinite);
            }
            finally
            {
                this._isSyncing = false;
            }
        }

        private void UpdateSyncStatus(string status)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => this.UpdateSyncStatus(status)));
                return;
            }

            this.lblSyncStatus.Text = status;
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (this._isProgrammaticUpdate > 0)
            {
                return;
            }

            this.RefreshMailList();
        }

        private void RefreshMailList()
        {
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                string readFilter = this.cboReadFilter.SelectedItem?.ToString() ?? "All";
                string typeFilter = this.cboTypeFilter.SelectedItem?.ToString() ?? "All";
                string searchText = this.txtSearch.Text ?? string.Empty;

                IReadOnlyList<MailMessage> filtered = MailService.GetFilteredMessages(
                    this.playerContext,
                    readFilter,
                    typeFilter,
                    searchText);

                this.lvwMail.Items.Clear();

                Font boldFont = new Font(this.Font, FontStyle.Bold);

                for (int i = 0; i < filtered.Count; i++)
                {
                    MailMessage msg = filtered[i];

                    string dateDisplay;
                    if (string.IsNullOrEmpty(msg.SentTime))
                    {
                        dateDisplay = "(no date)";
                    }
                    else
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(
                            msg.SentTime,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out parsed))
                        {
                            dateDisplay = parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            dateDisplay = "(no date)";
                        }
                    }

                    var item = new ListViewItem(new[] { msg.FromName, msg.Subject, dateDisplay });
                    item.Tag = msg;

                    if (!msg.LocalRead)
                    {
                        item.Font = boldFont;
                    }

                    this.lvwMail.Items.Add(item);
                }

                this.lblMessageCount.Text = filtered.Count + " messages";

                int unreadCount = MailService.GetUnreadCount(this.playerContext);
                this.Text = unreadCount > 0
                    ? "Mail (" + unreadCount + " unread)"
                    : "Mail";

                if (filtered.Count == 0)
                {
                    this.txtDetailContent.Text = "No messages";
                    this.lblDetailFrom.Text = "From:";
                    this.lblDetailDate.Text = "Date:";
                    this.lblDetailSubject.Text = "Subject:";
                }
                else
                {
                    this.lblDetailFrom.Text = "From:";
                    this.lblDetailDate.Text = "Date:";
                    this.lblDetailSubject.Text = "Subject:";
                    this.txtDetailContent.Text = string.Empty;
                }
            }
        }

        private void OnMailSelected(object sender, EventArgs e)
        {
            if (this._isProgrammaticUpdate > 0)
            {
                return;
            }

            if (this.lvwMail.SelectedItems.Count == 0)
            {
                this.lblDetailFrom.Text = "From:";
                this.lblDetailDate.Text = "Date:";
                this.lblDetailSubject.Text = "Subject:";
                this.txtDetailContent.Text = string.Empty;
                return;
            }

            var selectedItem = this.lvwMail.SelectedItems[0];
            var msg = selectedItem.Tag as MailMessage;
            if (msg == null)
            {
                return;
            }

            this.lblDetailFrom.Text = "From: " + msg.FromName;
            this.lblDetailSubject.Text = "Subject: " + msg.Subject;
            this.txtDetailContent.Text = msg.MailContent;

            string dateText = "Date:";
            if (!string.IsNullOrEmpty(msg.SentTime))
            {
                DateTime parsed;
                if (DateTime.TryParse(msg.SentTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    dateText = "Date: " + parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                }
            }

            this.lblDetailDate.Text = dateText;

            if (!msg.LocalRead)
            {
                MailService.MarkAsRead(msg.MailId, this.playerContext);
                selectedItem.Font = this.lvwMail.Font;

                int unreadCount = MailService.GetUnreadCount(this.playerContext);
                this.Text = unreadCount > 0
                    ? "Mail (" + unreadCount + " unread)"
                    : "Mail";
            }
        }
    }
}
