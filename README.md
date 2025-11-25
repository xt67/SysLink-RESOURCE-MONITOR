# SysLink - Remote System Monitor

<div align="center">

![SysLink Logo](docs/images/logo.png)

**Monitor your Windows PC from anywhere on your network using your Android device**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Android](https://img.shields.io/badge/Android-8.0+-3DDC84?style=flat-square&logo=android&logoColor=white)](https://developer.android.com/)
[![Kotlin](https://img.shields.io/badge/Kotlin-1.9+-7F52FF?style=flat-square&logo=kotlin&logoColor=white)](https://kotlinlang.org/)
[![Jetpack Compose](https://img.shields.io/badge/Jetpack%20Compose-Material3-4285F4?style=flat-square&logo=jetpackcompose&logoColor=white)](https://developer.android.com/jetpack/compose)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

</div>

---

A comprehensive two-part system for monitoring Windows PC hardware and system performance from Android devices in real-time.

## 🌟 Features

### Windows Agent (C# / .NET 8)
| Feature | Description |
|---------|-------------|
| 🖥️ **Hardware Monitoring** | CPU, GPU, RAM, Storage, Network, Battery, Fans |
| 📊 **Process Monitoring** | Full process list with CPU/Memory/Disk usage |
| ⚡ **Real-time Streaming** | WebSocket-based live updates (1s interval) |
| 🔌 **REST API** | Comprehensive endpoints for all metrics |
| 💾 **Historical Data** | SQLite storage with configurable retention |
| 🔒 **Security** | HTTPS, token-based auth, device pairing |

### Android App (Kotlin / Jetpack Compose)
| Feature | Description |
|---------|-------------|
| 📱 **Simple View** | Quick glance with circular gauges |
| 📋 **Detailed View** | Full hardware dashboard with expandable cards |
| 📈 **Graph View** | Historical charts (1h, 6h, 24h) |
| 🔄 **Process View** | Sortable, searchable process list |
| 🖥️ **Multi-PC Support** | Monitor multiple computers |
| 🔔 **Alerts** | Customizable threshold notifications |

## 📸 Screenshots

<div align="center">
<table>
<tr>
<td><img src="docs/images/simple-view.png" width="200" alt="Simple View"/></td>
<td><img src="docs/images/detailed-view.png" width="200" alt="Detailed View"/></td>
<td><img src="docs/images/graphs-view.png" width="200" alt="Graphs View"/></td>
<td><img src="docs/images/processes-view.png" width="200" alt="Processes View"/></td>
</tr>
<tr>
<td align="center">Simple View</td>
<td align="center">Detailed View</td>
<td align="center">Graphs View</td>
<td align="center">Processes View</td>
</tr>
</table>
</div>

## 📋 Requirements

### Windows Agent
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Administrator privileges (for hardware access)
- Open firewall port 5443

### Android App
- Android 8.0+ (API 26+)
- Network connectivity to Windows PC

## 🚀 Quick Start

### Windows Agent

```powershell
# Clone the repository
git clone https://github.com/your-username/SysLink.git
cd SysLink/WindowsAgent

# Restore dependencies and build
dotnet restore
dotnet build -c Release

# Run the agent (as Administrator)
dotnet run --project SysLink.Agent
```

The agent will start on `https://localhost:5443` by default.

### Android App

1. Open `AndroidApp` folder in Android Studio
2. Sync Gradle files
3. Build and run on device/emulator
4. Go to Settings → Add Server → Enter your PC's IP address

## 📁 Project Structure

```
SysLink-RESOURCE MONITOR/
├── WindowsAgent/                 # .NET 8 Windows Service
│   ├── SysLink.Agent/           # Entry point, configuration
│   ├── SysLink.Core/            # Models & interfaces
│   │   ├── Models/              # SystemMetrics, ProcessInfo, etc.
│   │   └── Interfaces/          # IHardwareMonitor, IMetricsStorage
│   ├── SysLink.Hardware/        # LibreHardwareMonitor integration
│   ├── SysLink.Api/             # REST API & WebSocket
│   │   ├── Controllers/         # StatusController, ProcessesController
│   │   ├── Services/            # AuthService, AlertService
│   │   └── WebSocket/           # MetricsWebSocketHandler
│   └── SysLink.Data/            # SQLite storage
│
├── AndroidApp/                   # Kotlin + Jetpack Compose
│   └── app/src/main/java/com/syslink/monitor/
│       ├── data/                # Models, API, Repository
│       ├── di/                  # Hilt DI modules
│       └── ui/                  # Compose screens
│           ├── screens/         # Simple, Detailed, Graphs, Processes, Settings
│           ├── navigation/      # Bottom nav setup
│           └── theme/           # Material 3 theming
│
└── docs/                        # Documentation
    ├── API.md                   # API reference
    ├── SETUP.md                 # Detailed setup guide
    └── images/                  # Screenshots
```

## 🔌 API Reference

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/status` | GET | Full system metrics snapshot |
| `/api/status/info` | GET | Static system information |
| `/api/processes` | GET | Process list (sortable, searchable) |
| `/api/history/{metric}` | GET | Historical data points |
| `/api/config` | GET/PUT | Agent configuration |
| `/api/auth/pair` | POST | Device pairing |
| `/api/auth/validate` | POST | Token validation |
| `/ws/stream` | WebSocket | Real-time metrics stream |

📚 Full API documentation: [docs/API.md](docs/API.md)

## 🔒 Security

- 🔐 **HTTPS/TLS**: All connections encrypted
- 🎫 **Token Auth**: JWT-based authentication
- 👆 **Device Pairing**: Manual approval required
- 🏠 **Local Network**: Default restriction to LAN
- 🔑 **Optional mTLS**: Certificate pinning support

## ⚙️ Configuration

### Windows Agent (`appsettings.json`)
```json
{
  "SysLink": {
    "Port": 5443,
    "CollectionIntervalMs": 1000,
    "DataRetentionDays": 7,
    "RequireAuthentication": true
  }
}
```

### Android App (Settings Screen)
- Server IP/Port configuration
- Refresh interval (500ms - 5000ms)
- Alert thresholds (CPU/GPU temp, battery)
- Dark/Light theme

## 🛠️ Tech Stack

### Windows Agent
- **.NET 8** - Runtime platform
- **ASP.NET Core** - Web API framework
- **LibreHardwareMonitor** - Hardware sensor access
- **Microsoft.Data.Sqlite** - Local storage
- **System.Text.Json** - JSON serialization

### Android App
- **Kotlin 1.9** - Programming language
- **Jetpack Compose** - Modern UI toolkit
- **Material 3** - Design system
- **Hilt** - Dependency injection
- **Retrofit + OkHttp** - Networking
- **Vico** - Chart library
- **Kotlin Coroutines** - Async programming

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details on:
- Code of conduct
- Development setup
- Submitting pull requests

## 📞 Support

- 📖 [Documentation](docs/)
- 🐛 [Issue Tracker](https://github.com/your-username/SysLink/issues)
- 💬 [Discussions](https://github.com/your-username/SysLink/discussions)

---

<div align="center">
Made with ❤️ for PC enthusiasts and power users
</div>
