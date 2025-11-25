package com.syslink.monitor.util

import android.util.Log
import kotlinx.coroutines.CancellationException
import retrofit2.HttpException
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException
import javax.net.ssl.SSLException
import javax.net.ssl.SSLHandshakeException

/**
 * Sealed class representing different types of errors that can occur in the app.
 */
sealed class AppError(
    open val message: String,
    open val cause: Throwable? = null
) {
    /**
     * Network-related errors (no internet, timeout, etc.)
     */
    data class NetworkError(
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * Server returned an error response (4xx, 5xx)
     */
    data class ServerError(
        val code: Int,
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * SSL/TLS certificate errors
     */
    data class CertificateError(
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * Connection refused - server not running or unreachable
     */
    data class ConnectionRefused(
        override val message: String = "Connection refused. Is the server running?",
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * Authentication/authorization error
     */
    data class AuthError(
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * Data parsing error
     */
    data class ParseError(
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    /**
     * Unknown/unexpected error
     */
    data class UnknownError(
        override val message: String,
        override val cause: Throwable? = null
    ) : AppError(message, cause)
    
    companion object {
        private const val TAG = "AppError"
        
        /**
         * Converts a Throwable to an appropriate AppError type.
         */
        fun fromThrowable(throwable: Throwable): AppError {
            Log.e(TAG, "Error occurred: ${throwable.message}", throwable)
            
            return when (throwable) {
                is CancellationException -> throw throwable // Don't catch cancellation
                
                is HttpException -> {
                    when (throwable.code()) {
                        401, 403 -> AuthError(
                            "Authentication failed. Please re-pair with the server.",
                            throwable
                        )
                        404 -> ServerError(
                            404,
                            "Resource not found",
                            throwable
                        )
                        500, 502, 503, 504 -> ServerError(
                            throwable.code(),
                            "Server error (${throwable.code()}). Please try again.",
                            throwable
                        )
                        else -> ServerError(
                            throwable.code(),
                            "Server returned error: ${throwable.code()}",
                            throwable
                        )
                    }
                }
                
                is SSLHandshakeException -> CertificateError(
                    "SSL certificate error. The server's certificate may be invalid.",
                    throwable
                )
                
                is SSLException -> CertificateError(
                    "SSL connection failed. Check your network security settings.",
                    throwable
                )
                
                is ConnectException -> ConnectionRefused(
                    "Cannot connect to server. Is it running on the specified address?",
                    throwable
                )
                
                is SocketTimeoutException -> NetworkError(
                    "Connection timed out. Check your network connection.",
                    throwable
                )
                
                is UnknownHostException -> NetworkError(
                    "Cannot resolve server address. Check the IP address.",
                    throwable
                )
                
                is IOException -> NetworkError(
                    "Network error: ${throwable.message ?: "Unknown IO error"}",
                    throwable
                )
                
                is kotlinx.serialization.SerializationException -> ParseError(
                    "Failed to parse server response",
                    throwable
                )
                
                else -> UnknownError(
                    throwable.message ?: "An unexpected error occurred",
                    throwable
                )
            }
        }
    }
}

/**
 * Extension function to convert Result<T> with Throwable to Result<T> with AppError.
 */
fun <T> Result<T>.mapError(): Result<T> {
    return this.onFailure { throwable ->
        // Log the error for debugging
        Log.e("ResultError", "Operation failed", throwable)
    }
}

/**
 * Safe execution wrapper that catches exceptions and returns Result.
 */
suspend fun <T> safeApiCall(
    errorMessage: String = "API call failed",
    call: suspend () -> T
): Result<T> {
    return try {
        Result.success(call())
    } catch (e: CancellationException) {
        throw e // Don't catch cancellation exceptions
    } catch (e: Exception) {
        Log.e("SafeApiCall", errorMessage, e)
        Result.failure(e)
    }
}

/**
 * Retry mechanism for network calls.
 */
suspend fun <T> retryWithBackoff(
    times: Int = 3,
    initialDelayMs: Long = 100,
    maxDelayMs: Long = 1000,
    factor: Double = 2.0,
    block: suspend () -> T
): T {
    var currentDelay = initialDelayMs
    repeat(times - 1) { attempt ->
        try {
            return block()
        } catch (e: IOException) {
            Log.w("Retry", "Attempt ${attempt + 1} failed, retrying in ${currentDelay}ms")
            kotlinx.coroutines.delay(currentDelay)
            currentDelay = (currentDelay * factor).toLong().coerceAtMost(maxDelayMs)
        }
    }
    return block() // Last attempt - let exception propagate
}
