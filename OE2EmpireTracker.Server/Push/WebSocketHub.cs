using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Common.Models;

namespace OE2EmpireTracker.Server.Push;

/// <summary>
/// Holds metadata for a single WebSocket connection.
/// </summary>
public class WebSocketConnection
{
    public WebSocket Socket { get; set; } = null!;
    public string TokenId { get; set; } = string.Empty;
    public string? CharacterUUID { get; set; }
    public TokenRole Role { get; set; }
    public string? FactionUUID { get; set; }
    public CancellationTokenSource Cts { get; set; } = new CancellationTokenSource();
}

/// <summary>
/// Manages connected WebSocket clients keyed by connection ID.
/// </summary>
public class WebSocketHub
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new ConcurrentDictionary<string, WebSocketConnection>();
    private readonly ILogger<WebSocketHub> _logger;

    public WebSocketHub(ILogger<WebSocketHub> logger)
    {
        _logger = logger;
    }

    /// <summary>Gets the total number of active connections.</summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>Registers a new WebSocket connection.</summary>
    public void AddConnection(string connectionId, WebSocket ws, ApiToken token)
    {
        var connection = new WebSocketConnection
        {
            Socket = ws,
            TokenId = token.Id,
            CharacterUUID = token.CharacterUUID,
            Role = token.Role,
            FactionUUID = token.FactionUUID,
            Cts = new CancellationTokenSource(),
        };

        _connections[connectionId] = connection;
        _logger.LogInformation(
            "WebSocket connected: {ConnectionId} (Token: {TokenId})",
            connectionId,
            token.Id);
    }

    /// <summary>Removes a connection from the hub.</summary>
    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var conn))
        {
            conn.Cts.Cancel();
            conn.Cts.Dispose();
            _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);
        }
    }

    /// <summary>Gets all connections for a specific character.</summary>
    public IEnumerable<KeyValuePair<string, WebSocketConnection>> GetConnectionsForCharacter(string characterUUID)
    {
        return _connections.Where(c => c.Value.CharacterUUID == characterUUID);
    }

    /// <summary>Gets all connections for members of a faction.</summary>
    public IEnumerable<KeyValuePair<string, WebSocketConnection>> GetConnectionsForFaction(string factionUUID)
    {
        return _connections.Where(c => c.Value.FactionUUID == factionUUID);
    }

    /// <summary>Sends a message to all connected clients.</summary>
    public async Task BroadcastToAll(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        foreach (var (id, conn) in _connections)
        {
            await SendToSocketAsync(id, conn, segment);
        }
    }

    /// <summary>Sends a message to a specific connection.</summary>
    public async Task SendToConnection(string connectionId, string message)
    {
        if (_connections.TryGetValue(connectionId, out var conn))
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            await SendToSocketAsync(connectionId, conn, segment);
        }
    }

    private async Task SendToSocketAsync(
        string connectionId,
        WebSocketConnection conn,
        ArraySegment<byte> data)
    {
        try
        {
            if (conn.Socket.State == WebSocketState.Open)
            {
                await conn.Socket.SendAsync(
                    data,
                    WebSocketMessageType.Text,
                    true,
                    conn.Cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send to {ConnectionId}, removing",
                connectionId);
            RemoveConnection(connectionId);
        }
    }
}