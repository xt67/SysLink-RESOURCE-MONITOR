namespace SysLink.Core.Models;

/// <summary>
/// Authentication and pairing models.
/// </summary>
public class PairRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Android";
    public string PublicKey { get; set; } = string.Empty;
}

public class PairResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
}

public class AuthToken
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsValid => DateTime.UtcNow < ExpiresAt;
}

public class PairedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime PairedAt { get; set; }
    public DateTime LastConnected { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
