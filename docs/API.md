# SysLink API Documentation

## Overview

SysLink Windows Agent provides a RESTful API and WebSocket interface for real-time system monitoring. The API uses HTTPS with self-signed certificates and token-based authentication.

**Base URL**: `https://<ip>:5443/api`  
**WebSocket**: `wss://<ip>:5443/ws/stream`

---

## Authentication

### Device Pairing

Before accessing the API, devices must be paired with the Windows Agent.

#### POST `/api/auth/pair`

Initiates a pairing request. A prompt will appear on the Windows Agent for user approval.

**Request Body:**
```json
{
  "deviceName": "My Android Phone",
  "deviceId": "unique-device-uuid",
  "deviceType": "Android",
  "publicKey": "base64-encoded-public-key"
}
```

**Response (Success):**
```json
{
  "success": true,
  "token": "jwt-access-token",
  "expiresAt": "2025-01-01T00:00:00Z",
  "serverName": "DESKTOP-PC",
  "serverId": "server-uuid"
}
```

**Response (Denied):**
```json
{
  "success": false,
  "error": "Pairing request was denied by user"
}
```

#### POST `/api/auth/validate`

Validates an existing token.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "valid": true,
  "deviceName": "My Android Phone",
  "expiresAt": "2025-01-01T00:00:00Z"
}
```

#### POST `/api/auth/revoke`

Revokes a device's access.

**Request Body:**
```json
{
  "deviceId": "device-uuid-to-revoke"
}
```

---

## System Status

### GET `/api/status`

Returns current system metrics.

**Headers:**
```
Authorization: Bearer <token>
```

**Response:**
```json
{
  "timestamp": "2025-01-01T12:00:00Z",
  "systemInfo": {
    "deviceName": "DESKTOP-PC",
    "operatingSystem": "Windows 11 Pro",
    "osVersion": "10.0.22631",
    "cpuName": "AMD Ryzen 9 5900X",
    "cpuCores": 12,
    "cpuThreads": 24,
    "gpuName": "NVIDIA GeForce RTX 3080",
    "totalRamGB": 32.0,
    "motherboard": "ASUS ROG CROSSHAIR VIII",
    "bootTime": "2025-01-01T08:00:00Z",
    "agentVersion": "1.0.0",
    "hasBattery": false
  },
  "metrics": {
    "cpu": {
      "totalUsage": 25.5,
      "perCoreUsage": [30.2, 20.1, 25.5, 28.3],
      "temperature": 55.0,
      "clockSpeedMHz": 4200,
      "powerWatts": 85.5
    },
    "gpu": {
      "usage": 45.0,
      "temperature": 65.0,
      "vramUsedMB": 4096,
      "vramTotalMB": 10240,
      "vramUsagePercent": 40.0,
      "clockSpeedMHz": 1800,
      "powerWatts": 180.5,
      "fanSpeedPercent": 55.0
    },
    "ram": {
      "usedGB": 16.5,
      "totalGB": 32.0,
      "usagePercent": 51.6,
      "speedMHz": 3600
    },
    "disks": [
      {
        "name": "C:",
        "label": "Windows",
        "totalGB": 500.0,
        "usedGB": 250.0,
        "freeGB": 250.0,
        "usagePercent": 50.0,
        "readMBps": 150.5,
        "writeMBps": 75.2,
        "temperature": 35.0,
        "health": "Good"
      }
    ],
    "network": {
      "uploadSpeedMbps": 25.5,
      "downloadSpeedMbps": 150.2,
      "totalUploadedGB": 10.5,
      "totalDownloadedGB": 50.2,
      "activeInterface": "Ethernet",
      "localIPAddress": "192.168.1.100",
      "macAddress": "00:11:22:33:44:55"
    },
    "battery": {
      "chargePercent": 85.0,
      "isCharging": true,
      "estimatedMinutesRemaining": 180,
      "health": "Good",
      "cycleCount": 150,
      "powerDrawWatts": 45.0
    },
    "fans": [
      {
        "name": "CPU Fan",
        "rpm": 1200,
        "speedPercent": 45.0
      }
    ]
  }
}
```

### GET `/api/status/info`

Returns static system information only (no metrics).

**Response:**
```json
{
  "deviceName": "DESKTOP-PC",
  "operatingSystem": "Windows 11 Pro",
  ...
}
```

---

## Processes

### GET `/api/processes`

Returns list of running processes with resource usage.

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sortBy` | string | `CpuUsage` | Sort field: `CpuUsage`, `MemoryUsage`, `Name`, `Pid` |
| `sortDesc` | bool | `true` | Sort descending |
| `top` | int | `50` | Number of processes to return |
| `search` | string | - | Filter by process name |

