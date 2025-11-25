using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for metrics data storage.
/// </summary>
public interface IMetricsStorage : IDisposable
{
    /// <summary>
    /// Initializes the storage (creates tables, etc.).
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Stores a metric data point.
    /// </summary>
    Task StoreMetricAsync(MetricDataPoint dataPoint);
    
    /// <summary>
    /// Stores multiple metric data points in batch.
    /// </summary>
    Task StoreMetricsAsync(IEnumerable<MetricDataPoint> dataPoints);
    
    /// <summary>
    /// Stores a complete system metrics snapshot.
    /// </summary>
    Task StoreSnapshotAsync(SystemMetrics metrics);
    
    /// <summary>
    /// Retrieves historical data for a metric.
    /// </summary>
    Task<HistoryResponse> GetHistoryAsync(HistoryQueryOptions options);
    
    /// <summary>
    /// Removes data older than retention period.
    /// </summary>
    Task CleanupOldDataAsync(TimeSpan retention);
    
    /// <summary>
    /// Gets database size in bytes.
    /// </summary>
    Task<long> GetDatabaseSizeAsync();
}
