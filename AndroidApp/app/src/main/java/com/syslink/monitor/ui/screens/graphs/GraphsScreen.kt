package com.syslink.monitor.ui.screens.graphs

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.patrykandpatrick.vico.compose.axis.horizontal.rememberBottomAxis
import com.patrykandpatrick.vico.compose.axis.vertical.rememberStartAxis
import com.patrykandpatrick.vico.compose.chart.Chart
import com.patrykandpatrick.vico.compose.chart.line.lineChart
import com.patrykandpatrick.vico.compose.chart.line.lineSpec
import com.patrykandpatrick.vico.compose.component.shape.shader.fromBrush
import com.patrykandpatrick.vico.core.component.shape.shader.DynamicShaders
import com.patrykandpatrick.vico.core.entry.ChartEntryModelProducer
import com.patrykandpatrick.vico.core.entry.entryOf
import androidx.compose.ui.graphics.Brush
import com.syslink.monitor.ui.theme.*

@Composable
fun GraphsScreen(
    viewModel: GraphsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Time period selector
        TimePeriodSelector(
            selectedPeriod = uiState.selectedPeriod,
            onPeriodSelected = viewModel::selectPeriod
        )
        
        // Error display
        uiState.error?.let { error ->
            Card(
                colors = CardDefaults.cardColors(
                    containerColor = StatusRed.copy(alpha = 0.1f)
                )
            ) {
                Row(
                    modifier = Modifier.padding(16.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        Icons.Default.Error, 
                        contentDescription = null, 
                        tint = StatusRed
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Text(error, color = StatusRed)
                }
            }
        }
        
        // Loading indicator
        if (uiState.isLoading) {
            LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
        }
        
        // CPU Usage Chart
        MetricChartCard(
            title = "CPU Usage",
            icon = Icons.Default.Memory,
            color = ChartCpu,
            dataPoints = uiState.cpuUsageHistory,
            unit = "%"
        )
        
        // CPU Temperature Chart
        MetricChartCard(
            title = "CPU Temperature",
            icon = Icons.Default.Thermostat,
            color = StatusOrange,
            dataPoints = uiState.cpuTempHistory,
            unit = "°C"
        )
        
        // GPU Usage Chart
        MetricChartCard(
            title = "GPU Usage",
            icon = Icons.Default.Videocam,
            color = ChartGpu,
            dataPoints = uiState.gpuUsageHistory,
            unit = "%"
        )
        
        // GPU Temperature Chart
        MetricChartCard(
            title = "GPU Temperature",
            icon = Icons.Default.Thermostat,
            color = StatusRed,
            dataPoints = uiState.gpuTempHistory,
            unit = "°C"
        )
        
        // RAM Usage Chart
        MetricChartCard(
            title = "RAM Usage",
            icon = Icons.Default.Storage,
            color = ChartRam,
            dataPoints = uiState.ramUsageHistory,
            unit = "%"
        )
        
        // Network Charts
        NetworkChartCard(
            uploadData = uiState.networkUploadHistory,
            downloadData = uiState.networkDownloadHistory
        )
        
        // Battery Chart (if available)
        if (uiState.batteryHistory.isNotEmpty()) {
            MetricChartCard(
                title = "Battery Level",
                icon = Icons.Default.BatteryFull,
                color = ChartBattery,
                dataPoints = uiState.batteryHistory,
                unit = "%"
            )
        }
        
        // Bottom padding for navigation bar
        Spacer(modifier = Modifier.height(8.dp))
    }
}

@Composable
fun TimePeriodSelector(
    selectedPeriod: String,
    onPeriodSelected: (String) -> Unit
) {
    val periods = listOf("1h", "6h", "24h")
    
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(8.dp),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            periods.forEach { period ->
                FilterChip(
                    selected = selectedPeriod == period,
                    onClick = { onPeriodSelected(period) },
                    label = { Text(period) }
                )
            }
        }
    }
}

