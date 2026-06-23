// <copyright file="FactionServerException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client.FactionServer
{
    /// <summary>
    /// Base exception for all Faction Server errors.
    /// </summary>
    [Serializable]
    public class FactionServerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactionServerException"/> class.
        /// </summary>
        public FactionServerException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionServerException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public FactionServerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionServerException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FactionServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactionServerException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FactionServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
