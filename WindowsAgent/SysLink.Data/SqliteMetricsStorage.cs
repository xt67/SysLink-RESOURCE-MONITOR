using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Data;

/// <summary>
/// SQLite-based metrics storage for historical data.
/// </summary>
public class SqliteMetricsStorage : IMetricsStorage
{
    private readonly ILogger<SqliteMetricsStorage> _logger;
    private readonly string _connectionString;
    private readonly string _databasePath;
    private SqliteConnection? _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteMetricsStorage(ILogger<SqliteMetricsStorage> logger, string databasePath = "syslink_data.db")
    {
        _logger = logger;
        _databasePath = databasePath;
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS metrics (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    metric_type TEXT NOT NULL,
                    metric_name TEXT NOT NULL,
                    value REAL NOT NULL,
                    unit TEXT,
                    tags TEXT
                );
                
                CREATE INDEX IF NOT EXISTS idx_metrics_type_time 
                ON metrics (metric_type, timestamp);
                
                CREATE INDEX IF NOT EXISTS idx_metrics_timestamp 
                ON metrics (timestamp);

                CREATE TABLE IF NOT EXISTS alerts (
                    id TEXT PRIMARY KEY,
                    timestamp TEXT NOT NULL,
                    alert_type INTEGER NOT NULL,
                    severity INTEGER NOT NULL,
                    title TEXT NOT NULL,
                    message TEXT NOT NULL,
                    current_value REAL,
                    threshold_value REAL,
                    metric_name TEXT,
                    acknowledged INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS paired_devices (
                    device_id TEXT PRIMARY KEY,
                    device_name TEXT NOT NULL,
                    device_type TEXT NOT NULL,
                    paired_at TEXT NOT NULL,
                    last_connected TEXT,
                    ip_address TEXT,
                    is_active INTEGER DEFAULT 1,
                    token_hash TEXT
                );
            ";

            using var command = new SqliteCommand(createTableSql, _connection);
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Database initialized at {Path}", _databasePath);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StoreMetricAsync(MetricDataPoint dataPoint)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();

            var sql = @"
                INSERT INTO metrics (timestamp, metric_type, metric_name, value, unit, tags)
                VALUES (@timestamp, @metricType, @metricName, @value, @unit, @tags)";

            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@timestamp", dataPoint.Timestamp.ToString("O"));
            command.Parameters.AddWithValue("@metricType", dataPoint.MetricType);
            command.Parameters.AddWithValue("@metricName", dataPoint.MetricName);
            command.Parameters.AddWithValue("@value", dataPoint.Value);
            command.Parameters.AddWithValue("@unit", dataPoint.Unit ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@tags", dataPoint.Tags ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StoreMetricsAsync(IEnumerable<MetricDataPoint> dataPoints)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();

            using var transaction = _connection!.BeginTransaction();

            var sql = @"
                INSERT INTO metrics (timestamp, metric_type, metric_name, value, unit, tags)
                VALUES (@timestamp, @metricType, @metricName, @value, @unit, @tags)";

            foreach (var dataPoint in dataPoints)
            {
                using var command = new SqliteCommand(sql, _connection);
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@timestamp", dataPoint.Timestamp.ToString("O"));
                command.Parameters.AddWithValue("@metricType", dataPoint.MetricType);
                command.Parameters.AddWithValue("@metricName", dataPoint.MetricName);
                command.Parameters.AddWithValue("@value", dataPoint.Value);
                command.Parameters.AddWithValue("@unit", dataPoint.Unit ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tags", dataPoint.Tags ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StoreSnapshotAsync(SystemMetrics metrics)
    {
        var dataPoints = new List<MetricDataPoint>
        {
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.CpuUsage, MetricName = "cpu_avg", Value = metrics.Cpu.AverageUsage, Unit = "%" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.CpuTemperature, MetricName = "cpu_max_temp", Value = metrics.Cpu.MaxTemperature, Unit = "°C" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.GpuUsage, MetricName = "gpu_usage", Value = metrics.Gpu.Usage, Unit = "%" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.GpuTemperature, MetricName = "gpu_temp", Value = metrics.Gpu.Temperature, Unit = "°C" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.RamUsage, MetricName = "ram_percent", Value = metrics.Ram.UsagePercent, Unit = "%" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.NetworkUpload, MetricName = "net_up", Value = metrics.Network.UploadSpeedMbps, Unit = "Mbps" },
            new() { Timestamp = metrics.Timestamp, MetricType = MetricTypes.NetworkDownload, MetricName = "net_down", Value = metrics.Network.DownloadSpeedMbps, Unit = "Mbps" }
        };

        if (metrics.Battery != null)
        {
            dataPoints.Add(new MetricDataPoint
            {
                Timestamp = metrics.Timestamp,
                MetricType = MetricTypes.BatteryPercent,
                MetricName = "battery",
                Value = metrics.Battery.ChargePercent,
                Unit = "%"
            });
        }

        foreach (var disk in metrics.Disks)
        {
            dataPoints.Add(new MetricDataPoint
            {
                Timestamp = metrics.Timestamp,
                MetricType = MetricTypes.DiskRead,
                MetricName = $"disk_{disk.DriveLetter}_read",
                Value = disk.ReadSpeedMBps,
                Unit = "MB/s"
            });
            dataPoints.Add(new MetricDataPoint
            {
                Timestamp = metrics.Timestamp,
                MetricType = MetricTypes.DiskWrite,
                MetricName = $"disk_{disk.DriveLetter}_write",
                Value = disk.WriteSpeedMBps,
                Unit = "MB/s"
            });
        }

        await StoreMetricsAsync(dataPoints);
    }

    public async Task<HistoryResponse> GetHistoryAsync(HistoryQueryOptions options)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();

            var endTime = options.EndTime ?? DateTime.UtcNow;
            var startTime = options.StartTime ?? endTime - (options.Period ?? TimeSpan.FromHours(1));

            var sql = @"
                SELECT timestamp, value
                FROM metrics
                WHERE metric_type = @metricType
                AND timestamp >= @startTime
                AND timestamp <= @endTime
                ORDER BY timestamp ASC";

            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@metricType", options.MetricType);
            command.Parameters.AddWithValue("@startTime", startTime.ToString("O"));
            command.Parameters.AddWithValue("@endTime", endTime.ToString("O"));

            var dataPoints = new List<HistoryDataPoint>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                dataPoints.Add(new HistoryDataPoint
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    Value = reader.GetDouble(1)
                });
            }

            // Apply aggregation if needed
            if (options.MaxPoints.HasValue && dataPoints.Count > options.MaxPoints.Value)
            {
                dataPoints = AggregateDataPoints(dataPoints, options.MaxPoints.Value, options.Aggregation);
            }

            return new HistoryResponse
            {
                MetricType = options.MetricType,
                StartTime = startTime,
                EndTime = endTime,
                DataPointCount = dataPoints.Count,
                DataPoints = dataPoints
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CleanupOldDataAsync(TimeSpan retention)
    {
        await _semaphore.WaitAsync();
        try
        {
            EnsureConnection();

            var cutoffTime = DateTime.UtcNow - retention;
            var sql = "DELETE FROM metrics WHERE timestamp < @cutoffTime";

            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@cutoffTime", cutoffTime.ToString("O"));

            var deleted = await command.ExecuteNonQueryAsync();
            if (deleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old metric records", deleted);
                
                // Vacuum to reclaim space
                using var vacuumCommand = new SqliteCommand("VACUUM", _connection);
                await vacuumCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var fileInfo = new FileInfo(_databasePath);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    private List<HistoryDataPoint> AggregateDataPoints(List<HistoryDataPoint> dataPoints, int maxPoints, AggregationType aggregation)
    {
        var bucketSize = (int)Math.Ceiling((double)dataPoints.Count / maxPoints);
        var aggregated = new List<HistoryDataPoint>();

        for (var i = 0; i < dataPoints.Count; i += bucketSize)
        {
            var bucket = dataPoints.Skip(i).Take(bucketSize).ToList();
            var point = new HistoryDataPoint
            {
                Timestamp = bucket.First().Timestamp,
                Min = bucket.Min(p => p.Value),
                Max = bucket.Max(p => p.Value)
            };

            point.Value = aggregation switch
            {
                AggregationType.Average => bucket.Average(p => p.Value),
                AggregationType.Min => point.Min.Value,
                AggregationType.Max => point.Max.Value,
                AggregationType.Sum => bucket.Sum(p => p.Value),
                _ => bucket.Average(p => p.Value)
            };

            aggregated.Add(point);
        }

        return aggregated;
    }

    private void EnsureConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("Database connection not initialized. Call InitializeAsync first.");
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _semaphore.Dispose();
    }
}
