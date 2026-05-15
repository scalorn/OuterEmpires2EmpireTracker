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

        // Load data on startup
        var dataService = provider.GetRequiredService<DataService>();
        dataService.LoadData();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
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
                System.IO.Path.Combine(logDir, "oe2tracker-.log"),
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
        services.AddSingleton<DataService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<ColonyListViewModel>();
    }
}
