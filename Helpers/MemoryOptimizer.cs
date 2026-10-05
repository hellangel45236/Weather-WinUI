using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WeatherApp.Helpers;

public static class MemoryOptimizer
{
    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    public static void TrimMemory()
    {
        try
        {
            // Thu gom rác cấp độ sâu nhất của .NET CLR
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            // Yêu cầu Windows giải phóng các trang bộ nhớ đệm JIT và tài nguyên XAML không còn sử dụng
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }
        catch { }
    }
}
