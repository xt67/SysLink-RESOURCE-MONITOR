package com.syslink.monitor.ui.screens.processes

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.syslink.monitor.data.model.ProcessInfo
import com.syslink.monitor.data.repository.MetricsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ProcessesUiState(
    val processes: List<ProcessInfo> = emptyList(),
    val totalCount: Int = 0,
    val searchQuery: String = "",
    val sortBy: String = "CpuUsage",
    val isLoading: Boolean = false,
    val error: String? = null
)

@HiltViewModel
class ProcessesViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(ProcessesUiState())
    val uiState: StateFlow<ProcessesUiState> = _uiState.asStateFlow()
    
    private var pollingJob: Job? = null
    
    init {
        startPolling()
    }
    
    private fun startPolling() {
        pollingJob?.cancel()
        pollingJob = viewModelScope.launch {
            while (true) {
                fetchProcesses()
                delay(2000) // Update every 2 seconds
            }
        }
    }
    
    private suspend fun fetchProcesses() {
        val state = _uiState.value
        
        repository.getProcesses(
            sortBy = state.sortBy,
            sortDesc = true,
            top = 100,
            search = state.searchQuery.ifEmpty { null }
        ).fold(
            onSuccess = { response ->
                _uiState.update { 
                    it.copy(
                        processes = response.processes,
                        totalCount = response.totalProcessCount,
                        error = null
                    )
                }
            },
            onFailure = { error ->
                _uiState.update { it.copy(error = error.message) }
            }
        )
    }
    
    fun updateSearch(query: String) {
        _uiState.update { it.copy(searchQuery = query) }
        viewModelScope.launch {
            delay(300) // Debounce
            fetchProcesses()
        }
    }
    
    fun updateSort(sortBy: String) {
        _uiState.update { it.copy(sortBy = sortBy) }
        viewModelScope.launch {
            fetchProcesses()
        }
    }
    
    override fun onCleared() {
        super.onCleared()
        pollingJob?.cancel()
    }
}
