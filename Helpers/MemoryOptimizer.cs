using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WeatherApp.Helpers;

public static class MemoryOptimizer
{
    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    private static DateTime _lastTrimTime = DateTime.MinValue;

    /// <summary>
    /// Thu gom bộ nhớ nhẹ nhàng trên Background Task, không bao giờ chặn UI thread.
    /// Giới hạn tần suất tối đa 1 lần mỗi 30 giây để tránh làm nghẽn CPU và disk page fault.
    /// </summary>
    public static void TrimMemory()
    {
        if ((DateTime.Now - _lastTrimTime).TotalSeconds < 30) return;
        _lastTrimTime = DateTime.Now;

        Task.Run(() =>
        {
            try
            {
                GC.Collect(1, GCCollectionMode.Optimized, false);
            }
            catch { }
        });
    }

    /// <summary>
    /// Thu gom bộ nhớ sâu khi người dùng chủ động yêu cầu (hoặc khi thu nhỏ vào khay hệ thống).
    /// </summary>
    public static void DeepCleanup()
    {
        Task.Run(() =>
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Aggressive, false);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Aggressive, false);
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }
            catch { }
        });
    }
}
