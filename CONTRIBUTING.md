# Contributing to SysLink

Thank you for your interest in contributing to SysLink! This guide will help you get started.

---

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on improving the project
- Help others learn and grow

---

## Getting Started

### 1. Fork the Repository

Click the **Fork** button on GitHub to create your own copy.

### 2. Clone Your Fork

```bash
git clone https://github.com/YOUR-USERNAME/SysLink.git
cd SysLink
```

### 3. Set Up Development Environment

#### Windows Agent (.NET 8)
```bash
cd WindowsAgent
dotnet restore
dotnet build
```

#### Android App (Kotlin)
- Open `AndroidApp` folder in Android Studio
- Sync Gradle files
- Build the project

### 4. Create a Branch

```bash
git checkout -b feature/your-feature-name
```

---

## Project Structure

```
SysLink/
├── WindowsAgent/
│   ├── SysLink.Core/       # Models, interfaces
│   ├── SysLink.Hardware/   # Hardware monitoring
│   ├── SysLink.Data/       # SQLite storage
│   ├── SysLink.Api/        # REST API, WebSocket
│   └── SysLink.Agent/      # Entry point, config
│
├── AndroidApp/
│   └── app/src/main/java/com/syslink/monitor/
│       ├── data/           # API, models, repository
│       ├── di/             # Hilt modules
│       └── ui/             # Compose screens
│
└── docs/                   # Documentation
```

---

## Development Guidelines

### Windows Agent (C#/.NET)

#### Code Style
- Follow Microsoft C# coding conventions
- Use `PascalCase` for public members
- Use `camelCase` for private members with `_` prefix
- Use nullable reference types

#### Example:
```csharp
public class MetricsService : IMetricsService
{
    private readonly IHardwareMonitor _hardwareMonitor;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(
        IHardwareMonitor hardwareMonitor,
        ILogger<MetricsService> logger)
    {
        _hardwareMonitor = hardwareMonitor;
        _logger = logger;
    }

    public async Task<SystemMetrics> GetMetricsAsync()
    {
        // Implementation
    }
}
```

#### Testing
```bash
dotnet test
```

### Android App (Kotlin)

#### Code Style
- Follow Kotlin coding conventions
- Use Jetpack Compose for UI
- Use Hilt for dependency injection
- Use Kotlin Coroutines for async

#### Example:
```kotlin
@HiltViewModel
class MetricsViewModel @Inject constructor(
    private val repository: MetricsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(MetricsUiState())
    val uiState: StateFlow<MetricsUiState> = _uiState.asStateFlow()
    
    fun loadMetrics() {
        viewModelScope.launch {
            repository.getMetrics().fold(
                onSuccess = { metrics ->
                    _uiState.update { it.copy(metrics = metrics) }
                },
                onFailure = { error ->
                    _uiState.update { it.copy(error = error.message) }
                }
            )
        }
    }
}
```

#### Testing
```bash
./gradlew test
./gradlew connectedAndroidTest
```

---

## Making Changes

### Commit Messages

Use conventional commits:
```
type(scope): description

feat(api): add process kill endpoint
fix(android): resolve connection timeout issue
docs(readme): update installation instructions
refactor(hardware): simplify CPU monitoring logic
test(api): add unit tests for auth controller
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

### Pull Request Process

1. **Update your fork**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Push your changes**
   ```bash
   git push origin feature/your-feature-name
   ```

3. **Create Pull Request**
   - Go to your fork on GitHub
   - Click "New Pull Request"
   - Select your branch
   - Fill in the PR template

4. **PR Requirements**
   - Clear description of changes
   - Link related issues
   - Add tests if applicable
   - Update documentation
   - Ensure CI passes

---

## Areas for Contribution

### High Priority
- [ ] Add more hardware sensors
- [ ] Improve battery monitoring accuracy
- [ ] Add iOS app support
- [ ] Implement end-to-end encryption
- [ ] Add multi-language support

### Good First Issues
- [ ] Improve error messages
- [ ] Add loading states
- [ ] Fix typos in documentation
- [ ] Add more unit tests

### Feature Ideas
- Linux agent support
- macOS agent support
- Web dashboard
- Alerting via push notifications
- Remote power control
- GPU overclocking controls

---

## Reporting Issues

### Bug Reports

Include:
1. OS version (Windows build, Android version)
2. App version
3. Steps to reproduce
4. Expected vs actual behavior
5. Logs if available

### Feature Requests

Include:
1. Clear description of the feature
2. Use case / why it's needed
3. Proposed implementation (optional)
4. Mockups if UI-related

---

## Code Review

All PRs require at least one review. Reviewers will check:
- Code quality and style
- Test coverage
- Documentation
- Security considerations
- Performance impact

---

## Release Process

1. Version bump in `appsettings.json` and `build.gradle.kts`
2. Update `CHANGELOG.md`
3. Create release branch
4. Build and test
5. Create GitHub release with artifacts
6. Merge to main

---

## Getting Help

- **GitHub Issues**: For bugs and features
- **Discussions**: For questions and ideas
- **Discord**: [Join our community](#) (link TBD)

---

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to SysLink! 🎉
