using SysLink.Core.Interfaces;

namespace SysLink.Api.Services;

/// <summary>
/// Background service for cleaning up old data.
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly IMetricsStorage _metricsStorage;
    private readonly IConfigService _configService;
    private readonly ILogger<DataCleanupService> _logger;

    public DataCleanupService(
        IMetricsStorage metricsStorage,
        IConfigService configService,
        ILogger<DataCleanupService> logger)
    {
        _metricsStorage = metricsStorage;
        _configService = configService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = _configService.GetConfig();
                var retention = TimeSpan.FromHours(config.Storage.RetentionHours);

                await _metricsStorage.CleanupOldDataAsync(retention);

                // Check database size
                var dbSize = await _metricsStorage.GetDatabaseSizeAsync();
                var maxSize = config.Storage.MaxDatabaseSizeMB * 1024 * 1024L;

                if (dbSize > maxSize)
                {
                    _logger.LogWarning("Database size ({Size}MB) exceeds limit ({Limit}MB), reducing retention",
                        dbSize / (1024 * 1024), config.Storage.MaxDatabaseSizeMB);
                    
                    // Reduce retention to half to free up space
                    await _metricsStorage.CleanupOldDataAsync(retention / 2);
                }

                // Run cleanup every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Data cleanup service stopped");
    }
}
