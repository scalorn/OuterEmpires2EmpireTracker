// <copyright file="FactionAuthorizationException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Thrown on HTTP 403 (authorization denied).
    /// </summary>
    [Serializable]
    public class FactionAuthorizationException : FactionServerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionAuthorizationException"/> class.
        /// </summary>
        public FactionAuthorizationException()
            : base("Authorization denied")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionAuthorizationException"/> class.
        /// </summary>
        /// <param name="message">The authorization error message.</param>
        public FactionAuthorizationException(string message)
            : base($"Authorization denied: {message}")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionAuthorizationException"/> class.
        /// </summary>
        /// <param name="message">The authorization error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FactionAuthorizationException(string message, Exception innerException)
            : base($"Authorization denied: {message}", innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionAuthorizationException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FactionAuthorizationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
