namespace SysLink.Core.Models;

/// <summary>
/// System information containing static PC details.
/// </summary>
public class SystemInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string CpuName { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string GpuName { get; set; } = string.Empty;
    public double TotalRamGB { get; set; }
    public string Motherboard { get; set; } = string.Empty;
    public DateTime BootTime { get; set; }
    public TimeSpan Uptime => DateTime.Now - BootTime;
    public string AgentVersion { get; set; } = "1.0.0";
    public bool HasBattery { get; set; }
}
