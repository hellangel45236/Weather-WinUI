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

        // Tương thích đa nền tảng Windows 10 & 11: áp dụng Mica nếu hỗ trợ (Win11), ngược lại dùng DesktopAcrylic (Win10)
        try
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            }
            else if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
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
