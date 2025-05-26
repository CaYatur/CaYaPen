using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using ThreadingTimer = System.Threading.Timer;

namespace AdvancedScreenPen
{
    public class PerformanceManager : IDisposable
    {
        private readonly ThreadingTimer performanceTimer;
        private readonly List<PerformanceMetric> metrics;
        private readonly Queue<float> frameTimeHistory;
        private readonly Queue<long> memoryHistory;
        private readonly Queue<float> cpuHistory;

        // Performance counters
        private PerformanceCounter cpuCounter;
        private PerformanceCounter memoryCounter;
        private Process currentProcess;

        // Timing and statistics
        private Stopwatch frameStopwatch;
        private DateTime lastUpdateTime;
        private int frameCount;
        private float currentFps;
        private long currentMemoryUsage;
        private float currentCpuUsage;

        // Optimization settings
        public bool AutoOptimization { get; set; } = true;
        public int MaxFrameRate { get; set; } = 60;
        public long MaxMemoryUsage { get; set; } = 512 * 1024 * 1024; // 512MB
        public float MaxCpuUsage { get; set; } = 80.0f;
        public int HistorySize { get; set; } = 100;
        public int MonitoringInterval { get; set; } = 1000; // 1 second

        // Advanced features
        public bool EnableGpuAcceleration { get; set; } = true;
        public bool EnableMultithreading { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public bool EnableLOD { get; set; } = true; // Level of Detail
        public bool EnableVSync { get; set; } = true;

        // Resource management
        private readonly Dictionary<string, object> resourceCache;
        private readonly Queue<WeakReference> disposableQueue;
        private long cacheSize;
        private readonly object cacheLock = new object();

        // Events
        public event EventHandler<PerformanceEventArgs> PerformanceUpdated;
        public event EventHandler<OptimizationEventArgs> OptimizationApplied;
        public event EventHandler<PerformanceWarningEventArgs> PerformanceWarning;

        // Properties
        public float CurrentFPS => currentFps;
        public long CurrentMemoryUsage => currentMemoryUsage;
        public float CurrentCpuUsage => currentCpuUsage;
        public bool IsOptimized { get; private set; }
        public PerformanceLevel CurrentPerformanceLevel { get; private set; }

        public PerformanceManager()
        {
            metrics = new List<PerformanceMetric>();
            frameTimeHistory = new Queue<float>();
            memoryHistory = new Queue<long>();
            cpuHistory = new Queue<float>();
            resourceCache = new Dictionary<string, object>();
            disposableQueue = new Queue<WeakReference>();

            frameStopwatch = new Stopwatch();
            lastUpdateTime = DateTime.UtcNow;
            CurrentPerformanceLevel = PerformanceLevel.High;

            InitializeCounters();

            performanceTimer = new ThreadingTimer(UpdateMetrics, null, 0, MonitoringInterval);
        }

        public void StartFrame()
        {
            frameStopwatch.Restart();
        }

        public void EndFrame()
        {
            frameStopwatch.Stop();
            var frameTime = (float)frameStopwatch.Elapsed.TotalMilliseconds;

            lock (frameTimeHistory)
            {
                frameTimeHistory.Enqueue(frameTime);
                if (frameTimeHistory.Count > HistorySize)
                    frameTimeHistory.Dequeue();
            }

            frameCount++;

            // Calculate FPS
            var now = DateTime.UtcNow;
            var elapsed = (now - lastUpdateTime).TotalSeconds;

            if (elapsed >= 1.0)
            {
                currentFps = frameCount / (float)elapsed;
                frameCount = 0;
                lastUpdateTime = now;

                // Check for performance issues
                CheckPerformanceThresholds();
            }
        }

        public void RecordMetric(string name, float value, PerformanceMetricType type = PerformanceMetricType.Custom)
        {
            var metric = new PerformanceMetric
            {
                Name = name,
                Value = value,
                Type = type,
                Timestamp = DateTime.UtcNow
            };

            lock (metrics)
            {
                metrics.Add(metric);

                // Keep only recent metrics
                var cutoff = DateTime.UtcNow.AddMinutes(-10);
                metrics.RemoveAll(m => m.Timestamp < cutoff);
            }
        }

        public T GetCachedResource<T>(string key, Func<T> factory) where T : class
        {
            lock (cacheLock)
            {
                if (resourceCache.TryGetValue(key, out object cached) && cached is T)
                {
                    return (T)cached;
                }

                var resource = factory();
                if (resource != null)
                {
                    resourceCache[key] = resource;

                    // Track cache size
                    if (resource is Bitmap bitmap)
                    {
                        cacheSize += bitmap.Width * bitmap.Height * 4; // ARGB
                    }
                    else if (resource is Font font)
                    {
                        cacheSize += 1024; // Estimate font size
                    }

                    // Add to disposal queue if disposable
                    if (resource is IDisposable)
                    {
                        disposableQueue.Enqueue(new WeakReference(resource));
                    }
                }

                return resource;
            }
        }

        public void ClearCache()
        {
            lock (cacheLock)
            {
                // Dispose cached resources
                foreach (var item in resourceCache.Values)
                {
                    if (item is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch { }
                    }
                }

                resourceCache.Clear();
                cacheSize = 0;

                // Clear disposal queue
                disposableQueue.Clear();
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void OptimizeForDevice()
        {
            try
            {
                var systemInfo = GetSystemInfo();
                var recommendedLevel = DetermineOptimalPerformanceLevel(systemInfo);

                ApplyPerformanceLevel(recommendedLevel);

                OnOptimizationApplied($"Optimized for {systemInfo.DeviceType}", recommendedLevel);
            }
            catch (Exception ex)
            {
                OnPerformanceWarning($"Optimization failed: {ex.Message}", PerformanceWarningLevel.Low);
            }
        }

        public void ApplyPerformanceLevel(PerformanceLevel level)
        {
            CurrentPerformanceLevel = level;

            switch (level)
            {
                case PerformanceLevel.Low:
                    ApplyLowPerformanceSettings();
                    break;
                case PerformanceLevel.Medium:
                    ApplyMediumPerformanceSettings();
                    break;
                case PerformanceLevel.High:
                    ApplyHighPerformanceSettings();
                    break;
                case PerformanceLevel.Ultra:
                    ApplyUltraPerformanceSettings();
                    break;
            }

            IsOptimized = true;
        }

        public PerformanceReport GenerateReport()
        {
            var report = new PerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                AverageFPS = CalculateAverageFPS(),
                AverageMemoryUsage = CalculateAverageMemoryUsage(),
                AverageCpuUsage = CalculateAverageCpuUsage(),
                PeakMemoryUsage = GetPeakMemoryUsage(),
                MinFPS = GetMinFPS(),
                MaxFPS = GetMaxFPS(),
                FrameTimeVariation = CalculateFrameTimeVariation(),
                CacheHitRate = CalculateCacheHitRate(),
                OptimizationLevel = CurrentPerformanceLevel,
                SystemInfo = GetSystemInfo(),
                Recommendations = GenerateRecommendations()
            };

            return report;
        }

        public void EnablePerformanceMode(PerformanceMode mode)
        {
            switch (mode)
            {
                case PerformanceMode.PowerSaver:
                    MaxFrameRate = 30;
                    EnableGpuAcceleration = false;
                    EnableMultithreading = false;
                    break;

                case PerformanceMode.Balanced:
                    MaxFrameRate = 60;
                    EnableGpuAcceleration = true;
                    EnableMultithreading = true;
                    break;

                case PerformanceMode.Performance:
                    MaxFrameRate = 120;
                    EnableGpuAcceleration = true;
                    EnableMultithreading = true;
                    EnableCaching = true;
                    break;

                case PerformanceMode.Gaming:
                    MaxFrameRate = 144;
                    EnableGpuAcceleration = true;
                    EnableMultithreading = true;
                    EnableCaching = true;
                    EnableVSync = false;
                    break;
            }
        }

        public void SetMemoryLimit(long limitBytes)
        {
            MaxMemoryUsage = limitBytes;

            // Check if current usage exceeds limit
            if (currentMemoryUsage > MaxMemoryUsage)
            {
                ForceMemoryCleanup();
            }
        }

        public async Task<bool> PreloadResourcesAsync(IEnumerable<string> resourcePaths)
        {
            try
            {
                var tasks = resourcePaths.Select(async path =>
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            // Simulate resource preloading
                            GetCachedResource(path, () => LoadResource(path));
                        }
                        catch { }
                    });
                });

