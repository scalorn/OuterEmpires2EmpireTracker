using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels;
using OE2EmpireTracker.Desktop.Views;
using Serilog;

namespace OE2EmpireTracker.Desktop;

public partial class App : Application
{
    public static ServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Services = provider;

        // Auto-open on launch (REQ-MM-010/011/012)
        var dataService = provider.GetRequiredService<DataService>();
        var configService = provider.GetRequiredService<AppConfigService>();
        var logger = provider.GetRequiredService<ILogger<App>>();

        AutoLoadData(dataService, configService, logger);

        // Start background processing after data is loaded
        var backgroundProcessor = provider.GetRequiredService<BackgroundProcessor>();
        if (dataService.IsLoaded)
        {
            backgroundProcessor.Start();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                backgroundProcessor.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void AutoLoadData(DataService dataService, AppConfigService configService, Microsoft.Extensions.Logging.ILogger logger)
    {
        var lastPath = configService.LastOpenedPath;

        if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
        {
            dataService.LoadFromFile(lastPath);
            logger.LogInformation("Auto-loaded last opened file: {Path}", lastPath);
        }
        else
        {
            if (!string.IsNullOrEmpty(lastPath))
            {
                logger.LogWarning("Last opened path does not exist, clearing: {Path}", lastPath);
                configService.LastOpenedPath = null;
            }

            dataService.LoadData();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        var fileSystem = new FileSystemService();
        var logDir = fileSystem.GetLogDirectory();
        fileSystem.EnsureDirectoryExists(logDir);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDir, "oe2tracker-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddSerilog(serilogLogger, dispose: true);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Platform services
        services.AddSingleton<IFileSystemService>(fileSystem);
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<SafeFileWriter>();
        services.AddSingleton<AppConfigService>();
        services.AddSingleton<PreferencesStore>();
        services.AddSingleton<DataService>();
        services.AddSingleton<ColonyProcessingContext>();
        services.AddSingleton<BackgroundProcessor>();

        // Domain services
        services.AddSingleton<ColonyService>();
        services.AddSingleton<BlueprintService>();
        services.AddSingleton<SurveyService>();
        services.AddSingleton<PlayerProfileService>();
        services.AddSingleton<DeliveryRouteService>();
        services.AddSingleton<DeliveryPlanService>();
        services.AddSingleton<PricingPlanService>();
        services.AddSingleton<BuildPlanService>();
        services.AddSingleton<MarketService>();
        services.AddSingleton<StationService>();
        services.AddSingleton<ShipTemplateService>();
        services.AddSingleton<ShipService>();
        services.AddSingleton<StockTargetService>();
        services.AddSingleton<SupplyChainService>();
        services.AddSingleton<ContactsService>();
        services.AddSingleton<AsteroidService>();
        services.AddSingleton<SystemRepository>();
        services.AddSingleton<ReferenceCountService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<ColonyListViewModel>();
    }
}
