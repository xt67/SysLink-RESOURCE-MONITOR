package com.syslink.monitor.ui.screens.simple

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.repository.MetricsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class SimpleUiState(
    val isConnected: Boolean = false,
    val serverName: String = "",
    val cpuUsage: Double = 0.0,
    val gpuUsage: Double = 0.0,
    val ramUsage: Double = 0.0,
    val batteryPercent: Double? = null,
    val maxCpuTemp: Double = 0.0,
    val gpuTemp: Double = 0.0,
    val networkUpload: Double = 0.0,
    val networkDownload: Double = 0.0,
    val isLoading: Boolean = false,
    val error: String? = null
)

@HiltViewModel
class SimpleViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(SimpleUiState())
    val uiState: StateFlow<SimpleUiState> = _uiState.asStateFlow()
    
    init {
        startMetricsPolling()
    }
    
    private fun startMetricsPolling() {
        viewModelScope.launch {
            while (true) {
                fetchMetrics()
                delay(1000) // Poll every second
            }
        }
    }
    
    private suspend fun fetchMetrics() {
        val result = repository.getMinimalMetrics()
        result.fold(
            onSuccess = { metrics ->
                _uiState.update { current ->
                    current.copy(
                        isConnected = true,
                        cpuUsage = metrics.cpuUsage,
                        gpuUsage = metrics.gpuUsage,
                        ramUsage = metrics.ramUsage,
                        batteryPercent = metrics.batteryPercent,
                        maxCpuTemp = metrics.maxCpuTemp,
                        gpuTemp = metrics.gpuTemp,
                        error = null
                    )
                }
            },
            onFailure = { error ->
                _uiState.update { current ->
                    current.copy(
                        isConnected = false,
                        error = error.message
                    )
                }
            }
        )
        
        // Also fetch network data from full metrics
        val fullResult = repository.getMetrics()
        fullResult.onSuccess { metrics ->
            _uiState.update { current ->
                current.copy(
                    networkUpload = metrics.network.uploadSpeedMbps,
                    networkDownload = metrics.network.downloadSpeedMbps,
                    serverName = metrics.cpu.name.take(20)
                )
            }
        }
    }
    
    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            fetchMetrics()
            _uiState.update { it.copy(isLoading = false) }
        }
    }
}
