# Data Models — Ships

## ShipTemplate

```csharp
public class ShipTemplate
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string HullBlueprintUUID { get; set; } = string.Empty;
    public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();
}

public class ShipComponentSlot
{
    public string SlotType { get; set; } = string.Empty;
    public int SlotIndex { get; set; } = 0;
    public string BlueprintUUID { get; set; } = string.Empty;

    // Damage state (Ship and Station instances — ignored on ShipTemplate)
    public int CurrentHP { get; set; } = 0;
    public int MaxHP { get; set; } = 0;
    public decimal MaxRepairPercent { get; set; } = 0m;
}
```

- SlotType is string (game may add new slot types). Hull blueprint defines valid types/counts.
- Damage fields default to 0 ("undamaged"), omitted from JSON via `DefaultValueHandling.Ignore`.

## Ship

```csharp
public class Ship
{
    public string UUID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerUUID { get; set; } = string.Empty;
    public string TemplateUUID { get; set; } = string.Empty;
    public string HullBlueprintUUID { get; set; } = string.Empty;
    public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();

    [JsonConverter(typeof(StringEnumConverter))]
    public DestinationType LocationType { get; set; } = DestinationType.Station;
    public string LocationUUID { get; set; } = string.Empty;

    public ItemBag Cargo { get; set; } = new ItemBag();
    public ItemBag Hopper { get; set; } = new ItemBag();

    // Hull damage state
    public int HullCurrentHP { get; set; } = 0;
    public int HullMaxHP { get; set; } = 0;
    public decimal HullMaxRepairPercent { get; set; } = 0m;
}
```

- Ship duplicates hull/components from template (independent entity — template can change without affecting ship).
- Hopper: separate ItemBag for unrefined resources (High/Medium/Low purity only). Capacity from hull `Raw Material Capacity` + sum(Ore Hopper `Raw Material Capacity`).
- Hull damage: same three-field pattern as components. All default to 0 (undamaged, omitted from JSON).
