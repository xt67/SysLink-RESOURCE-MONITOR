package com.syslink.monitor.di

import com.syslink.monitor.data.api.ApiProvider
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {
    
    @Provides
    @Singleton
    fun provideApiProvider(): ApiProvider {
        return ApiProvider()
    }
}
