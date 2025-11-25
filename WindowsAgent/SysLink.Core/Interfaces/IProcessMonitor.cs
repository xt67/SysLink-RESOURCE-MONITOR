using SysLink.Core.Models;

namespace SysLink.Core.Interfaces;

/// <summary>
/// Interface for process monitoring service.
/// </summary>
public interface IProcessMonitor
{
    /// <summary>
    /// Gets list of processes with optional filtering and sorting.
    /// </summary>
    Task<ProcessListResponse> GetProcessesAsync(ProcessQueryOptions? options = null);
    
    /// <summary>
    /// Gets details for a specific process.
    /// </summary>
    Task<ProcessInfo?> GetProcessAsync(int pid);
    
    /// <summary>
    /// Gets top N processes by CPU usage.
    /// </summary>
    Task<List<ProcessInfo>> GetTopByCpuAsync(int count = 10);
    
    /// <summary>
    /// Gets top N processes by memory usage.
    /// </summary>
    Task<List<ProcessInfo>> GetTopByMemoryAsync(int count = 10);
    
    /// <summary>
    /// Updates process metrics cache.
    /// </summary>
    Task UpdateAsync();
}
