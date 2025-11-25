using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Data;

/// <summary>
/// Configuration service for managing agent settings.
/// </summary>
public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configPath;
    private AgentConfig _config = new();
    private readonly object _lock = new();

    public event EventHandler<AgentConfig>? ConfigChanged;

    public ConfigService(ILogger<ConfigService> logger, string configPath = "config.json")
    {
        _logger = logger;
        _configPath = configPath;
    }

    public AgentConfig GetConfig()
    {
        lock (_lock)
        {
            return _config;
        }
    }

    public async Task UpdateConfigAsync(AgentConfig config)
    {
        lock (_lock)
        {
            _config = config;
        }

        await SaveAsync();
        ConfigChanged?.Invoke(this, config);
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<AgentConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config != null)
                {
                    lock (_lock)
                    {
                        _config = config;
                    }
                    _logger.LogInformation("Configuration loaded from {Path}", _configPath);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration, using defaults");
        }

        // Initialize with defaults
        await ResetToDefaultsAsync();
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogInformation("Configuration saved to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            throw;
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        lock (_lock)
        {
            _config = new AgentConfig();
            
            // Generate a default auth token if none exists
            if (string.IsNullOrEmpty(_config.Security.AuthToken))
            {
                _config.Security.AuthToken = GenerateAuthToken();
            }
        }

        await SaveAsync();
        ConfigChanged?.Invoke(this, _config);
    }

    private string GenerateAuthToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
