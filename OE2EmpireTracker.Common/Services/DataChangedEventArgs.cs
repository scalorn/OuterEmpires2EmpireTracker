using System;

namespace OE2EmpireTracker.Services
{
    public class ColonyDataChangedEventArgs : EventArgs
    {
        public ColonyDataChangedEventArgs(string colonyUUID) { ColonyUUID = colonyUUID; }

        public string ColonyUUID { get; }
    }

    public class BlueprintDataChangedEventArgs : EventArgs
    {
        public BlueprintDataChangedEventArgs(string blueprintUUID) { BlueprintUUID = blueprintUUID; }

        public string BlueprintUUID { get; }
    }

    public class SurveyDataChangedEventArgs : EventArgs
    {
        public SurveyDataChangedEventArgs(string surveyUUID) { SurveyUUID = surveyUUID; }

        public string SurveyUUID { get; }
    }

    public class PlayerProfileDataChangedEventArgs : EventArgs
    {
        public PlayerProfileDataChangedEventArgs(string playerUUID) { PlayerUUID = playerUUID; }

        public string PlayerUUID { get; }
    }

    public class BuildPlanDataChangedEventArgs : EventArgs
    {
        public BuildPlanDataChangedEventArgs(string uuid) { BuildPlanUUID = uuid; }

        public string BuildPlanUUID { get; }
    }

    public class AsteroidDataChangedEventArgs : EventArgs
    {
        public AsteroidDataChangedEventArgs(string asteroidUUID) { AsteroidUUID = asteroidUUID; }

        public string AsteroidUUID { get; }
    }

    public class ShipTemplateDataChangedEventArgs : EventArgs
    {
        public ShipTemplateDataChangedEventArgs(string uuid) { ShipTemplateUUID = uuid; }

        public string ShipTemplateUUID { get; }
    }

    public class ShipDataChangedEventArgs : EventArgs
    {
        public ShipDataChangedEventArgs(string uuid) { ShipUUID = uuid; }

        public string ShipUUID { get; }
    }

    public class StockDataChangedEventArgs : EventArgs
    {
        public StockDataChangedEventArgs(string uuid) { StockPlanUUID = uuid; }

        public string StockPlanUUID { get; }
    }

    public class SupplyChainDataChangedEventArgs : EventArgs
    {
        public SupplyChainDataChangedEventArgs(string uuid) { SupplyChainUUID = uuid; }

        public string SupplyChainUUID { get; }
    }

    public class ContactDataChangedEventArgs : EventArgs
    {
        public ContactDataChangedEventArgs(string uuid) { CharacterUUID = uuid; }

        public string CharacterUUID { get; }
    }
}
