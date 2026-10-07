using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;
using System.IO;

namespace WeatherApp;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindow { get; private set; }
    
    [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);
    private const uint FR_PRIVATE = 0x10;

    public App()
    {
        try
        {
            // Đảm bảo thư mục làm việc luôn trỏ về thư mục cài đặt ứng dụng để nạp font và assets unpackaged
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Đăng ký Font Awesome vào bảng font tiến trình qua Win32 GDI/DirectWrite cho Windows 10 & 11
            string fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "fa-solid-900.ttf");
            if (!File.Exists(fontPath))
            {
                fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "fa-solid-900.ttf");
            }
            if (File.Exists(fontPath))
            {
                AddFontResourceEx(fontPath, FR_PRIVATE, IntPtr.Zero);
            }
        }
        catch { }

        UnhandledException += (s, e) =>
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherAppWinUI");
                Directory.CreateDirectory(folder);
                string log = $"[UnhandledException] Message: {e.Message}\nException: {e.Exception}\nStackTrace: {e.Exception?.StackTrace}\nInner: {e.Exception?.InnerException}";
                File.WriteAllText(Path.Combine(folder, "crash.log"), log);
            }
            catch { }
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeatherAppWinUI");
                Directory.CreateDirectory(folder);
                string log = $"[AppDomainException] ExceptionObject: {e.ExceptionObject}";
                File.WriteAllText(Path.Combine(folder, "crash_domain.log"), log);
            }
            catch { }
        };

        InitializeComponent();

        try
        {
            Resources["FontAwesomeSolid"] = new Microsoft.UI.Xaml.Media.FontFamily("Font Awesome 6 Free Solid");
        }
        catch { }
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launch_error.log"), $"Launch Error: {ex}\nStackTrace: {ex.StackTrace}");
            throw;
        }
    }
}
