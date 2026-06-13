// <copyright file="ApiHttpException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Thrown when the API returns an HTTP 4xx or 5xx status code.
    /// </summary>
    [Serializable]
    public class ApiHttpException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class.
        /// </summary>
        public ApiHttpException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ApiHttpException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="responseBody">The raw response body.</param>
        public ApiHttpException(int statusCode, string responseBody)
            : base($"HTTP {statusCode}: {responseBody}")
        {
            this.StatusCode = statusCode;
            this.ResponseBody = responseBody;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="responseBody">The raw response body.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiHttpException(int statusCode, string responseBody, Exception innerException)
            : base($"HTTP {statusCode}: {responseBody}", innerException)
        {
            this.StatusCode = statusCode;
            this.ResponseBody = responseBody;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHttpException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ApiHttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.StatusCode = info.GetInt32(nameof(this.StatusCode));
            this.ResponseBody = info.GetString(nameof(this.ResponseBody));
        }

        /// <summary>
        /// Gets the HTTP status code returned by the server.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the raw response body returned by the server.
        /// </summary>
        public string ResponseBody { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.StatusCode), this.StatusCode);
            info.AddValue(nameof(this.ResponseBody), this.ResponseBody);
        }
    }
}
