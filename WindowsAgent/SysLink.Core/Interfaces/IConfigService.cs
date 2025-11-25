using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for configuration management.
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Gets the current agent configuration.
    /// </summary>
    AgentConfig GetConfig();
    
    /// <summary>
    /// Updates the agent configuration.
    /// </summary>
    Task UpdateConfigAsync(AgentConfig config);
    
    /// <summary>
    /// Loads configuration from file.
    /// </summary>
    Task LoadAsync();
    
    /// <summary>
    /// Saves configuration to file.
    /// </summary>
    Task SaveAsync();
    
    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    Task ResetToDefaultsAsync();
    
    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    event EventHandler<AgentConfig>? ConfigChanged;
}
