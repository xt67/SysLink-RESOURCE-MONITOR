package com.syslink.monitor.ui.screens.simple

import androidx.compose.animation.core.*
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.syslink.monitor.ui.theme.*

@Composable
fun SimpleScreen(
    viewModel: SimpleViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Connection status header
        ConnectionStatusCard(
            serverName = uiState.serverName,
            isConnected = uiState.isConnected
        )
        
        // Main metrics row
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            CircularMetricCard(
                modifier = Modifier.weight(1f),
                title = "CPU",
                value = uiState.cpuUsage,
                unit = "%",
                color = ChartCpu,
                icon = Icons.Default.Memory
            )
            CircularMetricCard(
                modifier = Modifier.weight(1f),
                title = "GPU",
                value = uiState.gpuUsage,
                unit = "%",
                color = ChartGpu,
                icon = Icons.Default.Videocam
            )
        }
        
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            CircularMetricCard(
                modifier = Modifier.weight(1f),
                title = "RAM",
                value = uiState.ramUsage,
                unit = "%",
                color = ChartRam,
                icon = Icons.Default.Storage
            )
            if (uiState.batteryPercent != null) {
                CircularMetricCard(
                    modifier = Modifier.weight(1f),
                    title = "Battery",
                    value = uiState.batteryPercent ?: 0.0,
                    unit = "%",
                    color = ChartBattery,
                    icon = Icons.Default.BatteryFull
                )
            } else {
                Spacer(modifier = Modifier.weight(1f))
            }
        }
        
        // Temperature summary
        TemperatureCard(
            cpuTemp = uiState.maxCpuTemp,
            gpuTemp = uiState.gpuTemp
        )
        
        // Quick stats
        QuickStatsCard(
            networkUp = uiState.networkUpload,
            networkDown = uiState.networkDownload
        )
    }
}

@Composable
fun ConnectionStatusCard(
    serverName: String,
    isConnected: Boolean
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = if (isConnected) StatusGreen.copy(alpha = 0.1f) 
                            else StatusRed.copy(alpha = 0.1f)
        )
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = if (isConnected) Icons.Default.CheckCircle else Icons.Default.Error,
                    contentDescription = null,
                    tint = if (isConnected) StatusGreen else StatusRed
                )
                Spacer(modifier = Modifier.width(8.dp))
                Column {
                    Text(
                        text = serverName.ifEmpty { "No Server" },
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = if (isConnected) "Connected" else "Disconnected",
                        style = MaterialTheme.typography.bodySmall,
                        color = if (isConnected) StatusGreen else StatusRed
                    )
                }
            }
            Icon(
                imageVector = Icons.Default.Computer,
                contentDescription = null,
                modifier = Modifier.size(32.dp)
            )
        }
    }
}

@Composable
fun CircularMetricCard(
    modifier: Modifier = Modifier,
    title: String,
    value: Double,
    unit: String,
    color: Color,
    icon: ImageVector
) {
    val animatedValue by animateFloatAsState(
        targetValue = value.toFloat(),
        animationSpec = tween(500),
        label = "value"
    )
    
    Card(modifier = modifier) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = color,
                    modifier = Modifier.size(16.dp)
                )
                Text(
                    text = title,
                    style = MaterialTheme.typography.labelMedium
                )
            }
            
            Spacer(modifier = Modifier.height(12.dp))
            
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier.size(100.dp)
            ) {
                CircularProgressIndicator(
                    progress = { animatedValue / 100f },
                    modifier = Modifier.fillMaxSize(),
                    color = color,
                    strokeWidth = 8.dp,
                    trackColor = color.copy(alpha = 0.2f),
                    strokeCap = StrokeCap.Round
                )
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        text = "%.1f".format(animatedValue),
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = unit,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}

@Composable
fun TemperatureCard(
    cpuTemp: Double,
    gpuTemp: Double
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = "Temperatures",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            
            Spacer(modifier = Modifier.height(12.dp))
            
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                TemperatureItem(
                    label = "CPU",
                    temp = cpuTemp,
                    icon = Icons.Default.Memory
                )
                TemperatureItem(
                    label = "GPU",
                    temp = gpuTemp,
                    icon = Icons.Default.Videocam
                )
            }
        }
    }
}

@Composable
fun TemperatureItem(
    label: String,
    temp: Double,
    icon: ImageVector
) {
    val color = when {
        temp < 50 -> TempCold
        temp < 70 -> TempNormal
        temp < 85 -> TempWarm
        else -> TempHot
    }
    
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = color,
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = label,
            style = MaterialTheme.typography.bodySmall
        )
        Text(
            text = "%.0f°C".format(temp),
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            color = color
        )
    }
}

@Composable
fun QuickStatsCard(
    networkUp: Double,
    networkDown: Double
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Text(
                text = "Network",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            
            Spacer(modifier = Modifier.height(12.dp))
            
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                NetworkStatItem(
                    label = "Upload",
                    value = networkUp,
                    icon = Icons.Default.ArrowUpward,
                    color = StatusGreen
                )
                NetworkStatItem(
                    label = "Download",
                    value = networkDown,
                    icon = Icons.Default.ArrowDownward,
                    color = ChartNetwork
                )
            }
        }
    }
}

@Composable
fun NetworkStatItem(
    label: String,
    value: Double,
    icon: ImageVector,
    color: Color
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = color,
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = label,
            style = MaterialTheme.typography.bodySmall
        )
        Text(
            text = "%.2f Mbps".format(value),
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )
    }
}
