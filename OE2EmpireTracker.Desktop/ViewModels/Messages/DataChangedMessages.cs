namespace OE2EmpireTracker.Desktop.ViewModels.Messages;

/// <summary>Sent when the current player selection changes.</summary>
public sealed record PlayerChangedMessage(string playerUuid);

/// <summary>Sent when colony data is modified.</summary>
public sealed record ColonyDataChangedMessage(string colonyUuid);

/// <summary>Sent when blueprint data is modified.</summary>
public sealed record BlueprintDataChangedMessage(string blueprintUuid);

/// <summary>Sent when survey data is modified.</summary>
public sealed record SurveyDataChangedMessage(string surveyUuid);

/// <summary>Sent when delivery route/plan data is modified.</summary>
public sealed record DeliveryDataChangedMessage;

/// <summary>Sent when pricing plan data is modified.</summary>
public sealed record PricingDataChangedMessage;

/// <summary>Sent when player profile data is modified.</summary>
public sealed record PlayerProfileDataChangedMessage(string playerUuid);

/// <summary>Sent when build plan data is modified.</summary>
public sealed record BuildPlanDataChangedMessage(string buildPlanUuid);

/// <summary>Sent when market data is modified.</summary>
public sealed record MarketDataChangedMessage;

/// <summary>Sent when station data is modified.</summary>
public sealed record StationDataChangedMessage;

/// <summary>Sent when asteroid data is modified.</summary>
public sealed record AsteroidDataChangedMessage(string asteroidUuid);

/// <summary>Sent when ship template data is modified.</summary>
public sealed record ShipTemplateDataChangedMessage(string shipTemplateUuid);

/// <summary>Sent when ship data is modified.</summary>
public sealed record ShipDataChangedMessage(string shipUuid);

/// <summary>Sent when stock plan/profile data is modified.</summary>
public sealed record StockDataChangedMessage(string stockPlanUuid);

/// <summary>Sent when supply chain data is modified.</summary>
public sealed record SupplyChainDataChangedMessage(string supplyChainUuid);

/// <summary>Sent when contact (faction/character) data is modified.</summary>
public sealed record ContactDataChangedMessage(string characterUuid);

/// <summary>Sent when the user presses F5 to refresh the active view.</summary>
public sealed record RefreshRequestedMessage;
