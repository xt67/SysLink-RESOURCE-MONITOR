package com.syslink.monitor.ui.screens.detailed

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.syslink.monitor.data.model.*
import com.syslink.monitor.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DetailedScreen(
    viewModel: DetailedViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        contentPadding = PaddingValues(vertical = 16.dp)
    ) {
        // System Info Header
        item {
            SystemInfoCard(systemInfo = uiState.systemInfo)
        }
        
        // CPU Section
        item {
            CpuDetailCard(cpu = uiState.metrics.cpu)
        }
        
        // GPU Section
        item {
            GpuDetailCard(gpu = uiState.metrics.gpu)
        }
        
        // RAM Section
        item {
            RamDetailCard(ram = uiState.metrics.ram)
        }
        
        // Disk Section
        item {
            DisksDetailCard(disks = uiState.metrics.disks)
        }
        
        // Network Section
        item {
            NetworkDetailCard(network = uiState.metrics.network)
        }
        
        // Battery Section (if present)
        uiState.metrics.battery?.let { battery ->
            item {
                BatteryDetailCard(battery = battery)
            }
        }
        
        // Fans Section
        if (uiState.metrics.fans.isNotEmpty()) {
            item {
                FansDetailCard(fans = uiState.metrics.fans)
            }
        }
        
        // Top Processes Preview
        item {
            TopProcessesCard(processes = uiState.topProcesses)
        }
    }
}

@Composable
fun SystemInfoCard(systemInfo: SystemInfo) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Icon(Icons.Default.Computer, contentDescription = null, tint = AccentCyan)
                Text(
                    text = systemInfo.deviceName.ifEmpty { "Windows PC" },
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            InfoRow("OS", "${systemInfo.operatingSystem} ${systemInfo.osVersion}")
            InfoRow("CPU", systemInfo.cpuName)
            InfoRow("GPU", systemInfo.gpuName)
            InfoRow("RAM", "%.1f GB".format(systemInfo.totalRamGB))
        }
    }
}

