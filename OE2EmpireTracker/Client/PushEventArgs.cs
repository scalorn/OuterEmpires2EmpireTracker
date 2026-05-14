// <copyright file="PushEventArgs.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// Event arguments for push events received via WebSocket.
    /// </summary>
    public class PushEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the event type (Created, Updated, Deleted, TimerTick, MembershipChanged, etc.).
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the entity type (Colony, Blueprint, Character, etc.).
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity UUID.
        /// </summary>
        public string EntityUUID { get; set; }

        /// <summary>
        /// Gets or sets the character UUID that owns the entity.
        /// </summary>
        public string CharacterUUID { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the event (ISO 8601).
        /// </summary>
        public string Timestamp { get; set; }
    }
}