                await Task.WhenAll(tasks);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeCounters()
        {
            try
            {
                currentProcess = Process.GetCurrentProcess();
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                // First reading is often invalid
                cpuCounter.NextValue();
                memoryCounter.NextValue();
            }
            catch (Exception ex)
            {
                OnPerformanceWarning($"Failed to initialize performance counters: {ex.Message}", PerformanceWarningLevel.Medium);
            }
        }

        private void UpdateMetrics(object state)
        {
            try
            {
                // Update CPU usage
                if (cpuCounter != null)
                {
                    currentCpuUsage = cpuCounter.NextValue();
                    lock (cpuHistory)
                    {
                        cpuHistory.Enqueue(currentCpuUsage);
                        if (cpuHistory.Count > HistorySize)
                            cpuHistory.Dequeue();
                    }
                }

                // Update memory usage
                if (currentProcess != null)
                {
                    currentMemoryUsage = currentProcess.WorkingSet64;
                    lock (memoryHistory)
                    {
                        memoryHistory.Enqueue(currentMemoryUsage);
                        if (memoryHistory.Count > HistorySize)
                            memoryHistory.Dequeue();
                    }
                }

                // Cleanup disposal queue
                CleanupDisposalQueue();

                // Auto-optimization check
                if (AutoOptimization)
                {
                    CheckForAutoOptimization();
                }

                // Fire update event
                OnPerformanceUpdated();
            }
            catch (Exception ex)
            {
                OnPerformanceWarning($"Error updating metrics: {ex.Message}", PerformanceWarningLevel.Low);
            }
        }

