// <copyright file="SkillBonusCalculator.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralized calculator for skill-adjusted rates and times.
    /// All skill bonus formulas live here so consumers don't duplicate the math.
    /// </summary>
    public static class SkillBonusCalculator
    {
        /// <summary>
        /// Returns the ExtractionFocus multiplier for mining output.
        /// Formula: 1.0 + (level * 0.01) — +1% per level.
        /// </summary>
        public static decimal GetExtractionMultiplier(int extractionFocusLevel)
        {
            return 1.0m + (extractionFocusLevel * GameConstants.ExtractionFocusRatePerLevel);
        }

        /// <summary>
        /// Returns the RefiningFocus multiplier for refining output.
        /// Formula: 1.0 + (level * 0.02) — +2% per level.
        /// </summary>
        public static decimal GetRefiningMultiplier(int refiningFocusLevel)
        {
            return 1.0m + (refiningFocusLevel * GameConstants.RefiningFocusRatePerLevel);
        }

        /// <summary>
        /// Returns the ProductionFocus multiplier for manufacturing/commodity time reduction.
        /// Formula: 1.0 + (level * 0.03) — +3% per level (reduces time, so divide by this).
        /// </summary>
        public static decimal GetProductionMultiplier(int productionFocusLevel)
        {
            return 1.0m + (productionFocusLevel * GameConstants.ProductionFocusRatePerLevel);
        }

        /// <summary>
        /// Returns the Builder multiplier for structure build time reduction.
        /// Formula: 1.0 + (level * 0.02) — +2% per level (reduces time, so divide by this).
        /// </summary>
        public static decimal GetBuilderMultiplier(int builderLevel)
        {
            return 1.0m + (builderLevel * GameConstants.BuilderRatePerLevel);
        }

        /// <summary>
        /// Returns the skill-adjusted mining output rate for a given base amount.
        /// </summary>
        public static decimal GetAdjustedMiningRate(decimal baseAmount, int extractionFocusLevel)
        {
            return baseAmount * GetExtractionMultiplier(extractionFocusLevel);
        }

        /// <summary>
        /// Returns the skill-adjusted refining output rate for a standard (non-synthetic) refiner.
        /// Formula: consumeRate * purityMultiplier * refiningMultiplier.
        /// </summary>
        public static decimal GetAdjustedRefiningOutputRate(
            string purity,
            int consumeRate,
            int refiningFocusLevel)
        {
            int purityMultiplier = GetPurityMultiplier(purity);
            decimal refiningMultiplier = GetRefiningMultiplier(refiningFocusLevel);
            return consumeRate * purityMultiplier * refiningMultiplier;
        }

        /// <summary>
        /// Returns the skill-adjusted refining output rate for a synthetic recipe.
        /// Formula: recipe.ProduceRate * refiningMultiplier.
        /// </summary>
        public static decimal GetAdjustedSyntheticRefiningOutputRate(
            int recipeProduceRate,
            int refiningFocusLevel)
        {
            decimal refiningMultiplier = GetRefiningMultiplier(refiningFocusLevel);
            return recipeProduceRate * refiningMultiplier;
        }

        /// <summary>
        /// Returns the skill-adjusted refining output rate for a structure,
        /// automatically detecting whether it uses a synthetic recipe or standard refining.
        /// </summary>
        public static decimal GetAdjustedRefiningOutputRate(
            ColonyStructure structure,
            int refiningFocusLevel)
        {
            if (!string.IsNullOrEmpty(structure.RefiningResource) &&
                !string.IsNullOrEmpty(structure.RefiningResourcePurity))
            {
                var recipe = RefiningRecipes.FindByInput(
                    structure.RefiningResource, structure.RefiningResourcePurity);
                if (recipe != null)
                {
                    return GetAdjustedSyntheticRefiningOutputRate(
                        recipe.ProduceRate, refiningFocusLevel);
                }
            }

            return GetAdjustedRefiningOutputRate(
                structure.RefiningResourcePurity ?? GameConstants.PurityLow,
                GameConstants.RefiningBaseRate,
                refiningFocusLevel);
        }

        /// <summary>
        /// Returns the purity multiplier for standard refining output.
        /// </summary>
        private static int GetPurityMultiplier(string purity)
        {
            switch (purity)
            {
                case GameConstants.PurityLow:
                    return GameConstants.PurityMultiplierLow;
                case GameConstants.PurityMedium:
                    return GameConstants.PurityMultiplierMedium;
                case GameConstants.PurityHigh:
                    return GameConstants.PurityMultiplierHigh;
                default:
                    return GameConstants.PurityMultiplierLow;
            }
        }
    }
}
