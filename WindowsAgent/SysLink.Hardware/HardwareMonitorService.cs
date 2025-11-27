using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Hardware;

/// <summary>
/// Main hardware monitoring service using LibreHardwareMonitor.
/// </summary>
public class HardwareMonitorService : IHardwareMonitor
{
    private readonly ILogger<HardwareMonitorService> _logger;
    private readonly Computer _computer;
    private readonly object _lock = new();
    
    private SystemMetrics _cachedMetrics = new();
    private SystemInfo? _systemInfo;
    private bool _isInitialized;

    public HardwareMonitorService(ILogger<HardwareMonitorService> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsBatteryEnabled = true,
            IsControllerEnabled = true,
            IsMotherboardEnabled = true
        };
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await Task.Run(() =>
        {
            lock (_lock)
            {
                _computer.Open();
                _computer.Accept(new UpdateVisitor());
                _isInitialized = true;
                _logger.LogInformation("Hardware monitoring initialized");
            }
        });
    }

    public async Task UpdateAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        await Task.Run(() =>
        {
            lock (_lock)
            {
                _computer.Accept(new UpdateVisitor());
            }
        });
    }

    public async Task<SystemMetrics> GetMetricsAsync()
    {
        await UpdateAsync();

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var metrics = new SystemMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    Cpu = GetCpuMetrics(),
                    Gpu = GetGpuMetrics(),
                    Ram = GetRamMetrics(),
                    Disks = GetDiskMetrics(),
                    Network = GetNetworkMetrics(),
                    Battery = GetBatteryMetrics(),
                    Fans = GetFanMetrics()
                };

                _cachedMetrics = metrics;
                return metrics;
            }
        });
    }

    public async Task<MinimalMetrics> GetMinimalMetricsAsync()
    {
        var metrics = await GetMetricsAsync();
        
        return new MinimalMetrics
        {
            Timestamp = metrics.Timestamp,
            CpuUsage = metrics.Cpu.AverageUsage,
            GpuUsage = metrics.Gpu.Usage,
            RamUsage = metrics.Ram.UsagePercent,
            BatteryPercent = metrics.Battery?.ChargePercent,
            MaxCpuTemp = metrics.Cpu.MaxTemperature,
            GpuTemp = metrics.Gpu.Temperature
        };
    }

    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        if (_systemInfo != null) return _systemInfo;

        await InitializeAsync();

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                _systemInfo = new SystemInfo
                {
                    DeviceName = Environment.MachineName,
                    OperatingSystem = "Windows",
                    OsVersion = Environment.OSVersion.VersionString,
                    CpuName = GetCpuName(),
                    CpuCores = Environment.ProcessorCount,
                    CpuThreads = Environment.ProcessorCount,
                    GpuName = GetGpuName(),
                    TotalRamGB = GetTotalRam(),
                    Motherboard = GetMotherboardName(),
                    BootTime = GetBootTime(),
                    HasBattery = HasBattery()
                };

                return _systemInfo;
            }
        });
    }

    private CpuMetrics GetCpuMetrics()
    {
        var cpuMetrics = new CpuMetrics();
        var cores = new List<CpuCoreMetrics>();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                cpuMetrics.Name = hardware.Name;
                int coreId = 0;

                foreach (var sensor in hardware.Sensors)
                {
                    switch (sensor.SensorType)
                    {
                        case SensorType.Load when sensor.Name.Contains("Total"):
                            cpuMetrics.AverageUsage = sensor.Value ?? 0;
                            break;
                        case SensorType.Load when sensor.Name.Contains("Core"):
                            if (coreId < cores.Count)
                                cores[coreId].Usage = sensor.Value ?? 0;
                            else
                                cores.Add(new CpuCoreMetrics { CoreId = coreId, Usage = sensor.Value ?? 0 });
                            coreId++;
                            break;
                        case SensorType.Temperature when sensor.Name.Contains("Core"):
                            var tempCoreId = ExtractCoreId(sensor.Name);
                            if (tempCoreId < cores.Count)
                                cores[tempCoreId].Temperature = sensor.Value ?? 0;
                            else if (tempCoreId == cores.Count)
                                cores.Add(new CpuCoreMetrics { CoreId = tempCoreId, Temperature = sensor.Value ?? 0 });
                            break;
                        case SensorType.Temperature when sensor.Name.Contains("Package") || sensor.Name.Contains("Average"):
                            cpuMetrics.AverageTemperature = sensor.Value ?? 0;
                            break;
                        case SensorType.Temperature when sensor.Name.Contains("Max"):
                            cpuMetrics.MaxTemperature = sensor.Value ?? 0;
                            break;
                        case SensorType.Power when sensor.Name.Contains("Package"):
                            cpuMetrics.TotalPower = sensor.Value ?? 0;
                            break;
                        case SensorType.Clock when sensor.Name.Contains("Core"):
                            var clockCoreId = ExtractCoreId(sensor.Name);
                            if (clockCoreId < cores.Count)
                                cores[clockCoreId].ClockSpeed = sensor.Value ?? 0;
                            break;
                    }
                }
            }
        }

        if (cpuMetrics.MaxTemperature == 0 && cores.Count > 0)
            cpuMetrics.MaxTemperature = cores.Max(c => c.Temperature);
        
        if (cpuMetrics.AverageTemperature == 0 && cores.Count > 0)
            cpuMetrics.AverageTemperature = cores.Average(c => c.Temperature);

        cpuMetrics.Cores = cores;
        return cpuMetrics;
    }

    private GpuMetrics GetGpuMetrics()
    {
        var gpuMetrics = new GpuMetrics();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                gpuMetrics.Name = hardware.Name;

                foreach (var sensor in hardware.Sensors)
                {
                    switch (sensor.SensorType)
                    {
                        case SensorType.Temperature when sensor.Name.Contains("Core") || sensor.Name.Contains("GPU"):
                            gpuMetrics.Temperature = sensor.Value ?? 0;
                            break;
                        case SensorType.Load when sensor.Name.Contains("Core") || sensor.Name.Contains("GPU"):
                            gpuMetrics.Usage = sensor.Value ?? 0;
                            break;
                        case SensorType.SmallData when sensor.Name.Contains("Memory Used"):
                            gpuMetrics.VramUsed = sensor.Value ?? 0;
                            break;
                        case SensorType.SmallData when sensor.Name.Contains("Memory Total"):
                            gpuMetrics.VramTotal = sensor.Value ?? 0;
                            break;
                        case SensorType.Clock when sensor.Name.Contains("Core"):
                            gpuMetrics.CoreClock = sensor.Value ?? 0;
                            break;
                        case SensorType.Clock when sensor.Name.Contains("Memory"):
                            gpuMetrics.MemoryClock = sensor.Value ?? 0;
                            break;
                        case SensorType.Power:
                            gpuMetrics.Power = sensor.Value ?? 0;
                            break;
                        case SensorType.Fan:
                            gpuMetrics.FanSpeed = sensor.Value ?? 0;
                            break;
                    }
                }
                break; // Use first GPU found
            }
        }

        return gpuMetrics;
    }

    private RamMetrics GetRamMetrics()
    {
        var ramMetrics = new RamMetrics();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Memory)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    switch (sensor.SensorType)
                    {
                        case SensorType.Data when sensor.Name.Contains("Used"):
                            ramMetrics.UsedGB = sensor.Value ?? 0;
                            break;
                        case SensorType.Data when sensor.Name.Contains("Available"):
                            ramMetrics.TotalGB = ramMetrics.UsedGB + (sensor.Value ?? 0);
                            break;
                        case SensorType.Load when sensor.Name.Contains("Memory"):
                            // Use load to calculate if data not available
                            break;
                    }
                }
            }
        }

        // Get swap info from system
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            if (ramMetrics.TotalGB == 0)
            {
                ramMetrics.TotalGB = gcMemoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
            }
        }
        catch { }

        return ramMetrics;
    }

    private List<DiskMetrics> GetDiskMetrics()
    {
        var disks = new List<DiskMetrics>();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Storage)
            {
                var disk = new DiskMetrics { Name = hardware.Name };

                foreach (var sensor in hardware.Sensors)
                {
                    switch (sensor.SensorType)
                    {
                        case SensorType.Temperature:
                            disk.Temperature = sensor.Value ?? 0;
                            break;
                        case SensorType.Load when sensor.Name.Contains("Used"):
                            disk.UsagePercent.ToString(); // Placeholder
                            break;
                        case SensorType.Throughput when sensor.Name.Contains("Read"):
                            disk.ReadSpeedMBps = (sensor.Value ?? 0);
                            break;
                        case SensorType.Throughput when sensor.Name.Contains("Write"):
                            disk.WriteSpeedMBps = (sensor.Value ?? 0);
                            break;
                    }
                }

                disks.Add(disk);
            }
        }

        // Add drive letter info
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            foreach (var drive in drives)
            {
                var existingDisk = disks.FirstOrDefault();
                if (existingDisk != null && string.IsNullOrEmpty(existingDisk.DriveLetter))
                {
                    existingDisk.DriveLetter = drive.Name;
                    existingDisk.TotalGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                    existingDisk.UsedGB = (drive.TotalSize - drive.TotalFreeSpace) / (1024.0 * 1024 * 1024);
                }
                else
                {
                    disks.Add(new DiskMetrics
                    {
                        Name = drive.VolumeLabel ?? drive.Name,
                        DriveLetter = drive.Name,
                        TotalGB = drive.TotalSize / (1024.0 * 1024 * 1024),
                        UsedGB = (drive.TotalSize - drive.TotalFreeSpace) / (1024.0 * 1024 * 1024)
                    });
                }
            }
        }
        catch { }

        return disks;
    }

    private NetworkMetrics GetNetworkMetrics()
    {
        var networkMetrics = new NetworkMetrics();

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Network)
            {
                networkMetrics.AdapterName = hardware.Name;
                networkMetrics.ConnectionType = hardware.Name.Contains("Wi-Fi") || hardware.Name.Contains("Wireless") 
                    ? "WiFi" 
                    : "Ethernet";

                foreach (var sensor in hardware.Sensors)
                {
                    switch (sensor.SensorType)
                    {
                        case SensorType.Throughput when sensor.Name.Contains("Upload"):
                            networkMetrics.UploadSpeedMbps = (sensor.Value ?? 0) * 8 / 1000000; // Convert to Mbps
                            break;
                        case SensorType.Throughput when sensor.Name.Contains("Download"):
                            networkMetrics.DownloadSpeedMbps = (sensor.Value ?? 0) * 8 / 1000000;
                            break;
                        case SensorType.Data when sensor.Name.Contains("Uploaded"):
                            networkMetrics.TotalBytesSent = (long)(sensor.Value ?? 0) * 1024 * 1024;
                            break;
                        case SensorType.Data when sensor.Name.Contains("Downloaded"):
                            networkMetrics.TotalBytesReceived = (long)(sensor.Value ?? 0) * 1024 * 1024;
                            break;
                    }
                }

                if (networkMetrics.DownloadSpeedMbps > 0 || networkMetrics.UploadSpeedMbps > 0)
                {
                    networkMetrics.IsConnected = true;
                    break;
                }
            }
        }

        return networkMetrics;
    }

    // Native Windows API for power status
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;          // 0=Offline, 1=Online, 255=Unknown
        public byte BatteryFlag;           // 1=High, 2=Low, 4=Critical, 8=Charging, 128=No battery
        public byte BatteryLifePercent;    // 0-100 or 255 if unknown
        public byte SystemStatusFlag;
        public int BatteryLifeTime;        // seconds remaining, -1 if unknown
        public int BatteryFullLifeTime;
    }

    private BatteryMetrics? GetBatteryMetrics()
    {
        // First check if battery hardware exists
        var batteryHardware = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Battery);
        if (batteryHardware == null)
            return null;

        var battery = new BatteryMetrics { IsPresent = true };

        // Use native Windows API for accurate battery status (most reliable)
        try
        {
            if (GetSystemPowerStatus(out var powerStatus))
            {
                // Get charge percentage
                if (powerStatus.BatteryLifePercent != 255)
                {
                    battery.ChargePercent = powerStatus.BatteryLifePercent;
                }

                // Determine charging status based on ACLineStatus and BatteryFlag
                bool isPluggedIn = powerStatus.ACLineStatus == 1;
                bool isCharging = (powerStatus.BatteryFlag & 8) != 0;
                
                if (isPluggedIn)
                {
                    if (isCharging)
                        battery.Status = BatteryStatus.Charging;
                    else if (powerStatus.BatteryLifePercent >= 95)
                        battery.Status = BatteryStatus.Full;
                    else
                        battery.Status = BatteryStatus.NotCharging; // Plugged in but not charging (conservation mode, etc.)
                }
                else
                {
                    battery.Status = BatteryStatus.Discharging;
                }

                // Get estimated time remaining
                if (powerStatus.BatteryLifeTime > 0 && powerStatus.BatteryLifeTime != -1)
                {
                    battery.EstimatedTimeRemaining = TimeSpan.FromSeconds(powerStatus.BatteryLifeTime);
                }
            }
        }
        catch
        {
            // Fallback: try WMI
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var estimatedCharge = obj["EstimatedChargeRemaining"];
                    if (estimatedCharge != null)
                    {
                        battery.ChargePercent = Convert.ToDouble(estimatedCharge);
                    }
                    break;
                }
            }
            catch { }
        }

        // Get additional info from LibreHardwareMonitor
        foreach (var sensor in batteryHardware.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Level when sensor.Name.Contains("Charge"):
                    // Only use if WMI didn't work
                    if (battery.ChargePercent == 0)
                        battery.ChargePercent = sensor.Value ?? 0;
                    break;
                case SensorType.Energy when sensor.Name.Contains("Designed"):
                    battery.DesignCapacityWh = sensor.Value ?? 0;
                    break;
                case SensorType.Energy when sensor.Name.Contains("Full"):
                    battery.FullChargeCapacityWh = sensor.Value ?? 0;
                    break;
                case SensorType.Power when sensor.Name.Contains("Charge"):
                    battery.ChargeRateW = sensor.Value ?? 0;
                    break;
                case SensorType.Power when sensor.Name.Contains("Discharge"):
                    battery.DischargeRateW = sensor.Value ?? 0;
                    break;
                case SensorType.Voltage:
                    battery.Voltage = sensor.Value ?? 0;
                    break;
                case SensorType.TimeSpan when sensor.Name.Contains("Remaining"):
                    battery.EstimatedTimeRemaining = TimeSpan.FromSeconds(sensor.Value ?? 0);
                    break;
            }
        }

        return battery;
    }

    private List<FanMetrics> GetFanMetrics()
    {
        var fans = new List<FanMetrics>();

        foreach (var hardware in _computer.Hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Fan)
                {
                    var fanType = FanType.Other;
                    if (sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
                        fanType = FanType.CpuFan;
                    else if (sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase))
                        fanType = FanType.GpuFan;
                    else if (sensor.Name.Contains("Pump", StringComparison.OrdinalIgnoreCase))
                        fanType = FanType.PumpFan;
                    else if (sensor.Name.Contains("Case", StringComparison.OrdinalIgnoreCase) || 
                             sensor.Name.Contains("Chassis", StringComparison.OrdinalIgnoreCase))
                        fanType = FanType.CaseFan;

                    fans.Add(new FanMetrics
                    {
                        Name = sensor.Name,
                        Rpm = sensor.Value ?? 0,
                        Type = fanType
                    });
                }
            }

            // Check subhardware for fans
            foreach (var subHardware in hardware.SubHardware)
            {
                foreach (var sensor in subHardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Fan)
                    {
                        fans.Add(new FanMetrics
                        {
                            Name = sensor.Name,
                            Rpm = sensor.Value ?? 0,
                            Type = FanType.Other
                        });
                    }
                }
            }
        }

        return fans;
    }

    private string GetCpuName()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
                return hardware.Name;
        }
        return "Unknown CPU";
    }

    private string GetGpuName()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
                return hardware.Name;
        }
        return "Unknown GPU";
    }

    private string GetMotherboardName()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Motherboard)
                return hardware.Name;
        }
        return "Unknown Motherboard";
    }

    private double GetTotalRam()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        return gcMemoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
    }

    private bool HasBattery()
    {
        return _computer.Hardware.Any(h => h.HardwareType == HardwareType.Battery);
    }

    private DateTime GetBootTime()
    {
        try
        {
            return DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        }
        catch
        {
            return DateTime.Now;
        }
    }

    private int ExtractCoreId(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"#(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
            return id - 1;
        return 0;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _computer.Close();
        }
    }
}

/// <summary>
/// Visitor to update all hardware sensors.
/// </summary>
internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
            subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}
