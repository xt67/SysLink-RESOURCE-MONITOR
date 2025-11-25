namespace SysLink.Core.Models;

/// <summary>
/// Process information for process monitoring.
/// </summary>
public class ProcessInfo
{
    public int Pid { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsageMB { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double DiskReadMBps { get; set; }
    public double DiskWriteMBps { get; set; }
    public double NetworkUsageMbps { get; set; }
    public ProcessStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public string? WindowTitle { get; set; }
    public string? FilePath { get; set; }
}

public enum ProcessStatus
{
    Running,
    Suspended,
    NotResponding,
    Unknown
}

/// <summary>
/// Process list response with filtering options applied.
/// </summary>
public class ProcessListResponse
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TotalProcessCount { get; set; }
    public List<ProcessInfo> Processes { get; set; } = new();
    public string? FilterApplied { get; set; }
    public string? SortBy { get; set; }
}

/// <summary>
/// Options for filtering and sorting process list.
/// </summary>
public class ProcessQueryOptions
{
    public ProcessSortField SortBy { get; set; } = ProcessSortField.CpuUsage;
    public bool SortDescending { get; set; } = true;
    public int? Top { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public bool IncludeSystemProcesses { get; set; } = false;
}

public enum ProcessSortField
{
    Name,
    Pid,
    CpuUsage,
    MemoryUsage,
    DiskUsage,
    NetworkUsage,
    StartTime
}
