using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;
using WeatherApp.Views.Tabs;

namespace WeatherApp;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    private WidgetWindow? _widgetWindow;
    private bool _isDialogOpen = false;

    // Full HD Share Card Cached fields
    private byte[]? _cachedSharePixels;
    private uint _cachedShareWidth;
    private uint _cachedShareHeight;
    private bool _isRenderingShareCard;

    public MainPage()
    {
        var httpClient = new HttpClient();
        var weatherService = new WeatherService(httpClient);
        var locationService = new LocationService(httpClient);
        var adviceService = new WeatherAdviceService();
        var settingsService = new SettingsService();

        ViewModel = new MainViewModel(weatherService, locationService, adviceService, settingsService);
        DataContext = ViewModel;

        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Sync theme combo box in top bar
            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectedIndex = ViewModel.Settings.ThemeMode switch
                {
                    "light" => 0,
                    "dark" => 1,
                    _ => 2
                };
            }
            UpdateThemeComboBoxLabels();
            LocalizationService.Instance.LanguageChanged += (s, ev) => UpdateThemeComboBoxLabels();

            // Nạp dữ liệu thời tiết
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();

                // Khởi tạo khay hệ thống (System Tray Icon)
                if (App.MainWindow != null && ViewModel.TrayService == null)
                {
                    var trayService = new TrayIconService();
                    trayService.Initialize(App.MainWindow, ViewModel, () => DesktopWidgetButton_Click(this, new RoutedEventArgs()));
                    ViewModel.TrayService = trayService;
                }

                // Tự động hiển thị Changelog nếu vừa cập nhật phiên bản mới
                CheckAndShowChangelog();
            }
        }
        catch { }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double width = e.NewSize.Width;

        // 1. Tinh chỉnh Top Bar linh hoạt để thanh tìm kiếm luôn rộng rãi trên mọi độ phân giải
        if (width < 1220)
        {
            if (BtnTextWidget != null) BtnTextWidget.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (BtnTextWidget != null) BtnTextWidget.Visibility = Visibility.Visible;
        }

        if (width < 1140)
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Collapsed;
            if (ThemeComboBox != null) ThemeComboBox.Width = width < 980 ? 95 : 105;
        }
        else
        {
            if (AppSubtitleText != null) AppSubtitleText.Visibility = Visibility.Visible;
            if (ThemeComboBox != null) ThemeComboBox.Width = 110;
        }

        // Tự co giãn MaxWidth ô tìm kiếm khi cửa sổ hẹp
        if (LocationSearchBox != null)
        {
            LocationSearchBox.MaxWidth = width < 950 ? 280 : (width < 1140 ? 380 : 520);
        }

        // 2. Tinh chỉnh lề ngoài (Root Margin) gọn gàng trên màn hình nhỏ
        if (Content is Grid rootGrid)
        {
            rootGrid.Margin = width < 1250 ? new Thickness(14, 10, 14, 14) : new Thickness(24, 12, 24, 24);
        }
    }

    private bool _isShareDialogOpen = false;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.CurrentWeather))
        {
            if (_isShareDialogOpen)
            {
                UpdateShareCardContent();
            }
        }
    }

    #region Tab Navigation & Lazy Realization

    private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        try
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                ViewModel?.SwitchNav(tag);

                switch (tag)
                {
                    case "overview":
                        TabOverview?.UpdateWeatherVisuals();
                        TabOverview?.StartEffects();
                        TabOverview?.RedrawCanvases();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        AnimationHelper.SlideUpFadeIn(TabOverview);
                        break;

                    case "flood":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var floodTab = FindName(nameof(TabFlood)) as UrbanFloodTab;
                        floodTab?.RenderTidalSineWave();
                        AnimationHelper.SlideUpFadeIn(floodTab);
                        break;

                    case "radar":
                        TabOverview?.StopEffects();
                        var radarTab = FindName(nameof(TabRadar)) as RadarTab;
                        radarTab?.StartRadarSweep();
                        AnimationHelper.SlideUpFadeIn(radarTab);
                        break;

                    case "lifestyle":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var lifestyleTab = FindName(nameof(TabLifestyle)) as LifestyleTab;
                        lifestyleTab?.UpdateOccasionButtonsVisual();
                        lifestyleTab?.UpdateGenderButtonsVisual();
                        AnimationHelper.SlideUpFadeIn(lifestyleTab);
                        break;

                    case "calendar":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var calendarTab = FindName(nameof(TabCalendar)) as CalendarTab;
                        AnimationHelper.SlideUpFadeIn(calendarTab);
                        break;

                    case "widget":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var widgetTab = FindName(nameof(TabWidgetStudio)) as WidgetStudioTab;
                        AnimationHelper.SlideUpFadeIn(widgetTab);
                        break;

                    case "settings":
                        TabOverview?.StopEffects();
                        (FindName(nameof(TabRadar)) as RadarTab)?.StopRadarSweep();
                        var settingsTab = FindName(nameof(TabSettings)) as SettingsTab;
                        settingsTab?.SyncAllSettings();
                        AnimationHelper.SlideUpFadeIn(settingsTab);
                        break;
                }
            }
        }
        catch { }
    }

    private async void OverviewTab_ShareRequested(object? sender, EventArgs e)
    {
        if (ViewModel?.CurrentWeather == null) return;
        UpdateShareCardContent();
        if (ShareCardDialog != null && !_isShareDialogOpen)
        {
            _isShareDialogOpen = true;
            ShareCardDialog.XamlRoot = this.XamlRoot;
            _ = RefreshShareCardBitmapAsync();
            try
            {
                await ShareCardDialog.ShowAsync();
            }
            finally
            {
                _isShareDialogOpen = false;
            }
        }
    }

    private void SettingsTab_OpenChangelogRequested(object? sender, EventArgs e)
    {
        OpenChangelogDialog();
    }

    private void SettingsTab_ReopenOnboardingRequested(object? sender, EventArgs e)
    {
        Frame.Navigate(typeof(OnboardingPage));
    }

    #endregion

    #region Top Bar & Search Handlers

    private async void LocationSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string query = sender.Text;
            if (!string.IsNullOrWhiteSpace(query) && query.Trim().Length >= 2)
            {
                await ViewModel.SearchLocationsCommand.ExecuteAsync(query);
                sender.ItemsSource = ViewModel.SearchResults;
            }
            else
            {
                sender.ItemsSource = null;
            }
        }
    }

    private void LocationSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GeocodingItem item)
        {
            sender.Text = item.DisplayText;
        }
    }

    private async void LocationSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (ViewModel == null) return;

        if (args.ChosenSuggestion is GeocodingItem item)
        {
            sender.Text = item.DisplayText;
            await ViewModel.SelectLocationCommand.ExecuteAsync(item);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await ViewModel.SearchLocationsCommand.ExecuteAsync(args.QueryText);
            if (ViewModel.SearchResults.Count > 0)
            {
                var first = ViewModel.SearchResults[0];
                sender.Text = first.DisplayText;
                await ViewModel.SelectLocationCommand.ExecuteAsync(first);
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;
        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ViewModel.ChangeTheme(tag);
        }
    }

    private void UpdateThemeComboBoxLabels()
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (ThemeComboBox != null && ThemeComboBox.Items.Count >= 3)
        {
            ((ComboBoxItem)ThemeComboBox.Items[0]).Content = isVi ? "☀️ Sáng" : "☀️ Light";
            ((ComboBoxItem)ThemeComboBox.Items[1]).Content = isVi ? "🌙 Tối" : "🌙 Dark";
            ((ComboBoxItem)ThemeComboBox.Items[2]).Content = isVi ? "💻 Hệ thống" : "💻 System";
        }
    }

    private async void QuickLocationChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is QuickLocationChipItem chip && ViewModel != null)
        {
            await ViewModel.SelectQuickChipAsync(chip);
        }
    }

    #endregion

    #region Desktop Widget Windows

    private void DesktopWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        OpenCurrentCityWidget_Click(sender, e);
    }

    private void OpenCurrentCityWidget_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        string chosenStyle = ViewModel.Settings.WidgetStyle ?? "BryanCDynamic";
        double chosenOpacity = ViewModel.Settings.WidgetOpacity;
        if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

        if (_widgetWindow == null)
        {
            _widgetWindow = new WidgetWindow(ViewModel);
            _widgetWindow.Closed += (s, args) => _widgetWindow = null;
        }

        _widgetWindow.ApplyWidgetStyle(chosenStyle);
        _widgetWindow.SetOpacity(chosenOpacity);
        _widgetWindow.Activate();
    }

    private async void OpenCustomCityWidget_Click(object sender, RoutedEventArgs e)
    {
        var inputTextBox = new TextBox
        {
            PlaceholderText = "Nhập tên thành phố (VD: Đà Nẵng, Tokyo, Paris, New York...)",
            Height = 38
        };
        var dialog = new ContentDialog
        {
            Title = "➕ Tạo Widget Mới Cho Thành Phố Khác",
            Content = inputTextBox,
            PrimaryButtonText = "Tạo Widget",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
        {
            string city = inputTextBox.Text.Trim();
            string chosenStyle = ViewModel?.Settings.WidgetStyle ?? "BryanCDynamic";
            double chosenOpacity = ViewModel?.Settings.WidgetOpacity ?? 1.0;
            if (chosenOpacity <= 0.05) chosenOpacity = 1.0;

            try
            {
                var locService = new LocationService();
                var locs = await locService.SearchLocationsAsync(city);
                if (locs != null && locs.Count > 0)
                {
                    var first = locs[0];
                    var widget = new WidgetWindow(ViewModel, $"{first.Name}, {first.Country}", first.Latitude, first.Longitude);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
                else
                {
                    var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                    widget.ApplyWidgetStyle(chosenStyle);
                    widget.SetOpacity(chosenOpacity);
                    widget.Activate();
                }
            }
            catch
            {
                var widget = new WidgetWindow(ViewModel, city, 21.0285, 105.8542);
                widget.ApplyWidgetStyle(chosenStyle);
                widget.SetOpacity(chosenOpacity);
                widget.Activate();
            }
        }
    }

    private void CloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetWindow.CloseAllWidgets();
        _widgetWindow = null;
    }

    #endregion

    #region Full HD Share Card Export

    private async void ShareFormatRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (LandscapeCardContainer == null || StoryCardContainer == null || SharePreviewContainer == null) return;

        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        LandscapeCardContainer.Visibility = isStory ? Visibility.Collapsed : Visibility.Visible;
        StoryCardContainer.Visibility = isStory ? Visibility.Visible : Visibility.Collapsed;

        SharePreviewContainer.Width = isStory ? 1080 : 1920;
        SharePreviewContainer.Height = isStory ? 1920 : 1080;

        await RefreshShareCardBitmapAsync();
    }

    private void UpdateShareCardContent()
    {
        if (ViewModel?.CurrentWeather == null) return;
        var w = ViewModel.CurrentWeather;
        var o = ViewModel.CurrentOutfitAdvice;

        bool isVi = LocalizationService.Instance.IsVietnamese;
        string location = ViewModel.LocationTitle.ToUpper();
        string nowTime = isVi ? $"{DateTime.Now:dddd, dd/MM/yyyy • HH:mm}" : $"{DateTime.Now:dddd, MM/dd/yyyy • HH:mm}";
        string lunar = isVi ? $"Âm lịch: {w.LunarDateText} • {w.SolarTermText}" : $"Lunar: {w.LunarDateText} • {w.SolarTermText}";
        string temp = w.TemperatureText.Replace("C", "").Trim();
        string feelsLike = $"{w.FeelsLikeText} • {w.MinMaxText}";
        string uv = $"{w.UvIndexText} - {w.UvIndexDescription}";
        string aqi = $"{w.AqiValue} - {w.AqiDescription}";
        string rain = w.RainProbabilityText;
        string wind = $"{w.WindText} {w.WindDirectionText}";
        string humidity = w.HumidityText;
        string sun = $"{w.SunriseText} • {w.SunsetText}";

        // 1. Cập nhật Khổ Ngang FHD (1920x1080)
        if (FhdLandscapeLocationText != null) FhdLandscapeLocationText.Text = location;
        if (FhdLandscapeDateText != null) FhdLandscapeDateText.Text = nowTime;
        if (FhdLandscapeLunarText != null) FhdLandscapeLunarText.Text = lunar;
        if (FhdLandscapeTempText != null) FhdLandscapeTempText.Text = temp;
        if (FhdLandscapeConditionText != null) FhdLandscapeConditionText.Text = w.ConditionText;
        if (FhdLandscapeFeelsLikeText != null) FhdLandscapeFeelsLikeText.Text = feelsLike;
        if (FhdLandscapeUvText != null) FhdLandscapeUvText.Text = uv;
        if (FhdLandscapeAqiText != null) FhdLandscapeAqiText.Text = aqi;
        if (FhdLandscapeRainProbText != null) FhdLandscapeRainProbText.Text = rain;
        if (FhdLandscapeWindText != null) FhdLandscapeWindText.Text = wind;
        if (FhdLandscapeHumidityText != null) FhdLandscapeHumidityText.Text = humidity;
        if (FhdLandscapeSunText != null) FhdLandscapeSunText.Text = sun;

        if (o != null)
        {
            if (FhdLandscapeOotdTitle != null) FhdLandscapeOotdTitle.Text = isVi ? $"HÔM NAY MẶC GÌ? (OOTD ADVISOR) — {o.OccasionTitle.ToUpper()}" : $"WHAT TO WEAR TODAY? (OOTD ADVISOR) — {o.OccasionTitle.ToUpper()}";
            if (FhdLandscapeThermalTag != null) FhdLandscapeThermalTag.Text = o.ThermalComfortNotice;
            if (FhdLandscapeOotdHeadline != null) FhdLandscapeOotdHeadline.Text = o.Headline;
            if (FhdLandscapeOotdClothing != null) FhdLandscapeOotdClothing.Text = $"{o.TopClothing}, {o.BottomClothing}, {o.Footwear}";
            if (FhdLandscapeOotdAccessories != null) FhdLandscapeOotdAccessories.Text = string.Join(" • ", o.Accessories.Select(a => $"{a.Name}"));
            if (FhdLandscapeMotorbikeText != null) FhdLandscapeMotorbikeText.Text = o.MotorbikeWarning;
        }

        // Cập nhật 3 ngày dự báo
        if (ViewModel.DailyForecast != null && ViewModel.DailyForecast.Count >= 3)
        {
            var d1 = ViewModel.DailyForecast[0];
            var d2 = ViewModel.DailyForecast[1];
            var d3 = ViewModel.DailyForecast[2];

            if (FhdD1Day != null) FhdD1Day.Text = d1.DayName;
            if (FhdD1Temp != null) FhdD1Temp.Text = $"{d1.TempMinDisplay} / {d1.TempMaxDisplay}";
            if (FhdD2Day != null) FhdD2Day.Text = d2.DayName;
            if (FhdD2Temp != null) FhdD2Temp.Text = $"{d2.TempMinDisplay} / {d2.TempMaxDisplay}";
            if (FhdD3Day != null) FhdD3Day.Text = d3.DayName;
            if (FhdD3Temp != null) FhdD3Temp.Text = $"{d3.TempMinDisplay} / {d3.TempMaxDisplay}";
        }

        // 2. Cập nhật Khổ Dọc Story FHD (1080x1920)
        if (FhdStoryLocationText != null) FhdStoryLocationText.Text = location;
        if (FhdStoryDateText != null) FhdStoryDateText.Text = isVi ? $"{DateTime.Now:dd/MM/yyyy • HH:mm}" : $"{DateTime.Now:MM/dd/yyyy • HH:mm}";
        if (FhdStoryLunarText != null) FhdStoryLunarText.Text = lunar;
        if (FhdStoryTempText != null) FhdStoryTempText.Text = temp;
        if (FhdStoryConditionText != null) FhdStoryConditionText.Text = w.ConditionText;
        if (FhdStoryFeelsLikeText != null) FhdStoryFeelsLikeText.Text = feelsLike;
        if (FhdStoryUvText != null) FhdStoryUvText.Text = uv;
        if (FhdStoryAqiText != null) FhdStoryAqiText.Text = aqi;
        if (FhdStoryRainProbText != null) FhdStoryRainProbText.Text = rain;
        if (FhdStoryWindText != null) FhdStoryWindText.Text = wind;
        if (FhdStoryHumidityText != null) FhdStoryHumidityText.Text = humidity;
        if (FhdStorySunText != null) FhdStorySunText.Text = sun;

        if (o != null)
        {
            if (FhdStoryOotdTitle != null) FhdStoryOotdTitle.Text = isVi ? $"👗 HÔM NAY MẶC GÌ? — {o.OccasionTitle.ToUpper()}" : $"👗 WHAT TO WEAR TODAY? — {o.OccasionTitle.ToUpper()}";
            if (FhdStoryOotdHeadline != null) FhdStoryOotdHeadline.Text = o.Headline;
            if (FhdStoryOotdAccessories != null) FhdStoryOotdAccessories.Text = isVi ? $"🎒 Phụ kiện: {string.Join(" • ", o.Accessories.Select(a => a.Name))}" : $"🎒 Accessories: {string.Join(" • ", o.Accessories.Select(a => a.Name))}";
            if (FhdStoryMotorbikeText != null) FhdStoryMotorbikeText.Text = o.MotorbikeWarning;
        }

        if (ViewModel.DailyForecast != null && ViewModel.DailyForecast.Count >= 3)
        {
            var d1 = ViewModel.DailyForecast[0];
            var d2 = ViewModel.DailyForecast[1];
            var d3 = ViewModel.DailyForecast[2];

            if (FhdStoryD1Day != null) FhdStoryD1Day.Text = d1.DayName;
            if (FhdStoryD1Temp != null) FhdStoryD1Temp.Text = $"{d1.TempMinDisplay} / {d1.TempMaxDisplay}";
            if (FhdStoryD2Day != null) FhdStoryD2Day.Text = d2.DayName;
            if (FhdStoryD2Temp != null) FhdStoryD2Temp.Text = $"{d2.TempMinDisplay} / {d2.TempMaxDisplay}";
            if (FhdStoryD3Day != null) FhdStoryD3Day.Text = d3.DayName;
            if (FhdStoryD3Temp != null) FhdStoryD3Temp.Text = $"{d3.TempMinDisplay} / {d3.TempMaxDisplay}";
        }

        // Hình nền thành phố nếu có
        if (w.HasCityImage && !string.IsNullOrEmpty(w.CityImagePath))
        {
            try
            {
                var bmp = CityBackgroundHelper.LoadOptimizedBitmap(w.CityImagePath);
                if (FhdLandscapeCityImage != null) FhdLandscapeCityImage.Source = bmp;
                if (FhdStoryCityImage != null) FhdStoryCityImage.Source = bmp;
            }
            catch { }
        }
    }

    public async Task RefreshShareCardBitmapAsync(bool? forceStory = null)
    {
        if (_isRenderingShareCard) return;
        _isRenderingShareCard = true;
        try
        {
            if (SharePreviewProgressRing != null)
            {
                SharePreviewProgressRing.Visibility = Visibility.Visible;
                SharePreviewProgressRing.IsActive = true;
            }

            bool isStory = forceStory ?? (ShareFormatStoryRadio?.IsChecked == true);
            if (LandscapeCardContainer != null) LandscapeCardContainer.Visibility = isStory ? Visibility.Collapsed : Visibility.Visible;
            if (StoryCardContainer != null) StoryCardContainer.Visibility = isStory ? Visibility.Visible : Visibility.Collapsed;
            if (SharePreviewContainer != null)
            {
                SharePreviewContainer.Width = isStory ? 1080 : 1920;
                SharePreviewContainer.Height = isStory ? 1920 : 1080;
            }

            UIElement? targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
            int targetWidth = isStory ? 1080 : 1920;
            int targetHeight = isStory ? 1920 : 1080;

            if (targetElement == null) return;

            // Đảm bảo layout 1:1 Full HD không bị co kéo hay scale bởi Viewbox
            targetElement.Measure(new Windows.Foundation.Size(targetWidth, targetHeight));
            targetElement.Arrange(new Windows.Foundation.Rect(0, 0, targetWidth, targetHeight));
            targetElement.UpdateLayout();

            await Task.Delay(150);

            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(targetElement);

            var buffer = await rtb.GetPixelsAsync();
            _cachedSharePixels = buffer.ToArray();
            _cachedShareWidth = (uint)rtb.PixelWidth;
            _cachedShareHeight = (uint)rtb.PixelHeight;

            if (SharePreviewImage != null)
            {
                SharePreviewImage.Source = rtb;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshShareCardBitmapAsync] Error: {ex.Message}");
        }
        finally
        {
            _isRenderingShareCard = false;
            if (SharePreviewProgressRing != null)
            {
                SharePreviewProgressRing.IsActive = false;
                SharePreviewProgressRing.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void CopyFhdShareCardButton_Click(object sender, RoutedEventArgs e)
    {
        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        UIElement targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
        int targetWidth = isStory ? 1080 : 1920;
        int targetHeight = isStory ? 1920 : 1080;

        await ExportCardAsync(targetElement, targetWidth, targetHeight, copyToClipboard: true, isStory: isStory);
    }

    private async void SaveFhdShareCardButton_Click(object sender, RoutedEventArgs e)
    {
        bool isStory = ShareFormatStoryRadio?.IsChecked == true;
        UIElement targetElement = isStory ? StoryCardContainer : LandscapeCardContainer;
        int targetWidth = isStory ? 1080 : 1920;
        int targetHeight = isStory ? 1920 : 1080;

        await ExportCardAsync(targetElement, targetWidth, targetHeight, copyToClipboard: false, isStory: isStory);
    }

    private async Task ExportCardAsync(UIElement element, int width, int height, bool copyToClipboard, bool isStory)
    {
        try
        {
            if (_cachedSharePixels == null || _cachedSharePixels.Length == 0)
            {
                await RefreshShareCardBitmapAsync(isStory);
            }

            if (_cachedSharePixels == null || _cachedSharePixels.Length == 0)
            {
                return;
            }

            uint outWidth = _cachedShareWidth > 0 ? _cachedShareWidth : (uint)width;
            uint outHeight = _cachedShareHeight > 0 ? _cachedShareHeight : (uint)height;

            if (copyToClipboard)
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"WeatherFHD_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                using (var fileStream = File.OpenWrite(tempFile))
                {
                    var randomAccessStream = fileStream.AsRandomAccessStream();
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        outWidth,
                        outHeight,
                        96, 96,
                        _cachedSharePixels);
                    await encoder.FlushAsync();
                }

                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempFile);
                var dataPackage = new DataPackage();
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(storageFile));
                var isVi = LocalizationService.Instance.IsVietnamese;
                dataPackage.SetText(isVi
                    ? $"🌤️ Dự báo thời tiết {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Thẻ Full HD {outWidth}x{outHeight} từ Weather WinUI v3.0.5)"
                    : $"🌤️ Weather Forecast {ViewModel.LocationTitle}: {ViewModel.CurrentWeather?.TemperatureText}, {ViewModel.CurrentWeather?.ConditionText} (Full HD Card {outWidth}x{outHeight} from Weather WinUI v3.0.5)");
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(dataPackage);

                ShareCardDialog?.Hide();
                if (ShareSuccessInfoBar != null)
                {
                    ShareSuccessInfoBar.Title = isVi ? "Đã sao chép ảnh Full HD thành công!" : "Full HD image copied successfully!";
                    ShareSuccessInfoBar.Message = isVi
                        ? $"Ảnh thời tiết {outWidth}x{outHeight} siêu nét đã lưu vào Clipboard. Bạn có thể nhấn Ctrl+V để dán trực tiếp vào Zalo, Facebook, Messenger."
                        : $"Ultra-sharp {outWidth}x{outHeight} weather card copied to Clipboard. You can press Ctrl+V to paste directly into social apps or chats.";
                    ShareSuccessInfoBar.IsOpen = true;
                }
            }
            else
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("PNG Image (*.png)", new List<string> { ".png" });
                savePicker.SuggestedFileName = $"Weather_{ViewModel.LocationTitle.Replace(" ", "_")}_{outWidth}x{outHeight}_{DateTime.Now:yyyyMMdd}";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied,
                            outWidth,
                            outHeight,
                            96, 96,
                            _cachedSharePixels);
                        await encoder.FlushAsync();
                    }

                    ShareCardDialog?.Hide();
                    if (ShareSuccessInfoBar != null)
                    {
                        var isVi = LocalizationService.Instance.IsVietnamese;
                        ShareSuccessInfoBar.Title = isVi ? "Đã lưu ảnh Full HD thành công!" : "Full HD image saved successfully!";
                        ShareSuccessInfoBar.Message = isVi
                            ? $"File ảnh chất lượng cao {outWidth}x{outHeight}px đã lưu tại: {file.Path}."
                            : $"High quality {outWidth}x{outHeight}px image saved to: {file.Path}.";
                        ShareSuccessInfoBar.IsOpen = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExportCardAsync] Error: {ex.Message}");
        }
    }

    #endregion

    #region Changelog Dialog & Version Tracking

    private void CheckAndShowChangelog()
    {
        string currentVersion = "3.0.6";
        string lastSeen = ViewModel.Settings.LastSeenVersion ?? string.Empty;

        if (string.IsNullOrEmpty(lastSeen) || lastSeen != currentVersion)
        {
            ViewModel.Settings.LastSeenVersion = currentVersion;
            ViewModel.ApplySettings();

            _ = Task.Delay(1000).ContinueWith(_ =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await ShowChangelogDialogAsync();
                });
            });
        }
    }

    public async void OpenChangelogDialog()
    {
        await ShowChangelogDialogAsync();
    }

    private async Task ShowChangelogDialogAsync()
    {
        if (this.XamlRoot == null || _isDialogOpen) return;
        _isDialogOpen = true;
        try
        {
            if (ChangelogVersionComboBox != null)
            {
                ChangelogVersionComboBox.SelectedIndex = 0;
            }
            if (ChangelogDialog != null)
            {
                ChangelogDialog.PrimaryButtonText = LocalizationService.Instance.IsVietnamese
                    ? "Bắt đầu trải nghiệm ngay 🚀"
                    : "Get Started Now 🚀";
                ChangelogDialog.XamlRoot = this.XamlRoot;
                await ChangelogDialog.ShowAsync();
            }
        }
        catch { }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void ChangelogVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChangelogVersionComboBox == null) return;
        if (ChangelogVersionComboBox.SelectedItem is ComboBoxItem item && item.Tag is string ver)
        {
            SwitchChangelogVersion(ver);
        }
    }

    private void SwitchChangelogVersion(string versionTag)
    {
        var panels = new[]
        {
            (ChangelogContent_v306, "3.0.6"),
            (ChangelogContent_v305, "3.0.5"),
            (ChangelogContent_v304, "3.0.4"),
            (ChangelogContent_v303, "3.0.3"),
            (ChangelogContent_v302, "3.0.2"),
            (ChangelogContent_v301, "3.0.1"),
            (ChangelogContent_v300, "3.0.0"),
            (ChangelogContent_v223, "2.2.3"),
            (ChangelogContent_v221, "2.2.1"),
            (ChangelogContent_v22, "2.2"),
            (ChangelogContent_v21, "2.1"),
            (ChangelogContent_v20, "2.0"),
            (ChangelogContent_v19, "1.9"),
            (ChangelogContent_v18, "1.8"),
            (ChangelogContent_v17, "1.7"),
            (ChangelogContent_v16, "1.6"),
            (ChangelogContent_v15, "1.5"),
            (ChangelogContent_v14, "1.4"),
            (ChangelogContent_v13, "1.3"),
            (ChangelogContent_v12, "1.2"),
            (ChangelogContent_v11, "1.1"),
            (ChangelogContent_v10, "1.0")
        };

        foreach (var (panel, tag) in panels)
        {
            if (panel != null)
            {
                panel.Visibility = (tag == versionTag) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        var isVi = LocalizationService.Instance.IsVietnamese;
        if (ChangelogHeaderTitle != null)
        {
            ChangelogHeaderTitle.Text = versionTag switch
            {
                "3.0.6" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.6" : "Official Release Notes v3.0.6",
                "3.0.5" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.5" : "Official Release Notes v3.0.5",
                "3.0.4" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.4" : "Official Release Notes v3.0.4",
                "3.0.3" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.3" : "Official Release Notes v3.0.3",
                "3.0.2" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.2" : "Official Release Notes v3.0.2",
                "3.0.1" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.1" : "Official Release Notes v3.0.1",
                "3.0.0" => isVi ? "Chi Tiết Bản Phát Hành Chính Thức v3.0.0" : "Official Release Notes v3.0.0",
                "2.2.3" => isVi ? "Chi Tiết Bản Cập Nhật v2.2.3" : "Release Notes v2.2.3",
                "2.2.1" => isVi ? "Chi Tiết Bản Cập Nhật v2.2.1" : "Release Notes v2.2.1",
                "2.2" => isVi ? "Chi Tiết Bản Cập Nhật v2.2" : "Release Notes v2.2",
                "v2.1" => isVi ? "Chi Tiết Bản Cập Nhật v2.1" : "Release Notes v2.1",
                "v2.0" => isVi ? "Chi Tiết Bản Cập Nhật v2.0" : "Release Notes v2.0",
                "v1.9" => isVi ? "Chi Tiết Bản Cập Nhật v1.9" : "Release Notes v1.9",
                "v1.8" => isVi ? "Chi Tiết Bản Cập Nhật v1.8" : "Release Notes v1.8",
                "v1.7" => isVi ? "Chi Tiết Bản Cập Nhật v1.7" : "Release Notes v1.7",
                _ => isVi ? $"Chi Tiết Bản Cập Nhật {versionTag}" : $"Release Notes {versionTag}"
            };
        }

        if (ChangelogHeaderBadge != null)
        {
            if (versionTag == "3.0.6")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.6 BẢN CHÍNH THỨC" : "v3.0.6 OFFICIAL";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderBadge.Text = isVi ? "v3.0.5 BẢN CHÍNH THỨC" : "v3.0.5 OFFICIAL";
            }
        }

        if (ChangelogHeaderSubtitle != null)
        {
            if (versionTag == "3.0.6")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "Bản cập nhật v3.0.6 ra mắt cấu trúc Bento Grid hiện đại cho tab Tổng quan, hệ thống hoạt họa Fluent Motion nhịp nhàng (kim la bàn xoay ngắn nhất, UV/AQI lướt êm, biểu tượng thở lơ lửng, vật lý vi mô thẻ card) và chuyển tab mượt mà 100% trên Win 10 & 11."
                    : "Version 3.0.6 introduces a modern Bento Grid Overview layout, a comprehensive Fluent Motion animation system (shortest-arc compass, gliding UV/AQI indicators, breathing hero icon, card hover physics) and seamless tab transitions on Win 10 & 11.";
            }
            else if (versionTag == "3.0.5")
            {
                ChangelogHeaderSubtitle.Text = isVi
                    ? "Bản cập nhật v3.0.5 hoàn thiện 100% song ngữ Tiếng Anh toàn bộ ứng dụng, đại tu bố cục giao diện co giãn Responsive chống tràn cắt chữ và đảm bảo độ tương thích hiển thị tuyệt đối trên Windows 10 & 11."
                    : "Version 3.0.5 delivers 100% comprehensive English localization across the entire app, responsive layout scaling overhaul preventing text clipping, and guaranteed visual compatibility on Windows 10 & 11.";
            }
        }
    }

    private void ChangelogDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        sender.Hide();
    }

    private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        sender.Hide();
    }

    #endregion
}
