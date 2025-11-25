using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for alert monitoring and notification service.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Event raised when a new alert is triggered.
    /// </summary>
    event EventHandler<Alert>? AlertTriggered;
    
    /// <summary>
    /// Checks metrics against thresholds and generates alerts.
    /// </summary>
    Task CheckMetricsAsync(SystemMetrics metrics);
    
    /// <summary>
    /// Gets all active (unacknowledged) alerts.
    /// </summary>
    Task<List<Alert>> GetActiveAlertsAsync();
    
    /// <summary>
    /// Gets alert history.
    /// </summary>
    Task<List<Alert>> GetAlertHistoryAsync(int count = 100);
    
    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task AcknowledgeAlertAsync(string alertId);
    
    /// <summary>
    /// Updates alert thresholds from config.
    /// </summary>
    void UpdateThresholds(AlertSettings settings);
}
