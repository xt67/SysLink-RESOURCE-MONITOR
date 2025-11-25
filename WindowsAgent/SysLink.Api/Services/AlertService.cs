using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Services;

/// <summary>
/// Alert service for monitoring thresholds and generating notifications.
/// </summary>
public class AlertService : IAlertService
{
    private readonly IConfigService _configService;
    private readonly ILogger<AlertService> _logger;
    private readonly List<Alert> _activeAlerts = new();
    private readonly List<Alert> _alertHistory = new();
    private readonly Dictionary<AlertType, DateTime> _lastAlertTime = new();
    private AlertSettings _settings;
    private readonly object _lock = new();

    public event EventHandler<Alert>? AlertTriggered;

    public AlertService(IConfigService configService, ILogger<AlertService> logger)
    {
        _configService = configService;
        _logger = logger;
        _settings = configService.GetConfig().Alerts;
        
        configService.ConfigChanged += (s, c) => UpdateThresholds(c.Alerts);
    }

    public async Task CheckMetricsAsync(SystemMetrics metrics)
    {
        await Task.CompletedTask;

        if (!_settings.EnableAlerts) return;

        var cooldown = TimeSpan.FromSeconds(_settings.AlertCooldownSeconds);

        // CPU Temperature
        if (metrics.Cpu.MaxTemperature > _settings.CpuTempThreshold)
        {
            TryCreateAlert(AlertType.CpuTemperature, AlertSeverity.Warning,
                "High CPU Temperature",
                $"CPU temperature is {metrics.Cpu.MaxTemperature:F1}°C",
                metrics.Cpu.MaxTemperature, _settings.CpuTempThreshold, "CPU Temperature",
                cooldown);
        }

        // GPU Temperature
        if (metrics.Gpu.Temperature > _settings.GpuTempThreshold)
        {
            TryCreateAlert(AlertType.GpuTemperature, AlertSeverity.Warning,
                "High GPU Temperature",
                $"GPU temperature is {metrics.Gpu.Temperature:F1}°C",
                metrics.Gpu.Temperature, _settings.GpuTempThreshold, "GPU Temperature",
                cooldown);
        }

        // CPU Usage
        if (metrics.Cpu.AverageUsage > _settings.CpuUsageThreshold)
        {
            TryCreateAlert(AlertType.CpuUsage, AlertSeverity.Info,
                "High CPU Usage",
                $"CPU usage is {metrics.Cpu.AverageUsage:F1}%",
                metrics.Cpu.AverageUsage, _settings.CpuUsageThreshold, "CPU Usage",
                cooldown);
        }

        // RAM Usage
        if (metrics.Ram.UsagePercent > _settings.RamUsageThreshold)
        {
            TryCreateAlert(AlertType.RamUsage, AlertSeverity.Warning,
                "High RAM Usage",
                $"RAM usage is {metrics.Ram.UsagePercent:F1}%",
                metrics.Ram.UsagePercent, _settings.RamUsageThreshold, "RAM Usage",
                cooldown);
        }

        // Battery Low
        if (metrics.Battery != null)
        {
            if (metrics.Battery.ChargePercent < _settings.BatteryLowThreshold &&
                metrics.Battery.Status == BatteryStatus.Discharging)
            {
                TryCreateAlert(AlertType.BatteryLow, AlertSeverity.Warning,
                    "Low Battery",
                    $"Battery is at {metrics.Battery.ChargePercent:F1}%",
                    metrics.Battery.ChargePercent, _settings.BatteryLowThreshold, "Battery",
                    cooldown);
            }
        }

        // Disk Usage
        foreach (var disk in metrics.Disks)
        {
            if (disk.UsagePercent > _settings.DiskUsageThreshold)
            {
                TryCreateAlert(AlertType.DiskUsage, AlertSeverity.Warning,
                    "High Disk Usage",
                    $"Disk {disk.DriveLetter} is {disk.UsagePercent:F1}% full",
                    disk.UsagePercent, _settings.DiskUsageThreshold, $"Disk {disk.DriveLetter}",
                    cooldown);
            }
        }
    }

    private void TryCreateAlert(AlertType type, AlertSeverity severity, string title, 
        string message, double currentValue, double threshold, string metricName, TimeSpan cooldown)
    {
        lock (_lock)
        {
            if (_lastAlertTime.TryGetValue(type, out var lastTime))
            {
                if (DateTime.UtcNow - lastTime < cooldown)
                    return;
            }

            var alert = new Alert
            {
                Type = type,
                Severity = severity,
                Title = title,
                Message = message,
                CurrentValue = currentValue,
                ThresholdValue = threshold,
                MetricName = metricName
            };

            _activeAlerts.Add(alert);
            _alertHistory.Add(alert);
            _lastAlertTime[type] = DateTime.UtcNow;

            // Keep history limited
            while (_alertHistory.Count > 1000)
            {
                _alertHistory.RemoveAt(0);
            }

            _logger.LogWarning("Alert triggered: {Title} - {Message}", title, message);
            AlertTriggered?.Invoke(this, alert);
        }
    }

    public async Task<List<Alert>> GetActiveAlertsAsync()
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            return _activeAlerts.Where(a => !a.IsAcknowledged).ToList();
        }
    }

    public async Task<List<Alert>> GetAlertHistoryAsync(int count = 100)
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            return _alertHistory.TakeLast(count).Reverse().ToList();
        }
    }

    public async Task AcknowledgeAlertAsync(string alertId)
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.IsAcknowledged = true;
            }

            var historyAlert = _alertHistory.FirstOrDefault(a => a.Id == alertId);
            if (historyAlert != null)
            {
                historyAlert.IsAcknowledged = true;
            }
        }
    }

    public void UpdateThresholds(AlertSettings settings)
    {
        lock (_lock)
        {
            _settings = settings;
            _logger.LogInformation("Alert thresholds updated");
        }
    }
}
