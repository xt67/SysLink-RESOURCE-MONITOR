package com.syslink.monitor.data.api

import com.syslink.monitor.data.model.*
import retrofit2.Response
import retrofit2.http.*

/**
 * Retrofit API interface for SysLink agent.
 */
interface SysLinkApi {
    
    @GET("api/status")
    suspend fun getStatus(): Response<SystemMetrics>
    
    @GET("api/minimal")
    suspend fun getMinimal(): Response<MinimalMetrics>
    
    @GET("api/info")
    suspend fun getSystemInfo(): Response<SystemInfo>
    
    @GET("api/processes")
    suspend fun getProcesses(
        @Query("sortBy") sortBy: String = "CpuUsage",
        @Query("sortDesc") sortDesc: Boolean = true,
        @Query("top") top: Int = 50,
        @Query("search") search: String? = null,
        @Query("includeSystem") includeSystem: Boolean = false
    ): Response<ProcessListResponse>
    
    @GET("api/processes/top/cpu")
    suspend fun getTopByCpu(@Query("count") count: Int = 10): Response<List<ProcessInfo>>
    
    @GET("api/processes/top/memory")
    suspend fun getTopByMemory(@Query("count") count: Int = 10): Response<List<ProcessInfo>>
    
    @GET("api/history/{metric}")
    suspend fun getHistory(
        @Path("metric") metric: String,
        @Query("period") period: String = "1h",
        @Query("maxPoints") maxPoints: Int = 360
    ): Response<HistoryResponse>
    
    @POST("api/auth/pair")
    suspend fun pair(@Body request: PairRequest): Response<PairResponse>
    
    @GET("api/auth/validate")
    suspend fun validateToken(): Response<Unit>
    
    @GET("health")
    suspend fun healthCheck(): Response<Map<String, String>>
}
