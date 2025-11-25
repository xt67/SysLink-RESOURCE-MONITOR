using Microsoft.AspNetCore.Mvc;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Controllers;

/// <summary>
/// Controller for historical metrics endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly IMetricsStorage _metricsStorage;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(IMetricsStorage metricsStorage, ILogger<HistoryController> logger)
    {
        _metricsStorage = metricsStorage;
        _logger = logger;
    }

    /// <summary>
    /// Gets historical data for a specific metric type.
    /// </summary>
    [HttpGet("{metric}")]
    [ProducesResponseType(typeof(HistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HistoryResponse>> GetHistory(
        string metric,
        [FromQuery] string? period = "1h",
        [FromQuery] int? maxPoints = 360,
        [FromQuery] string? aggregation = "average")
    {
        try
        {
            // Validate metric type
            if (!MetricTypes.All.Contains(metric, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new 
                { 
                    error = $"Invalid metric type: {metric}", 
                    validTypes = MetricTypes.All 
                });
            }

            // Parse period
            var timeSpan = ParsePeriod(period ?? "1h");

            // Parse aggregation
            var aggregationType = Enum.TryParse<AggregationType>(aggregation, true, out var agg)
                ? agg
                : AggregationType.Average;

            var options = new HistoryQueryOptions
            {
                MetricType = metric,
                Period = timeSpan,
                MaxPoints = maxPoints,
                Aggregation = aggregationType
            };

            var history = await _metricsStorage.GetHistoryAsync(options);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for {Metric}", metric);
            return StatusCode(500, new { error = "Failed to get history" });
        }
    }

    /// <summary>
    /// Gets available metric types.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public ActionResult<string[]> GetMetricTypes()
    {
        return Ok(MetricTypes.All);
    }

    private TimeSpan ParsePeriod(string period)
    {
        var value = int.TryParse(period.TrimEnd('h', 'm', 'd'), out var num) ? num : 1;
        
        return period.ToLower() switch
        {
            var p when p.EndsWith("m") => TimeSpan.FromMinutes(value),
            var p when p.EndsWith("h") => TimeSpan.FromHours(value),
            var p when p.EndsWith("d") => TimeSpan.FromDays(value),
            _ => TimeSpan.FromHours(1)
        };
    }
}
