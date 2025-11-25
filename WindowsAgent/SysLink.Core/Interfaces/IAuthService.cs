using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Processes a pairing request from a new device.
    /// </summary>
    Task<PairResponse> PairDeviceAsync(PairRequest request);
    
    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Revokes a token (unpairs device).
    /// </summary>
    Task<bool> RevokeTokenAsync(string token);
    
    /// <summary>
    /// Gets all paired devices.
    /// </summary>
    Task<List<PairedDevice>> GetPairedDevicesAsync();
    
    /// <summary>
    /// Generates a new pairing code for display.
    /// </summary>
    Task<string> GeneratePairingCodeAsync();
}
