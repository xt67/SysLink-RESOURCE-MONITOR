using Microsoft.AspNetCore.Mvc;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Controllers;

/// <summary>
/// Controller for system status and metrics endpoints.
/// </summary>
[ApiController]
[Route("api")]
public class StatusController : ControllerBase
{
    private readonly IHardwareMonitor _hardwareMonitor;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IHardwareMonitor hardwareMonitor, ILogger<StatusController> logger)
    {
        _hardwareMonitor = hardwareMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Gets full system metrics snapshot.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemMetrics), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemMetrics>> GetStatus()
    {
        try
        {
            var metrics = await _hardwareMonitor.GetMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, new { error = "Failed to get system status" });
        }
    }

    /// <summary>
    /// Gets minimal metrics for simple view.
    /// </summary>
    [HttpGet("minimal")]
    [ProducesResponseType(typeof(MinimalMetrics), StatusCodes.Status200OK)]
    public async Task<ActionResult<MinimalMetrics>> GetMinimal()
    {
        try
        {
            var metrics = await _hardwareMonitor.GetMinimalMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting minimal metrics");
            return StatusCode(500, new { error = "Failed to get minimal metrics" });
        }
    }

    /// <summary>
    /// Gets static system information.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfo), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemInfo>> GetInfo()
    {
        try
        {
            var info = await _hardwareMonitor.GetSystemInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system info");
            return StatusCode(500, new { error = "Failed to get system info" });
        }
    }
}
