using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using OE2EmpireTracker.Server.Config;

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

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

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
});

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
