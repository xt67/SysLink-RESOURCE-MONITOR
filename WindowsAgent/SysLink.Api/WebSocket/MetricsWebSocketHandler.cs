using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SysLink.Core.Interfaces;
using SysLink.Core.Models;

namespace SysLink.Api.WebSocket;

/// <summary>
/// WebSocket handler for real-time metrics streaming.
/// </summary>
public class MetricsWebSocketHandler
{
    private readonly IHardwareMonitor _hardwareMonitor;
    private readonly IProcessMonitor _processMonitor;
    private readonly IConfigService _configService;
    private readonly ILogger<MetricsWebSocketHandler> _logger;
    private readonly List<WebSocketConnection> _connections = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _broadcastCts;
    private Task? _broadcastTask;

    public MetricsWebSocketHandler(
        IHardwareMonitor hardwareMonitor,
        IProcessMonitor processMonitor,
        IConfigService configService,
        ILogger<MetricsWebSocketHandler> logger)
    {
        _hardwareMonitor = hardwareMonitor;
        _processMonitor = processMonitor;
        _configService = configService;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket webSocket, string clientId)
    {
        var connection = new WebSocketConnection
        {
            Id = clientId,
            Socket = webSocket,
            ConnectedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _connections.Add(connection);
            _logger.LogInformation("WebSocket client connected: {ClientId}. Total connections: {Count}", 
                clientId, _connections.Count);

            // Start broadcast if this is the first connection
            if (_connections.Count == 1)
            {
                StartBroadcast();
            }
        }

        try
        {
            await ReceiveMessagesAsync(connection);
        }
        finally
        {
            lock (_lock)
            {
                _connections.Remove(connection);
                _logger.LogInformation("WebSocket client disconnected: {ClientId}. Total connections: {Count}", 
                    clientId, _connections.Count);

                // Stop broadcast if no more connections
                if (_connections.Count == 0)
                {
                    StopBroadcast();
                }
            }
        }
    }

    private void StartBroadcast()
    {
        _broadcastCts = new CancellationTokenSource();
        _broadcastTask = Task.Run(async () => await BroadcastLoopAsync(_broadcastCts.Token));
    }

    private void StopBroadcast()
    {
        _broadcastCts?.Cancel();
        _broadcastTask = null;
    }

    private async Task BroadcastLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var config = _configService.GetConfig();
                var interval = config.Monitoring.UpdateIntervalMs;

                var metrics = await _hardwareMonitor.GetMetricsAsync();
                var payload = new WebSocketPayload
                {
                    Type = "metrics",
                    Timestamp = DateTime.UtcNow,
                    Data = metrics
                };

                await BroadcastMessageAsync(payload);

                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in broadcast loop");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task BroadcastMessageAsync(WebSocketPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);

        List<WebSocketConnection> connectionsSnapshot;
        lock (_lock)
        {
            connectionsSnapshot = _connections.ToList();
        }

        var tasks = connectionsSnapshot
            .Where(c => c.Socket.State == WebSocketState.Open)
            .Select(c => SendMessageAsync(c, bytes));

        await Task.WhenAll(tasks);
    }

    private async Task SendMessageAsync(WebSocketConnection connection, byte[] data)
    {
        try
        {
            await connection.Socket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending to client {ClientId}", connection.Id);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocketConnection connection)
    {
        var buffer = new byte[4096];

        while (connection.Socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await connection.Socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.Socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleClientMessageAsync(connection, message);
                }
            }
            catch (WebSocketException)
            {
                break;
            }
        }
    }

    private async Task HandleClientMessageAsync(WebSocketConnection connection, string message)
    {
        try
        {
            var request = JsonSerializer.Deserialize<WebSocketRequest>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null) return;

            switch (request.Type?.ToLower())
            {
                case "subscribe":
                    connection.Subscriptions.Add(request.Channel ?? "metrics");
                    break;

                case "unsubscribe":
                    connection.Subscriptions.Remove(request.Channel ?? "metrics");
                    break;

                case "ping":
                    var pong = new WebSocketPayload { Type = "pong", Timestamp = DateTime.UtcNow };
                    var json = JsonSerializer.Serialize(pong);
                    await connection.Socket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                    break;

                case "get_processes":
                    var processes = await _processMonitor.GetTopByCpuAsync(10);
                    var processPayload = new WebSocketPayload
                    {
                        Type = "processes",
                        Timestamp = DateTime.UtcNow,
                        Data = processes
                    };
                    var processJson = JsonSerializer.Serialize(processPayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    await connection.Socket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(processJson)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling client message");
        }
    }

    public int GetConnectionCount()
    {
        lock (_lock)
        {
            return _connections.Count;
        }
    }
}

public class WebSocketConnection
{
    public string Id { get; set; } = string.Empty;
    public System.Net.WebSockets.WebSocket Socket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public HashSet<string> Subscriptions { get; set; } = new() { "metrics" };
}

public class WebSocketPayload
{
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

public class WebSocketRequest
{
    public string? Type { get; set; }
    public string? Channel { get; set; }
    public object? Data { get; set; }
}
