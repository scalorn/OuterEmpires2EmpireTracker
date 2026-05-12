namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Defines a synthetic refining recipe. Normal unrefined resources use
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
}
