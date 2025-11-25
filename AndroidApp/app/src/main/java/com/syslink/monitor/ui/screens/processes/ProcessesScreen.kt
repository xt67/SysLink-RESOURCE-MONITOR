package com.syslink.monitor.ui.screens.processes

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.syslink.monitor.data.model.ProcessInfo
import com.syslink.monitor.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProcessesScreen(
    viewModel: ProcessesViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    
    Column(modifier = Modifier.fillMaxSize()) {
        // Search and filter bar
        SearchFilterBar(
            searchQuery = uiState.searchQuery,
            onSearchChange = viewModel::updateSearch,
            sortBy = uiState.sortBy,
            onSortChange = viewModel::updateSort
        )
        
        // Process count header
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "${uiState.totalCount} processes",
                style = MaterialTheme.typography.labelLarge
            )
            Row {
                Text("CPU", modifier = Modifier.width(60.dp), style = MaterialTheme.typography.labelSmall)
                Text("RAM", modifier = Modifier.width(70.dp), style = MaterialTheme.typography.labelSmall)
            }
        }
        
        HorizontalDivider()
        
        // Process list
        if (uiState.isLoading && uiState.processes.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(bottom = 16.dp)
            ) {
                items(uiState.processes, key = { it.pid }) { process ->
                    ProcessItem(process = process)
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SearchFilterBar(
    searchQuery: String,
    onSearchChange: (String) -> Unit,
    sortBy: String,
    onSortChange: (String) -> Unit
) {
    var showSortMenu by remember { mutableStateOf(false) }
    
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(16.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        OutlinedTextField(
            value = searchQuery,
            onValueChange = onSearchChange,
            modifier = Modifier.weight(1f),
            placeholder = { Text("Search processes...") },
            leadingIcon = { Icon(Icons.Default.Search, contentDescription = null) },
            trailingIcon = {
                if (searchQuery.isNotEmpty()) {
                    IconButton(onClick = { onSearchChange("") }) {
                        Icon(Icons.Default.Clear, contentDescription = "Clear")
                    }
                }
            },
            singleLine = true,
            colors = OutlinedTextFieldDefaults.colors()
        )
        
        Box {
            FilterChip(
                selected = true,
                onClick = { showSortMenu = true },
                label = { Text(sortBy) },
                trailingIcon = { Icon(Icons.Default.ArrowDropDown, contentDescription = null) }
            )
            
            DropdownMenu(
                expanded = showSortMenu,
                onDismissRequest = { showSortMenu = false }
            ) {
                listOf("CpuUsage", "MemoryUsage", "Name", "Pid").forEach { option ->
                    DropdownMenuItem(
                        text = { Text(option) },
                        onClick = {
                            onSortChange(option)
                            showSortMenu = false
                        },
                        leadingIcon = {
                            if (sortBy == option) {
                                Icon(Icons.Default.Check, contentDescription = null)
                            }
                        }
                    )
                }
            }
        }
    }
}

@Composable
fun ProcessItem(process: ProcessInfo) {
    val cpuColor = when {
        process.cpuUsage > 50 -> StatusRed
        process.cpuUsage > 20 -> StatusOrange
        process.cpuUsage > 5 -> StatusYellow
        else -> StatusGreen
    }
    
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Process icon and name
        Icon(
            imageVector = Icons.Default.Apps,
            contentDescription = null,
            modifier = Modifier.size(24.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        
        Spacer(modifier = Modifier.width(12.dp))
        
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = process.name,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Medium,
                maxLines = 1
            )
            Text(
                text = "PID: ${process.pid}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        
        // CPU usage
        Column(
            modifier = Modifier.width(60.dp),
            horizontalAlignment = Alignment.End
        ) {
            Text(
                text = "%.1f%%".format(process.cpuUsage),
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold,
                color = cpuColor
            )
            LinearProgressIndicator(
                progress = { (process.cpuUsage / 100).toFloat().coerceIn(0f, 1f) },
                modifier = Modifier.width(50.dp),
                color = cpuColor,
                trackColor = cpuColor.copy(alpha = 0.2f)
            )
        }
        
        Spacer(modifier = Modifier.width(8.dp))
        
        // Memory usage
        Column(
            modifier = Modifier.width(70.dp),
            horizontalAlignment = Alignment.End
        ) {
            Text(
                text = formatMemory(process.memoryUsageMB),
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Bold
            )
            Text(
                text = "%.1f%%".format(process.memoryUsagePercent),
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

fun formatMemory(mb: Double): String {
    return when {
        mb >= 1024 -> "%.1f GB".format(mb / 1024)
        mb >= 1 -> "%.0f MB".format(mb)
        else -> "%.1f MB".format(mb)
    }
}
