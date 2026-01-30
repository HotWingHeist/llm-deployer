using System;
using System.Diagnostics;

namespace LLMDeployer.Core.Services;

/// <summary>
/// Monitors system resource usage (CPU, Memory, I/O) for the application
/// </summary>
public class ResourceMonitor : IDisposable
{
    private readonly Process _currentProcess;
    private System.Diagnostics.PerformanceCounter? _cpuCounter;
    private System.Diagnostics.PerformanceCounter? _ramCounter;
    private long _previousPagedMemory = 0;
    private DateTime _lastMeasureTime = DateTime.UtcNow;
    private bool _gpuAvailable = false;
    private string _cachedGpuName = "N/A";
    private double _cachedGpuUsagePercent = 0;
    private double _cachedGpuMemoryMegabytes = 0;
    private DateTime _lastGpuCheck = DateTime.UtcNow;
    private DateTime _lastOllamaSampleTime = DateTime.UtcNow;
    private TimeSpan _lastOllamaCpuTime = TimeSpan.Zero;

    public class ResourceMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryMegabytes { get; set; }
        public double MemoryPercent { get; set; }
        public double LlmCpuUsagePercent { get; set; }
        public double LlmMemoryMegabytes { get; set; }
        public double LlmMemoryPercent { get; set; }
        public int LlmProcessCount { get; set; }
        public double DiskReadMBps { get; set; }
        public double DiskWriteMBps { get; set; }
        public double GpuUsagePercent { get; set; }
        public double GpuMemoryMegabytes { get; set; }
        public string GpuName { get; set; } = "N/A";
        public long ProcessId { get; set; }
        public int ThreadCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public ResourceMonitor()
    {
        _currentProcess = Process.GetCurrentProcess();
        InitializePerformanceCounters();
        CheckGpuAvailability();
    }

