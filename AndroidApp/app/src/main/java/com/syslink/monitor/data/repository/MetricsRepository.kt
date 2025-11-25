package com.syslink.monitor.data.repository

import android.content.Context
import android.util.Log
import com.syslink.monitor.data.api.ApiProvider
import com.syslink.monitor.data.api.SysLinkApi
import com.syslink.monitor.data.model.*
import com.syslink.monitor.util.AppError
import com.syslink.monitor.util.retryWithBackoff
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import retrofit2.Response
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Repository for fetching system metrics from the agent.
 * Provides comprehensive error handling and retry logic.
 */
@Singleton
class MetricsRepository @Inject constructor(
    private val apiProvider: ApiProvider,
    @ApplicationContext private val context: Context
) {
    companion object {
        private const val TAG = "MetricsRepository"
        private const val PREFS_NAME = "syslink_prefs"
        private const val KEY_SERVER_IP = "server_ip"
        private const val KEY_SERVER_PORT = "server_port"
    }
    
    private val prefs by lazy { context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE) }
    
    /**
     * Gets the current API based on saved server settings.
     */
    private fun getApi(): SysLinkApi? {
        val ip = prefs.getString(KEY_SERVER_IP, null) ?: return null
        val port = prefs.getInt(KEY_SERVER_PORT, 5443)
        return apiProvider.getApi(ip, port)
    }
    
    /**
     * Sets the server to connect to.
     */
    fun setServer(ipAddress: String, port: Int) {
        prefs.edit()
            .putString(KEY_SERVER_IP, ipAddress)
            .putInt(KEY_SERVER_PORT, port)
            .apply()
        Log.i(TAG, "Server set to $ipAddress:$port")
    }
    
    /**
     * Gets current server info.
     */
    fun getServerInfo(): Pair<String?, Int> {
        val ip = prefs.getString(KEY_SERVER_IP, null)
        val port = prefs.getInt(KEY_SERVER_PORT, 5443)
        return Pair(ip, port)
    }
    
    /**
     * Clears the server configuration.
     */
    fun clearServer() {
        prefs.edit()
            .remove(KEY_SERVER_IP)
            .remove(KEY_SERVER_PORT)
            .apply()
        apiProvider.clearApi()
    }
    
    /**
     * Generic wrapper for API calls with proper error handling.
     */
    private suspend fun <T> safeApiCall(
        operation: String,
        call: suspend (SysLinkApi) -> Response<T>
    ): Result<T> {
        val api = getApi()
        if (api == null) {
            Log.w(TAG, "$operation: No server configured")
            return Result.failure(Exception("No server configured. Please add a server in Settings."))
        }
        
        return try {
            val response = call(api)
            when {
                response.isSuccessful && response.body() != null -> {
                    Result.success(response.body()!!)
                }
                response.code() == 401 || response.code() == 403 -> {
                    Log.w(TAG, "$operation: Authentication error ${response.code()}")
                    Result.failure(Exception("Authentication failed. Please re-pair with the server."))
                }
                response.code() == 404 -> {
                    Log.w(TAG, "$operation: Resource not found")
                    Result.failure(Exception("Resource not found on server"))
                }
                response.code() >= 500 -> {
                    Log.e(TAG, "$operation: Server error ${response.code()}")
                    Result.failure(Exception("Server error (${response.code()}). Please try again later."))
                }
                else -> {
                    val errorBody = response.errorBody()?.string() ?: "Unknown error"
                    Log.e(TAG, "$operation failed: ${response.code()} - $errorBody")
                    Result.failure(Exception("$operation failed: ${response.code()}"))
                }
            }
        } catch (e: CancellationException) {
            throw e // Never catch cancellation
        } catch (e: Exception) {
            val appError = AppError.fromThrowable(e)
            Log.e(TAG, "$operation error: ${appError.message}", e)
            Result.failure(Exception(appError.message, e))
        }
    }

    /**
     * Gets complete system metrics.
     */
    suspend fun getMetrics(): Result<SystemMetrics> {
        return safeApiCall("GetMetrics") { it.getStatus() }
    }
    
    /**
     * Gets minimal metrics for simple view.
     */
    suspend fun getMinimalMetrics(): Result<MinimalMetrics> {
        return safeApiCall("GetMinimalMetrics") { it.getMinimal() }
    }
    
    /**
     * Gets system information.
     */
    suspend fun getSystemInfo(): Result<SystemInfo> {
        return safeApiCall("GetSystemInfo") { it.getSystemInfo() }
    }
    
    /**
     * Gets process list with optional filtering and sorting.
     */
    suspend fun getProcesses(
        sortBy: String = "CpuUsage",
        sortDesc: Boolean = true,
        top: Int = 50,
        search: String? = null
    ): Result<ProcessListResponse> {
        return safeApiCall("GetProcesses") { api -> 
            api.getProcesses(sortBy, sortDesc, top, search) 
        }
    }
    
    /**
     * Gets historical data for a metric with retry logic.
     */
    suspend fun getHistory(
        metric: String,
        period: String = "1h"
    ): Result<HistoryResponse> {
        val api = getApi()
        if (api == null) {
            return Result.failure(Exception("No server configured"))
        }
        
        return try {
            val response = retryWithBackoff(times = 2) {
                api.getHistory(metric, period)
            }
            if (response.isSuccessful && response.body() != null) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Failed to get history: ${response.code()}"))
            }
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            val appError = AppError.fromThrowable(e)
            Result.failure(Exception(appError.message, e))
        }
    }
    
    /**
     * Pairs with a new server.
     */
    suspend fun pairWithServer(request: PairRequest): Result<PairResponse> {
        return safeApiCall("PairWithServer") { it.pair(request) }
    }
    
    /**
     * Checks if connection to server is healthy.
     */
    suspend fun checkHealth(): Boolean {
        val api = getApi() ?: return false
        return try {
            val response = api.healthCheck()
            response.isSuccessful
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            Log.d(TAG, "Health check failed: ${e.message}")
            false
        }
    }
    
    /**
     * Flow for continuous metrics updates with error recovery.
     */
    fun metricsFlow(intervalMs: Long = 1000): Flow<Result<SystemMetrics>> = flow {
        var consecutiveErrors = 0
        val maxConsecutiveErrors = 5
        
        while (true) {
            val result = getMetrics()
            emit(result)
            
            // Track consecutive errors for backoff
            if (result.isFailure) {
                consecutiveErrors++
                if (consecutiveErrors >= maxConsecutiveErrors) {
                    // Increase delay on repeated failures
                    kotlinx.coroutines.delay(intervalMs * 2)
                    consecutiveErrors = 0 // Reset after backoff
                }
            } else {
                consecutiveErrors = 0
            }
            
            kotlinx.coroutines.delay(intervalMs)
        }
    }
    
    /**
     * Tests connection to a specific server.
     */
    suspend fun testConnection(ipAddress: String, port: Int): Boolean {
        return try {
            val api = apiProvider.getApi(ipAddress, port)
            val response = api.healthCheck()
            response.isSuccessful
        } catch (e: Exception) {
            Log.e(TAG, "Connection test failed: ${e.message}")
            false
        }
    }
}
