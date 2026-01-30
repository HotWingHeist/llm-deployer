using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using LLMDeployer.Core.Services;

namespace LLMDeployer.UI.Desktop;

public class ResourceViewModel : INotifyPropertyChanged
{
    private readonly ResourceMonitor _resourceMonitor;
    private readonly DispatcherTimer _updateTimer;
    private double _cpuUsagePercent;
    private double _memoryMegabytes;
    private double _memoryPercent;
    private double _diskReadMBps;
    private double _diskWriteMBps;
    private double _gpuUsagePercent;
    private double _gpuMemoryMegabytes;
    private string _gpuName = "N/A";
    private int _threadCount;
    private bool _isMonitoring;

    public double CpuUsagePercent
    {
        get => _cpuUsagePercent;
        set { if (_cpuUsagePercent != value) { _cpuUsagePercent = value; OnPropertyChanged(); } }
    }

    public double MemoryMegabytes
    {
        get => _memoryMegabytes;
        set { if (_memoryMegabytes != value) { _memoryMegabytes = value; OnPropertyChanged(); } }
    }

    public double MemoryPercent
    {
        get => _memoryPercent;
        set { if (_memoryPercent != value) { _memoryPercent = value; OnPropertyChanged(); } }
    }

    public double DiskReadMBps
    {
        get => _diskReadMBps;
        set { if (_diskReadMBps != value) { _diskReadMBps = value; OnPropertyChanged(); } }
    }

    public double DiskWriteMBps
    {
        get => _diskWriteMBps;
        set { if (_diskWriteMBps != value) { _diskWriteMBps = value; OnPropertyChanged(); } }
    }

    public double GpuUsagePercent
    {
        get => _gpuUsagePercent;
        set { if (_gpuUsagePercent != value) { _gpuUsagePercent = value; OnPropertyChanged(); } }
    }

    public double GpuMemoryMegabytes
    {
        get => _gpuMemoryMegabytes;
        set { if (_gpuMemoryMegabytes != value) { _gpuMemoryMegabytes = value; OnPropertyChanged(); } }
    }

    public string GpuName
    {
        get => _gpuName;
        set { if (_gpuName != value) { _gpuName = value; OnPropertyChanged(); } }
    }

    public int ThreadCount
    {
        get => _threadCount;
        set { if (_threadCount != value) { _threadCount = value; OnPropertyChanged(); } }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set { if (_isMonitoring != value) { _isMonitoring = value; OnPropertyChanged(); } }
    }

    public ResourceViewModel()
    {
        _resourceMonitor = new ResourceMonitor();
        _updateTimer = new DispatcherTimer();
        _updateTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms
        _updateTimer.Tick += (s, e) => UpdateMetrics();
    }

    public void Start()
    {
        IsMonitoring = true;
        _updateTimer.Start();
        System.Diagnostics.Debug.WriteLine("[ResourceViewModel] Resource monitoring started");
    }

    public void Stop()
    {
        IsMonitoring = false;
        _updateTimer.Stop();
        System.Diagnostics.Debug.WriteLine("[ResourceViewModel] Resource monitoring stopped");
    }

    private void UpdateMetrics()
    {
        try
        {
            var metrics = _resourceMonitor.GetCurrentMetrics();
            CpuUsagePercent = metrics.CpuUsagePercent;
            MemoryMegabytes = metrics.MemoryMegabytes;
            MemoryPercent = metrics.MemoryPercent;
            DiskReadMBps = metrics.DiskReadMBps;
            DiskWriteMBps = metrics.DiskWriteMBps;
            GpuUsagePercent = metrics.GpuUsagePercent;
            GpuMemoryMegabytes = metrics.GpuMemoryMegabytes;
            GpuName = metrics.GpuName;
            ThreadCount = metrics.ThreadCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResourceViewModel] Error updating metrics: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _resourceMonitor?.Dispose();
        _updateTimer?.Stop();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