    private void CheckGpuAvailability()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Try to detect NVIDIA GPU via nvidia-smi
                var psi = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=index,name --format=csv,noheader,nounits",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null && proc.WaitForExit(3000))
                    {
                        var output = proc.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            _gpuAvailable = true;
                            var parts = output.Split(',');
                            if (parts.Length >= 2)
                            {
                                _cachedGpuName = parts[1].Trim();
                            }
                            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] GPU detected: {_cachedGpuName}");
                        }
                        else
                        {
                            _gpuAvailable = false;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] GPU detection failed: {ex.Message}");
            _gpuAvailable = false;
        }
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            var processName = _currentProcess.ProcessName;
            var processId = _currentProcess.Id;

            // CPU Performance Counter
            _cpuCounter = new System.Diagnostics.PerformanceCounter(
                "Process",
                "% Processor Time",
                processName,
                true);

            // Memory Performance Counter (Working Set in bytes)
            _ramCounter = new System.Diagnostics.PerformanceCounter(
                "Process",
                "Working Set",
                processName,
                true);

            // Warm up the counters
            if (OperatingSystem.IsWindows())
            {
                if (_cpuCounter != null) _ = _cpuCounter.NextValue();
                if (_ramCounter != null) _ = _ramCounter.NextValue();
            }

            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] Performance counters initialized for process: {processName} (PID: {processId})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] Warning: Could not initialize performance counters: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets current resource usage metrics
    /// </summary>
    public ResourceMetrics GetCurrentMetrics()
    {
        var metrics = new ResourceMetrics
        {
            ProcessId = _currentProcess.Id,
            Timestamp = DateTime.UtcNow,
            ThreadCount = _currentProcess.Threads.Count
        };

        try
        {
            // Refresh process info
            _currentProcess.Refresh();

            // CPU Usage
            if (OperatingSystem.IsWindows() && _cpuCounter != null)
            {
                float cpuValue = _cpuCounter.NextValue();
                // Divide by number of cores for actual usage percentage
                metrics.CpuUsagePercent = Math.Round(cpuValue / Environment.ProcessorCount, 2);
            }

            // Memory Usage
            if (OperatingSystem.IsWindows() && _ramCounter != null)
            {
                float ramBytes = _ramCounter.NextValue();
                metrics.MemoryMegabytes = Math.Round(ramBytes / (1024 * 1024), 2);

                var totalSystemMemory = GetEstimatedTotalSystemMemoryBytes();
                metrics.MemoryPercent = Math.Round((ramBytes / (double)totalSystemMemory) * 100, 2);
            }
            else if (!OperatingSystem.IsWindows())
            {
                // Fallback for non-Windows: use direct memory info
                var workingSet = _currentProcess.WorkingSet64;
                metrics.MemoryMegabytes = Math.Round((double)workingSet / (1024 * 1024), 2);
                long totalSystemMemory = GetEstimatedTotalSystemMemoryBytes();
                metrics.MemoryPercent = Math.Round((workingSet / (double)totalSystemMemory) * 100, 2);
            }

            // LLM (Ollama) Usage
            UpdateOllamaMetrics(metrics);

            // I/O Usage (estimated from paged memory changes)
            try
            {
                var currentPagedMemory = _currentProcess.PagedMemorySize64;
                var timeSinceLastMeasure = (DateTime.UtcNow - _lastMeasureTime).TotalSeconds;
                
                if (timeSinceLastMeasure > 0.1) // Only update if enough time has passed
                {
                    long pagedDelta = Math.Max(0, currentPagedMemory - _previousPagedMemory);
                    metrics.DiskWriteMBps = Math.Round(pagedDelta / (1024 * 1024 * timeSinceLastMeasure), 3);
                    
                    _previousPagedMemory = currentPagedMemory;
                    _lastMeasureTime = DateTime.UtcNow;
                }
            }
            catch
            {
                // I/O counters might not be available on all systems
                metrics.DiskReadMBps = 0;
                metrics.DiskWriteMBps = 0;
            }

            // GPU Usage (if available)
            if (_gpuAvailable)
            {
                metrics.GpuName = _cachedGpuName;
                metrics.GpuUsagePercent = _cachedGpuUsagePercent;
                metrics.GpuMemoryMegabytes = _cachedGpuMemoryMegabytes;
                
                if ((DateTime.UtcNow - _lastGpuCheck).TotalSeconds > 1)
                {
                    try
                    {
                        GetGpuMetrics(metrics);
                        _lastGpuCheck = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] Error reading GPU metrics: {ex.Message}");
                    }
                }
            }
            else
            {
                metrics.GpuName = "N/A";
            }

            System.Diagnostics.Debug.WriteLine(
                $"[ResourceMonitor] CPU: {metrics.CpuUsagePercent}% | RAM: {metrics.MemoryMegabytes}MB ({metrics.MemoryPercent}%) | LLM CPU: {metrics.LlmCpuUsagePercent}% | LLM RAM: {metrics.LlmMemoryMegabytes}MB ({metrics.LlmMemoryPercent}%) | GPU: {metrics.GpuUsagePercent}% | Threads: {metrics.ThreadCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] Error reading metrics: {ex.Message}");
        }

        return metrics;
    }

    private void GetGpuMetrics(ResourceMetrics metrics)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=index,name,utilization.gpu,memory.used --format=csv,noheader,nounits",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                if (proc != null && proc.WaitForExit(5000))
                {
                    var line = proc.StandardOutput.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 4)
                        {
                            // Don't update name, use cached value
                            if (double.TryParse(parts[2].Trim(), out var gpuUtil))
                            {
                                metrics.GpuUsagePercent = gpuUtil;
                                _cachedGpuUsagePercent = gpuUtil; // Cache the value
                            }
                            if (double.TryParse(parts[3].Trim(), out var gpuMem))
                            {
                                metrics.GpuMemoryMegabytes = gpuMem;
                                _cachedGpuMemoryMegabytes = gpuMem; // Cache the value
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] GPU query failed: {ex.Message}");
            // Keep using cached values, don't reset to 0
        }
    }

    private static long GetEstimatedTotalSystemMemoryBytes()
    {
        long totalSystemMemory = 8L * 1024 * 1024 * 1024; // 8GB default estimate
        try
        {
            var info = GC.GetTotalMemory(false);
            totalSystemMemory = Math.Max(info * 2, totalSystemMemory);
        }
        catch
        {
            // Ignore and use default estimate
        }

        return totalSystemMemory;
    }

    private void UpdateOllamaMetrics(ResourceMetrics metrics)
    {
        try
        {
            var processes = Process.GetProcessesByName("ollama");
            metrics.LlmProcessCount = processes.Length;

            if (processes.Length == 0)
            {
                metrics.LlmCpuUsagePercent = 0;
                metrics.LlmMemoryMegabytes = 0;
                metrics.LlmMemoryPercent = 0;
                return;
            }

            double totalWorkingSet = 0;
            TimeSpan totalCpu = TimeSpan.Zero;

            foreach (var proc in processes)
            {
                using (proc)
                {
                    proc.Refresh();
                    totalWorkingSet += proc.WorkingSet64;
                    totalCpu += proc.TotalProcessorTime;
                }
            }

            metrics.LlmMemoryMegabytes = Math.Round(totalWorkingSet / (1024 * 1024), 2);
            var totalSystemMemory = GetEstimatedTotalSystemMemoryBytes();
            metrics.LlmMemoryPercent = Math.Round((totalWorkingSet / totalSystemMemory) * 100, 2);

            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - _lastOllamaSampleTime).TotalSeconds;

            if (elapsedSeconds > 0.1)
            {
                if (_lastOllamaCpuTime != TimeSpan.Zero && totalCpu >= _lastOllamaCpuTime)
                {
                    var cpuDeltaMs = (totalCpu - _lastOllamaCpuTime).TotalMilliseconds;
                    var cpuPercent = (cpuDeltaMs / (elapsedSeconds * 1000 * Environment.ProcessorCount)) * 100;
                    metrics.LlmCpuUsagePercent = Math.Round(cpuPercent, 2);
                }

                _lastOllamaCpuTime = totalCpu;
                _lastOllamaSampleTime = now;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceMonitor] Ollama metrics error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
        _currentProcess?.Dispose();
    }
}
