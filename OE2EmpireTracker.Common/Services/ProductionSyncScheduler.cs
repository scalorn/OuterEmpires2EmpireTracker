// <copyright file="ProductionSyncScheduler.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Production subclass of <see cref="GameApiSyncScheduler"/> that delegates
    /// virtual method calls to <see cref="PlayerContext"/> for real data access.
    /// </summary>
    internal class ProductionSyncScheduler : GameApiSyncScheduler
    {
        private readonly PlayerContext _playerContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionSyncScheduler"/> class.
        /// </summary>
        /// <param name="client">The game API client used for profile requests.</param>
        /// <param name="credentialManager">The credential manager for retrieving secrets.</param>
        /// <param name="connectionMonitor">The connection monitor for detecting connect/disconnect transitions.</param>
        /// <param name="appId">The registered application GUID.</param>
        /// <param name="clientId">The player's account identifier.</param>
        /// <param name="playerContext">The player context for data access and persistence.</param>
        public ProductionSyncScheduler(
            GameApiClient client,
            GameApiCredentialManager credentialManager,
            GameApiConnectionMonitor connectionMonitor,
            string appId,
            string clientId,
            PlayerContext playerContext)
            : base(client, credentialManager, connectionMonitor, appId, clientId)
        {
            _playerContext = playerContext;
        }

        /// <inheritdoc/>
        internal override PlayerProfile GetPlayerProfile(string playerUUID)
        {
            return _playerContext.FindMutablePlayerProfile(playerUUID);
        }

        /// <inheritdoc/>
        internal override List<Colony> GetPlayerColonies(string playerUUID)
        {
            return _playerContext.GetMutableColoniesForOwner(playerUUID);
        }

        /// <inheritdoc/>
        internal override void WriteContext()
        {
            _playerContext.WriteContext();
        }

        /// <inheritdoc/>
        internal override void RaiseColonyDataChanged()
        {
            _playerContext.OnColonyDataChanged(string.Empty);
        }

        /// <inheritdoc/>
        internal override void RaiseColonyDataChanged(string colonyUUID)
        {
            _playerContext.OnColonyDataChanged(colonyUUID);
        }
    }
}
