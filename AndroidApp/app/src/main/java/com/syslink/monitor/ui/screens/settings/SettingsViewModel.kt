package com.syslink.monitor.ui.screens.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.model.ServerConnection
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.util.UUID
import javax.inject.Inject

data class SettingsUiState(
    val servers: List<ServerConnection> = emptyList(),
    val selectedServerId: String = "",
    val darkTheme: Boolean = true,
    val autoRefresh: Boolean = true,
    val refreshInterval: Int = 1000,
    val notificationsEnabled: Boolean = true,
    val cpuTempThreshold: Float = 85f,
    val gpuTempThreshold: Float = 85f,
    val batteryLowThreshold: Float = 20f
)

@HiltViewModel
class SettingsViewModel @Inject constructor() : ViewModel() {
    
    private val _uiState = MutableStateFlow(SettingsUiState())
    val uiState: StateFlow<SettingsUiState> = _uiState.asStateFlow()
    
    init {
        // Load saved settings
        loadSettings()
    }
    
    private fun loadSettings() {
        // TODO: Load from DataStore
        // For now, add a demo server
        _uiState.update { 
            it.copy(
                servers = listOf(
                    ServerConnection(
                        id = "default",
                        name = "My PC",
                        ipAddress = "192.168.1.100",
                        port = 5443,
                        isConnected = false
                    )
                ),
                selectedServerId = "default"
            )
        }
    }
    
    fun selectServer(serverId: String) {
        _uiState.update { it.copy(selectedServerId = serverId) }
        // TODO: Reconnect to selected server
    }
    
    fun addServer(name: String, ip: String, port: Int) {
        val newServer = ServerConnection(
            id = UUID.randomUUID().toString(),
            name = name,
            ipAddress = ip,
            port = port,
            isConnected = false
        )
        _uiState.update { 
            it.copy(servers = it.servers + newServer)
        }
        // TODO: Save to DataStore
    }
    
    fun removeServer(serverId: String) {
        _uiState.update { state ->
            state.copy(
                servers = state.servers.filter { it.id != serverId },
                selectedServerId = if (state.selectedServerId == serverId) {
                    state.servers.firstOrNull { it.id != serverId }?.id ?: ""
                } else {
                    state.selectedServerId
                }
            )
        }
        // TODO: Save to DataStore
    }
    
    fun setDarkTheme(enabled: Boolean) {
        _uiState.update { it.copy(darkTheme = enabled) }
        // TODO: Save to DataStore and apply theme
    }
    
    fun setAutoRefresh(enabled: Boolean) {
        _uiState.update { it.copy(autoRefresh = enabled) }
        // TODO: Save to DataStore
    }
    
    fun setRefreshInterval(interval: Int) {
        _uiState.update { it.copy(refreshInterval = interval) }
        // TODO: Save to DataStore
    }
    
    fun setNotificationsEnabled(enabled: Boolean) {
        _uiState.update { it.copy(notificationsEnabled = enabled) }
        // TODO: Save to DataStore
    }
    
    fun setCpuTempThreshold(threshold: Float) {
        _uiState.update { it.copy(cpuTempThreshold = threshold) }
        // TODO: Save to DataStore
    }
    
    fun setGpuTempThreshold(threshold: Float) {
        _uiState.update { it.copy(gpuTempThreshold = threshold) }
        // TODO: Save to DataStore
    }
    
    fun setBatteryLowThreshold(threshold: Float) {
        _uiState.update { it.copy(batteryLowThreshold = threshold) }
        // TODO: Save to DataStore
    }
}
