using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for hardware monitoring services.
/// </summary>
public interface IHardwareMonitor : IDisposable
{
    /// <summary>
    /// Gets the complete system metrics snapshot.
    /// </summary>
    Task<SystemMetrics> GetMetricsAsync();
    
    /// <summary>
    /// Gets minimal metrics for simple view.
    /// </summary>
    Task<MinimalMetrics> GetMinimalMetricsAsync();
    
    /// <summary>
    /// Gets static system information.
    /// </summary>
    Task<SystemInfo> GetSystemInfoAsync();
    
    /// <summary>
    /// Initializes hardware monitoring.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Updates cached metrics.
    /// </summary>
    Task UpdateAsync();
}

/// <summary>
/// Interface for CPU monitoring.
/// </summary>
public interface ICpuMonitor
{
    Task<CpuMetrics> GetMetricsAsync();
    string GetCpuName();
    int GetCoreCount();
    int GetThreadCount();
}

/// <summary>
/// Interface for GPU monitoring.
/// </summary>
public interface IGpuMonitor
{
    Task<GpuMetrics> GetMetricsAsync();
    string GetGpuName();
    bool IsAvailable { get; }
}

/// <summary>
/// Interface for RAM monitoring.
/// </summary>
public interface IRamMonitor
{
    Task<RamMetrics> GetMetricsAsync();
    double GetTotalRamGB();
}

/// <summary>
/// Interface for disk monitoring.
/// </summary>
public interface IDiskMonitor
{
    Task<List<DiskMetrics>> GetMetricsAsync();
}

/// <summary>
/// Interface for network monitoring.
/// </summary>
public interface INetworkMonitor
{
    Task<NetworkMetrics> GetMetricsAsync();
}

/// <summary>
/// Interface for battery monitoring.
/// </summary>
public interface IBatteryMonitor
{
    Task<BatteryMetrics?> GetMetricsAsync();
    bool HasBattery { get; }
}

/// <summary>
/// Interface for fan monitoring.
/// </summary>
public interface IFanMonitor
{
    Task<List<FanMetrics>> GetMetricsAsync();
}
