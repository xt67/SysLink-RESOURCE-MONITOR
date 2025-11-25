package com.syslink.monitor.ui.screens.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.model.ServerConnection
import com.syslink.monitor.data.repository.MetricsRepository
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
    val batteryLowThreshold: Float = 20f,
    val isTestingConnection: Boolean = false,
    val connectionTestResult: String? = null
)

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(SettingsUiState())
    val uiState: StateFlow<SettingsUiState> = _uiState.asStateFlow()
    
    init {
        // Load saved settings
        loadSettings()
    }
    
    private fun loadSettings() {
        // Load current server from repository
        val (currentIp, currentPort) = repository.getServerInfo()
        
        val servers = if (currentIp != null) {
            listOf(
                ServerConnection(
                    id = "current",
                    name = "My PC",
                    ipAddress = currentIp,
                    port = currentPort,
                    isConnected = false
                )
            )
        } else {
            emptyList()
        }
        
        _uiState.update { 
            it.copy(
                servers = servers,
                selectedServerId = if (servers.isNotEmpty()) "current" else ""
            )
        }
        
        // Check connection status
        if (currentIp != null) {
            checkConnectionStatus("current", currentIp, currentPort)
        }
    }
    
    private fun checkConnectionStatus(serverId: String, ip: String, port: Int) {
        viewModelScope.launch {
            val isConnected = repository.testConnection(ip, port)
            _uiState.update { state ->
                state.copy(
                    servers = state.servers.map { server ->
                        if (server.id == serverId) {
                            server.copy(isConnected = isConnected)
                        } else {
                            server
                        }
                    }
                )
            }
        }
    }
    
    fun selectServer(serverId: String) {
        val server = _uiState.value.servers.find { it.id == serverId } ?: return
        
        // Set this server as active
        repository.setServer(server.ipAddress, server.port)
        
        _uiState.update { it.copy(selectedServerId = serverId) }
        
        // Test connection
        checkConnectionStatus(serverId, server.ipAddress, server.port)
    }
    
    fun addServer(name: String, ip: String, port: Int) {
        val newServerId = UUID.randomUUID().toString()
        val newServer = ServerConnection(
            id = newServerId,
            name = name,
            ipAddress = ip,
            port = port,
            isConnected = false
        )
        
        _uiState.update { 
            it.copy(
                servers = it.servers + newServer,
                isTestingConnection = true,
                connectionTestResult = null
            )
        }
        
        // Set as active and test connection
        repository.setServer(ip, port)
        _uiState.update { it.copy(selectedServerId = newServerId) }
        
        viewModelScope.launch {
            val isConnected = repository.testConnection(ip, port)
            _uiState.update { state ->
                state.copy(
                    servers = state.servers.map { server ->
                        if (server.id == newServerId) {
                            server.copy(isConnected = isConnected)
                        } else {
                            server
                        }
                    },
                    isTestingConnection = false,
                    connectionTestResult = if (isConnected) "Connected successfully!" else "Connection failed. Check IP and port."
                )
            }
        }
    }
    
    fun removeServer(serverId: String) {
        val removingSelected = _uiState.value.selectedServerId == serverId
        
        _uiState.update { state ->
            val newServers = state.servers.filter { it.id != serverId }
            state.copy(
                servers = newServers,
                selectedServerId = if (removingSelected) {
                    newServers.firstOrNull()?.id ?: ""
                } else {
                    state.selectedServerId
                }
            )
        }
        
        // If we removed the active server, clear or switch
        if (removingSelected) {
            val remaining = _uiState.value.servers.firstOrNull()
            if (remaining != null) {
                repository.setServer(remaining.ipAddress, remaining.port)
            } else {
                repository.clearServer()
            }
        }
    }
    
    fun clearConnectionTestResult() {
        _uiState.update { it.copy(connectionTestResult = null) }
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
