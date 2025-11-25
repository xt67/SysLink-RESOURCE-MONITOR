# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure
- Windows Agent with hardware monitoring
- Android App with Jetpack Compose UI
- REST API endpoints
- WebSocket real-time streaming
- SQLite data storage
- Token-based authentication
- Dark mode only theme
- Fullscreen immersive mode

## [1.0.0] - 2025-11-25

### Added

#### Windows Agent
- **Hardware Monitoring**
  - CPU: Usage, temperature, power, per-core metrics
  - GPU: Usage, temperature, VRAM, clock speeds
  - RAM: Usage, total/available memory
  - Storage: Disk usage, read/write speeds, temperatures
  - Network: Upload/download speeds, adapter info
  - Battery: Charge level, status, health (laptops)
  - Fans: RPM, speed percentages

- **Process Monitoring**
  - Full process list with CPU/RAM usage
  - Sortable by various criteria
  - Search/filter functionality
  - Top processes by resource usage

- **API Endpoints**
  - `GET /api/status` - Full system metrics
  - `GET /api/minimal` - Lightweight metrics
  - `GET /api/info` - System information
  - `GET /api/processes` - Process list
  - `GET /api/history/{metric}` - Historical data
  - `GET/PUT /api/config` - Configuration
  - `POST /api/auth/pair` - Device pairing

- **WebSocket Streaming**
  - Real-time metrics at `wss://host:5443/ws/stream`
  - Configurable update interval
  - Auto-reconnection support

- **Data Storage**
  - SQLite for historical metrics
  - Configurable retention period
  - Automatic cleanup

- **Security**
  - HTTPS with self-signed certificates
  - Token-based authentication
  - Device pairing workflow

#### Android App
- **Simple View**
  - Circular gauges for CPU, GPU, RAM
  - Temperature summary
  - Network stats
  - Connection status

- **Detailed View**
  - Expandable cards for all hardware
  - Per-core CPU metrics
  - Disk information
  - Battery details
  - Fan speeds

- **Graphs View**
  - Time-series charts (1h, 6h, 24h)
  - CPU, GPU, RAM, Network history
  - Min/Avg/Max statistics

- **Processes View**
  - Sortable process list
  - Search functionality
  - CPU/RAM usage display

- **Settings View**
  - Server management
  - Add/remove servers
  - Alert thresholds
  - Display preferences

- **Features**
  - Dark mode only design
  - Fullscreen immersive mode
  - Material 3 theming
  - Smooth animations
  - Error handling with user feedback

### Technical Details
- Windows Agent: .NET 8.0, ASP.NET Core, LibreHardwareMonitor
- Android App: Kotlin 1.9, Jetpack Compose, Material 3, Hilt, Retrofit
- Communication: REST API + WebSocket over HTTPS
- Storage: SQLite on both platforms

---

## Release History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0 | 2025-11-25 | Initial release |

---

[Unreleased]: https://github.com/your-username/SysLink/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/your-username/SysLink/releases/tag/v1.0.0
