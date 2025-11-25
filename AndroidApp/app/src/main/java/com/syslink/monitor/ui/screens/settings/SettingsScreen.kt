package com.syslink.monitor.ui.screens.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
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
import com.syslink.monitor.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    viewModel: SettingsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var showAddServerDialog by remember { mutableStateOf(false) }
    
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Servers section
        SettingsSection(title = "Servers") {
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(16.dp)) {
                    uiState.servers.forEach { server ->
                        ServerItem(
                            name = server.name,
                            address = "${server.ipAddress}:${server.port}",
                            isConnected = server.isConnected,
                            isSelected = server.id == uiState.selectedServerId,
                            onSelect = { viewModel.selectServer(server.id) },
                            onDelete = { viewModel.removeServer(server.id) }
                        )
                        if (server != uiState.servers.last()) {
                            HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                        }
                    }
                    
                    if (uiState.servers.isEmpty()) {
                        Text(
                            "No servers configured",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    
                    Spacer(modifier = Modifier.height(12.dp))
                    
                    OutlinedButton(
                        onClick = { showAddServerDialog = true },
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Icon(Icons.Default.Add, contentDescription = null)
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Add Server")
                    }
                }
            }
        }
        
        // Display settings
        SettingsSection(title = "Display") {
            SettingsCard {
                SwitchSetting(
                    title = "Dark Theme",
                    subtitle = "Use dark color scheme",
                    icon = Icons.Default.DarkMode,
                    checked = uiState.darkTheme,
                    onCheckedChange = viewModel::setDarkTheme
                )
                
                HorizontalDivider()
                
                SwitchSetting(
                    title = "Auto Refresh",
                    subtitle = "Automatically update metrics",
                    icon = Icons.Default.Refresh,
                    checked = uiState.autoRefresh,
                    onCheckedChange = viewModel::setAutoRefresh
                )
                
                HorizontalDivider()
                
                SliderSetting(
                    title = "Refresh Interval",
                    subtitle = "${uiState.refreshInterval}ms",
                    icon = Icons.Default.Timer,
                    value = uiState.refreshInterval.toFloat(),
                    valueRange = 500f..5000f,
                    onValueChange = { viewModel.setRefreshInterval(it.toInt()) }
                )
            }
        }
        
        // Alerts settings
        SettingsSection(title = "Alerts") {
            SettingsCard {
                SwitchSetting(
                    title = "Enable Notifications",
                    subtitle = "Show alert notifications",
                    icon = Icons.Default.Notifications,
                    checked = uiState.notificationsEnabled,
                    onCheckedChange = viewModel::setNotificationsEnabled
                )
                
                HorizontalDivider()
                
                SliderSetting(
                    title = "CPU Temp Threshold",
                    subtitle = "${uiState.cpuTempThreshold.toInt()}°C",
                    icon = Icons.Default.Thermostat,
                    value = uiState.cpuTempThreshold,
                    valueRange = 50f..100f,
                    onValueChange = viewModel::setCpuTempThreshold
                )
                
                HorizontalDivider()
                
                SliderSetting(
                    title = "GPU Temp Threshold",
                    subtitle = "${uiState.gpuTempThreshold.toInt()}°C",
                    icon = Icons.Default.Thermostat,
                    value = uiState.gpuTempThreshold,
                    valueRange = 50f..100f,
                    onValueChange = viewModel::setGpuTempThreshold
                )
                
                HorizontalDivider()
                
                SliderSetting(
                    title = "Battery Low Threshold",
                    subtitle = "${uiState.batteryLowThreshold.toInt()}%",
                    icon = Icons.Default.BatteryAlert,
                    value = uiState.batteryLowThreshold,
                    valueRange = 5f..50f,
                    onValueChange = viewModel::setBatteryLowThreshold
                )
            }
        }
        
        // About section
        SettingsSection(title = "About") {
            SettingsCard {
                SettingsItem(
                    title = "Version",
                    subtitle = "1.0.0",
                    icon = Icons.Default.Info
                )
                
                HorizontalDivider()
                
                SettingsItem(
                    title = "Developer",
                    subtitle = "SysLink Team",
                    icon = Icons.Default.Code
                )
                
                HorizontalDivider()
                
                SettingsItem(
                    title = "License",
                    subtitle = "MIT License",
                    icon = Icons.Default.Policy
                )
            }
        }
    }
    
    // Add server dialog
    if (showAddServerDialog) {
        AddServerDialog(
            onDismiss = { showAddServerDialog = false },
            onConfirm = { name, ip, port ->
                viewModel.addServer(name, ip, port)
                showAddServerDialog = false
            }
        )
    }
}

@Composable
fun SettingsSection(
    title: String,
    content: @Composable () -> Unit
) {
    Column {
        Text(
            text = title,
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.padding(bottom = 8.dp)
        )
        content()
    }
}

@Composable
fun SettingsCard(content: @Composable ColumnScope.() -> Unit) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            content()
        }
    }
}

@Composable
fun ServerItem(
    name: String,
    address: String,
    isConnected: Boolean,
    isSelected: Boolean,
    onSelect: () -> Unit,
    onDelete: () -> Unit
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {
        RadioButton(
            selected = isSelected,
            onClick = onSelect
        )
        
        Column(modifier = Modifier.weight(1f)) {
            Text(name, style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.Medium)
            Text(
                address,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        
        Icon(
            imageVector = if (isConnected) Icons.Default.CheckCircle else Icons.Default.Error,
            contentDescription = null,
            tint = if (isConnected) StatusGreen else StatusRed,
            modifier = Modifier.size(20.dp)
        )
        
        IconButton(onClick = onDelete) {
            Icon(
                Icons.Default.Delete,
                contentDescription = "Delete",
                tint = StatusRed
            )
        }
    }
}

@Composable
fun SwitchSetting(
    title: String,
    subtitle: String,
    icon: ImageVector,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(modifier = Modifier.width(16.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(title, style = MaterialTheme.typography.bodyMedium)
            Text(
                subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        Switch(
            checked = checked,
            onCheckedChange = onCheckedChange
        )
    }
}

@Composable
fun SliderSetting(
    title: String,
    subtitle: String,
    icon: ImageVector,
    value: Float,
    valueRange: ClosedFloatingPointRange<Float>,
    onValueChange: (Float) -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary
            )
            Spacer(modifier = Modifier.width(16.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(title, style = MaterialTheme.typography.bodyMedium)
                Text(
                    subtitle,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
        Slider(
            value = value,
            onValueChange = onValueChange,
            valueRange = valueRange,
            modifier = Modifier.padding(start = 40.dp)
        )
    }
}

@Composable
fun SettingsItem(
    title: String,
    subtitle: String,
    icon: ImageVector
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(modifier = Modifier.width(16.dp))
        Column {
            Text(title, style = MaterialTheme.typography.bodyMedium)
            Text(
                subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AddServerDialog(
    onDismiss: () -> Unit,
    onConfirm: (name: String, ip: String, port: Int) -> Unit
) {
    var name by remember { mutableStateOf("") }
    var ip by remember { mutableStateOf("") }
    var port by remember { mutableStateOf("5443") }
    
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Add Server") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Server Name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = ip,
                    onValueChange = { ip = it },
                    label = { Text("IP Address") },
                    placeholder = { Text("192.168.1.100") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = port,
                    onValueChange = { port = it },
                    label = { Text("Port") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = { onConfirm(name, ip, port.toIntOrNull() ?: 5443) },
                enabled = name.isNotBlank() && ip.isNotBlank()
            ) {
                Text("Add")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}
