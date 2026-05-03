using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for pricing plan data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only PricingPlanService mutates the actual entity.
    /// </summary>
    public class PricingPlanViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Snapshot loaded from - kept for dirty comparison
        private ReadOnlyPricingPlan _original;

        // Local edit state - disconnected from entity
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private decimal _fixedCostPerItem;
        private decimal _hourlyCostRate;
        private Dictionary<string, decimal> _resourcePrices = new Dictionary<string, decimal>();

        // -----------------------------------------------------------------------
        // Read-only state
        // -----------------------------------------------------------------------

        /// <summary>Gets a value indicating whether this is a new plan not yet saved.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets the plan UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the owner UUID.</summary>
        public string OwnerUUID => _ownerUUID;

        /// <summary>Gets the original snapshot this edit buffer was loaded from.</summary>
        public ReadOnlyPricingPlan Original => _original;

        // -----------------------------------------------------------------------
        // Editable fields - local edit state
        // -----------------------------------------------------------------------

        public string Name { get => _name; set => _name = value; }

        public string Description { get => _description; set => _description = value; }

        public decimal FixedCostPerItem { get => _fixedCostPerItem; set => _fixedCostPerItem = value; }

        public decimal HourlyCostRate { get => _hourlyCostRate; set => _hourlyCostRate = value; }

        public Dictionary<string, decimal> ResourcePrices => _resourcePrices;

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether any local field differs from the original snapshot.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    // New plan - dirty once any field has a non-default value
                    return !string.IsNullOrEmpty(_name)
                        || !string.IsNullOrEmpty(_description)
                        || _fixedCostPerItem != 0m
                        || _hourlyCostRate != 0m
                        || _resourcePrices.Count > 0;
                }

                // Scalar fields
                if (_name != _original.Name) return true;
                if (_description != _original.Description) return true;
                if (_fixedCostPerItem != _original.FixedCostPerItem) return true;
                if (_hourlyCostRate != _original.HourlyCostRate) return true;

                // ResourcePrices dictionary
                if (!ResourcePricesEqual(_resourcePrices, _original.ResourcePrices)) return true;

                return false;
            }
        }

        // -----------------------------------------------------------------------
        // LoadFrom / Reset
        // -----------------------------------------------------------------------

        /// <summary>
        /// Loads field values from a ReadOnlyPricingPlan snapshot.
        /// Retains the original for dirty comparison.
        /// </summary>
        public void LoadFrom(ReadOnlyPricingPlan ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID;
            _name = ro.Name;
            _description = ro.Description;
            _fixedCostPerItem = ro.FixedCostPerItem;
            _hourlyCostRate = ro.HourlyCostRate;

            // Deep copy ResourcePrices
            _resourcePrices = ro.ResourcePrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Resets to empty state for a new plan.
        /// </summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _description = string.Empty;
            _fixedCostPerItem = 0m;
            _hourlyCostRate = 0m;
            _resourcePrices = new Dictionary<string, decimal>();
        }

        // -----------------------------------------------------------------------
        // Build request DTOs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds an update request carrying both the original snapshot
        /// and the current local state.
        /// </summary>
        public PricingPlanUpdateRequest BuildUpdateRequest()
        {
            return new PricingPlanUpdateRequest
            {
                Original = _original,
                Name = _name,
                Description = _description,
                FixedCostPerItem = _fixedCostPerItem,
                HourlyCostRate = _hourlyCostRate,
                ResourcePrices = new Dictionary<string, decimal>(_resourcePrices),
            };
        }

        /// <summary>
        /// Builds a create request for a new pricing plan.
        /// </summary>
        public PricingPlanCreateRequest BuildCreateRequest()
        {
            return new PricingPlanCreateRequest
            {
                Name = _name,
                Description = _description,
                FixedCostPerItem = _fixedCostPerItem,
                HourlyCostRate = _hourlyCostRate,
                ResourcePrices = new Dictionary<string, decimal>(_resourcePrices),
            };
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private static bool ResourcePricesEqual(
            Dictionary<string, decimal> local,
            IReadOnlyDictionary<string, decimal> original)
        {
            if (local.Count != original.Count) return false;
            foreach (var kvp in local)
            {
                if (!original.TryGetValue(kvp.Key, out decimal origVal)) return false;
                if (kvp.Value != origVal) return false;
            }

            return true;
        }
    }
}