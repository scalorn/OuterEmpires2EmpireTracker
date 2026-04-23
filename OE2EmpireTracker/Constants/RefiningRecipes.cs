using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Constants
{
    public static class RefiningRecipes
    {
        private static List<RefiningRecipe> _recipes = GetFallbackRecipes();

        public static IReadOnlyList<RefiningRecipe> Recipes => _recipes.AsReadOnly();

        /// <summary>
        /// Replaces the recipe list with externally-loaded data (e.g. from BaselineData.json).
        /// THREADING: BackgroundProcessor must be stopped before calling this method.
        /// </summary>
        public static void SetRecipes(List<RefiningRecipe> recipes)
        {
            _recipes = new List<RefiningRecipe>(recipes);
        }

        /// <summary>
        /// Returns the hardcoded fallback recipe list. Used when BaselineData.json
        /// does not contain a RefiningRecipe array.
        /// </summary>
        public static List<RefiningRecipe> GetFallbackRecipes()
        {
            return new List<RefiningRecipe>
            {
                // S1 synthetics: consume 1250 refined natural, produce 25 S1
                new RefiningRecipe
                {
                    InputResource = "Lanthanides", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S1. Translanthanic Exotics",
                    ConsumeRate = 1250, ProduceRate = 25, Tier = 1
                },
                new RefiningRecipe
                {
                    InputResource = "Superheavy Exotics", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S1. Translivermoric Exotics",
                    ConsumeRate = 1250, ProduceRate = 25, Tier = 1
                },
                new RefiningRecipe
                {
                    InputResource = "Transuranic Volatiles", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S1. Transuranic Exotics",
                    ConsumeRate = 1250, ProduceRate = 25, Tier = 1
                },

                // S2 synthetics: consume 500 refined S1, produce 25 S2
                new RefiningRecipe
                {
                    InputResource = "S1. Translanthanic Exotics", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S2. Element 126",
                    ConsumeRate = 500, ProduceRate = 25, Tier = 2
                },
                new RefiningRecipe
                {
                    InputResource = "S1. Translivermoric Exotics", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S2. Element 127",
                    ConsumeRate = 500, ProduceRate = 25, Tier = 2
                },
                new RefiningRecipe
                {
                    InputResource = "S1. Transuranic Exotics", InputPurity = GameConstants.PurityRefined,
                    OutputResource = "S2. Superactinides",
                    ConsumeRate = 500, ProduceRate = 25, Tier = 2
                },
            };
        }

        /// <summary>
        /// Finds a synthetic recipe by its output resource name.
        /// Returns null if no synthetic recipe exists (normal refining).
        /// </summary>
        public static RefiningRecipe FindByOutput(string outputResource)
        {
            return _recipes.FirstOrDefault(r =>
                string.Equals(r.OutputResource, outputResource, System.StringComparison.Ordinal));
        }

        /// <summary>
        /// Finds a synthetic recipe by its input resource and purity.
        /// Returns null if no synthetic recipe matches.
        /// </summary>
        public static RefiningRecipe FindByInput(string inputResource, string inputPurity)
        {
            return _recipes.FirstOrDefault(r =>
                string.Equals(r.InputResource, inputResource, System.StringComparison.Ordinal) &&
                string.Equals(r.InputPurity, inputPurity, System.StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns the processing tier for a refinery structure.
        /// 0 = normal resource, 1 = S1 synthetic, 2 = S2 synthetic.
        /// </summary>
        public static int GetTier(string refiningResource, string refiningPurity)
        {
            var recipe = FindByInput(refiningResource, refiningPurity);
            return recipe?.Tier ?? 0;
        }
    }
}
