using Microsoft.UI.Xaml;
using Windows.Graphics;
using WeatherApp.Helpers;

namespace WeatherApp;

public sealed partial class MainWindow : Window
{
    private readonly bool _forceWin10Mode = false;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Thiết lập icon tiêu đề và icon taskbar sắc nét chuẩn Windows 10 & 11
        try
        {
            string icoPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (System.IO.File.Exists(icoPath))
            {
                AppWindow.SetIcon(icoPath);
            }
        }
        catch { }

        // Kích thước cửa sổ mặc định
        AppWindow.Resize(new SizeInt32(1260, 880));

        _forceWin10Mode = Environment.GetCommandLineArgs().Any(a =>
            a.Equals("--win10", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--force-win10", StringComparison.OrdinalIgnoreCase));

        // Khởi tạo ThemeHelper
        ThemeHelper.Initialize(this);

        // Kiểm tra khởi động lần đầu & nạp theme ban đầu
        var settingsService = new Services.SettingsService();
        var settings = settingsService.LoadSettings();

        string cmd = Environment.CommandLine;
        ElementTheme initialTheme = ElementTheme.Default;
        if (cmd.Contains("--theme=light", StringComparison.OrdinalIgnoreCase))
            initialTheme = ElementTheme.Light;
        else if (cmd.Contains("--theme=dark", StringComparison.OrdinalIgnoreCase))
            initialTheme = ElementTheme.Dark;
        else if (string.Equals(settings.ThemeMode, "light", StringComparison.OrdinalIgnoreCase))
            initialTheme = ElementTheme.Light;
        else if (string.Equals(settings.ThemeMode, "dark", StringComparison.OrdinalIgnoreCase))
            initialTheme = ElementTheme.Dark;
        RootWindowGrid.RequestedTheme = initialTheme;

        // Lắng nghe sự kiện đổi theme để cập nhật nền cửa sổ và titlebar tức thời
        ThemeHelper.ThemeChanged += (theme) =>
        {
            DispatcherQueue.TryEnqueue(UpdateWindowTheme);
        };
        RootWindowGrid.ActualThemeChanged += (s, e) =>
        {
            UpdateWindowTheme();
        };

        // Áp dụng theme và backdrop ngay lúc khởi tạo
        UpdateWindowTheme();

        // Xử lý thu nhỏ hoặc đóng về khay hệ thống (System Tray)
        AppWindow.Closing += (sender, args) =>
        {
            var s = new Services.SettingsService().LoadSettings();
            if (s.CloseToTray)
            {
                args.Cancel = true;
                AppWindow.Hide();
            }
        };

        AppWindow.Changed += (sender, args) =>
        {
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                bool isMinimized = presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized;
                (RootFrame?.Content as MainPage)?.OnWindowStateChanged(isMinimized);

                if (isMinimized)
                {
                    var s = new Services.SettingsService().LoadSettings();
                    if (s.MinimizeToTray)
                    {
                        AppWindow.Hide();
                    }
                }
            }
        };

        if (!settings.HasCompletedOnboarding)
        {
            RootFrame.Navigate(typeof(OnboardingPage));
        }
        else
        {
            RootFrame.Navigate(typeof(MainPage));
        }
    }

    public void UpdateWindowTheme()
    {
        try
        {
            var settings = new Services.SettingsService().LoadSettings();
            string cmd = Environment.CommandLine;
            bool isDark = ThemeHelper.IsDarkTheme(RootWindowGrid);
            bool isWindows11 = Environment.OSVersion.Version.Build >= 22000 && !_forceWin10Mode;
            string dl = settings.DesignLanguage ?? "Fluent2";
            if (cmd.Contains("--design=nothing", StringComparison.OrdinalIgnoreCase)) dl = "Nothing";
            else if (cmd.Contains("--design=spatial", StringComparison.OrdinalIgnoreCase)) dl = "SpatialGlass";
            else if (cmd.Contains("--design=fluent", StringComparison.OrdinalIgnoreCase)) dl = "Fluent2";

            bool isNothing = string.Equals(dl, "Nothing", StringComparison.OrdinalIgnoreCase);
            bool isEInk = isNothing && (settings.EnableNothingEInkMode || cmd.Contains("--eink", StringComparison.OrdinalIgnoreCase)) && !isDark;

            if (isNothing)
            {
                // SystemBackdrop remains active; opaque background cleanly covers the window
                if (isEInk)
                {
                    RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 244, 241, 234)); // Warm E-Ink Paper #F4F1EA
                    isDark = false;
                }
                else if (isDark)
                {
                    RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 10, 10, 12));
                }
                else
                {
                    RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 240, 240, 242));
                }
            }
            else if (isWindows11 && Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                if (SystemBackdrop is not Microsoft.UI.Xaml.Media.MicaBackdrop)
                {
                    SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop
                    {
                        Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                    };
                }
                RootWindowGrid.Background = null;
            }
            else
            {
                // Windows 10 (Build < 22000) hoặc máy không hỗ trợ Mica:
                if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
                {
                    if (SystemBackdrop is not Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop)
                    {
                        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                    }

                    if (isDark)
                    {
                        // Nền tối Slate đầm chắc chống lóa trên Windows 10
                        RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(246, 15, 23, 42)); // #0F172A
                    }
                    else
                    {
                        // Nền sáng Slate tinh khiết, sạch sẽ, chuẩn Fluent trên Windows 10
                        RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(246, 241, 245, 249)); // #F1F5F9
                    }
                }
                else
                {
                    // Fallback vững chắc cho Windows 10 cũ không có Acrylic Controller
                    // SystemBackdrop remains active; opaque background cleanly covers the window
                    if (isDark)
                    {
                        RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 15, 23, 42));
                    }
                    else
                    {
                        RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Windows.UI.Color.FromArgb(255, 241, 245, 249));
                    }
                }
            }

            // Cập nhật màu các nút điều khiển cửa sổ (Minimize, Maximize, Close) trên thanh tiêu đề
            UpdateTitleBarColors(isDark);
        }
        catch { }
    }

    private void UpdateTitleBarColors(bool isDark)
    {
        try
        {
            if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar != null)
            {
                var titleBar = AppWindow.TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

                if (isDark)
                {
                    titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 241, 245, 249);
                    titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 255, 255, 255);
                    titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 148, 163, 184);
                }
                else
                {
                    titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 0, 0, 0);
                    titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 15, 23, 42);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(40, 0, 0, 0);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(140, 100, 116, 139);
                }
            }
        }
        catch { }
    }
}
