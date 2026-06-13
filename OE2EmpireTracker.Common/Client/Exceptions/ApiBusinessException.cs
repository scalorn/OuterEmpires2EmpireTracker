// <copyright file="ApiBusinessException.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OE2EmpireTracker.Common.Client
{
    /// <summary>
    /// Thrown when the API envelope contains success=false, indicating a business logic error.
    /// </summary>
    [Serializable]
    public class ApiBusinessException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class.
        /// </summary>
        public ApiBusinessException()
        {
            this.Errors = Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ApiBusinessException(string message)
            : base(message)
        {
            this.Errors = Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiBusinessException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.Errors = Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class.
        /// </summary>
        /// <param name="returnCode">The business error return code from the envelope.</param>
        /// <param name="returnString">The business error message from the envelope.</param>
        /// <param name="errors">The list of field-level validation errors, if any.</param>
        public ApiBusinessException(int returnCode, string returnString, IReadOnlyList<ApiValidationError> errors)
            : base($"API business error {returnCode}: {returnString}")
        {
            this.ReturnCode = returnCode;
            this.ReturnString = returnString;
            this.Errors = errors ?? Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class.
        /// </summary>
        /// <param name="returnCode">The business error return code from the envelope.</param>
        /// <param name="returnString">The business error message from the envelope.</param>
        /// <param name="errors">The list of field-level validation errors, if any.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiBusinessException(
            int returnCode,
            string returnString,
            IReadOnlyList<ApiValidationError> errors,
            Exception innerException)
            : base($"API business error {returnCode}: {returnString}", innerException)
        {
            this.ReturnCode = returnCode;
            this.ReturnString = returnString;
            this.Errors = errors ?? Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBusinessException"/> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ApiBusinessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.ReturnCode = info.GetInt32(nameof(this.ReturnCode));
            this.ReturnString = info.GetString(nameof(this.ReturnString));
            this.Errors = Array.Empty<ApiValidationError>();
        }

        /// <summary>
        /// Gets the business error return code from the API envelope.
        /// </summary>
        public int ReturnCode { get; }

        /// <summary>
        /// Gets the business error message from the API envelope.
        /// </summary>
        public string ReturnString { get; }

        /// <summary>
        /// Gets the list of field-level validation errors returned by the API.
        /// </summary>
        public IReadOnlyList<ApiValidationError> Errors { get; }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(this.ReturnCode), this.ReturnCode);
            info.AddValue(nameof(this.ReturnString), this.ReturnString);
        }
    }
}
