namespace SysLink.Core.Models;

/// <summary>
/// Historical metric data point for time-series storage.
/// </summary>
public class MetricDataPoint
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Historical data query options.
/// </summary>
public class HistoryQueryOptions
{
    public string MetricType { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Period { get; set; } = TimeSpan.FromHours(1);
    public int? MaxPoints { get; set; } = 360;
    public AggregationType Aggregation { get; set; } = AggregationType.Average;
}

public enum AggregationType
{
    None,
    Average,
    Min,
    Max,
    Sum
}

/// <summary>
/// Historical data response.
/// </summary>
public class HistoryResponse
{
    public string MetricType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DataPointCount { get; set; }
    public List<HistoryDataPoint> DataPoints { get; set; } = new();
}

public class HistoryDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}

/// <summary>
/// Supported metric types for history queries.
/// </summary>
public static class MetricTypes
{
    public const string CpuUsage = "cpu_usage";
    public const string CpuTemperature = "cpu_temp";
    public const string GpuUsage = "gpu_usage";
    public const string GpuTemperature = "gpu_temp";
    public const string RamUsage = "ram_usage";
    public const string NetworkUpload = "net_upload";
    public const string NetworkDownload = "net_download";
    public const string BatteryPercent = "battery_percent";
    public const string DiskUsage = "disk_usage";
    public const string DiskRead = "disk_read";
    public const string DiskWrite = "disk_write";
    
    public static readonly string[] All = new[]
    {
        CpuUsage, CpuTemperature, GpuUsage, GpuTemperature,
        RamUsage, NetworkUpload, NetworkDownload, BatteryPercent,
        DiskUsage, DiskRead, DiskWrite
    };
}
