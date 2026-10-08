using Microsoft.UI.Xaml;
using Windows.Graphics;
using WeatherApp.Helpers;

namespace WeatherApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Kích thước cửa sổ mặc định
        AppWindow.Resize(new SizeInt32(1260, 880));

        // Khởi tạo ThemeHelper
        ThemeHelper.Initialize(this);

        // Tương thích đa nền tảng Windows 10 & 11:
        // - Windows 11: Áp dụng MicaAlt cao cấp tự nhiên
        // - Windows 10: Áp dụng DesktopAcrylic với lớp nền bổ trợ Slate Dark chống lóa/chống trong suốt quá mức
        try
        {
            bool isWindows11 = Environment.OSVersion.Version.Build >= 22000;
            if (isWindows11 && Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop
                {
                    Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
            }
            else if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                // Bổ trợ lớp nền slate tối đầm chắc cho Windows 10 để các thẻ card nổi bật, không bị lóa
                RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(248, 15, 23, 42));
            }
            else
            {
                // Fallback vững chắc cho Windows 10 cũ không hỗ trợ Acrylic Controller
                RootWindowGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 15, 23, 42));
            }
        }
        catch { }

        // Kiểm tra khởi động lần đầu
        var settingsService = new Services.SettingsService();
        var settings = settingsService.LoadSettings();

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
                if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
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
}
