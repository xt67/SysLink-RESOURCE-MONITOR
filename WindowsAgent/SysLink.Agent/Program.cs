using SysLink.Api.Services;
using SysLink.Api.WebSocket;
using SysLink.Core.Interfaces;
using SysLink.Data;
using SysLink.Hardware;

var builder = WebApplication.CreateBuilder(args);

// Configure Windows Service
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "SysLink Agent";
});

// Configure Kestrel for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5443, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register core services
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<IHardwareMonitor, HardwareMonitorService>();
builder.Services.AddSingleton<IProcessMonitor, ProcessMonitorService>();
builder.Services.AddSingleton<IMetricsStorage>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<SqliteMetricsStorage>>();
    var config = sp.GetRequiredService<IConfigService>().GetConfig();
    return new SqliteMetricsStorage(logger, config.Storage.DatabasePath);
});
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAlertService, AlertService>();
builder.Services.AddSingleton<MetricsWebSocketHandler>();

// Register background services
builder.Services.AddHostedService<MetricsCollectionService>();
builder.Services.AddHostedService<DataCleanupService>();

// Configure CORS for local network access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Load configuration
var configService = app.Services.GetRequiredService<IConfigService>();
await configService.LoadAsync();

// Configure middleware
app.UseCors();

// WebSocket endpoint
app.UseWebSockets();
app.Map("/ws/stream", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<MetricsWebSocketHandler>();
        var clientId = Guid.NewGuid().ToString();
        await handler.HandleConnectionAsync(webSocket, clientId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Authentication middleware
app.Use(async (context, next) =>
{
    // Skip auth for certain endpoints
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var skipAuth = path.Contains("/api/auth/pair") || 
                   path.Contains("/api/auth/pairing-code") ||
                   path.Contains("/ws/stream");

    if (skipAuth)
    {
        await next();
        return;
    }

    var authService = context.RequestServices.GetRequiredService<IAuthService>();
    var config = context.RequestServices.GetRequiredService<IConfigService>().GetConfig();

    if (!config.Security.RequireAuthentication)
    {
        await next();
        return;
    }

    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    
    if (string.IsNullOrEmpty(token) || !await authService.ValidateTokenAsync(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║                    SysLink Agent                          ║
║               Remote System Monitor v1.0                  ║
╠═══════════════════════════════════════════════════════════╣
║  REST API:    https://localhost:5443/api/status           ║
║  WebSocket:   wss://localhost:5443/ws/stream              ║
║  Health:      https://localhost:5443/health               ║
╚═══════════════════════════════════════════════════════════╝
");

app.Run();
