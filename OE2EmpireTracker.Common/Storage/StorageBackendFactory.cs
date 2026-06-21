// -----------------------------------------------------------------------
// <copyright file="StorageBackendFactory.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using OE2EmpireTracker.Common.Interfaces;

namespace OE2EmpireTracker.Common.Storage
{
    /// <summary>
    /// Factory that creates the correct <see cref="IStorageBackend"/> from configuration.
    /// </summary>
    public static class StorageBackendFactory
    {
        /// <summary>
        /// Creates and initializes a storage backend.
        /// </summary>
        /// <param name="type">The backend type to create.</param>
        /// <param name="config">Configuration parameters.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An initialized storage backend.</returns>
        /// <exception cref="ArgumentException">Thrown if the backend type is not supported.</exception>
        public static async Task<IStorageBackend> CreateAsync(
            StorageBackendType type,
            StorageBackendConfig config,
            CancellationToken ct = default)
        {
            IStorageBackend backend;
            switch (type)
            {
                case StorageBackendType.JsonSingleFile:
                    throw new NotImplementedException("JsonSingleFileBackend not yet implemented");
                case StorageBackendType.JsonMultiFile:
                    backend = new JsonMultiFileBackend(config.ConnectionString);
                    break;
                case StorageBackendType.Sqlite:
                    throw new NotImplementedException("SqliteBackend not yet implemented");
                case StorageBackendType.DynamoDb:
                    throw new NotImplementedException("DynamoDbBackend not yet implemented");
                case StorageBackendType.Postgres:
                    throw new NotImplementedException("PostgresBackend not yet implemented");
                default:
                    throw new ArgumentException(
                        string.Format("Unsupported backend type: {0}", type),
                        nameof(type));
            }

            await backend.InitializeAsync(ct).ConfigureAwait(false);
            return backend;
        }
    }
}
