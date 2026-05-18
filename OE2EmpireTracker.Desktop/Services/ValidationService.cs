using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Provides basic validation methods for entity names and numeric fields.
/// Returns null when valid, or an error message string when invalid.
/// </summary>
public sealed class ValidationService
{
    /// <summary>
    /// Validates that a name is not empty or whitespace.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="entityType">The entity type for the error message (e.g. "Colony").</param>
    /// <returns>An error message if invalid; null if valid.</returns>
    public string? ValidateName(string name, string entityType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return $"{entityType} name cannot be empty.";
        }

        return null;
    }

    /// <summary>
    /// Validates that a name is not a duplicate among existing names.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="entityType">The entity type for the error message.</param>
    /// <param name="existingNames">The collection of existing names to check against.</param>
    /// <param name="excludeUuid">Optional UUID to exclude (for edit scenarios where the current entity's name is in the list).</param>
    /// <returns>An error message if duplicate; null if valid.</returns>
    public string? ValidateDuplicateName(string name, string entityType, IEnumerable<string> existingNames, string? excludeUuid)
    {
        _ = excludeUuid;

        if (existingNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"A {entityType} with the name \"{name}\" already exists.";
        }

        return null;
    }

    /// <summary>
    /// Validates that a numeric value is positive (greater than zero).
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="fieldName">The field name for the error message.</param>
    /// <returns>An error message if not positive; null if valid.</returns>
    public string? ValidatePositive(long value, string fieldName)
    {
        if (value <= 0)
        {
            return $"{fieldName} must be greater than zero.";
        }

        return null;
    }
}
