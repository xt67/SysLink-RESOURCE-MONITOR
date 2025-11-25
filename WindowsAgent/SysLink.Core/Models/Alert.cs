namespace SysLink.Core.Models;

/// <summary>
/// Alert notification model.
/// </summary>
public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double ThresholdValue { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public bool IsAcknowledged { get; set; }
}

public enum AlertType
{
    CpuTemperature,
    GpuTemperature,
    CpuUsage,
    GpuUsage,
    RamUsage,
    DiskUsage,
    BatteryLow,
    BatteryHigh,
    NetworkDisconnected,
    ProcessNotResponding,
    Custom
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
