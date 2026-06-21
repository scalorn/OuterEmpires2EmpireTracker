// <copyright file="DirtyTracker.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Composite key identifying a dirty/deleted entity by type and UUID.
    /// </summary>
    internal struct DirtyKey : IEquatable<DirtyKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirtyKey"/> struct.
        /// </summary>
        /// <param name="entityType">The .NET type of the entity.</param>
        /// <param name="entityUUID">The UUID of the entity.</param>
        public DirtyKey(Type entityType, string entityUUID)
        {
            EntityType = entityType;
            EntityUUID = entityUUID;
        }

        /// <summary>
        /// Gets the .NET type of the entity (Colony, Blueprint, etc.).
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// Gets the UUID of the entity.
        /// </summary>
        public string EntityUUID { get; }

        /// <summary>
        /// Determines whether this key equals another key.
        /// </summary>
        /// <param name="other">The other key to compare.</param>
        /// <returns>True if both type and UUID match.</returns>
        public bool Equals(DirtyKey other)
        {
            return EntityType == other.EntityType
                && string.Equals(EntityUUID, other.EntityUUID, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is DirtyKey other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (EntityType?.GetHashCode() ?? 0);
                hash = (hash * 31) + (EntityUUID?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// Tracks which entities have been modified or deleted since the last persistence.
    /// Thread-safe via internal locking.
    /// </summary>
    public class DirtyTracker
    {
        private readonly object _lock = new object();
        private readonly HashSet<DirtyKey> _dirty = new HashSet<DirtyKey>();
        private readonly HashSet<DirtyKey> _deleted = new HashSet<DirtyKey>();

        /// <summary>
        /// Gets a value indicating whether any entity of any type is dirty or deleted.
        /// </summary>
        public bool HasChanges
        {
            get
            {
                lock (_lock)
                {
                    return _dirty.Count > 0 || _deleted.Count > 0;
                }
            }
        }

        /// <summary>
        /// Marks an entity as modified. Called by service classes after mutations.
        /// </summary>
        /// <typeparam name="T">The entity type (Colony, Blueprint, etc.).</typeparam>
        /// <param name="entityUUID">The UUID of the modified entity.</param>
        public void MarkDirty<T>(string entityUUID)
        {
            lock (_lock)
            {
                _dirty.Add(new DirtyKey(typeof(T), entityUUID));
            }
        }

        /// <summary>
        /// Marks an entity as deleted. Called by service classes after removals.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityUUID">The UUID of the deleted entity.</param>
        public void MarkDeleted<T>(string entityUUID)
        {
            lock (_lock)
            {
                _deleted.Add(new DirtyKey(typeof(T), entityUUID));
            }
        }

        /// <summary>
        /// Gets all dirty entity UUIDs for a given type.
        /// </summary>
        /// <typeparam name="T">The entity type to query.</typeparam>
        /// <returns>A list of UUIDs that have been marked dirty for the given type.</returns>
        public IReadOnlyList<string> GetDirtyUUIDs<T>()
        {
            lock (_lock)
            {
                return _dirty
                    .Where(k => k.EntityType == typeof(T))
                    .Select(k => k.EntityUUID)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets all deleted entity UUIDs for a given type.
        /// </summary>
        /// <typeparam name="T">The entity type to query.</typeparam>
        /// <returns>A list of UUIDs that have been marked deleted for the given type.</returns>
        public IReadOnlyList<string> GetDeletedUUIDs<T>()
        {
            lock (_lock)
            {
                return _deleted
                    .Where(k => k.EntityType == typeof(T))
                    .Select(k => k.EntityUUID)
                    .ToList();
            }
        }

        /// <summary>
        /// Clears the dirty flag for a single entity after successful persistence.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityUUID">The UUID of the entity to clear.</param>
        public void ClearDirty<T>(string entityUUID)
        {
            lock (_lock)
            {
                _dirty.Remove(new DirtyKey(typeof(T), entityUUID));
            }
        }

        /// <summary>
        /// Clears the deleted record for a single entity after successful deletion.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityUUID">The UUID of the entity to clear.</param>
        public void ClearDeleted<T>(string entityUUID)
        {
            lock (_lock)
            {
                _deleted.Remove(new DirtyKey(typeof(T), entityUUID));
            }
        }

        /// <summary>
        /// Gets all dirty entity UUIDs for a given type (non-generic overload for runtime type dispatch).
        /// </summary>
        /// <param name="entityType">The entity type to query.</param>
        /// <returns>A list of UUIDs that have been marked dirty for the given type.</returns>
        public IReadOnlyList<string> GetDirtyUUIDs(Type entityType)
        {
            lock (_lock)
            {
                return _dirty
                    .Where(k => k.EntityType == entityType)
                    .Select(k => k.EntityUUID)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets all deleted entity UUIDs for a given type (non-generic overload for runtime type dispatch).
        /// </summary>
        /// <param name="entityType">The entity type to query.</param>
        /// <returns>A list of UUIDs that have been marked deleted for the given type.</returns>
        public IReadOnlyList<string> GetDeletedUUIDs(Type entityType)
        {
            lock (_lock)
            {
                return _deleted
                    .Where(k => k.EntityType == entityType)
                    .Select(k => k.EntityUUID)
                    .ToList();
            }
        }

        /// <summary>
        /// Clears the dirty flag for a single entity after successful persistence (non-generic overload).
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityUUID">The UUID of the entity to clear.</param>
        public void ClearDirty(Type entityType, string entityUUID)
        {
            lock (_lock)
            {
                _dirty.Remove(new DirtyKey(entityType, entityUUID));
            }
        }

        /// <summary>
        /// Clears the deleted record for a single entity after successful deletion (non-generic overload).
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityUUID">The UUID of the entity to clear.</param>
        public void ClearDeleted(Type entityType, string entityUUID)
        {
            lock (_lock)
            {
                _deleted.Remove(new DirtyKey(entityType, entityUUID));
            }
        }

        /// <summary>
        /// Clears all dirty and deleted flags (used after full-file write or fresh load).
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _dirty.Clear();
                _deleted.Clear();
            }
        }
    }
}
