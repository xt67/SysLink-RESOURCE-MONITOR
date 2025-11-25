using System.Security.Cryptography;
using System.Text;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.Services;

/// <summary>
/// Authentication service for managing device pairing and tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfigService _configService;
    private readonly ILogger<AuthService> _logger;
    private readonly Dictionary<string, AuthToken> _tokens = new();
    private readonly Dictionary<string, PairedDevice> _pairedDevices = new();
    private readonly Dictionary<string, (string Code, DateTime Expiry)> _pairingCodes = new();
    private readonly object _lock = new();

    public AuthService(IConfigService configService, ILogger<AuthService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<PairResponse> PairDeviceAsync(PairRequest request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request.DeviceName) || string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return new PairResponse
            {
                Success = false,
                Error = "Device name and ID are required"
            };
        }

        lock (_lock)
        {
            // Generate token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes);
            var config = _configService.GetConfig();

            var authToken = new AuthToken
            {
                Token = token,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(config.Security.TokenExpirationMinutes)
            };

            _tokens[token] = authToken;

            // Store paired device
            var device = new PairedDevice
            {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                DeviceType = request.DeviceType,
                PairedAt = DateTime.UtcNow,
                LastConnected = DateTime.UtcNow,
                IsActive = true
            };

            _pairedDevices[request.DeviceId] = device;

            _logger.LogInformation("Device paired: {DeviceName} ({DeviceId})", request.DeviceName, request.DeviceId);

            return new PairResponse
            {
                Success = true,
                Token = token,
                ExpiresAt = authToken.ExpiresAt,
                ServerName = Environment.MachineName,
                ServerId = GetServerId()
            };
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var config = _configService.GetConfig();
        
        // If auth is disabled, allow all
        if (!config.Security.RequireAuthentication)
            return true;

        // Check static token from config
        if (token == config.Security.AuthToken)
            return true;

        lock (_lock)
        {
            if (_tokens.TryGetValue(token, out var authToken))
            {
                if (authToken.IsValid)
                {
                    // Update last connected for the device
                    if (_pairedDevices.TryGetValue(authToken.DeviceId, out var device))
                    {
                        device.LastConnected = DateTime.UtcNow;
                    }
                    return true;
                }

                // Token expired, remove it
                _tokens.Remove(token);
            }
        }

        return false;
    }

    public async Task<bool> RevokeTokenAsync(string deviceId)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            // Find and remove tokens for this device
            var tokensToRemove = _tokens
                .Where(t => t.Value.DeviceId == deviceId)
                .Select(t => t.Key)
                .ToList();

            foreach (var token in tokensToRemove)
            {
                _tokens.Remove(token);
            }

            // Mark device as inactive or remove
            if (_pairedDevices.TryGetValue(deviceId, out var device))
            {
                device.IsActive = false;
                _logger.LogInformation("Device revoked: {DeviceName} ({DeviceId})", device.DeviceName, deviceId);
                return true;
            }

            return tokensToRemove.Count > 0;
        }
    }

    public async Task<List<PairedDevice>> GetPairedDevicesAsync()
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            return _pairedDevices.Values.ToList();
        }
    }

    public async Task<string> GeneratePairingCodeAsync()
    {
        await Task.CompletedTask;

        // Generate a 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        
        lock (_lock)
        {
            // Clean up expired codes
            var expiredCodes = _pairingCodes
                .Where(c => c.Value.Expiry < DateTime.UtcNow)
                .Select(c => c.Key)
                .ToList();

            foreach (var expiredCode in expiredCodes)
            {
                _pairingCodes.Remove(expiredCode);
            }

            // Store new code with 5 minute expiry
            _pairingCodes[code] = (code, DateTime.UtcNow.AddMinutes(5));
        }

        return code;
    }

    public bool ValidatePairingCode(string code)
    {
        lock (_lock)
        {
            if (_pairingCodes.TryGetValue(code, out var entry))
            {
                if (entry.Expiry > DateTime.UtcNow)
                {
                    _pairingCodes.Remove(code);
                    return true;
                }
                _pairingCodes.Remove(code);
            }
        }
        return false;
    }

    private string GetServerId()
    {
        // Generate a consistent server ID based on machine name
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
        return Convert.ToHexString(hash).Substring(0, 16);
    }
}
