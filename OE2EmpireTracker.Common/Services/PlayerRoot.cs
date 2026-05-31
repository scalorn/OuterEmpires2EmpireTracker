using Newtonsoft.Json;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    public class PlayerRoot
    {
        public PlayerRoot()
        {
            DataVersion = 0;
            CurrentPlayerUUID = string.Empty;
            PlayerProfile = new PlayerProfile[0];
            Blueprint = new Blueprint[0];
            Survey = new Survey[0];
            Colony = new Colony[0];
            DeliveryRoute = new DeliveryRoute[0];
            DeliveryPlan = new DeliveryPlan[0];
            PricingPlan = new PricingPlan[0];
            BuildPlan = new BuildPlan[0];
            ShipTemplate = new ShipTemplate[0];
            Ship = new Ship[0];
            Station = new Station[0];
            MarketListing = new MarketListing[0];
            MarketTransaction = new MarketTransaction[0];
            StockPlan = new StockPlan[0];
            StockProfile = new StockProfile[0];
            SupplyChain = new SupplyChain[0];
            WarehouseOverflowRule = new WarehouseOverflowRule[0];
            Faction = new Faction[0];
            ExternalCharacter = new ExternalCharacter[0];
            Asteroid = new Asteroid[0];
            BankingTransaction = new BankingTransaction[0];
            BankingBalance = 0m;
        }

        public int DataVersion { get; set; }

        public string CurrentPlayerUUID { get; set; }

        public PlayerProfile[] PlayerProfile { get; set; }

        public Blueprint[] Blueprint { get; set; }

        public Survey[] Survey { get; set; }

        public Colony[] Colony { get; set; }

        public DeliveryRoute[] DeliveryRoute { get; set; }

        public DeliveryPlan[] DeliveryPlan { get; set; }

        public PricingPlan[] PricingPlan { get; set; }

        public BuildPlan[] BuildPlan { get; set; }

        public ShipTemplate[] ShipTemplate { get; set; }

        public Ship[] Ship { get; set; }

        public Station[] Station { get; set; }

        public MarketListing[] MarketListing { get; set; }

        public MarketTransaction[] MarketTransaction { get; set; }

        public StockPlan[] StockPlan { get; set; }

        public StockProfile[] StockProfile { get; set; }

        public SupplyChain[] SupplyChain { get; set; }

        public WarehouseOverflowRule[] WarehouseOverflowRule { get; set; }

        public Faction[] Faction { get; set; }

        public ExternalCharacter[] ExternalCharacter { get; set; }

        public Asteroid[] Asteroid { get; set; }

        [JsonProperty("bankingTransaction")]
        public BankingTransaction[] BankingTransaction { get; set; }

        [JsonProperty("bankingBalance")]
        public decimal BankingBalance { get; set; }
    }
}
