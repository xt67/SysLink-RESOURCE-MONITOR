using Microsoft.AspNetCore.Mvc;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Controllers;

/// <summary>
/// Controller for agent configuration endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfigService configService, ILogger<ConfigController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current agent configuration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AgentConfig), StatusCodes.Status200OK)]
    public ActionResult<AgentConfig> GetConfig()
    {
        try
        {
            var config = _configService.GetConfig();
            
            // Remove sensitive data from response
            var safeConfig = new AgentConfig
            {
                Server = config.Server,
                Monitoring = config.Monitoring,
                Storage = config.Storage,
                Alerts = config.Alerts,
                Security = new SecuritySettings
                {
                    RequireAuthentication = config.Security.RequireAuthentication,
                    AllowRemoteAccess = config.Security.AllowRemoteAccess,
                    EnableMutualTls = config.Security.EnableMutualTls,
                    TokenExpirationMinutes = config.Security.TokenExpirationMinutes,
                    // Don't expose the actual token
                    AuthToken = "********"
                }
            };

            return Ok(safeConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration");
            return StatusCode(500, new { error = "Failed to get configuration" });
        }
    }

    /// <summary>
    /// Updates agent configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateConfig([FromBody] AgentConfig config)
    {
        try
        {
            // Preserve existing auth token if not provided
            var currentConfig = _configService.GetConfig();
            if (string.IsNullOrEmpty(config.Security.AuthToken) || config.Security.AuthToken == "********")
            {
                config.Security.AuthToken = currentConfig.Security.AuthToken;
            }

            await _configService.UpdateConfigAsync(config);
            return Ok(new { message = "Configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(500, new { error = "Failed to update configuration" });
        }
    }

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ResetConfig()
    {
        try
        {
            await _configService.ResetToDefaultsAsync();
            return Ok(new { message = "Configuration reset to defaults" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration");
            return StatusCode(500, new { error = "Failed to reset configuration" });
        }
    }
}
