# LLM Deployer - Resource Monitor UI Guide

## Application Structure

```
┌─────────────────────────────────────────────────────┐
│  🤖 LLM Deployer - Chat & Resources                 │
│  ✓ Ready                                             │
├─────────────────────────────────────────────────────┤
│  [💬 Chat]  [📊 Resources]                          │ ← TAB CONTROL
├─────────────────────────────────────────────────────┤
│                                                     │
│  RESOURCES TAB CONTENT:                             │
│  ────────────────────────────────────────────────  │
│                                                     │
│  [▶ Start]  [⏹ Stop]  Monitoring: Stopped           │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ CPU Usage:              ████░░░░░░░░░░░░░   │   │
│  │                         35.5%                │   │
│  │                                             │   │
│  │ Memory Usage:           ██████░░░░░░░░░░░  │   │
│  │                         512 MB (25.6%)       │   │
│  │                                             │   │
│  │ Active Threads:         24 threads           │   │
│  │                                             │   │
│  │ Disk Read Rate:         2.35 MB/s           │   │
│  │                                             │   │
│  │ Disk Write Rate:        0.18 MB/s           │   │
│  │                                             │   │
│  │ ┌─────────────────────────────────────────┐ │   │
│  │ │ 📌 Metric Explanations:                 │ │   │
│  │ │                                         │ │   │
│  │ │ CPU Usage: Percentage of processor...   │ │   │
│  │ │ Memory Usage: RAM consumed by app...    │ │   │
│  │ │ Active Threads: Number of concurrent... │ │   │
│  │ │ Disk I/O Rates: MB/s of operations...   │ │   │
│  │ └─────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
├─────────────────────────────────────────────────────┤
│  Ready                                              │ ← STATUS BAR
└─────────────────────────────────────────────────────┘
```

## Tab Navigation

### 💬 Chat Tab (Original)
- Send/receive messages from LLM
- View chat history
- Clear history button
- Exit application button

### 📊 Resources Tab (New)
- Real-time resource monitoring dashboard
- Start/Stop monitoring controls
- Visual progress bars for CPU and Memory
- Detailed metric cards
- Helpful information legend

## Monitoring States

### Stopped State
```
[▶ Start]  [⏹ Stop (disabled)]  Monitoring: Stopped
```

### Running State
```
[▶ Start (disabled)]  [⏹ Stop]  Monitoring: Running
```

Metrics update every 500ms when monitoring is active.

## Metric Descriptions

### CPU Usage
- **Display**: Percentage (0-100%)
- **Description**: How much processor power the app is using
- **Indicator**: Blue progress bar
- **Example**: 35.5% = Using 35.5% of available CPU capacity

### Memory Usage
- **Display**: Megabytes and Percentage
- **Description**: RAM consumed and % of total system memory
- **Indicator**: Green progress bar
- **Example**: "512 MB (25.6%)" = 512MB of 2GB system memory

### Active Threads
- **Display**: Number of threads
- **Description**: How many concurrent operations are running
- **Example**: "24 threads"

### Disk Read/Write Rates
- **Display**: MB/s (Megabytes per second)
- **Description**: Speed of disk operations
- **Note**: Estimated from memory operations, shows when app accesses disk

## Key Features

✅ **Real-time Updates** - Refreshes every 500ms for smooth monitoring
✅ **Non-intrusive** - Monitoring doesn't impact chat functionality
✅ **Start/Stop Control** - Turn monitoring on and off as needed
✅ **Visual Indicators** - Progress bars for quick assessment
✅ **Detailed Breakdown** - Individual cards for each metric
✅ **Cross-platform** - Works on Windows with graceful degradation on other OS
✅ **Professional UI** - Integrated seamlessly with existing chat interface

## Usage Scenarios

### Scenario 1: Monitor During Large Conversations
```
1. Start resource monitoring
2. Have a long chat session
3. Observe how memory grows with conversation history
4. Useful for identifying memory leaks
```

### Scenario 2: Performance Testing
```
1. Start monitoring
2. Send multiple rapid requests to LLM
3. Watch CPU spike during inference
4. Monitor thread count during concurrent operations
```

### Scenario 3: System Resource Awareness
```
1. Keep Resources tab open while chatting
2. Monitor impact of background operations
3. Adjust usage patterns based on available resources
4. Plan resource-heavy tasks appropriately
```

## Technical Implementation

### Architecture
```
┌─────────────────────────────────────────┐
│         MainWindow (WPF)                │
├─────────────────────────────────────────┤
│    ChatViewModel  │  ResourceViewModel   │
├─────────────────────────────────────────┤
│    ChatService    │  ResourceMonitor     │
├─────────────────────────────────────────┤
│           LLMDeployer.Core               │
└─────────────────────────────────────────┘
```

### Data Flow
```
ResourceMonitor.GetCurrentMetrics()
         ↓
   ResourceMetrics
         ↓
   ResourceViewModel Properties
         ↓
   WPF Bindings
         ↓
   UI Display (Updated every 500ms)
```

## Color Scheme

| Element | Color | Meaning |
|---------|-------|---------|
| CPU Bar | Blue (#007bff) | Processor usage |
| Memory Bar | Green (#28a745) | RAM usage |
| Legend Box | Light Gray (#f8f9fa) | Information section |

## Performance Impact

- **Minimal overhead**: Performance Counters are lightweight
- **No blocking**: Updates happen on UI thread every 500ms
- **Opt-in**: Only active when explicitly started
- **Auto-cleanup**: Resources properly disposed on app exit

