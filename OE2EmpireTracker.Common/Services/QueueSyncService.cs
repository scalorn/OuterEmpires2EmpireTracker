// <copyright file="QueueSyncService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using NLog;
using OE2EmpireTracker.Client;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Central orchestrator for queue-based background sync.
    /// Replaces sequential API calls with parallel dispatch via GameApiRequestQueue.
    /// </summary>
    public class QueueSyncService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;
        private readonly GameApiClient _apiClient;
        private readonly GameApiConnectionSettings _settings;

        private readonly object _syncLock = new object();

        private volatile bool _isSyncRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSyncService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for data persistence.</param>
        /// <param name="empireContext">The empire context for shared game data.</param>
        /// <param name="apiClient">The game API client for HTTP communication.</param>
        /// <param name="settings">The connection settings (TPS, AppId, etc.).</param>
        public QueueSyncService(
            PlayerContext playerContext,
            EmpireContext empireContext,
            GameApiClient apiClient,
            GameApiConnectionSettings settings)
        {
            _playerContext = playerContext;
            _empireContext = empireContext;
            _apiClient = apiClient;
            _settings = settings;
        }

        /// <summary>
        /// Gets a value indicating whether a sync cycle is currently running.
        /// </summary>
        public bool IsSyncRunning => _isSyncRunning;

        /// <summary>
        /// Runs a single sync cycle. Returns immediately if a cycle is already in progress.
        /// </summary>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>The sync result with success/failure counts and elapsed time.</returns>
        public async Task<QueueSyncResult> RunSyncAsync(CancellationToken ct = default)
        {
            lock (_syncLock)
            {
                if (_isSyncRunning)
                {
                    Log.Debug("Sync cycle already in progress, skipping.");
                    return new QueueSyncResult();
                }

                _isSyncRunning = true;
            }

            try
            {
                Log.Info("Queue sync cycle started.");

                // TODO: Future tasks will add queue construction, seed work items, and DrainAsync.
                await Task.CompletedTask;

                Log.Info("Queue sync cycle completed.");
                return new QueueSyncResult();
            }
            finally
            {
                _isSyncRunning = false;
            }
        }
    }
}
