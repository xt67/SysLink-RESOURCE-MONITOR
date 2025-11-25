using SysLink.Core.Interfaces;

namespace SysLink.Api.Services;

/// <summary>
/// Background service for collecting and storing metrics.
/// </summary>
public class MetricsCollectionService : BackgroundService
{
    private readonly IHardwareMonitor _hardwareMonitor;
    private readonly IMetricsStorage _metricsStorage;
    private readonly IAlertService _alertService;
    private readonly IConfigService _configService;
    private readonly ILogger<MetricsCollectionService> _logger;

    public MetricsCollectionService(
        IHardwareMonitor hardwareMonitor,
        IMetricsStorage metricsStorage,
        IAlertService alertService,
        IConfigService configService,
        ILogger<MetricsCollectionService> logger)
    {
        _hardwareMonitor = hardwareMonitor;
        _metricsStorage = metricsStorage;
        _alertService = alertService;
        _configService = configService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collection service starting");

        // Initialize hardware monitor
        await _hardwareMonitor.InitializeAsync();
        await _metricsStorage.InitializeAsync();

        var config = _configService.GetConfig();
        var storageInterval = TimeSpan.FromSeconds(config.Storage.StorageIntervalSeconds);
        var lastStorageTime = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                config = _configService.GetConfig();
                var updateInterval = config.Monitoring.UpdateIntervalMs;

                // Collect metrics
                var metrics = await _hardwareMonitor.GetMetricsAsync();

                // Check alerts
                await _alertService.CheckMetricsAsync(metrics);

                // Store periodically
                if (DateTime.UtcNow - lastStorageTime >= storageInterval)
                {
                    await _metricsStorage.StoreSnapshotAsync(metrics);
                    lastStorageTime = DateTime.UtcNow;
                }

                await Task.Delay(updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics collection");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Metrics collection service stopped");
    }
}
