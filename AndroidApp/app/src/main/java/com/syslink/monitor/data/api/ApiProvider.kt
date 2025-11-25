package com.syslink.monitor.data.api

import com.jakewharton.retrofit2.converter.kotlinx.serialization.asConverterFactory
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import java.security.SecureRandom
import java.security.cert.X509Certificate
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager

/**
 * Provides dynamically configured API instances based on server settings.
 */
@Singleton
class ApiProvider @Inject constructor() {
    
    private val json = Json {
        ignoreUnknownKeys = true
        isLenient = true
        coerceInputValues = true
    }
    
    private var currentBaseUrl: String? = null
    private var currentApi: SysLinkApi? = null
    
    private val okHttpClient: OkHttpClient by lazy {
        val logging = HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BODY
        }
        
        // Trust all certificates for local development
        val trustAllCerts = arrayOf<TrustManager>(object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun checkServerTrusted(chain: Array<X509Certificate>, authType: String) {}
            override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
        })
        
        val sslContext = SSLContext.getInstance("TLS").apply {
            init(null, trustAllCerts, SecureRandom())
        }
        
        OkHttpClient.Builder()
            .sslSocketFactory(sslContext.socketFactory, trustAllCerts[0] as X509TrustManager)
            .hostnameVerifier { _, _ -> true }
            .addInterceptor(logging)
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(15, TimeUnit.SECONDS)
            .writeTimeout(15, TimeUnit.SECONDS)
            .build()
    }
    
    /**
     * Gets an API instance for the specified server.
     * Creates a new instance if the URL has changed.
     */
    @Synchronized
    fun getApi(ipAddress: String, port: Int): SysLinkApi {
        val baseUrl = "https://$ipAddress:$port/"
        
        if (currentBaseUrl != baseUrl || currentApi == null) {
            currentBaseUrl = baseUrl
            currentApi = createApi(baseUrl)
        }
        
        return currentApi!!
    }
    
    /**
     * Gets the currently configured API, or null if none.
     */
    fun getCurrentApi(): SysLinkApi? = currentApi
    
    /**
     * Clears the current API configuration.
     */
    @Synchronized
    fun clearApi() {
        currentBaseUrl = null
        currentApi = null
    }
    
    private fun createApi(baseUrl: String): SysLinkApi {
        val retrofit = Retrofit.Builder()
            .baseUrl(baseUrl)
            .client(okHttpClient)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()
        
        return retrofit.create(SysLinkApi::class.java)
    }
}
