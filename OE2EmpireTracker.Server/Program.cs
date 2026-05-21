using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Config;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Server.Middleware;
using OE2EmpireTracker.Server.Processing;
using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

// --- CLI: --regenerate-owner-token ---
if (args.Contains("--regenerate-owner-token"))
{
    var cliBuilder = WebApplication.CreateBuilder();
    var cliConfig = cliBuilder.Configuration;
    var cliLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var cliLogger = cliLoggerFactory.CreateLogger("CLI");

    var cliBackendType = cliConfig.GetValue<string>("Storage:Backend", "JsonFile");
    IStorageBackend storage;
    switch (cliBackendType)
    {
        case "Sqlite":
            var sqliteLogger = cliLoggerFactory.CreateLogger<SqliteStorageBackend>();
            storage = new SqliteStorageBackend(cliConfig, sqliteLogger);
            break;
        case "Postgres":
            var pgLogger = cliLoggerFactory.CreateLogger<PostgresStorageBackend>();
            storage = new PostgresStorageBackend(cliConfig, pgLogger);
            break;
        case "DynamoDB":
            var dynamoLogger = cliLoggerFactory.CreateLogger<DynamoStorageBackend>();
            storage = new DynamoStorageBackend(cliConfig, dynamoLogger);
            break;
        default:
            var storageLogger = cliLoggerFactory.CreateLogger<JsonFileStorageBackend>();
            storage = new JsonFileStorageBackend(cliConfig, storageLogger);
            break;
    }

    await storage.InitializeAsync();
    await TokenService.RegenerateOwnerTokenAsync(storage, cliLogger);

    cliLoggerFactory.Dispose();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Support reverse proxy headers
var useForwardedHeaders = builder.Configuration.GetValue<bool>("Server:UseForwardedHeaders", false);
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
}

// Register storage backend
var backendType = builder.Configuration.GetValue<string>("Storage:Backend", "JsonFile");
switch (backendType)
{
    case "Sqlite":
        builder.Services.AddSingleton<IStorageBackend, SqliteStorageBackend>();
        break;
    case "Postgres":
        builder.Services.AddSingleton<IStorageBackend, PostgresStorageBackend>();
        break;
    case "DynamoDB":
        builder.Services.AddSingleton<IStorageBackend, DynamoStorageBackend>();
        break;
    default:
        builder.Services.AddSingleton<IStorageBackend, JsonFileStorageBackend>();
        break;
}

// Register WebSocket hub and event dispatcher
builder.Services.AddSingleton<WebSocketHub>();
builder.Services.AddSingleton<EventDispatcher>();

// Register background processor as hosted service
builder.Services.AddSingleton<ServerBackgroundProcessor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ServerBackgroundProcessor>());

// Authentication
builder.Services.AddAuthentication(TokenAuthHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthHandler>(
        TokenAuthHandler.SchemeName, _ => { });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", policy =>
        policy.RequireRole(TokenRole.Owner.ToString()));

    options.AddPolicy("FactionLeader", policy =>
        policy.RequireRole(TokenRole.Owner.ToString(), TokenRole.FactionLeader.ToString()));

    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});

// Configure Kestrel for HTTPS with certificate
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var port = context.Configuration.GetValue<int>("Server:Port", 5443);
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    var cert = CertificateManager.GetOrCreateCertificate(context.Configuration, logger);

    options.ListenAnyIP(port, listenOptions =>
    {
        listenOptions.UseHttps(cert);
    });
});

var app = builder.Build();

// --- Startup Configuration Validation ---
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    var port = builder.Configuration.GetValue<int>("Server:Port", 5443);
    var dataPath = builder.Configuration.GetValue<string>("Storage:DataPath", "./data");

    if (port < 1 || port > 65535)
    {
        startupLogger.LogCritical("Invalid port {Port}. Must be between 1 and 65535.", port);
        throw new InvalidOperationException($"Invalid port configuration: {port}");
    }

    startupLogger.LogInformation(
        "Configuration: Port={Port}, DataPath={DataPath}, UseForwardedHeaders={UseForwardedHeaders}",
        port,
        dataPath,
        useForwardedHeaders);
}

