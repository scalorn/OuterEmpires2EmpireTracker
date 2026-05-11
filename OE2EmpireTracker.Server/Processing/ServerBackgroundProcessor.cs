using OE2EmpireTracker.Server.Push;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Processing;

/// <summary>
/// Background service that periodically processes opted-in characters.
/// Configurable via Server:ProcessingIntervalSeconds and Server:ProcessingEnabled.
/// Can be enabled/disabled at runtime via <see cref="IsEnabled"/>.
/// </summary>
public class ServerBackgroundProcessor : BackgroundService
{
    private readonly IStorageBackend _storage;
    private readonly EventDispatcher _eventDispatcher;
    private readonly ILogger<ServerBackgroundProcessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly object _lock = new();

    private volatile bool _isEnabled;
    private DateTime? _lastTickUtc;
    private int _lastCharactersProcessed;

    public ServerBackgroundProcessor(
        IStorageBackend storage,
        EventDispatcher eventDispatcher,
        ILogger<ServerBackgroundProcessor> logger,
        IConfiguration configuration)
    {
        _storage = storage;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _configuration = configuration;
        _isEnabled = configuration.GetValue<bool>("Server:ProcessingEnabled", false);
    }

    /// <summary>Gets or sets whether processing is enabled at runtime.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _logger.LogInformation("Background processing {State}", value ? "enabled" : "disabled");
        }
    }

    /// <summary>Gets the UTC time of the last completed tick.</summary>
    public DateTime? LastTickUtc
    {
        get
        {
            lock (_lock)
            {
                return _lastTickUtc;
            }
        }
    }

    /// <summary>Gets the number of characters processed in the last tick.</summary>
    public int LastCharactersProcessed
    {
        get
        {
            lock (_lock)
            {
                return _lastCharactersProcessed;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>("Server:ProcessingIntervalSeconds", 60);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation(
            "ServerBackgroundProcessor started. Interval={Interval}s, Enabled={Enabled}",
            intervalSeconds,
            _isEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!_isEnabled)
            {
                continue;
            }

            await ProcessTickAsync(stoppingToken);
        }

        _logger.LogInformation("ServerBackgroundProcessor stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        try
        {
            var characters = await _storage.GetAllCharactersAsync();
            var processedCount = 0;

            foreach (var character in characters)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var prefs = await _storage.GetCharacterPreferencesAsync(character.UUID);
                if (prefs == null || !prefs.ServerProcessing)
                {
                    continue;
                }

                // Load colony data for the opted-in character
                var colonyData = await _storage.GetCharacterDataAsync(character.UUID, "colonies");

                _logger.LogDebug(
                    "Processing character {Name} ({UUID}) — colony data {Status}",
                    character.Name,
                    character.UUID,
                    colonyData != null ? "loaded" : "empty");

                // Placeholder: actual colony processing logic will come from Common library
                processedCount++;
            }

            // Dispatch TimerTick event
            await _eventDispatcher.DispatchEvent(new ServerEvent
            {
                EventType = ServerEventType.TimerTick,
                EntityType = "processing",
                EntityUUID = string.Empty,
                Timestamp = DateTime.UtcNow,
            });

            lock (_lock)
            {
                _lastTickUtc = DateTime.UtcNow;
                _lastCharactersProcessed = processedCount;
            }

            _logger.LogInformation(
                "Background tick completed. Processed {Count} character(s).",
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during background processing tick");
        }
    }
}
