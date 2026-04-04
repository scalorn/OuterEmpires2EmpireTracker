using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Defines synthetic refining recipes. Normal unrefined resources use
    /// the standard purity-based refining logic. Synthetic resources have
    /// explicit recipes with fixed consume/produce rates.
    /// </summary>
    public class RefiningRecipe
    {
        /// <summary>Resource consumed as input.</summary>
        public string InputResource { get; set; }

        /// <summary>Purity of the input resource (typically "Refined").</summary>
        public string InputPurity { get; set; }

        /// <summary>Resource produced as output.</summary>
        public string OutputResource { get; set; }

        /// <summary>Amount consumed per cycle.</summary>
        public int ConsumeRate { get; set; }

        /// <summary>Amount produced per cycle.</summary>
        public int ProduceRate { get; set; }

        /// <summary>Processing tier: 0=normal, 1=S1, 2=S2. Lower tiers process first.</summary>
        public int Tier { get; set; }
    }

    public static class RefiningRecipes
    {
        private static readonly List<RefiningRecipe> _recipes = new List<RefiningRecipe>
        {
            // S1 synthetics: consume 1250 refined natural, produce 25 S1
            new RefiningRecipe
            {
                InputResource = "Lanthanides", InputPurity = "Refined",
                OutputResource = "S1. Translanthanic Exotics",
                ConsumeRate = 1250, ProduceRate = 25, Tier = 1
            },
            new RefiningRecipe
            {
                InputResource = "Superheavy Exotics", InputPurity = "Refined",
                OutputResource = "S1. Translivermoric Exotics",
                ConsumeRate = 1250, ProduceRate = 25, Tier = 1
            },
            new RefiningRecipe
            {
                InputResource = "Transuranic Volatiles", InputPurity = "Refined",
                OutputResource = "S1. Transuranic Exotics",
                ConsumeRate = 1250, ProduceRate = 25, Tier = 1
            },

            // S2 synthetics: consume 500 refined S1, produce 25 S2
            new RefiningRecipe
            {
                InputResource = "S1. Translanthanic Exotics", InputPurity = "Refined",
                OutputResource = "S2. Element 126",
                ConsumeRate = 500, ProduceRate = 25, Tier = 2
            },
            new RefiningRecipe
            {
                InputResource = "S1. Translivermoric Exotics", InputPurity = "Refined",
                OutputResource = "S2. Element 127",
                ConsumeRate = 500, ProduceRate = 25, Tier = 2
            },
            new RefiningRecipe
            {
                InputResource = "S1. Transuranic Exotics", InputPurity = "Refined",
                OutputResource = "S2. Superactinides",
                ConsumeRate = 500, ProduceRate = 25, Tier = 2
            },
        };

        public static IReadOnlyList<RefiningRecipe> Recipes => _recipes.AsReadOnly();

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