// Initialize storage on startup
var appStorage = app.Services.GetRequiredService<IStorageBackend>();
await appStorage.InitializeAsync();
await TokenService.EnsureOwnerTokenAsync(
    appStorage,
    app.Services.GetRequiredService<ILogger<Program>>());

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

// WebSocket support (must be before routing)
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
});

app.UseAuthentication();
app.UseAuthorization();

// Rate limiting middleware (after auth so we have token identity)
app.UseMiddleware<RateLimitMiddleware>();

// Health endpoint (no auth required)
app.MapGet("/health", () =>
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.1.0";

    return Results.Ok(new
    {
        status = "ok",
        serverVersion = version,
    });
}).AllowAnonymous();

// Token management endpoints (Owner only)
app.MapTokenEndpoints();
app.MapFactionEndpoints();
app.MapFactionCapabilityEndpoints();
app.MapFactionClearanceLevelEndpoints();
app.MapFactionGroupEndpoints();
app.MapFactionMemberEndpoints();
app.MapCharacterEndpoints();
app.MapCharacterCapabilityEndpoints();
app.MapCharacterClearanceLevelEndpoints();
app.MapCharacterGroupEndpoints();
app.MapCharacterGranteeEndpoints();
app.MapMembershipEndpoints();
app.MapDataEndpoints();
app.MapSharingEndpoints();
app.MapRateLimitEndpoints();
app.MapAdminEndpoints();
app.MapIntelEndpoints();
app.MapColonyPlannerEndpoints();

// Static file serving for the React SPA
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name == "index.html")
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
        else
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
});

// SPA fallback — return index.html for unmatched routes (must be after API endpoints)
app.MapFallbackToFile("index.html");

// WebSocket endpoint
var heartbeatTimeout = builder.Configuration.GetValue<int>(
    "Server:WebSocketHeartbeatTimeoutSeconds", 60);

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket upgrade required");
        return;
    }

    // Authenticate via query parameter
    var tokenParam = context.Request.Query["token"].ToString();
    if (string.IsNullOrEmpty(tokenParam))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing token parameter");
        return;
    }

    var wsStorage = context.RequestServices.GetRequiredService<IStorageBackend>();
    var hash = TokenService.HashToken(tokenParam);
    var apiToken = await wsStorage.FindTokenByHashAsync(hash);

    if (apiToken == null || apiToken.IsRevoked)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await ws.CloseAsync(
            (WebSocketCloseStatus)4001,
            "Invalid or revoked token",
            CancellationToken.None);
        return;
    }

    var hub = context.RequestServices.GetRequiredService<WebSocketHub>();
    var connectionId = Guid.NewGuid().ToString("N");
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    hub.AddConnection(connectionId, webSocket, apiToken);

    try
    {
        var buffer = new byte[1024];
        using var timeoutCts = new CancellationTokenSource();

        while (webSocket.State == WebSocketState.Open)
        {
            timeoutCts.CancelAfter(
                TimeSpan.FromSeconds(heartbeatTimeout));

            try
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    timeoutCts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                // Handle ping/pong — echo back as heartbeat
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(
                        buffer, 0, result.Count);
                    if (msg == "ping")
                    {
                        var pong = Encoding.UTF8.GetBytes("pong");
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(pong),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }

                // Reset timeout after successful receive
                timeoutCts.CancelAfter(
                    TimeSpan.FromSeconds(heartbeatTimeout));
            }
            catch (OperationCanceledException)
            {
                // Heartbeat timeout — close connection
                break;
            }
        }
    }
    finally
    {
        hub.RemoveConnection(connectionId);
        if (webSocket.State == WebSocketState.Open ||
            webSocket.State == WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed",
                CancellationToken.None);
        }
    }
}).AllowAnonymous();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var port = builder.Configuration.GetValue<int>("Server:Port", 5443);
    app.Logger.LogInformation("OE2 Empire Tracker Server started on port {Port}", port);
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("OE2 Empire Tracker Server shutting down...");
});

app.Run();

/// <summary>Make Program class accessible for integration tests.</summary>
public partial class Program { }