        private void CheckPerformanceThresholds()
        {
            // Check FPS
            if (currentFps < MaxFrameRate * 0.8f)
            {
                OnPerformanceWarning($"Low FPS detected: {currentFps:F1}", PerformanceWarningLevel.Medium);
            }

            // Check memory
            if (currentMemoryUsage > MaxMemoryUsage)
            {
                OnPerformanceWarning($"High memory usage: {currentMemoryUsage / (1024 * 1024)} MB", PerformanceWarningLevel.High);
                ForceMemoryCleanup();
            }

            // Check CPU
            if (currentCpuUsage > MaxCpuUsage)
            {
                OnPerformanceWarning($"High CPU usage: {currentCpuUsage:F1}%", PerformanceWarningLevel.Medium);
            }
        }

        private void CheckForAutoOptimization()
        {
            var avgFps = CalculateAverageFPS();
            var avgMemory = CalculateAverageMemoryUsage();
            var avgCpu = CalculateAverageCpuUsage();

            // Determine if we need to change performance level
            PerformanceLevel? newLevel = null;

            if (avgFps < 30 || avgMemory > MaxMemoryUsage || avgCpu > 90)
            {
                // Performance is poor, reduce quality
                if (CurrentPerformanceLevel > PerformanceLevel.Low)
                {
                    newLevel = (PerformanceLevel)((int)CurrentPerformanceLevel - 1);
                }
            }
            else if (avgFps > 55 && avgMemory < MaxMemoryUsage * 0.7 && avgCpu < 60)
            {
                // Performance is good, we can increase quality
                if (CurrentPerformanceLevel < PerformanceLevel.Ultra)
                {
                    newLevel = (PerformanceLevel)((int)CurrentPerformanceLevel + 1);
                }
            }

            if (newLevel.HasValue && newLevel != CurrentPerformanceLevel)
            {
                ApplyPerformanceLevel(newLevel.Value);
                OnOptimizationApplied($"Auto-optimization to {newLevel.Value}", newLevel.Value);
            }
        }

        private void ApplyLowPerformanceSettings()
        {
            MaxFrameRate = 30;
            EnableGpuAcceleration = false;
            EnableMultithreading = false;
            EnableCaching = false;
            EnableLOD = true;
            EnableVSync = true;
        }

        private void ApplyMediumPerformanceSettings()
        {
            MaxFrameRate = 45;
            EnableGpuAcceleration = true;
            EnableMultithreading = true;
            EnableCaching = true;
            EnableLOD = true;
            EnableVSync = true;
        }

        private void ApplyHighPerformanceSettings()
        {
            MaxFrameRate = 60;
            EnableGpuAcceleration = true;
            EnableMultithreading = true;
            EnableCaching = true;
            EnableLOD = false;
            EnableVSync = true;
        }

        private void ApplyUltraPerformanceSettings()
        {
            MaxFrameRate = 120;
            EnableGpuAcceleration = true;
            EnableMultithreading = true;
            EnableCaching = true;
            EnableLOD = false;
            EnableVSync = false;
        }

