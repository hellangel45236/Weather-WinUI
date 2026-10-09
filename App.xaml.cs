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
    private static extern int AddFontResource(string lpFileName);

    [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        UIntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out UIntPtr lpdwResult);

    private const uint FR_PRIVATE = 0x10;
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
    private const uint WM_FONTCHANGE = 0x001D;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    private static void EnsureFontAwesomeRegistered()
    {
        try
        {
            // 1. Xác định file font gốc trong thư mục ứng dụng
            string baseDir = AppContext.BaseDirectory;
            string sourceFontPath = Path.Combine(baseDir, "Assets", "Fonts", "fa-solid-900.ttf");
            if (!File.Exists(sourceFontPath))
            {
                sourceFontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "fa-solid-900.ttf");
            }

            if (!File.Exists(sourceFontPath))
            {
                return;
            }

            // Nạp font gốc trực tiếp vào bảng font GDI / process
            AddFontResource(sourceFontPath);
            AddFontResourceEx(sourceFontPath, FR_PRIVATE, IntPtr.Zero);

            // 2. Tự động cài đặt / đăng ký vào User Fonts của Windows (HKCU)
            // Chuẩn Windows 10 (1803+) và Windows 11 cho phép user cài font không cần Admin
            string userFontsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Fonts");
            Directory.CreateDirectory(userFontsFolder);
            string userFontPath = Path.Combine(userFontsFolder, "fa-solid-900.ttf");

            bool needsRegistration = false;
            if (!File.Exists(userFontPath))
            {
                File.Copy(sourceFontPath, userFontPath, true);
                needsRegistration = true;
            }
            else
            {
                var srcInfo = new FileInfo(sourceFontPath);
                var dstInfo = new FileInfo(userFontPath);
                if (srcInfo.Length != dstInfo.Length)
                {
                    File.Copy(sourceFontPath, userFontPath, true);
                    needsRegistration = true;
                }
            }

            // Đăng ký các biến thể tên họ font vào Registry HKCU Fonts để DirectWrite phát hiện 100%
            using (var fontsKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts"))
            {
                if (fontsKey != null)
                {
                    string[] fontNames =
                    [
                        "Font Awesome 6 Free Solid (TrueType)",
                        "Font Awesome 6 Free (TrueType)",
                        "Font Awesome 6 Free Solid",
                        "Font Awesome 6 Free"
                    ];

                    foreach (var name in fontNames)
                    {
                        object? existingVal = fontsKey.GetValue(name);
                        if (existingVal == null || string.IsNullOrEmpty(existingVal.ToString()))
                        {
                            fontsKey.SetValue(name, userFontPath, Microsoft.Win32.RegistryValueKind.String);
                            needsRegistration = true;
                        }
                    }
                }
            }

            // Nạp thêm file userFontPath vào GDI
            AddFontResource(userFontPath);
            AddFontResourceEx(userFontPath, FR_PRIVATE, IntPtr.Zero);

            // Gửi thông điệp WM_FONTCHANGE toàn hệ thống để DirectWrite & Windows Shell reload font cache
            if (needsRegistration)
            {
                SendMessageTimeout(HWND_BROADCAST, WM_FONTCHANGE, UIntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 1000, out _);
            }
        }
        catch
        {
            // Bỏ qua nếu có lỗi hạn chế quyền hệ thống đặc thù
        }
    }

    public App()
    {
        try
        {
            // Đảm bảo thư mục làm việc luôn trỏ về thư mục cài đặt ứng dụng để nạp font và assets unpackaged
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Đăng ký font Font Awesome đa tầng cho Windows 10 & 11
            EnsureFontAwesomeRegistered();
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
            const string fontDefinition = "Assets/Fonts/fa-solid-900.ttf#Font Awesome 6 Free Solid, Font Awesome 6 Free Solid, ms-appx:///Assets/Fonts/fa-solid-900.ttf#Font Awesome 6 Free Solid, Font Awesome 6 Free";
            Resources["FontAwesomeSolid"] = new Microsoft.UI.Xaml.Media.FontFamily(fontDefinition);
        }
        catch { }
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            MainWindow = _window;

            string cmdLine = Environment.CommandLine;
            var settings = new Services.SettingsService().LoadSettings();
            bool isAutoStart = cmdLine.Contains("--autostart", StringComparison.OrdinalIgnoreCase) ||
                               cmdLine.Contains("--minimized", StringComparison.OrdinalIgnoreCase);

            if (isAutoStart && settings.StartMinimizedToTray)
            {
                // Khởi động ngầm thu nhỏ vào khay hệ thống theo cấu hình
                _window.AppWindow.Hide();
            }
            else
            {
                _window.Activate();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launch_error.log"), $"Launch Error: {ex}\nStackTrace: {ex.StackTrace}");
            throw;
        }
    }
}
