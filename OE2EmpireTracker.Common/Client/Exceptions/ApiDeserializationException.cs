// <copyright file="ApiDeserializationException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Thrown when the API response body cannot be deserialized as a valid envelope.
    /// </summary>
    [Serializable]
    public class ApiDeserializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class.
        /// </summary>
        public ApiDeserializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ApiDeserializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiDeserializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="rawBody">The raw response body that failed deserialization.</param>
        public ApiDeserializationException(int statusCode, string rawBody)
            : base($"Failed to deserialize API response (HTTP {statusCode}).")
        {
            this.StatusCode = statusCode;
            this.RawBody = rawBody;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="rawBody">The raw response body that failed deserialization.</param>
        /// <param name="innerException">The inner exception that caused the deserialization failure.</param>
        public ApiDeserializationException(int statusCode, string rawBody, Exception innerException)
            : base($"Failed to deserialize API response (HTTP {statusCode}).", innerException)
        {
            this.StatusCode = statusCode;
            this.RawBody = rawBody;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDeserializationException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ApiDeserializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.StatusCode = info.GetInt32(nameof(this.StatusCode));
            this.RawBody = info.GetString(nameof(this.RawBody));
        }

        /// <summary>
        /// Gets the HTTP status code of the response that failed deserialization.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the raw response body that could not be deserialized.
        /// </summary>
        public string RawBody { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.StatusCode), this.StatusCode);
            info.AddValue(nameof(this.RawBody), this.RawBody);
        }
    }
}