**Example:** `GET /api/processes?sortBy=CpuUsage&sortDesc=true&top=20&search=chrome`

**Response:**
```json
{
  "timestamp": "2025-01-01T12:00:00Z",
  "totalProcessCount": 150,
  "processes": [
    {
      "pid": 1234,
      "name": "chrome",
      "cpuUsage": 15.5,
      "memoryUsageMB": 512.0,
      "memoryUsagePercent": 1.6,
      "diskReadMBps": 5.2,
      "diskWriteMBps": 1.1,
      "networkUsageMbps": 10.5,
      "status": "Running",
      "startTime": "2025-01-01T08:30:00Z",
      "windowTitle": "Google Chrome",
      "filePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
    }
  ],
  "filterApplied": "chrome",
  "sortBy": "CpuUsage"
}
```

### POST `/api/processes/{pid}/kill`

Terminates a process by PID.

**Response (Success):**
```json
{
  "success": true,
  "message": "Process 1234 terminated"
}
```

---

## Historical Data

### GET `/api/history/{metricType}`

Returns historical data for a specific metric.

**Path Parameters:**
| Parameter | Values |
|-----------|--------|
| `metricType` | `cpu_usage`, `cpu_temp`, `gpu_usage`, `gpu_temp`, `ram_usage`, `net_upload`, `net_download`, `battery_percent` |

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `period` | string | `1h` | Time period: `1h`, `6h`, `24h`, `7d`, `30d` |
| `resolution` | string | `auto` | Data resolution: `raw`, `1m`, `5m`, `15m`, `1h` |

**Example:** `GET /api/history/cpu_usage?period=6h&resolution=5m`

**Response:**
```json
{
  "metricType": "cpu_usage",
  "startTime": "2025-01-01T06:00:00Z",
  "endTime": "2025-01-01T12:00:00Z",
  "dataPointCount": 72,
  "dataPoints": [
    {
      "timestamp": "2025-01-01T06:00:00Z",
      "value": 25.5,
      "min": 20.0,
      "max": 35.0
    }
  ]
}
```

---

## Configuration

### GET `/api/config`

Returns current agent configuration.

**Response:**
```json
{
  "collectionIntervalMs": 1000,
  "dataRetentionDays": 7,
  "enabledSensors": ["cpu", "gpu", "ram", "disk", "network", "battery", "fan"],
  "alertThresholds": {
    "cpuTempWarning": 80,
    "cpuTempCritical": 95,
    "gpuTempWarning": 80,
    "gpuTempCritical": 95,
    "ramUsageWarning": 85,
    "diskUsageWarning": 90,
    "batteryLowWarning": 20
  }
}
```

### PUT `/api/config`

Updates agent configuration.

**Request Body:**
```json
{
  "collectionIntervalMs": 2000,
  "alertThresholds": {
    "cpuTempWarning": 85
  }
}
```

---

## WebSocket Real-time Streaming

### Connect to WebSocket

**URL:** `wss://<ip>:5443/ws/stream?token=<jwt-token>`

### Message Format

**Outgoing (Server → Client):**
```json
{
  "type": "metrics",
  "timestamp": "2025-01-01T12:00:00Z",
  "data": {
    "cpu": { ... },
    "gpu": { ... },
    "ram": { ... },
    "network": { ... }
  }
}
```

**Alert Message:**
```json
{
  "type": "alert",
  "timestamp": "2025-01-01T12:00:00Z",
  "alert": {
    "level": "Warning",
    "source": "CPU Temperature",
    "message": "CPU temperature is 85°C",
    "value": 85.0,
    "threshold": 80.0
  }
}
```

### Subscribing to Updates

**Incoming (Client → Server):**
```json
{
  "action": "subscribe",
  "metrics": ["cpu", "gpu", "ram", "network"]
}
```

---

## Error Responses

All errors follow a consistent format:

```json
{
  "error": "Error description",
  "code": "ERROR_CODE",
  "details": "Additional information"
}
```

**Common HTTP Status Codes:**
| Code | Description |
|------|-------------|
| 200 | Success |
| 400 | Bad Request - Invalid parameters |
| 401 | Unauthorized - Missing or invalid token |
| 403 | Forbidden - Token valid but access denied |
| 404 | Not Found - Resource doesn't exist |
| 500 | Internal Server Error |

---

## Rate Limiting

- REST API: 100 requests per minute per client
- WebSocket: Metrics broadcast every 1 second (configurable)

---

## SSL/TLS

The agent uses a self-signed certificate. Android apps must:
1. Trust the certificate manually, OR
2. Use a custom TrustManager (development only), OR
3. Deploy a proper CA-signed certificate (production)

The certificate is generated on first run and stored in `%APPDATA%/SysLink/`.
