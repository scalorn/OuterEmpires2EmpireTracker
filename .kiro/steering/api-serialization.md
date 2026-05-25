# Server API Serialization Contract

## Critical: The server uses camelCase for ALL HTTP JSON responses

The server is configured in `Program.cs` with:
```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
```

### What this means for the wire format:

| C# Model | JSON Wire Format | Notes |
|-----------|-----------------|-------|
| `SurveyType` (property) | `surveyType` | CamelCase naming policy |
| `AsteroidUUID` (property) | `asteroidUUID` | CamelCase naming policy |
| `SurveyType.Asteroid` (enum value) | `"asteroid"` | JsonStringEnumConverter with CamelCase |
| `SurveyType.Planet` (enum value) | `"planet"` | JsonStringEnumConverter with CamelCase |
| `PlanetName` (property) | `planetName` | CamelCase naming policy |
| `MaxReserve` (property) | `maxReserve` | CamelCase naming policy |
| `null` properties | omitted entirely | WhenWritingNull |

### Rules for web UI specs:

1. **Always document the wire format, not the C# model.** When a spec says "check if surveyType is Asteroid", it must specify the exact string comparison: `surveyType === 'asteroid'` (lowercase).

2. **Enum comparisons must be case-insensitive or use the camelCase value.** The server serializes all enums with `CamelCase` policy, so `SurveyType.Asteroid` becomes `"asteroid"` on the wire.

3. **Property names in TypeScript must use camelCase.** The server transforms `PlanetName` → `planetName`, `AsteroidUUID` → `asteroidUUID`, etc.

4. **Null properties are omitted.** Don't check for `null` — check for `undefined` or use optional chaining. A property with value `null` in C# won't appear in the JSON at all.

5. **When writing a .kiro/specs/ design document**, include a "Wire Format" section showing the exact JSON the endpoint returns. Don't just show C# projections — show the actual JSON after serialization.

### Example — correct spec format:

```markdown
**Response (200):**
```json
{
  "uuid": "abc-123",
  "name": "Asteroid Alpha",
  "surveyType": "asteroid",
  "asteroidUUID": "77360093-351a-5af3-917a-c7f041c67814",
  "reserves": [
    { "resourceName": "Iron", "purity": "high", "maxReserve": 1250000 }
  ]
}
```
```

### Common mistakes this prevents:

- `surveyType === 'Asteroid'` ← WRONG (server sends `"asteroid"`)
- `survey.SurveyType` ← WRONG (wire format is `survey.surveyType`)
- `if (survey.asteroidUUID === null)` ← WRONG (null fields are omitted, check `undefined`)
- `survey.PlanetName` ← WRONG (wire format is `survey.planetName`)
