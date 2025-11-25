package com.syslink.monitor.ui.screens.detailed

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.model.ProcessInfo
import com.syslink.monitor.data.model.SystemInfo
import com.syslink.monitor.data.model.SystemMetrics
import com.syslink.monitor.data.repository.MetricsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class DetailedUiState(
    val metrics: SystemMetrics = SystemMetrics(),
    val systemInfo: SystemInfo = SystemInfo(),
    val topProcesses: List<ProcessInfo> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)

@HiltViewModel
class DetailedViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(DetailedUiState())
    val uiState: StateFlow<DetailedUiState> = _uiState.asStateFlow()
    
    init {
        loadSystemInfo()
        startMetricsPolling()
    }
    
    private fun loadSystemInfo() {
        viewModelScope.launch {
            repository.getSystemInfo().onSuccess { info ->
                _uiState.update { it.copy(systemInfo = info) }
            }
        }
    }
    
    private fun startMetricsPolling() {
        viewModelScope.launch {
            while (true) {
                fetchData()
                delay(1000)
            }
        }
    }
    
    private suspend fun fetchData() {
        // Fetch metrics
        repository.getMetrics().fold(
            onSuccess = { metrics ->
                _uiState.update { it.copy(metrics = metrics, error = null) }
            },
            onFailure = { error ->
                _uiState.update { it.copy(error = error.message) }
            }
        )
        
        // Fetch top processes
        repository.getProcesses(sortBy = "CpuUsage", top = 10).onSuccess { response ->
            _uiState.update { it.copy(topProcesses = response.processes) }
        }
    }
}
