// <copyright file="ServerContext.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Security;
using NLog;
using OE2EmpireTracker.Common.Client.FactionServer;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Singleton that holds the remote server infrastructure instances
    /// (IFactionServerTypedClient, FactionPushClient, SyncManager, OfflineQueue).
    /// Initialized on application startup when operating mode is not LocalOnly.
    /// </summary>
    public class ServerContext : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static ServerContext _instance;

        private bool _disposed;

        private ServerContext(
            IFactionServerTypedClient typedClient,
            FactionPushClient pushClient,
            RemoteFactionClient client,
            SyncManager syncManager,
            OfflineQueue offlineQueue,
            OperatingMode mode)
        {
            TypedClient = typedClient;
            PushClient = pushClient;
            Client = client;
            SyncManager = syncManager;
            OfflineQueue = offlineQueue;
            Mode = mode;
        }

        /// <summary>
        /// Gets the singleton instance, or null if not initialized.
        /// </summary>
        public static ServerContext Instance => _instance;

        /// <summary>
        /// Gets the typed faction server client for HTTP API calls (null if LocalOnly).
        /// </summary>
        public IFactionServerTypedClient TypedClient { get; }

        /// <summary>
        /// Gets the push client for WebSocket events (null if LocalOnly).
        /// </summary>
        public FactionPushClient PushClient { get; }

        /// <summary>
        /// Gets the legacy remote faction client (null if LocalOnly).
        /// Retained for backward compatibility until all consumers are migrated.
        /// </summary>
        public RemoteFactionClient Client { get; }

        /// <summary>
        /// Gets the sync manager.
        /// </summary>
        public SyncManager SyncManager { get; }

        /// <summary>
        /// Gets the offline queue.
        /// </summary>
        public OfflineQueue OfflineQueue { get; }

        /// <summary>
        /// Gets the current operating mode.
        /// </summary>
        public OperatingMode Mode { get; }

        /// <summary>
        /// Initializes the server context from stored preferences.
        /// If operating mode is LocalOnly or server URL is empty, no client is created.
        /// If connection fails on startup, logs a warning and continues in offline mode.
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            var prefs = PreferencesStore.GetInstance().Preferences;
            var settings = prefs.ServerConnection;

            if (settings.Mode == OperatingMode.LocalOnly)
            {
                Log.Info("ServerContext: Operating in LocalOnly mode, no server connection");
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.ServerUrl))
            {
                Log.Warn("ServerContext: Mode is {0} but ServerUrl is empty, staying offline", settings.Mode);
                return;
            }

            SecureString token = CredentialStore.Unprotect(settings.ProtectedBearerToken);

            FactionServerTypedClient typedClient = null;
            FactionPushClient pushClient = null;
            RemoteFactionClient legacyClient = null;
            try
            {
                typedClient = new FactionServerTypedClient(
                    settings.ServerUrl,
                    token,
                    settings.TrustedThumbprint);

                SecureString tokenCopy = token.Copy();
                pushClient = new FactionPushClient(
                    settings.ServerUrl,
                    tokenCopy,
                    settings.TrustedThumbprint,
                    typedClient);

                legacyClient = new RemoteFactionClient(
                    settings.ServerUrl,
                    token,
                    settings.TrustedThumbprint);

                Log.Info("ServerContext: Created FactionServerTypedClient + FactionPushClient for {0}", settings.ServerUrl);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "ServerContext: Failed to create clients, continuing offline");
                typedClient?.Dispose();
                pushClient?.Dispose();
                legacyClient?.Dispose();
                token?.Dispose();
                return;
            }

            var offlineQueue = new OfflineQueue();
            offlineQueue.Load();

            var syncManager = new SyncManager(typedClient, pushClient, offlineQueue);
            syncManager.Mode = settings.Mode;

            _instance = new ServerContext(typedClient, pushClient, legacyClient, syncManager, offlineQueue, settings.Mode);
            _instance.PushClient.RateLimitChanged += _instance.OnRateLimitChanged;

            Log.Info(
                "ServerContext: Initialized (Mode={0}, QueuedChanges={1})",
                settings.Mode,
                offlineQueue.Count);
        }

        /// <summary>
        /// Resets the singleton (disposes resources). Used for testing and shutdown.
        /// </summary>
        public static void Reset()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
            }
        }

        /// <summary>
        /// Releases all resources used by the server context.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and (optionally) managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    if (PushClient != null)
                    {
                        PushClient.RateLimitChanged -= OnRateLimitChanged;
                        PushClient.Dispose();
                    }

                    TypedClient?.Dispose();
                    Client?.Dispose();
                }
            }
        }

        private void OnRateLimitChanged(object sender, RateLimitChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var typedClient = TypedClient as FactionServerTypedClient;
            if (typedClient != null)
            {
                typedClient.ApplyRateLimit(e.RequestsPerMinute);
                Log.Debug("Rate limit synchronized to typed client: {0} RPM", e.RequestsPerMinute);
            }
        }
    }
}
