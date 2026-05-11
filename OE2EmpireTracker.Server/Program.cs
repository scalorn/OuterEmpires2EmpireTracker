using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    var port = builder.Configuration.GetValue<int>("Server:Port", 5443);
    options.ListenAnyIP(port, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

var app = builder.Build();

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
