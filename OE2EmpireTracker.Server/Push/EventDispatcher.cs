using System.Text.Json;
using System.Text.Json.Serialization;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Server.Push;

/// <summary>Event types dispatched to WebSocket clients.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServerEventType
{
    Created,
    Updated,
    Deleted,
    TimerTick,
    MembershipChanged,
    InvitationReceived,
    RequestReceived,
}

/// <summary>An event to be dispatched to connected clients.</summary>
public record ServerEvent
{
    public ServerEventType EventType { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityUUID { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = SystemClock.UtcNow;
    public string? OwnerCharacterUUID { get; init; }
}

/// <summary>
/// Dispatches server events to relevant WebSocket clients based on access rules.
/// Registered as a singleton.
/// </summary>
public class EventDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly WebSocketHub _hub;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(WebSocketHub hub, ILogger<EventDispatcher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches an event to clients that should receive it.
    /// Owner-character events go to that character's connections.
    /// Events without an owner are broadcast to all.
    /// </summary>
    public async Task DispatchEvent(ServerEvent evt)
    {
        var payload = SerializeEvent(evt);

        _logger.LogDebug(
            "Dispatching {EventType} for {EntityType}/{EntityUUID}",
            evt.EventType,
            evt.EntityType,
            evt.EntityUUID);

        if (!string.IsNullOrEmpty(evt.OwnerCharacterUUID))
        {
            // Send to the owning character's connections
            var connections = _hub.GetConnectionsForCharacter(evt.OwnerCharacterUUID);
            foreach (var (id, _) in connections)
            {
                await _hub.SendToConnection(id, payload);
            }
        }
        else
        {
            // No specific owner — broadcast to all
            await _hub.BroadcastToAll(payload);
        }
    }

    /// <summary>
    /// Dispatches an event to all members of a faction.
    /// </summary>
    public async Task DispatchToFaction(string factionUUID, ServerEvent evt)
    {
        var payload = SerializeEvent(evt);
        var connections = _hub.GetConnectionsForFaction(factionUUID);

        foreach (var (id, _) in connections)
        {
            await _hub.SendToConnection(id, payload);
        }
    }

    private static string SerializeEvent(ServerEvent evt)
    {
        var wire = new
        {
            type = evt.EventType.ToString(),
            entityType = evt.EntityType,
            entityUUID = evt.EntityUUID,
            timestamp = evt.Timestamp.ToString("o"),
        };

        return JsonSerializer.Serialize(wire, JsonOptions);
    }
}
