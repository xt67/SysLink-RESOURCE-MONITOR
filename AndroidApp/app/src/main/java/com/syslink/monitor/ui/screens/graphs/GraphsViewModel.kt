package com.syslink.monitor.ui.screens.graphs

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.repository.MetricsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class GraphsUiState(
    val selectedPeriod: String = "1h",
    val cpuUsageHistory: List<Float> = emptyList(),
    val cpuTempHistory: List<Float> = emptyList(),
    val gpuUsageHistory: List<Float> = emptyList(),
    val gpuTempHistory: List<Float> = emptyList(),
    val ramUsageHistory: List<Float> = emptyList(),
    val networkUploadHistory: List<Float> = emptyList(),
    val networkDownloadHistory: List<Float> = emptyList(),
    val batteryHistory: List<Float> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)

@HiltViewModel
class GraphsViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(GraphsUiState())
    val uiState: StateFlow<GraphsUiState> = _uiState.asStateFlow()
    
    init {
        loadHistoryData()
    }
    
    fun selectPeriod(period: String) {
        _uiState.update { it.copy(selectedPeriod = period) }
        loadHistoryData()
    }
    
    private fun loadHistoryData() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            
            val period = _uiState.value.selectedPeriod
            
            // Load all metric histories in parallel
            launch { loadCpuUsage(period) }
            launch { loadCpuTemp(period) }
            launch { loadGpuUsage(period) }
            launch { loadGpuTemp(period) }
            launch { loadRamUsage(period) }
            launch { loadNetworkUpload(period) }
            launch { loadNetworkDownload(period) }
            launch { loadBattery(period) }
            
            _uiState.update { it.copy(isLoading = false) }
        }
    }
    
    private suspend fun loadCpuUsage(period: String) {
        repository.getHistory("cpu_usage", period).onSuccess { response ->
            _uiState.update { it.copy(
                cpuUsageHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadCpuTemp(period: String) {
        repository.getHistory("cpu_temp", period).onSuccess { response ->
            _uiState.update { it.copy(
                cpuTempHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadGpuUsage(period: String) {
        repository.getHistory("gpu_usage", period).onSuccess { response ->
            _uiState.update { it.copy(
                gpuUsageHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadGpuTemp(period: String) {
        repository.getHistory("gpu_temp", period).onSuccess { response ->
            _uiState.update { it.copy(
                gpuTempHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadRamUsage(period: String) {
        repository.getHistory("ram_usage", period).onSuccess { response ->
            _uiState.update { it.copy(
                ramUsageHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadNetworkUpload(period: String) {
        repository.getHistory("net_upload", period).onSuccess { response ->
            _uiState.update { it.copy(
                networkUploadHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadNetworkDownload(period: String) {
        repository.getHistory("net_download", period).onSuccess { response ->
            _uiState.update { it.copy(
                networkDownloadHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
    
    private suspend fun loadBattery(period: String) {
        repository.getHistory("battery_percent", period).onSuccess { response ->
            _uiState.update { it.copy(
                batteryHistory = response.dataPoints.map { it.value.toFloat() }
            )}
        }
    }
}
