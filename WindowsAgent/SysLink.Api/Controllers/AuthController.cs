using Microsoft.AspNetCore.Mvc;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Controllers;

/// <summary>
/// Controller for authentication and pairing endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Pairs a new device with the agent.
    /// </summary>
    [HttpPost("pair")]
    [ProducesResponseType(typeof(PairResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PairResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PairResponse>> Pair([FromBody] PairRequest request)
    {
        try
        {
            _logger.LogInformation("Pairing request from device: {DeviceName}", request.DeviceName);
            var response = await _authService.PairDeviceAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pairing");
            return StatusCode(500, new PairResponse 
            { 
                Success = false, 
                Error = "Pairing failed due to server error" 
            });
        }
    }

    /// <summary>
    /// Validates current token.
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ValidateToken()
    {
        var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
        
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { error = "No token provided" });
        }

        var isValid = await _authService.ValidateTokenAsync(token);
        
        if (!isValid)
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        return Ok(new { valid = true });
    }

    /// <summary>
    /// Gets list of paired devices.
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<PairedDevice>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PairedDevice>>> GetPairedDevices()
    {
        try
        {
            var devices = await _authService.GetPairedDevicesAsync();
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paired devices");
            return StatusCode(500, new { error = "Failed to get paired devices" });
        }
    }

    /// <summary>
    /// Revokes a device's access.
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeDevice(string deviceId)
    {
        try
        {
            var result = await _authService.RevokeTokenAsync(deviceId);
            
            if (!result)
            {
                return NotFound(new { error = "Device not found" });
            }

            return Ok(new { message = "Device access revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to revoke device" });
        }
    }

    /// <summary>
    /// Generates a new pairing code for QR display.
    /// </summary>
    [HttpGet("pairing-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPairingCode()
    {
        try
        {
            var code = await _authService.GeneratePairingCodeAsync();
            return Ok(new 
            { 
                code,
                expiresIn = 300, // 5 minutes
                serverName = Environment.MachineName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pairing code");
            return StatusCode(500, new { error = "Failed to generate pairing code" });
        }
    }
}
