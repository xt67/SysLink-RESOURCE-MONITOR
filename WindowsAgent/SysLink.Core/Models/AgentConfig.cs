namespace SysLink.Core.Models;

/// <summary>
/// Agent configuration settings.
/// </summary>
public class AgentConfig
{
    public ServerSettings Server { get; set; } = new();
    public MonitoringSettings Monitoring { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public AlertSettings Alerts { get; set; } = new();
}

public class ServerSettings
{
    public int HttpsPort { get; set; } = 5443;
    public string BindAddress { get; set; } = "0.0.0.0";
    public bool EnableDiscovery { get; set; } = true;
    public int DiscoveryPort { get; set; } = 5444;
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
}

public class MonitoringSettings
{
    public int UpdateIntervalMs { get; set; } = 1000;
    public bool EnableCpuMonitoring { get; set; } = true;
    public bool EnableGpuMonitoring { get; set; } = true;
    public bool EnableRamMonitoring { get; set; } = true;
    public bool EnableDiskMonitoring { get; set; } = true;
    public bool EnableNetworkMonitoring { get; set; } = true;
    public bool EnableBatteryMonitoring { get; set; } = true;
    public bool EnableFanMonitoring { get; set; } = true;
    public bool EnableProcessMonitoring { get; set; } = true;
    public int ProcessUpdateIntervalMs { get; set; } = 2000;
    public int MaxProcessesToTrack { get; set; } = 100;
}

public class SecuritySettings
{
    public bool RequireAuthentication { get; set; } = true;
    public string AuthToken { get; set; } = string.Empty;
    public bool AllowRemoteAccess { get; set; } = false;
    public List<string> AllowedIpAddresses { get; set; } = new();
    public bool EnableMutualTls { get; set; } = false;
    public int TokenExpirationMinutes { get; set; } = 1440; // 24 hours
}

public class StorageSettings
{
    public string DatabasePath { get; set; } = "syslink_data.db";
    public int RetentionHours { get; set; } = 24;
    public int StorageIntervalSeconds { get; set; } = 10;
    public bool EnableCompression { get; set; } = true;
    public int MaxDatabaseSizeMB { get; set; } = 500;
}

public class AlertSettings
{
    public bool EnableAlerts { get; set; } = true;
    public double CpuTempThreshold { get; set; } = 85.0;
    public double GpuTempThreshold { get; set; } = 85.0;
    public double CpuUsageThreshold { get; set; } = 95.0;
    public double RamUsageThreshold { get; set; } = 90.0;
    public double BatteryLowThreshold { get; set; } = 20.0;
    public double BatteryHighThreshold { get; set; } = 80.0;
    public double DiskUsageThreshold { get; set; } = 90.0;
    public int AlertCooldownSeconds { get; set; } = 300;
}
