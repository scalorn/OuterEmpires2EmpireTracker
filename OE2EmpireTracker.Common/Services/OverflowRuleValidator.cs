using System.Collections.Generic;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Validates <see cref="WarehouseOverflowRule"/> instances before persistence.
    /// </summary>
    public static class OverflowRuleValidator
    {
        /// <summary>
        /// Validates the specified overflow rule and returns a list of error messages.
        /// An empty list indicates the rule is valid.
        /// </summary>
        /// <param name="rule">The overflow rule to validate.</param>
        /// <returns>A list of validation error messages.</returns>
        public static List<string> Validate(WarehouseOverflowRule rule)
        {
            var errors = new List<string>();

            if (rule.TriggerThreshold <= 0m)
            {
                errors.Add("TriggerThreshold must be greater than zero.");
            }

            if (string.IsNullOrEmpty(rule.DestinationUUID))
            {
                errors.Add("DestinationUUID is required.");
            }

            if (string.IsNullOrEmpty(rule.DeliveryRouteUUID))
            {
                errors.Add("DeliveryRouteUUID is required.");
            }

            if (rule.RuleType == OverflowRuleType.SpecificResource)
            {
                if (string.IsNullOrEmpty(rule.ResourceName))
                {
                    errors.Add("ResourceName is required for SpecificResource rules.");
                }

                if (string.IsNullOrEmpty(rule.ResourcePurity))
                {
                    errors.Add("ResourcePurity is required for SpecificResource rules.");
                }
            }

            return errors;
        }
    }
}