        private void ForceMemoryCleanup()
        {
            try
            {
                // Clear half of the cache
                lock (cacheLock)
                {
                    var itemsToRemove = resourceCache.Take(resourceCache.Count / 2).ToList();
                    foreach (var item in itemsToRemove)
                    {
                        if (item.Value is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                        resourceCache.Remove(item.Key);
                    }
                    cacheSize /= 2;
                }

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                OnOptimizationApplied("Emergency memory cleanup performed", CurrentPerformanceLevel);
            }
            catch (Exception ex)
            {
                OnPerformanceWarning($"Memory cleanup failed: {ex.Message}", PerformanceWarningLevel.High);
            }
        }

        private void CleanupDisposalQueue()
        {
            lock (cacheLock)
            {
                var itemsToRemove = new List<WeakReference>();

                foreach (var weakRef in disposableQueue)
                {
                    if (!weakRef.IsAlive)
                    {
                        itemsToRemove.Add(weakRef);
                    }
                }

                foreach (var item in itemsToRemove)
                {
                    while (disposableQueue.Contains(item))
                    {
                        disposableQueue.Dequeue();
                    }
                }
            }
        }

        private SystemInfo GetSystemInfo()
        {
            var info = new SystemInfo();

            try
            {
                // Get basic system information
                info.ProcessorCount = Environment.ProcessorCount;
                info.TotalMemory = GC.GetTotalMemory(false);
                info.OSVersion = Environment.OSVersion.ToString();
                info.MachineName = Environment.MachineName;

                // Get more detailed information using WMI
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.TotalPhysicalMemory = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                        info.Manufacturer = obj["Manufacturer"]?.ToString();
                        info.Model = obj["Model"]?.ToString();
                    }
                }

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.ProcessorName = obj["Name"]?.ToString();
                        info.ProcessorSpeed = Convert.ToInt32(obj["MaxClockSpeed"]);
                        break;
                    }
                }

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        info.GraphicsCard = obj["Name"]?.ToString();
                        if (obj["AdapterRAM"] != null)
                        {
                            info.GraphicsMemory = Convert.ToInt64(obj["AdapterRAM"]);
                        }
                        break;
                    }
                }

                // Determine device type
                if (info.TotalPhysicalMemory < 4L * 1024 * 1024 * 1024) // Less than 4GB
                {
                    info.DeviceType = DeviceType.LowEnd;
                }
                else if (info.TotalPhysicalMemory < 8L * 1024 * 1024 * 1024) // Less than 8GB
                {
                    info.DeviceType = DeviceType.MidRange;
                }
                else
                {
                    info.DeviceType = DeviceType.HighEnd;
                }
            }
            catch (Exception ex)
            {
                OnPerformanceWarning($"Failed to get system info: {ex.Message}", PerformanceWarningLevel.Low);
            }

            return info;
        }

        private PerformanceLevel DetermineOptimalPerformanceLevel(SystemInfo systemInfo)
        {
            var score = 0;

            // Memory score
            if (systemInfo.TotalPhysicalMemory > 16L * 1024 * 1024 * 1024) score += 3; // 16GB+
            else if (systemInfo.TotalPhysicalMemory > 8L * 1024 * 1024 * 1024) score += 2; // 8GB+
            else if (systemInfo.TotalPhysicalMemory > 4L * 1024 * 1024 * 1024) score += 1; // 4GB+

            // CPU score
            if (systemInfo.ProcessorSpeed > 3000) score += 2;
            else if (systemInfo.ProcessorSpeed > 2000) score += 1;

            if (systemInfo.ProcessorCount >= 8) score += 2;
            else if (systemInfo.ProcessorCount >= 4) score += 1;

            // Graphics score
            if (systemInfo.GraphicsMemory > 4L * 1024 * 1024 * 1024) score += 2; // 4GB+ VRAM
            else if (systemInfo.GraphicsMemory > 2L * 1024 * 1024 * 1024) score += 1; // 2GB+ VRAM

            // Determine level based on score
            if (score >= 8) return PerformanceLevel.Ultra;
            if (score >= 6) return PerformanceLevel.High;
            if (score >= 3) return PerformanceLevel.Medium;
            return PerformanceLevel.Low;
        }

        private object LoadResource(string path)
        {
            // Simulate resource loading
            if (path.EndsWith(".png") || path.EndsWith(".jpg"))
            {
                try
                {
                    return new Bitmap(path);
                }
                catch
                {
                    return null;
                }
            }

            return path; // Return path as placeholder
        }

        private float CalculateAverageFPS()
        {
            lock (frameTimeHistory)
            {
                if (frameTimeHistory.Count == 0) return 0;
                return frameTimeHistory.Count / frameTimeHistory.Sum() * 1000;
            }
        }

        private long CalculateAverageMemoryUsage()
        {
            lock (memoryHistory)
            {
                return memoryHistory.Count > 0 ? (long)memoryHistory.Average() : 0;
            }
        }

        private float CalculateAverageCpuUsage()
        {
            lock (cpuHistory)
            {
                return cpuHistory.Count > 0 ? cpuHistory.Average() : 0;
            }
        }

        private long GetPeakMemoryUsage()
        {
            lock (memoryHistory)
            {
                return memoryHistory.Count > 0 ? memoryHistory.Max() : 0;
            }
        }

        private float GetMinFPS()
        {
            lock (frameTimeHistory)
            {
                if (frameTimeHistory.Count == 0) return 0;
                return 1000 / frameTimeHistory.Max();
            }
        }

        private float GetMaxFPS()
        {
            lock (frameTimeHistory)
            {
                if (frameTimeHistory.Count == 0) return 0;
                return 1000 / frameTimeHistory.Min();
            }
        }

        private float CalculateFrameTimeVariation()
        {
            lock (frameTimeHistory)
            {
                if (frameTimeHistory.Count < 2) return 0;

                var avg = frameTimeHistory.Average();
                var variance = frameTimeHistory.Sum(ft => Math.Pow(ft - avg, 2)) / frameTimeHistory.Count;
                return (float)Math.Sqrt(variance);
            }
        }

        private float CalculateCacheHitRate()
        {
            // This would be calculated based on cache access statistics
            // For now, return a simulated value
            return EnableCaching ? 0.85f : 0.0f;
        }

        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();

            if (CalculateAverageFPS() < 30)
            {
                recommendations.Add("Consider reducing drawing quality or frame rate");
            }

            if (CalculateAverageMemoryUsage() > MaxMemoryUsage * 0.8)
            {
                recommendations.Add("Consider reducing cache size or stroke history");
            }

            if (CalculateAverageCpuUsage() > 80)
            {
                recommendations.Add("Consider disabling advanced effects or multithreading");
            }

            if (!EnableGpuAcceleration)
            {
                recommendations.Add("Enable GPU acceleration for better performance");
            }

            return recommendations;
        }

        // Event handlers
        protected virtual void OnPerformanceUpdated()
        {
            PerformanceUpdated?.Invoke(this, new PerformanceEventArgs
            {
                FPS = currentFps,
                MemoryUsage = currentMemoryUsage,
                CpuUsage = currentCpuUsage,
                Timestamp = DateTime.UtcNow
            });
        }

        protected virtual void OnOptimizationApplied(string description, PerformanceLevel level)
        {
            OptimizationApplied?.Invoke(this, new OptimizationEventArgs
            {
                Description = description,
                Level = level,
                Timestamp = DateTime.UtcNow
            });
        }

        protected virtual void OnPerformanceWarning(string message, PerformanceWarningLevel level)
        {
            PerformanceWarning?.Invoke(this, new PerformanceWarningEventArgs
            {
                Message = message,
                Level = level,
                Timestamp = DateTime.UtcNow
            });
        }

        public void Dispose()
        {
            performanceTimer?.Dispose();
            cpuCounter?.Dispose();
            memoryCounter?.Dispose();
            currentProcess?.Dispose();

            ClearCache();
        }
    }

    // Supporting classes and enums
    public enum PerformanceLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum PerformanceMode
    {
        PowerSaver,
        Balanced,
        Performance,
        Gaming
    }

    public enum PerformanceMetricType
    {
        FrameTime,
        Memory,
        CPU,
        GPU,
        Custom
    }

    public enum PerformanceWarningLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum DeviceType
    {
        LowEnd,
        MidRange,
        HighEnd
    }

    public class PerformanceMetric
    {
        public string Name { get; set; }
        public float Value { get; set; }
        public PerformanceMetricType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SystemInfo
    {
        public int ProcessorCount { get; set; }
        public long TotalMemory { get; set; }
        public long TotalPhysicalMemory { get; set; }
        public string OSVersion { get; set; }
        public string MachineName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string ProcessorName { get; set; }
        public int ProcessorSpeed { get; set; }
        public string GraphicsCard { get; set; }
        public long GraphicsMemory { get; set; }
        public DeviceType DeviceType { get; set; }
    }

    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public float AverageFPS { get; set; }
        public long AverageMemoryUsage { get; set; }
        public float AverageCpuUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public float MinFPS { get; set; }
        public float MaxFPS { get; set; }
        public float FrameTimeVariation { get; set; }
        public float CacheHitRate { get; set; }
        public PerformanceLevel OptimizationLevel { get; set; }
        public SystemInfo SystemInfo { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    // Event argument classes
    public class PerformanceEventArgs : EventArgs
    {
        public float FPS { get; set; }
        public long MemoryUsage { get; set; }
        public float CpuUsage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptimizationEventArgs : EventArgs
    {
        public string Description { get; set; }
        public PerformanceLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PerformanceWarningEventArgs : EventArgs
    {
        public string Message { get; set; }
        public PerformanceWarningLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }
}