@Composable
fun CpuDetailCard(cpu: CpuMetrics) {
    ExpandableDetailCard(
        title = "CPU",
        icon = Icons.Default.Memory,
        color = ChartCpu,
        summary = "%.1f%% @ %.0f°C".format(cpu.averageUsage, cpu.maxTemperature)
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(cpu.name, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Bold)
            
            MetricBar("Average Usage", cpu.averageUsage, 100.0, ChartCpu)
            MetricBar("Max Temperature", cpu.maxTemperature, 100.0, getTemperatureColor(cpu.maxTemperature))
            
            if (cpu.totalPower > 0) {
                InfoRow("Power", "%.1f W".format(cpu.totalPower))
            }
            
            if (cpu.cores.isNotEmpty()) {
                Spacer(modifier = Modifier.height(8.dp))
                Text("Per-Core Usage", style = MaterialTheme.typography.labelMedium)
                
                cpu.cores.chunked(2).forEach { row ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        row.forEach { core ->
                            Column(modifier = Modifier.weight(1f)) {
                                Text("Core ${core.coreId}", style = MaterialTheme.typography.bodySmall)
                                LinearProgressIndicator(
                                    progress = { (core.usage / 100).toFloat().coerceIn(0f, 1f) },
                                    modifier = Modifier.fillMaxWidth(),
                                    color = ChartCpu
                                )
                                Text(
                                    "%.0f%% | %.0f°C".format(core.usage, core.temperature),
                                    style = MaterialTheme.typography.labelSmall
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun GpuDetailCard(gpu: GpuMetrics) {
    ExpandableDetailCard(
        title = "GPU",
        icon = Icons.Default.Videocam,
        color = ChartGpu,
        summary = "%.1f%% @ %.0f°C".format(gpu.usage, gpu.temperature)
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(gpu.name, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Bold)
            
            MetricBar("Usage", gpu.usage, 100.0, ChartGpu)
            MetricBar("Temperature", gpu.temperature, 100.0, getTemperatureColor(gpu.temperature))
            MetricBar("VRAM", gpu.vramUsagePercent, 100.0, AccentPurple)
            
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                InfoRow("VRAM Used", "%.1f / %.1f GB".format(gpu.vramUsed / 1024, gpu.vramTotal / 1024))
            }
            
            if (gpu.coreClock > 0) {
                InfoRow("Core Clock", "%.0f MHz".format(gpu.coreClock))
            }
            if (gpu.memoryClock > 0) {
                InfoRow("Memory Clock", "%.0f MHz".format(gpu.memoryClock))
            }
            if (gpu.power > 0) {
                InfoRow("Power", "%.1f W".format(gpu.power))
            }
            if (gpu.fanSpeed > 0) {
                InfoRow("Fan Speed", "%.0f RPM".format(gpu.fanSpeed))
            }
        }
    }
}

@Composable
fun RamDetailCard(ram: RamMetrics) {
    ExpandableDetailCard(
        title = "RAM",
        icon = Icons.Default.Storage,
        color = ChartRam,
        summary = "%.1f%% (%.1f / %.1f GB)".format(ram.usagePercent, ram.usedGB, ram.totalGB)
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            MetricBar("Memory Usage", ram.usagePercent, 100.0, ChartRam)
            
            InfoRow("Used", "%.2f GB".format(ram.usedGB))
            InfoRow("Free", "%.2f GB".format(ram.freeGB))
            InfoRow("Total", "%.2f GB".format(ram.totalGB))
            
            if (ram.swapTotalGB > 0) {
                Spacer(modifier = Modifier.height(8.dp))
                Text("Swap/Page File", style = MaterialTheme.typography.labelMedium)
                MetricBar("Swap Usage", ram.swapUsagePercent, 100.0, AccentPurple)
                InfoRow("Swap", "%.2f / %.2f GB".format(ram.swapUsedGB, ram.swapTotalGB))
            }
        }
    }
}

@Composable
fun DisksDetailCard(disks: List<DiskMetrics>) {
    ExpandableDetailCard(
        title = "Storage",
        icon = Icons.Default.Dns,
        color = ChartDisk,
        summary = "${disks.size} drive(s)"
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
            disks.forEach { disk ->
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
                    )
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(
                            "${disk.driveLetter} ${disk.name}",
                            style = MaterialTheme.typography.bodyMedium,
                            fontWeight = FontWeight.Bold
                        )
                        
                        Spacer(modifier = Modifier.height(4.dp))
                        MetricBar("Usage", disk.usagePercent, 100.0, ChartDisk)
                        
                        InfoRow("Used", "%.1f / %.1f GB".format(disk.usedGB, disk.totalGB))
                        
                        if (disk.readSpeedMBps > 0 || disk.writeSpeedMBps > 0) {
                            InfoRow("Read", "%.1f MB/s".format(disk.readSpeedMBps))
                            InfoRow("Write", "%.1f MB/s".format(disk.writeSpeedMBps))
                        }
                        
                        if (disk.temperature > 0) {
                            InfoRow("Temperature", "%.0f°C".format(disk.temperature))
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun NetworkDetailCard(network: NetworkMetrics) {
    ExpandableDetailCard(
        title = "Network",
        icon = Icons.Default.Wifi,
        color = ChartNetwork,
        summary = "${network.connectionType} - ${if (network.isConnected) "Connected" else "Disconnected"}"
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            InfoRow("Adapter", network.adapterName)
            InfoRow("Type", network.connectionType)
            
            Spacer(modifier = Modifier.height(8.dp))
            
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(Icons.Default.ArrowUpward, contentDescription = null, tint = StatusGreen)
                    Text("Upload", style = MaterialTheme.typography.labelSmall)
                    Text(
                        "%.2f Mbps".format(network.uploadSpeedMbps),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                }
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(Icons.Default.ArrowDownward, contentDescription = null, tint = ChartNetwork)
                    Text("Download", style = MaterialTheme.typography.labelSmall)
                    Text(
                        "%.2f Mbps".format(network.downloadSpeedMbps),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}

@Composable
fun BatteryDetailCard(battery: BatteryMetrics) {
    val statusColor = when (battery.status) {
        "Charging" -> StatusGreen
        "Discharging" -> if (battery.chargePercent < 20) StatusRed else StatusYellow
        "Full" -> StatusGreen
        else -> MaterialTheme.colorScheme.onSurface
    }
    
    ExpandableDetailCard(
        title = "Battery",
        icon = Icons.Default.BatteryFull,
        color = ChartBattery,
        summary = "%.0f%% - ${battery.status}".format(battery.chargePercent)
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            MetricBar("Charge", battery.chargePercent, 100.0, statusColor)
            
            InfoRow("Status", battery.status)
            
            if (battery.estimatedTimeRemaining != null) {
                InfoRow("Time Remaining", battery.estimatedTimeRemaining)
            }
            
            InfoRow("Health", "%.1f%% wear".format(battery.wearLevel))
            InfoRow("Design Capacity", "%.1f Wh".format(battery.designCapacityWh))
            InfoRow("Full Charge Capacity", "%.1f Wh".format(battery.fullChargeCapacityWh))
            
            if (battery.cycleCount > 0) {
                InfoRow("Cycle Count", "${battery.cycleCount}")
            }
            
            if (battery.chargeRateW > 0) {
                InfoRow("Charge Rate", "%.1f W".format(battery.chargeRateW))
            }
            if (battery.dischargeRateW > 0) {
                InfoRow("Discharge Rate", "%.1f W".format(battery.dischargeRateW))
            }
        }
    }
}

@Composable
fun FansDetailCard(fans: List<FanMetrics>) {
    ExpandableDetailCard(
        title = "Fans",
        icon = Icons.Default.Air,
        color = AccentCyan,
        summary = "${fans.size} fan(s)"
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            fans.forEach { fan ->
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(fan.name, style = MaterialTheme.typography.bodyMedium)
                    Text(
                        "%.0f RPM".format(fan.rpm),
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}

@Composable
fun TopProcessesCard(processes: List<ProcessInfo>) {
    ExpandableDetailCard(
        title = "Top Processes",
        icon = Icons.Default.Apps,
        color = AccentPurple,
        summary = "${processes.size} processes"
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
            processes.take(5).forEach { process ->
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        process.name,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.weight(1f)
                    )
                    Text(
                        "%.1f%%".format(process.cpuUsage),
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(modifier = Modifier.width(16.dp))
                    Text(
                        "%.0f MB".format(process.memoryUsageMB),
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ExpandableDetailCard(
    title: String,
    icon: ImageVector,
    color: androidx.compose.ui.graphics.Color,
    summary: String,
    content: @Composable () -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = { expanded = !expanded }
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Icon(icon, contentDescription = null, tint = color)
                    Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                }
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Text(summary, style = MaterialTheme.typography.bodySmall)
                    Icon(
                        if (expanded) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                        contentDescription = null
                    )
                }
            }
            
            if (expanded) {
                Spacer(modifier = Modifier.height(12.dp))
                HorizontalDivider()
                Spacer(modifier = Modifier.height(12.dp))
                content()
            }
        }
    }
}

@Composable
fun MetricBar(
    label: String,
    value: Double,
    maxValue: Double,
    color: androidx.compose.ui.graphics.Color
) {
    Column {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(label, style = MaterialTheme.typography.bodySmall)
            Text("%.1f%%".format(value), style = MaterialTheme.typography.bodySmall, fontWeight = FontWeight.Bold)
        }
        LinearProgressIndicator(
            progress = { (value / maxValue).toFloat().coerceIn(0f, 1f) },
            modifier = Modifier.fillMaxWidth(),
            color = color,
            trackColor = color.copy(alpha = 0.2f)
        )
    }
}

@Composable
fun InfoRow(label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(label, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodySmall, fontWeight = FontWeight.Medium)
    }
}

fun getTemperatureColor(temp: Double): androidx.compose.ui.graphics.Color {
    return when {
        temp < 50 -> TempCold
        temp < 70 -> TempNormal
        temp < 85 -> TempWarm
        else -> TempHot
    }
}
