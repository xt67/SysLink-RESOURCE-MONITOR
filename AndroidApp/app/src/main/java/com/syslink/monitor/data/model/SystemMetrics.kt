package com.syslink.monitor.data.model

import kotlinx.serialization.Serializable

/**
 * Complete system metrics from the agent.
 */
@Serializable
data class SystemMetrics(
    val timestamp: String = "",
    val cpu: CpuMetrics = CpuMetrics(),
    val gpu: GpuMetrics = GpuMetrics(),
    val ram: RamMetrics = RamMetrics(),
    val disks: List<DiskMetrics> = emptyList(),
    val network: NetworkMetrics = NetworkMetrics(),
    val battery: BatteryMetrics? = null,
    val fans: List<FanMetrics> = emptyList()
)

/**
 * Minimal metrics for Simple View.
 */
@Serializable
data class MinimalMetrics(
    val timestamp: String = "",
    val cpuUsage: Double = 0.0,
    val gpuUsage: Double = 0.0,
    val ramUsage: Double = 0.0,
    val batteryPercent: Double? = null,
    val maxCpuTemp: Double = 0.0,
    val gpuTemp: Double = 0.0
)

@Serializable
data class CpuMetrics(
    val name: String = "",
    val averageUsage: Double = 0.0,
    val averageTemperature: Double = 0.0,
    val maxTemperature: Double = 0.0,
    val totalPower: Double = 0.0,
    val cores: List<CpuCoreMetrics> = emptyList()
)

@Serializable
data class CpuCoreMetrics(
    val coreId: Int = 0,
    val usage: Double = 0.0,
    val temperature: Double = 0.0,
    val clockSpeed: Double = 0.0
)

@Serializable
data class GpuMetrics(
    val name: String = "",
    val temperature: Double = 0.0,
    val usage: Double = 0.0,
    val vramUsed: Double = 0.0,
    val vramTotal: Double = 0.0,
    val vramUsagePercent: Double = 0.0,
    val coreClock: Double = 0.0,
    val memoryClock: Double = 0.0,
    val power: Double = 0.0,
    val fanSpeed: Double = 0.0
)

@Serializable
data class RamMetrics(
    val totalGB: Double = 0.0,
    val usedGB: Double = 0.0,
    val freeGB: Double = 0.0,
    val usagePercent: Double = 0.0,
    val swapTotalGB: Double = 0.0,
    val swapUsedGB: Double = 0.0,
    val swapUsagePercent: Double = 0.0
)

@Serializable
data class DiskMetrics(
    val name: String = "",
    val driveLetter: String = "",
    val totalGB: Double = 0.0,
    val usedGB: Double = 0.0,
    val freeGB: Double = 0.0,
    val usagePercent: Double = 0.0,
    val readSpeedMBps: Double = 0.0,
    val writeSpeedMBps: Double = 0.0,
    val temperature: Double = 0.0,
    val healthStatus: String = "Unknown"
)

@Serializable
data class NetworkMetrics(
    val adapterName: String = "",
    val connectionType: String = "Unknown",
    val uploadSpeedMbps: Double = 0.0,
    val downloadSpeedMbps: Double = 0.0,
    val totalBytesSent: Long = 0,
    val totalBytesReceived: Long = 0,
    val ipAddress: String = "",
    val isConnected: Boolean = false
)

@Serializable
data class BatteryMetrics(
    val isPresent: Boolean = false,
    val chargePercent: Double = 0.0,
    val status: String = "Unknown",
    val designCapacityWh: Double = 0.0,
    val fullChargeCapacityWh: Double = 0.0,
    val wearLevel: Double = 0.0,
    val estimatedTimeRemaining: String? = null,
    val cycleCount: Int = 0,
    val chargeRateW: Double = 0.0,
    val dischargeRateW: Double = 0.0,
    val voltage: Double = 0.0
)

@Serializable
data class FanMetrics(
    val name: String = "",
    val rpm: Double = 0.0,
    val speedPercent: Double = 0.0,
    val type: String = "Other"
)
