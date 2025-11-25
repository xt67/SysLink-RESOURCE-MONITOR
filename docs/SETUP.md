# SysLink Setup Guide

This guide covers how to set up both the Windows Agent and Android App for SysLink Remote System Monitor.

---

## Prerequisites

### Windows Agent
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Administrator privileges (for hardware sensor access)
- Open firewall port 5443

### Android App
- Android 8.0 (API 26) or higher
- Network connectivity to Windows PC

---

## Windows Agent Setup

### Option 1: Pre-built Release

1. Download the latest release from [Releases](https://github.com/your-repo/releases)
2. Extract to a folder (e.g., `C:\SysLink`)
3. Run `SysLink.Agent.exe` as Administrator

### Option 2: Build from Source

#### 1. Clone the Repository
```bash
git clone https://github.com/your-repo/SysLink.git
cd SysLink/WindowsAgent
```

#### 2. Build the Solution
```bash
dotnet restore
dotnet build -c Release
```

#### 3. Run the Agent
```bash
cd SysLink.Agent/bin/Release/net8.0
.\SysLink.Agent.exe
```

### Configuration

The agent uses `appsettings.json` for configuration:

```json
{
  "SysLink": {
    "Port": 5443,
    "CollectionIntervalMs": 1000,
    "DataRetentionDays": 7,
    "RequireAuthentication": true,
    "AllowedHosts": ["192.168.*.*", "10.0.*.*"]
  }
}
```

### Firewall Setup

Allow incoming connections on port 5443:

```powershell
# PowerShell (Run as Administrator)
New-NetFirewallRule -DisplayName "SysLink Agent" -Direction Inbound -LocalPort 5443 -Protocol TCP -Action Allow
```

### Running as Windows Service

To install as a Windows service:

```powershell
# Create the service
sc.exe create SysLinkAgent binPath= "C:\SysLink\SysLink.Agent.exe" start= auto
sc.exe description SysLinkAgent "SysLink Remote System Monitor Agent"

# Start the service
sc.exe start SysLinkAgent
```

To remove the service:
```powershell
sc.exe stop SysLinkAgent
sc.exe delete SysLinkAgent
```

---

## Android App Setup

### Option 1: Pre-built APK

1. Download the latest APK from [Releases](https://github.com/your-repo/releases)
2. Enable "Install from unknown sources" on your device
3. Install the APK

### Option 2: Build from Source

#### 1. Requirements
- Android Studio Hedgehog (2023.1.1) or newer
- JDK 17
- Android SDK 34

#### 2. Clone and Open
```bash
git clone https://github.com/your-repo/SysLink.git
```

Open `SysLink/AndroidApp` in Android Studio.

#### 3. Build
- Click **Build > Build Bundle(s) / APK(s) > Build APK(s)**
- Or use Gradle:
```bash
cd AndroidApp
./gradlew assembleDebug
```

The APK will be at `app/build/outputs/apk/debug/app-debug.apk`

---

## Connecting Android to Windows Agent

### Step 1: Find Your PC's IP Address

On Windows, run:
```powershell
ipconfig
```

Look for `IPv4 Address` under your network adapter (e.g., `192.168.1.100`).

### Step 2: Add Server in Android App

1. Open SysLink app
2. Go to **Settings** tab
3. Tap **Add Server**
4. Enter:
   - **Name**: My PC
   - **IP Address**: 192.168.1.100
   - **Port**: 5443

### Step 3: Pair Device

1. Tap **Connect** on the server
2. A pairing prompt will appear on your Windows PC
3. Click **Allow** to approve the connection
4. The app will receive an authentication token

### Step 4: Verify Connection

- Switch to **Simple** or **Detailed** view
- You should see real-time metrics from your PC
- A green connection indicator shows successful connection

---

## Troubleshooting

### "Connection Refused" Error

1. **Check firewall**: Ensure port 5443 is open
2. **Check agent is running**: Look for `SysLink.Agent.exe` in Task Manager
3. **Verify IP address**: Make sure you're using the correct local IP

### "Certificate Error" on Android

The agent uses a self-signed certificate. For development:
- The app includes a trust manager that accepts self-signed certs
- For production, deploy a proper CA-signed certificate

### "No Sensor Data" on Windows

1. **Run as Administrator**: Hardware sensors require admin access
2. **Check LibreHardwareMonitor**: Some systems need driver installation
3. **Check antivirus**: Some AV software blocks hardware monitoring

### Android App Crashes

1. **Check logs**: Use `adb logcat` to view crash logs
2. **Clear app data**: Settings > Apps > SysLink > Clear Data
3. **Reinstall**: Uninstall and reinstall the app

### High CPU Usage on Agent

1. **Increase collection interval**: Change `CollectionIntervalMs` to 2000 or higher
2. **Reduce data retention**: Lower `DataRetentionDays` to reduce database size
3. **Disable unused sensors**: Remove sensors from `EnabledSensors` list

---

## Network Security Recommendations

### For Home Use
- Use WPA3 encryption on your WiFi
- Keep the agent behind your router's NAT
- Use strong pairing approval (don't auto-approve)

### For Remote Access (Advanced)
- Set up a VPN (WireGuard, Tailscale)
- Use port forwarding with caution
- Consider deploying a proper SSL certificate

### For Enterprise
- Deploy CA-signed certificates
- Use network segmentation
- Implement centralized logging
- Regular security audits

---

## Updating

### Windows Agent
1. Stop the agent/service
2. Replace executable files with new version
3. Restart the agent/service

### Android App
1. Download new APK or update from store
2. Install over existing app (data preserved)

---

## Uninstalling

### Windows Agent
```powershell
# If running as service
sc.exe stop SysLinkAgent
sc.exe delete SysLinkAgent

# Remove files
Remove-Item -Recurse "C:\SysLink"

# Remove app data
Remove-Item -Recurse "$env:APPDATA\SysLink"
```

### Android App
- Settings > Apps > SysLink > Uninstall
- Or long-press the app icon and select Uninstall
