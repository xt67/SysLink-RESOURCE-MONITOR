package com.syslink.monitor.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.syslink.monitor.ui.screens.detailed.DetailedScreen
import com.syslink.monitor.ui.screens.processes.ProcessesScreen
import com.syslink.monitor.ui.screens.settings.SettingsScreen
import com.syslink.monitor.ui.screens.simple.SimpleScreen

sealed class Screen(
    val route: String,
    val title: String,
    val icon: ImageVector
) {
    data object Simple : Screen("simple", "Simple", Icons.Default.Speed)
    data object Detailed : Screen("detailed", "Detailed", Icons.Default.Dashboard)
    data object Processes : Screen("processes", "Processes", Icons.Default.Memory)
    data object Settings : Screen("settings", "Settings", Icons.Default.Settings)
}

val bottomNavItems = listOf(
    Screen.Simple,
    Screen.Detailed,
    Screen.Processes,
    Screen.Settings
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SysLinkNavGraph() {
    val navController = rememberNavController()
    
    Scaffold(
        bottomBar = {
            NavigationBar {
                val navBackStackEntry by navController.currentBackStackEntryAsState()
                val currentDestination = navBackStackEntry?.destination
                
                bottomNavItems.forEach { screen ->
                    NavigationBarItem(
                        icon = { Icon(screen.icon, contentDescription = screen.title) },
                        label = { Text(screen.title) },
                        selected = currentDestination?.hierarchy?.any { it.route == screen.route } == true,
                        onClick = {
                            navController.navigate(screen.route) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        }
                    )
                }
            }
        }
    ) { innerPadding ->
        NavHost(
            navController = navController,
            startDestination = Screen.Simple.route,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable(Screen.Simple.route) { SimpleScreen() }
            composable(Screen.Detailed.route) { DetailedScreen() }
            composable(Screen.Processes.route) { ProcessesScreen() }
            composable(Screen.Settings.route) { SettingsScreen() }
        }
    }
}
