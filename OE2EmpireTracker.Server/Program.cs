using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using OE2EmpireTracker.Server.Auth;
using OE2EmpireTracker.Server.Config;
using OE2EmpireTracker.Server.Endpoints;
using OE2EmpireTracker.Server.Storage;

// --- CLI: --regenerate-owner-token ---
if (args.Contains("--regenerate-owner-token"))
{
    var cliBuilder = WebApplication.CreateBuilder();
    var cliConfig = cliBuilder.Configuration;
    var cliLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var cliLogger = cliLoggerFactory.CreateLogger("CLI");
    var storageLogger = cliLoggerFactory.CreateLogger<JsonFileStorageBackend>();

    var storage = new JsonFileStorageBackend(cliConfig, storageLogger);
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
builder.Services.AddSingleton<IStorageBackend, JsonFileStorageBackend>();

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

app.UseAuthentication();
app.UseAuthorization();

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
app.MapCharacterEndpoints();
app.MapMembershipEndpoints();

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

// Make Program class accessible for integration tests
public partial class Program { }
