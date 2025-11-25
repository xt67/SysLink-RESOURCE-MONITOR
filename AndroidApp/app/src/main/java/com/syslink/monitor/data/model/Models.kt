package com.syslink.monitor.data.model

import kotlinx.serialization.Serializable

/**
 * System information from the agent.
 */
@Serializable
data class SystemInfo(
    val deviceName: String = "",
    val operatingSystem: String = "",
    val osVersion: String = "",
    val cpuName: String = "",
    val cpuCores: Int = 0,
    val cpuThreads: Int = 0,
    val gpuName: String = "",
    val totalRamGB: Double = 0.0,
    val motherboard: String = "",
    val bootTime: String = "",
    val agentVersion: String = "",
    val hasBattery: Boolean = false
)

/**
 * Process information.
 */
@Serializable
data class ProcessInfo(
    val pid: Int = 0,
    val name: String = "",
    val cpuUsage: Double = 0.0,
    val memoryUsageMB: Double = 0.0,
    val memoryUsagePercent: Double = 0.0,
    val diskReadMBps: Double = 0.0,
    val diskWriteMBps: Double = 0.0,
    val networkUsageMbps: Double = 0.0,
    val status: String = "Unknown",
    val startTime: String = "",
    val windowTitle: String? = null,
    val filePath: String? = null
)

/**
 * Process list response.
 */
@Serializable
data class ProcessListResponse(
    val timestamp: String = "",
    val totalProcessCount: Int = 0,
    val processes: List<ProcessInfo> = emptyList(),
    val filterApplied: String? = null,
    val sortBy: String? = null
)

/**
 * Historical data response.
 */
@Serializable
data class HistoryResponse(
    val metricType: String = "",
    val startTime: String = "",
    val endTime: String = "",
    val dataPointCount: Int = 0,
    val dataPoints: List<HistoryDataPoint> = emptyList()
)

@Serializable
data class HistoryDataPoint(
    val timestamp: String = "",
    val value: Double = 0.0,
    val min: Double? = null,
    val max: Double? = null
)

/**
 * Pairing request.
 */
@Serializable
data class PairRequest(
    val deviceName: String,
    val deviceId: String,
    val deviceType: String = "Android",
    val publicKey: String = ""
)

/**
 * Pairing response.
 */
@Serializable
data class PairResponse(
    val success: Boolean = false,
    val token: String? = null,
    val error: String? = null,
    val expiresAt: String? = null,
    val serverName: String = "",
    val serverId: String = ""
)

/**
 * Server (PC) connection info.
 */
data class ServerConnection(
    val id: String = "",
    val name: String = "",
    val ipAddress: String = "",
    val port: Int = 5443,
    val token: String = "",
    val isConnected: Boolean = false,
    val lastConnected: Long = 0
)
