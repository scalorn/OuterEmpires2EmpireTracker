// <copyright file="FactionConnectionException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Thrown on network failures or timeouts.
    /// </summary>
    [Serializable]
    public class FactionConnectionException : FactionServerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionConnectionException"/> class.
        /// </summary>
        public FactionConnectionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionConnectionException"/> class.
        /// </summary>
        /// <param name="message">The connection error message.</param>
        public FactionConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionConnectionException"/> class.
        /// </summary>
        /// <param name="message">The connection error message.</param>
        /// <param name="inner">The inner exception.</param>
        public FactionConnectionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionConnectionException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FactionConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
