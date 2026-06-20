using System;

namespace OE2EmpireTracker.Common.Interfaces
{
    /// <summary>
    /// Thrown when a read/load operation fails on any storage backend.
    /// </summary>
    public class StorageLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageLoadException"/> class.
        /// </summary>
        public StorageLoadException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageLoadException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        public StorageLoadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageLoadException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public StorageLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageLoadException"/> class.
        /// </summary>
        /// <param name="backendType">The backend type that failed.</param>
        /// <param name="location">The file path or connection info.</param>
        /// <param name="message">A human-readable error message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public StorageLoadException(string backendType, string location, string message, Exception innerException)
            : base(message, innerException)
        {
            BackendType = backendType;
            Location = location;
        }

        /// <summary>
        /// Gets the backend type that encountered the failure.
        /// </summary>
        public string BackendType { get; }

        /// <summary>
        /// Gets the file path or connection info where the failure occurred.
        /// </summary>
        public string Location { get; }
    }

    /// <summary>
    /// Thrown when a write operation fails on any storage backend.
    /// </summary>
    public class StorageWriteException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWriteException"/> class.
        /// </summary>
        public StorageWriteException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWriteException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        public StorageWriteException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWriteException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public StorageWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWriteException"/> class.
        /// </summary>
        /// <param name="backendType">The backend type that failed.</param>
        /// <param name="operation">Details about the write operation that failed.</param>
        /// <param name="message">A human-readable error message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public StorageWriteException(string backendType, string operation, string message, Exception innerException)
            : base(message, innerException)
        {
            BackendType = backendType;
            Operation = operation;
        }

        /// <summary>
        /// Gets the backend type that encountered the failure.
        /// </summary>
        public string BackendType { get; }

        /// <summary>
        /// Gets details about the write operation that failed.
        /// </summary>
        public string Operation { get; }
    }

    /// <summary>
    /// Thrown when a rollback itself fails after a schema migration failure,
    /// indicating possible data corruption.
    /// </summary>
    public class StorageCorruptionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCorruptionException"/> class.
        /// </summary>
        public StorageCorruptionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCorruptionException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        public StorageCorruptionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCorruptionException"/> class.
        /// </summary>
        /// <param name="message">A human-readable error message.</param>
        /// <param name="innerException">The underlying exception.</param>
        public StorageCorruptionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCorruptionException"/> class.
        /// </summary>
        /// <param name="backendType">The backend type that failed.</param>
        /// <param name="migrationError">The original migration error.</param>
        /// <param name="rollbackError">The rollback error that occurred after migration failure.</param>
        public StorageCorruptionException(string backendType, Exception migrationError, Exception rollbackError)
            : base(
                $"Storage corruption: migration failed and rollback also failed for {backendType}. " +
                $"Migration error: {migrationError?.Message}. Rollback error: {rollbackError?.Message}",
                migrationError)
        {
            BackendType = backendType;
            MigrationError = migrationError;
            RollbackError = rollbackError;
        }

        /// <summary>
        /// Gets the backend type that encountered the corruption.
        /// </summary>
        public string BackendType { get; }

        /// <summary>
        /// Gets the original migration error that triggered the rollback attempt.
        /// </summary>
        public Exception MigrationError { get; }

        /// <summary>
        /// Gets the rollback error that occurred after the migration failure.
        /// </summary>
        public Exception RollbackError { get; }
    }
}
