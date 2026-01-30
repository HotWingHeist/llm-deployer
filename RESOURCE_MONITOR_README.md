# Resource Monitor Implementation Summary

## Overview
Added comprehensive resource monitoring to the LLM Deployer application with a dedicated UI tab showing real-time CPU, Memory, and I/O metrics.

## New Components

### 1. Core Service: `ResourceMonitor.cs`
**Location**: `src/LLMDeployer.Core/Services/ResourceMonitor.cs`

**Features**:
- Windows-specific Performance Counters for accurate metrics
- Cross-platform support with fallback for non-Windows systems
- `ResourceMetrics` class tracking:
  - **CPU Usage %**: Processor time as percentage of total capacity
  - **Memory (MB)**: RAM consumed by application
  - **Memory %**: Percentage of total system memory
  - **Thread Count**: Active threads in the application
  - **Disk I/O Rates**: MB/s read and write (estimated from paged memory)

**Key Methods**:
```csharp
public ResourceMetrics GetCurrentMetrics()  // Returns current resource state
public void Dispose()  // Cleanup performance counters
```

### 2. UI ViewModel: `ResourceViewModel.cs`
**Location**: `src/LLMDeployer.UI.Desktop/ResourceViewModel.cs`

**Features**:
- MVVM binding support for WPF UI
- Automatic metrics updates every 500ms
- Properties:
  - `CpuUsagePercent`: Bound to progress bar
  - `MemoryMegabytes`, `MemoryPercent`: Memory usage display
  - `ThreadCount`: Thread information
  - `DiskReadMBps`, `DiskWriteMBps`: I/O rates
  - `IsMonitoring`: Status indicator

**Control Methods**:
```csharp
public void Start()   // Begin monitoring
public void Stop()    // Stop monitoring
public void Dispose() // Cleanup resources
```

### 3. Enhanced UI: `MainWindow.xaml & MainWindow.xaml.cs`

**XAML Changes**:
- Added **TabControl** with two tabs:
  1. **💬 Chat Tab** - Original chat interface (unchanged functionality)
  2. **📊 Resources Tab** - New resource monitoring dashboard

**Resources Tab Features**:
- **Start/Stop Buttons** - Control monitoring with visual feedback
- **Real-time Progress Bars** - Visual representation of:
  - CPU usage (blue bar)
  - Memory usage (green bar)
- **Metric Cards** showing:
  - CPU %
  - Memory MB and %
  - Active thread count
  - Disk I/O rates
- **Information Legend** - Help text explaining each metric

**Code-Behind Enhancements**:
- Dual ViewModel management (`_chatViewModel` + `_resourceViewModel`)
- Visual tree traversal to bind resources tab dynamically
- Proper cleanup on application exit

## How to Use

### Starting Resource Monitoring
1. Open the application
2. Click the **Resources** tab
3. Click **▶ Start** button
4. Monitor updates in real-time (refreshes every 500ms)

### Available Metrics

| Metric | Description | Normal Range |
|--------|-------------|--------------|
| **CPU Usage** | Processor utilization | 0-100% |
| **Memory** | RAM consumed | Varies by system |
| **Thread Count** | Active threads | 10-50+ |
| **Disk I/O** | Read/Write rates | 0-100+ MB/s during activity |

## Technical Details

### Performance Counter Usage
- Uses Windows Performance Counter API for accurate measurements
- Divided by `Environment.ProcessorCount` for multi-core systems
- Automatically warmed up on initialization

### Cross-Platform Support
- Windows: Full Performance Counter support
- Non-Windows: Fallback to Process.WorkingSet64

### Resource Cleanup
- `DispatcherTimer` stopped when monitoring ends
- Performance Counters disposed properly
- Full cleanup on application exit

## File Changes

### Modified Files
- `src/LLMDeployer.Core/LLMDeployer.Core.csproj` - Added NuGet packages
- `src/LLMDeployer.UI.Desktop/MainWindow.xaml` - Added TabControl and Resources tab
- `src/LLMDeployer.UI.Desktop/MainWindow.xaml.cs` - Added ResourceViewModel integration

### New Files
- `src/LLMDeployer.Core/Services/ResourceMonitor.cs` - Core monitoring service
- `src/LLMDeployer.UI.Desktop/ResourceViewModel.cs` - MVVM ViewModel for resources

## Dependencies

Added NuGet Package:
- `System.Diagnostics.PerformanceCounter` (v8.0.0) - For Windows performance metrics

## Testing the Feature

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run --project src/LLMDeployer.UI.Desktop
   ```

3. **Test resource monitoring**:
   - Go to Resources tab
   - Click Start
   - Perform actions (chat, type, send messages)
   - Observe metrics update in real-time
   - Click Stop to halt monitoring

## Future Enhancements

Potential additions:
- Historical graphing with live timeline
- CSV export of metrics
- Alerts/thresholds for high resource usage
- Network I/O monitoring
- GPU usage tracking
- Custom sampling intervals
- Performance profiling integration

## Notes

- Resource monitoring is **optional** - chat functionality is independent
- Metrics are **non-intrusive** - monitoring doesn't impact performance
- Update frequency (500ms) can be adjusted in `ResourceViewModel` constructor
- Performance Counters require Windows for full functionality

