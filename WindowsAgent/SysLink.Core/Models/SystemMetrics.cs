namespace SysLink.Core.Models;

/// <summary>
/// Complete system metrics snapshot containing all hardware and process data.
/// </summary>
public class SystemMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public CpuMetrics Cpu { get; set; } = new();
    public GpuMetrics Gpu { get; set; } = new();
    public RamMetrics Ram { get; set; } = new();
    public List<DiskMetrics> Disks { get; set; } = new();
    public NetworkMetrics Network { get; set; } = new();
    public BatteryMetrics? Battery { get; set; }
    public List<FanMetrics> Fans { get; set; } = new();
}

/// <summary>
/// Lightweight metrics for Simple View.
/// </summary>
public class MinimalMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuUsage { get; set; }
    public double GpuUsage { get; set; }
    public double RamUsage { get; set; }
    public double? BatteryPercent { get; set; }
    public double MaxCpuTemp { get; set; }
    public double GpuTemp { get; set; }
}

/// <summary>
/// CPU metrics including per-core data.
/// </summary>
public class CpuMetrics
{
    public string Name { get; set; } = string.Empty;
    public double AverageUsage { get; set; }
    public double AverageTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public double TotalPower { get; set; }
    public List<CpuCoreMetrics> Cores { get; set; } = new();
}

public class CpuCoreMetrics
{
    public int CoreId { get; set; }
    public double Usage { get; set; }
    public double Temperature { get; set; }
    public double ClockSpeed { get; set; }
}

/// <summary>
/// GPU metrics for graphics card monitoring.
/// </summary>
public class GpuMetrics
{
    public string Name { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Usage { get; set; }
    public double VramUsed { get; set; }
    public double VramTotal { get; set; }
    public double VramUsagePercent => VramTotal > 0 ? (VramUsed / VramTotal) * 100 : 0;
    public double CoreClock { get; set; }
    public double MemoryClock { get; set; }
    public double Power { get; set; }
    public double FanSpeed { get; set; }
}

/// <summary>
/// RAM metrics for memory monitoring.
/// </summary>
public class RamMetrics
{
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double FreeGB => TotalGB - UsedGB;
    public double UsagePercent => TotalGB > 0 ? (UsedGB / TotalGB) * 100 : 0;
    public double SwapTotalGB { get; set; }
    public double SwapUsedGB { get; set; }
    public double SwapUsagePercent => SwapTotalGB > 0 ? (SwapUsedGB / SwapTotalGB) * 100 : 0;
}

/// <summary>
/// Disk metrics for storage monitoring.
/// </summary>
public class DiskMetrics
{
    public string Name { get; set; } = string.Empty;
    public string DriveLetter { get; set; } = string.Empty;
    public double TotalGB { get; set; }
    public double UsedGB { get; set; }
    public double FreeGB => TotalGB - UsedGB;
    public double UsagePercent => TotalGB > 0 ? (UsedGB / TotalGB) * 100 : 0;
    public double ReadSpeedMBps { get; set; }
    public double WriteSpeedMBps { get; set; }
    public double Temperature { get; set; }
    public string HealthStatus { get; set; } = "Unknown";
    public DiskSmartData? SmartData { get; set; }
}

public class DiskSmartData
{
    public int PowerOnHours { get; set; }
    public int PowerCycleCount { get; set; }
    public int ReallocatedSectors { get; set; }
    public int Temperature { get; set; }
}

/// <summary>
/// Network metrics for connectivity monitoring.
/// </summary>
public class NetworkMetrics
{
    public string AdapterName { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "Unknown"; // Ethernet, WiFi
    public double UploadSpeedMbps { get; set; }
    public double DownloadSpeedMbps { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

/// <summary>
/// Battery metrics for laptop monitoring.
/// </summary>
public class BatteryMetrics
{
    public bool IsPresent { get; set; }
    public double ChargePercent { get; set; }
    public BatteryStatus Status { get; set; }
    public double DesignCapacityWh { get; set; }
    public double FullChargeCapacityWh { get; set; }
    public double WearLevel => DesignCapacityWh > 0 
        ? (1 - (FullChargeCapacityWh / DesignCapacityWh)) * 100 
        : 0;
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public int CycleCount { get; set; }
    public double ChargeRateW { get; set; }
    public double DischargeRateW { get; set; }
    public double Voltage { get; set; }
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum BatteryStatus
{
    Unknown,
    Charging,
    Discharging,
    Full,
    NotCharging,
    Critical
}

/// <summary>
/// Fan metrics for cooling monitoring.
/// </summary>
public class FanMetrics
{
    public string Name { get; set; } = string.Empty;
    public double Rpm { get; set; }
    public double SpeedPercent { get; set; }
    public FanType Type { get; set; }
}

public enum FanType
{
    CaseFan,
    CpuFan,
    GpuFan,
    PumpFan,
    Other
}
