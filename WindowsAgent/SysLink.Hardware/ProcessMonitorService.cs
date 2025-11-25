using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Hardware;

/// <summary>
/// Process monitoring service for tracking running processes.
/// </summary>
public class ProcessMonitorService : IProcessMonitor
{
    private readonly ILogger<ProcessMonitorService> _logger;
    private readonly Dictionary<int, ProcessCpuTracker> _cpuTrackers = new();
    private readonly object _lock = new();
    private List<ProcessInfo> _cachedProcesses = new();
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(1);

    public ProcessMonitorService(ILogger<ProcessMonitorService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessListResponse> GetProcessesAsync(ProcessQueryOptions? options = null)
    {
        options ??= new ProcessQueryOptions();
        
        await UpdateAsync();

        var processes = _cachedProcesses.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(options.SearchTerm))
        {
            processes = processes.Where(p => 
                p.Name.Contains(options.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Filter system processes if needed
        if (!options.IncludeSystemProcesses)
        {
            processes = processes.Where(p => !IsSystemProcess(p));
        }

        // Apply sorting
        processes = options.SortBy switch
        {
            ProcessSortField.Name => options.SortDescending 
                ? processes.OrderByDescending(p => p.Name) 
                : processes.OrderBy(p => p.Name),
            ProcessSortField.Pid => options.SortDescending 
                ? processes.OrderByDescending(p => p.Pid) 
                : processes.OrderBy(p => p.Pid),
            ProcessSortField.CpuUsage => options.SortDescending 
                ? processes.OrderByDescending(p => p.CpuUsage) 
                : processes.OrderBy(p => p.CpuUsage),
            ProcessSortField.MemoryUsage => options.SortDescending 
                ? processes.OrderByDescending(p => p.MemoryUsageMB) 
                : processes.OrderBy(p => p.MemoryUsageMB),
            ProcessSortField.StartTime => options.SortDescending 
                ? processes.OrderByDescending(p => p.StartTime) 
                : processes.OrderBy(p => p.StartTime),
            _ => processes.OrderByDescending(p => p.CpuUsage)
        };

        var processList = processes.ToList();
        var totalCount = processList.Count;

        // Apply top limit
        if (options.Top.HasValue && options.Top.Value > 0)
        {
            processList = processList.Take(options.Top.Value).ToList();
        }

        return new ProcessListResponse
        {
            Timestamp = DateTime.UtcNow,
            TotalProcessCount = totalCount,
            Processes = processList,
            FilterApplied = options.SearchTerm,
            SortBy = options.SortBy.ToString()
        };
    }

    public async Task<ProcessInfo?> GetProcessAsync(int pid)
    {
        await UpdateAsync();
        return _cachedProcesses.FirstOrDefault(p => p.Pid == pid);
    }

    public async Task<List<ProcessInfo>> GetTopByCpuAsync(int count = 10)
    {
        var response = await GetProcessesAsync(new ProcessQueryOptions
        {
            SortBy = ProcessSortField.CpuUsage,
            SortDescending = true,
            Top = count
        });
        return response.Processes;
    }

    public async Task<List<ProcessInfo>> GetTopByMemoryAsync(int count = 10)
    {
        var response = await GetProcessesAsync(new ProcessQueryOptions
        {
            SortBy = ProcessSortField.MemoryUsage,
            SortDescending = true,
            Top = count
        });
        return response.Processes;
    }

    public async Task UpdateAsync()
    {
        if (DateTime.Now - _lastUpdate < _cacheTimeout)
            return;

        await Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    var processes = Process.GetProcesses();
                    var totalMemory = GetTotalPhysicalMemory();
                    var processorCount = Environment.ProcessorCount;
                    var processInfos = new List<ProcessInfo>();

                    // Clean up old trackers
                    var currentPids = processes.Select(p => p.Id).ToHashSet();
                    var trackerPidsToRemove = _cpuTrackers.Keys.Where(k => !currentPids.Contains(k)).ToList();
                    foreach (var pid in trackerPidsToRemove)
                    {
                        _cpuTrackers.Remove(pid);
                    }

                    foreach (var process in processes)
                    {
                        try
                        {
                            var info = new ProcessInfo
                            {
                                Pid = process.Id,
                                Name = process.ProcessName,
                                Status = GetProcessStatus(process)
                            };

                            // Get memory info
                            try
                            {
                                info.MemoryUsageMB = process.WorkingSet64 / (1024.0 * 1024);
                                info.MemoryUsagePercent = totalMemory > 0 
                                    ? (process.WorkingSet64 / (double)totalMemory) * 100 
                                    : 0;
                            }
                            catch { }

                            // Calculate CPU usage
                            try
                            {
                                info.CpuUsage = CalculateCpuUsage(process, processorCount);
                            }
                            catch { }

                            // Get start time
                            try
                            {
                                info.StartTime = process.StartTime;
                            }
                            catch { }

                            // Get window title
                            try
                            {
                                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                                    info.WindowTitle = process.MainWindowTitle;
                            }
                            catch { }

                            // Get file path
                            try
                            {
                                info.FilePath = process.MainModule?.FileName;
                            }
                            catch { }

                            processInfos.Add(info);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogTrace(ex, "Could not get info for process {Pid}", process.Id);
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }

                    _cachedProcesses = processInfos;
                    _lastUpdate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating process list");
                }
            }
        });
    }

    private double CalculateCpuUsage(Process process, int processorCount)
    {
        if (!_cpuTrackers.TryGetValue(process.Id, out var tracker))
        {
            tracker = new ProcessCpuTracker();
            _cpuTrackers[process.Id] = tracker;
        }

        try
        {
            var currentTime = DateTime.UtcNow;
            var currentCpuTime = process.TotalProcessorTime;

            if (tracker.LastCpuTime != TimeSpan.Zero)
            {
                var cpuTimeDiff = currentCpuTime - tracker.LastCpuTime;
                var timeDiff = currentTime - tracker.LastCheckTime;

                if (timeDiff.TotalMilliseconds > 0)
                {
                    var cpuUsage = (cpuTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds) * 100 / processorCount;
                    tracker.LastCpuUsage = Math.Min(100, Math.Max(0, cpuUsage));
                }
            }

            tracker.LastCpuTime = currentCpuTime;
            tracker.LastCheckTime = currentTime;

            return tracker.LastCpuUsage;
        }
        catch
        {
            return 0;
        }
    }

    private ProcessStatus GetProcessStatus(Process process)
    {
        try
        {
            if (!process.Responding && process.MainWindowHandle != IntPtr.Zero)
                return ProcessStatus.NotResponding;
            
            return ProcessStatus.Running;
        }
        catch
        {
            return ProcessStatus.Unknown;
        }
    }

    private bool IsSystemProcess(ProcessInfo process)
    {
        var systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Idle", "Registry", "smss", "csrss", "wininit",
            "services", "lsass", "svchost", "dwm", "fontdrvhost",
            "WmiPrvSE", "SearchIndexer", "SecurityHealthService"
        };

        return systemProcesses.Contains(process.Name) || process.Pid <= 4;
    }

    private long GetTotalPhysicalMemory()
    {
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            return gcMemoryInfo.TotalAvailableMemoryBytes;
        }
        catch
        {
            return 0;
        }
    }

    private class ProcessCpuTracker
    {
        public TimeSpan LastCpuTime { get; set; }
        public DateTime LastCheckTime { get; set; }
        public double LastCpuUsage { get; set; }
    }
}
