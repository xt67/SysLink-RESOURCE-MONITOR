using Microsoft.AspNetCore.Mvc;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Controllers;

/// <summary>
/// Controller for process monitoring endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProcessesController : ControllerBase
{
    private readonly IProcessMonitor _processMonitor;
    private readonly ILogger<ProcessesController> _logger;

    public ProcessesController(IProcessMonitor processMonitor, ILogger<ProcessesController> logger)
    {
        _processMonitor = processMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Gets list of processes with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProcessListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProcessListResponse>> GetProcesses(
        [FromQuery] string? sortBy = "CpuUsage",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int? top = 50,
        [FromQuery] string? search = null,
        [FromQuery] bool includeSystem = false)
    {
        try
        {
            var options = new ProcessQueryOptions
            {
                SortBy = Enum.TryParse<ProcessSortField>(sortBy, true, out var field) 
                    ? field 
                    : ProcessSortField.CpuUsage,
                SortDescending = sortDesc,
                Top = top,
                SearchTerm = search,
                IncludeSystemProcesses = includeSystem
            };

            var processes = await _processMonitor.GetProcessesAsync(options);
            return Ok(processes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processes");
            return StatusCode(500, new { error = "Failed to get processes" });
        }
    }

    /// <summary>
    /// Gets details for a specific process.
    /// </summary>
    [HttpGet("{pid:int}")]
    [ProducesResponseType(typeof(ProcessInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProcessInfo>> GetProcess(int pid)
    {
        try
        {
            var process = await _processMonitor.GetProcessAsync(pid);
            if (process == null)
                return NotFound(new { error = $"Process {pid} not found" });

            return Ok(process);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process {Pid}", pid);
            return StatusCode(500, new { error = "Failed to get process" });
        }
    }

    /// <summary>
    /// Gets top processes by CPU usage.
    /// </summary>
    [HttpGet("top/cpu")]
    [ProducesResponseType(typeof(List<ProcessInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProcessInfo>>> GetTopByCpu([FromQuery] int count = 10)
    {
        try
        {
            var processes = await _processMonitor.GetTopByCpuAsync(count);
            return Ok(processes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top CPU processes");
            return StatusCode(500, new { error = "Failed to get processes" });
        }
    }

    /// <summary>
    /// Gets top processes by memory usage.
    /// </summary>
    [HttpGet("top/memory")]
    [ProducesResponseType(typeof(List<ProcessInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProcessInfo>>> GetTopByMemory([FromQuery] int count = 10)
    {
        try
        {
            var processes = await _processMonitor.GetTopByMemoryAsync(count);
            return Ok(processes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top memory processes");
            return StatusCode(500, new { error = "Failed to get processes" });
        }
    }
}
