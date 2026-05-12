// <copyright file="OfflineQueue.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Queues data changes made while disconnected for later sync to the server.
    /// Persists the queue to disk so changes survive application restarts.
    /// </summary>
    public class OfflineQueue
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _queueFilePath;

        private List<QueuedChange> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineQueue"/> class.
        /// Uses the default queue file path under %LOCALAPPDATA%\OE2EmpireTracker.
        /// </summary>
        public OfflineQueue()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OE2EmpireTracker",
                "offline-queue.json"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineQueue"/> class
        /// with a custom file path (for testing).
        /// </summary>
        /// <param name="queueFilePath">Path to the queue persistence file.</param>
        public OfflineQueue(string queueFilePath)
        {
            _queueFilePath = queueFilePath;
            _changes = new List<QueuedChange>();
        }

        /// <summary>
        /// Gets the number of queued changes.
        /// </summary>
        public int Count => _changes.Count;

        /// <summary>
        /// Enqueues a change for later sync.
        /// </summary>
        /// <param name="change">The change to queue.</param>
        public void Enqueue(QueuedChange change)
        {
            if (change == null)
            {
                return;
            }

            _changes.Add(change);
            Log.Debug("Queued offline change: {0}/{1}", change.CharacterUUID, change.DataType);
        }

        /// <summary>
        /// Gets all queued changes as a read-only list.
        /// </summary>
        /// <returns>Read-only list of queued changes.</returns>
        public IReadOnlyList<QueuedChange> GetAll()
        {
            return _changes.AsReadOnly();
        }

        /// <summary>
        /// Removes a specific change from the queue (after successful sync).
        /// </summary>
        /// <param name="change">The change to remove.</param>
        public void Remove(QueuedChange change)
        {
            _changes.Remove(change);
        }

        /// <summary>
        /// Clears all queued changes.
        /// </summary>
        public void Clear()
        {
            _changes.Clear();
            Log.Info("Offline queue cleared ({0} items removed)", _changes.Count);
        }

        /// <summary>
        /// Persists the queue to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(_queueFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(_changes, Formatting.Indented);
                File.WriteAllText(_queueFilePath, json);
                Log.Debug("Saved offline queue ({0} items) to {1}", _changes.Count, _queueFilePath);
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Failed to save offline queue to {0}", _queueFilePath);
            }
        }

        /// <summary>
        /// Loads the queue from disk.
        /// </summary>
        public void Load()
        {
            if (!File.Exists(_queueFilePath))
            {
                Log.Debug("No offline queue file found at {0}", _queueFilePath);
                _changes = new List<QueuedChange>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_queueFilePath);
                var loaded = JsonConvert.DeserializeObject<List<QueuedChange>>(json);
                _changes = loaded ?? new List<QueuedChange>();
                Log.Info("Loaded offline queue ({0} items) from {1}", _changes.Count, _queueFilePath);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Malformed offline queue file {0}, starting empty", _queueFilePath);
                _changes = new List<QueuedChange>();
            }
            catch (IOException ex)
            {
                Log.Error(ex, "Failed to read offline queue from {0}", _queueFilePath);
                _changes = new List<QueuedChange>();
            }
        }
    }
}
