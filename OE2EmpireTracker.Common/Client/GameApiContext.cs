// <copyright file="GameApiContext.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Top-level singleton coordinating all game API components.
    /// Owns the credential manager, HTTP client, connection monitor, and sync scheduler.
    /// Follows the startup sequence: read settings, check enabled, create components, start monitor and scheduler.
    /// Uses OAuth2 client_credentials flow (appId + clientId + secret) for authentication.
    /// </summary>
    public class GameApiContext : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static GameApiContext _instance;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameApiContext"/> class.
        /// Private to enforce singleton access via <see cref="Initialize"/>.
        /// </summary>
        private GameApiContext(
            GameApiCredentialManager credentialManager,
            GameApiClient client,
            GameApiConnectionMonitor connectionMonitor,
            GameApiSyncScheduler syncScheduler)
        {
            CredentialManager = credentialManager;
            Client = client;
            ConnectionMonitor = connectionMonitor;
            SyncScheduler = syncScheduler;
            LastSyncTimes = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// Gets the singleton instance. Returns null if not initialized or disabled.
        /// </summary>
        public static GameApiContext Instance => _instance;

        /// <summary>
        /// Gets the game API HTTP client.
        /// </summary>
        public GameApiClient Client { get; }

        /// <summary>
        /// Gets the credential manager for per-character secrets.
        /// </summary>
        public GameApiCredentialManager CredentialManager { get; }

        /// <summary>
        /// Gets the connection monitor for connectivity checks.
        /// </summary>
        public GameApiConnectionMonitor ConnectionMonitor { get; }

        /// <summary>
        /// Gets the sync scheduler for periodic profile synchronization.
        /// </summary>
        public GameApiSyncScheduler SyncScheduler { get; }

        /// <summary>
        /// Gets the dictionary of last sync times keyed by player UUID.
        /// </summary>
        public Dictionary<string, DateTime> LastSyncTimes { get; }

        /// <summary>
        /// Initializes the game API context singleton.
        /// Reads settings from preferences, creates all components, and starts the monitor and scheduler.
        /// If the integration is disabled or no characters have configured secrets, logs and returns without creating a context.
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null)
            {
                Log.Warn("GameApiContext.Initialize called but instance already exists; call Reset() first");
                return;
            }

            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            if (settings == null)
            {
                Log.Info("GameApiContext: No GameApiConnection settings found, skipping initialization");
                return;
            }

            if (!settings.Enabled)
            {
                Log.Info("GameApiContext: Game API integration is disabled");
                return;
            }

            if (string.IsNullOrEmpty(settings.ServerUrl))
            {
                Log.Info("GameApiContext: ServerUrl is empty, skipping initialization");
                return;
            }

            if (string.IsNullOrEmpty(settings.AppId))
            {
                Log.Info("GameApiContext: AppId is empty, skipping initialization");
                return;
            }

            if (string.IsNullOrEmpty(settings.ClientId))
            {
                Log.Info("GameApiContext: ClientId is empty, skipping initialization");
                return;
            }

            var credentialManager = new GameApiCredentialManager();
            var configuredPlayers = credentialManager.GetConfiguredPlayerUUIDs();
            if (configuredPlayers.Count == 0)
            {
                Log.Info("GameApiContext: No characters have configured secrets, skipping initialization");
                return;
            }

            string firstPlayerUUID = configuredPlayers[0];
            var client = new GameApiClient(settings.ServerUrl);
            var connectionMonitor = new GameApiConnectionMonitor(
                client,
                credentialManager,
                firstPlayerUUID,
                settings.AppId,
                settings.ClientId);
            var syncScheduler = new GameApiSyncScheduler(
                client,
                credentialManager,
                connectionMonitor,
                settings.AppId,
                settings.ClientId);

            _instance = new GameApiContext(credentialManager, client, connectionMonitor, syncScheduler);

            connectionMonitor.Start(settings.PollingIntervalMinutes);
            syncScheduler.Start(settings.PollingIntervalMinutes);

            Log.Info(
                "GameApiContext initialized: server={0}, appId={1}, clientId={2}, polling={3}min, characters={4}",
                settings.ServerUrl,
                settings.AppId,
                settings.ClientId,
                settings.PollingIntervalMinutes,
                configuredPlayers.Count);
        }

        /// <summary>
        /// Resets the singleton by disposing the current instance and clearing the reference.
        /// Safe to call when no instance exists.
        /// </summary>
        public static void Reset()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
                Log.Info("GameApiContext reset");
            }
        }

        /// <summary>
        /// Releases all resources: stops the monitor and scheduler, disposes the client.
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
                if (disposing)
                {
                    SyncScheduler?.Stop();
                    ConnectionMonitor?.Stop();
                    Client?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
