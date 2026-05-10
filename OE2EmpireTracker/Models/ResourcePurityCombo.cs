using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Identifies a unique resource+purity combination for distribution analysis.
    /// </summary>
    public class ResourcePurityCombo
    {
        public string ResourceName { get; set; } = string.Empty;

        public string Purity { get; set; } = string.Empty;

        public string DisplayName => $"{Purity} {ResourceName}".Trim();

        public override bool Equals(object obj)
        {
            if (obj is ResourcePurityCombo other)
            {
                return string.Equals(ResourceName, other.ResourceName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Purity, other.Purity, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (ResourceName?.ToLowerInvariant()?.GetHashCode() ?? 0)
                 ^ (Purity?.ToLowerInvariant()?.GetHashCode() ?? 0);
        }

        public override string ToString() => DisplayName;
    }
}
