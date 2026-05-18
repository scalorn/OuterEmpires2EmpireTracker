using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class BaselineRoot
    {
        public int DataVersion { get; set; }

        public BaselineGameConstants GameConstants { get; set; }

        public ShipClass[] ShipClass { get; set; }

        public BlueprintType[] BlueprintType { get; set; }

        public Blueprint[] Blueprint { get; set; }

        public TechLevel[] TechLevel { get; set; }

        public Commodity[] Commodity { get; set; }

        public RefiningRecipe[] RefiningRecipe { get; set; }

        public ResearchTimeEntry[] ResearchTime { get; set; }
    }
}