@Composable
fun MetricChartCard(
    title: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    color: Color,
    dataPoints: List<Float>,
    unit: String
) {
    val chartEntryModelProducer = remember { ChartEntryModelProducer() }
    
    LaunchedEffect(dataPoints) {
        if (dataPoints.isNotEmpty()) {
            try {
                chartEntryModelProducer.setEntries(
                    dataPoints.mapIndexed { index, value ->
                        entryOf(index.toFloat(), value.coerceIn(0f, 1000f))
                    }
                )
            } catch (e: Exception) {
                // Handle chart data errors gracefully
            }
        }
    }
    
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Icon(icon, contentDescription = null, tint = color)
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.weight(1f))
                if (dataPoints.isNotEmpty()) {
                    Text(
                        text = "%.1f$unit".format(dataPoints.lastOrNull() ?: 0f),
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = color
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(16.dp))
            
            if (dataPoints.isNotEmpty()) {
                Chart(
                    chart = lineChart(
                        lines = listOf(
                            lineSpec(
                                lineColor = color,
                                lineBackgroundShader = DynamicShaders.fromBrush(
                                    Brush.verticalGradient(
                                        listOf(color.copy(alpha = 0.4f), color.copy(alpha = 0f))
                                    )
                                )
                            )
                        )
                    ),
                    chartModelProducer = chartEntryModelProducer,
                    startAxis = rememberStartAxis(),
                    bottomAxis = rememberBottomAxis(),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(150.dp)
                )
            } else {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(150.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Icon(
                            Icons.Default.ShowChart,
                            contentDescription = null,
                            modifier = Modifier.size(48.dp),
                            tint = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.5f)
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "No data available",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
            
            // Stats row
            if (dataPoints.isNotEmpty()) {
                Spacer(modifier = Modifier.height(8.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceEvenly
                ) {
                    StatItem("Min", "%.1f$unit".format(dataPoints.minOrNull() ?: 0f))
                    StatItem("Avg", "%.1f$unit".format(dataPoints.average().toFloat()))
                    StatItem("Max", "%.1f$unit".format(dataPoints.maxOrNull() ?: 0f))
                }
            }
        }
    }
}

@Composable
fun NetworkChartCard(
    uploadData: List<Float>,
    downloadData: List<Float>
) {
    val uploadProducer = remember { ChartEntryModelProducer() }
    val downloadProducer = remember { ChartEntryModelProducer() }
    
    LaunchedEffect(uploadData, downloadData) {
        try {
            if (uploadData.isNotEmpty()) {
                uploadProducer.setEntries(
                    uploadData.mapIndexed { index, value -> 
                        entryOf(index.toFloat(), value.coerceIn(0f, 10000f)) 
                    }
                )
            }
            if (downloadData.isNotEmpty()) {
                downloadProducer.setEntries(
                    downloadData.mapIndexed { index, value -> 
                        entryOf(index.toFloat(), value.coerceIn(0f, 10000f)) 
                    }
                )
            }
        } catch (e: Exception) {
            // Handle chart data errors gracefully
        }
    }
    
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Icon(Icons.Default.Wifi, contentDescription = null, tint = ChartNetwork)
                Text(
                    text = "Network",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
            }
            
            Spacer(modifier = Modifier.height(16.dp))
            
            // Upload chart
            Text("Upload", style = MaterialTheme.typography.labelMedium, color = StatusGreen)
            if (uploadData.isNotEmpty()) {
                Chart(
                    chart = lineChart(
                        lines = listOf(lineSpec(lineColor = StatusGreen))
                    ),
                    chartModelProducer = uploadProducer,
                    startAxis = rememberStartAxis(),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(100.dp)
                )
            } else {
                NoDataPlaceholder()
            }
            
            Spacer(modifier = Modifier.height(12.dp))
            
            // Download chart
            Text("Download", style = MaterialTheme.typography.labelMedium, color = ChartNetwork)
            if (downloadData.isNotEmpty()) {
                Chart(
                    chart = lineChart(
                        lines = listOf(lineSpec(lineColor = ChartNetwork))
                    ),
                    chartModelProducer = downloadProducer,
                    startAxis = rememberStartAxis(),
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(100.dp)
                )
            } else {
                NoDataPlaceholder()
            }
        }
    }
}

@Composable
private fun NoDataPlaceholder() {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(100.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "No data",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@Composable
fun StatItem(label: String, value: String) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = label,
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = value,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.Bold
        )
    }
}
