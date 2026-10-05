using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp;

public sealed partial class OnboardingPage : Page
{
    private readonly SettingsService _settingsService = new();
    private readonly ILocationService _locationService;

    private string _selectedLocationName = "TP. Hồ Chí Minh";
    private double _selectedLatitude = 10.823;
    private double _selectedLongitude = 106.6296;
    private string _selectedTheme = "System";
    private bool _isGpsSelected = false;

    // Cơ sở dữ liệu các thành phố phổ biến có sẵn
    private readonly List<GeocodingItem> _builtInCities = new()
    {
        new GeocodingItem { Name = "TP. Hồ Chí Minh", Country = "Việt Nam", Latitude = 10.823, Longitude = 106.6296 },
        new GeocodingItem { Name = "Hà Nội", Country = "Việt Nam", Latitude = 21.0285, Longitude = 105.8542 },
        new GeocodingItem { Name = "Đà Nẵng", Country = "Việt Nam", Latitude = 16.0544, Longitude = 108.2022 },
        new GeocodingItem { Name = "Đà Lạt", Country = "Việt Nam", Latitude = 11.9404, Longitude = 108.4583 },
        new GeocodingItem { Name = "Nha Trang", Country = "Việt Nam", Latitude = 12.2388, Longitude = 109.1967 },
        new GeocodingItem { Name = "Cần Thơ", Country = "Việt Nam", Latitude = 10.0452, Longitude = 105.7469 },
        new GeocodingItem { Name = "Hải Phòng", Country = "Việt Nam", Latitude = 20.8449, Longitude = 106.6881 },
        new GeocodingItem { Name = "Sa Pa", Country = "Việt Nam", Latitude = 22.3364, Longitude = 103.8438 },
        new GeocodingItem { Name = "Huế", Country = "Việt Nam", Latitude = 16.4637, Longitude = 107.5909 },
        new GeocodingItem { Name = "Vũng Tàu", Country = "Việt Nam", Latitude = 10.346, Longitude = 107.0843 },
        new GeocodingItem { Name = "Tokyo", Country = "Nhật Bản", Latitude = 35.6762, Longitude = 139.6503 },
        new GeocodingItem { Name = "Seoul", Country = "Hàn Quốc", Latitude = 37.5665, Longitude = 126.978 },
        new GeocodingItem { Name = "Singapore", Country = "Singapore", Latitude = 1.3521, Longitude = 103.8198 },
        new GeocodingItem { Name = "Bangkok", Country = "Thái Lan", Latitude = 13.7563, Longitude = 100.5018 },
        new GeocodingItem { Name = "London", Country = "Vương Quốc Anh", Latitude = 51.5074, Longitude = -0.1278 },
        new GeocodingItem { Name = "Paris", Country = "Pháp", Latitude = 48.8566, Longitude = 2.3522 },
        new GeocodingItem { Name = "New York", Country = "Hoa Kỳ", Latitude = 40.7128, Longitude = -74.006 }
    };

    public OnboardingPage()
    {
        InitializeComponent();
        var httpClient = new HttpClient();
        _locationService = new LocationService(httpClient);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSettings();

        // Nạp vị trí cũ nếu có
        if (!string.IsNullOrEmpty(settings.LastLocationName))
        {
            _selectedLocationName = settings.LastLocationName;
            _selectedLatitude = settings.LastLatitude;
            _selectedLongitude = settings.LastLongitude;
            SelectedLocationText.Text = $"Đang chọn: {_selectedLocationName}";
        }
        else
        {
            SelectedLocationText.Text = $"Đang chọn: {_selectedLocationName} (Mặc định)";
        }
        UpdateFooterLocationSummary();

        // Đơn vị
        UnitCelsiusRadio.IsChecked = settings.TemperatureUnit != "F";
        UnitFahrenheitRadio.IsChecked = settings.TemperatureUnit == "F";

        WindKmhRadio.IsChecked = settings.WindSpeedUnit == "kmh";
        WindMsRadio.IsChecked = settings.WindSpeedUnit == "ms";
        WindMphRadio.IsChecked = settings.WindSpeedUnit == "mph";

        PressureHpaRadio.IsChecked = settings.PressureUnit != "mmHg";
        PressureMmHgRadio.IsChecked = settings.PressureUnit == "mmHg";

        // Thời gian
        Time24hRadio.IsChecked = settings.Is24HourFormat;
        Time12hRadio.IsChecked = !settings.Is24HourFormat;

        DateDmyRadio.IsChecked = settings.DateFormat == "dd/MM/yyyy";
        DateMdyRadio.IsChecked = settings.DateFormat == "MM/dd/yyyy";
        DateYmdRadio.IsChecked = settings.DateFormat == "yyyy-MM-dd";

        StartMondayRadio.IsChecked = settings.FirstDayOfWeek != "Sunday";
        StartSundayRadio.IsChecked = settings.FirstDayOfWeek == "Sunday";

        // Múi giờ
        for (int i = 0; i < TimezoneComboBox.Items.Count; i++)
        {
            if (TimezoneComboBox.Items[i] is ComboBoxItem item && (string)item.Tag == settings.TimezoneMode)
            {
                TimezoneComboBox.SelectedIndex = i;
                break;
            }
        }

        // Theme
        _selectedTheme = settings.ThemeMode;
        UpdateThemeButtonsVisual();

        // Lịch trình đi làm & tan ca (v2.0)
        OnboardingCommuteToggle.IsOn = settings.EnableCommuteAlerts;
        if (TimeSpan.TryParse(settings.MorningCommuteTime, out var mTs))
        {
            OnboardingMorningTimePicker.Time = mTs;
        }
        else
        {
            OnboardingMorningTimePicker.Time = new TimeSpan(7, 30, 0);
        }
        if (TimeSpan.TryParse(settings.EveningCommuteTime, out var eTs))
        {
            OnboardingEveningTimePicker.Time = eTs;
        }
        else
        {
            OnboardingEveningTimePicker.Time = new TimeSpan(17, 30, 0);
        }
        for (int i = 0; i < OnboardingLeadTimeComboBox.Items.Count; i++)
        {
            if (OnboardingLeadTimeComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == settings.MorningCommuteLeadMinutes.ToString())
            {
                OnboardingLeadTimeComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void UpdateFooterLocationSummary()
    {
        if (FooterLocationSummaryText != null)
        {
            FooterLocationSummaryText.Text = $"Đang chọn: {_selectedLocationName}";
        }
    }

    private async void GpsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        GpsLocationButton.IsEnabled = false;
        SelectedLocationText.Text = "Đang định vị qua GPS & Mạng...";

        try
        {
            var loc = await _locationService.GetCurrentLocationAsync();
            if (loc != null && loc.IsAutoDetected)
            {
                _selectedLocationName = loc.DisplayName;
                _selectedLatitude = loc.Latitude;
                _selectedLongitude = loc.Longitude;
                _isGpsSelected = true;
                SelectedLocationText.Text = $"✅ Đã định vị ({loc.DetectionSource}): {_selectedLocationName}";
                UpdateFooterLocationSummary();
            }
            else
            {
                _selectedLocationName = loc?.DisplayName ?? "TP. Hồ Chí Minh";
                _selectedLatitude = loc?.Latitude ?? 10.823;
                _selectedLongitude = loc?.Longitude ?? 106.6296;
                _isGpsSelected = false;
                SelectedLocationText.Text = $"📍 Mặc định: {_selectedLocationName} (Vui lòng chọn nút bên dưới nếu bạn ở tỉnh khác)";
                UpdateFooterLocationSummary();
            }
        }
        catch
        {
            SelectedLocationText.Text = "⚠️ Không thể nhận GPS (vui lòng bấm nút chọn nhanh bên dưới hoặc tìm kiếm)";
        }
        finally
        {
            GpsLocationButton.IsEnabled = true;
        }
    }

    private void QuickCity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            _isGpsSelected = false;
            switch (tag)
            {
                case "HCM":
                    _selectedLocationName = "TP. Hồ Chí Minh";
                    _selectedLatitude = 10.823;
                    _selectedLongitude = 106.6296;
                    break;
                case "HN":
                    _selectedLocationName = "Hà Nội";
                    _selectedLatitude = 21.0285;
                    _selectedLongitude = 105.8542;
                    break;
                case "DN":
                    _selectedLocationName = "Đà Nẵng";
                    _selectedLatitude = 16.0544;
                    _selectedLongitude = 108.2022;
                    break;
                case "CT":
                    _selectedLocationName = "Cần Thơ";
                    _selectedLatitude = 10.0452;
                    _selectedLongitude = 105.7469;
                    break;
                case "HP":
                    _selectedLocationName = "Hải Phòng";
                    _selectedLatitude = 20.8449;
                    _selectedLongitude = 106.6881;
                    break;
            }
            SelectedLocationText.Text = $"✅ Đã chọn địa điểm: {_selectedLocationName}";
            UpdateFooterLocationSummary();
        }
    }

    private async void CitySearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string query = sender.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                sender.ItemsSource = null;
                return;
            }

            // 1. Tìm trong cơ sở dữ liệu có sẵn của ứng dụng
            var matches = _builtInCities
                .Where(c => (c.Name != null && c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            (c.Country != null && c.Country.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // 2. Nếu ít hơn 3 kết quả, gọi thêm Geocoding API online
            if (matches.Count < 3 && query.Length >= 2)
            {
                try
                {
                    var onlineResults = await _locationService.SearchLocationsAsync(query);
                    if (onlineResults != null)
                    {
                        foreach (var item in onlineResults)
                        {
                            if (!matches.Any(m => m.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                matches.Add(item);
                            }
                        }
                    }
                }
                catch { }
            }

            sender.ItemsSource = matches;
        }
    }

    private void CitySearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GeocodingItem item)
        {
            _selectedLocationName = $"{item.Name}, {item.Country}";
            _selectedLatitude = item.Latitude;
            _selectedLongitude = item.Longitude;
            _isGpsSelected = false;
            SelectedLocationText.Text = $"✅ Đã chọn địa điểm: {_selectedLocationName}";
            UpdateFooterLocationSummary();
            sender.Text = item.Name;
        }
    }

    private async void CitySearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is GeocodingItem item)
        {
            _selectedLocationName = $"{item.Name}, {item.Country}";
            _selectedLatitude = item.Latitude;
            _selectedLongitude = item.Longitude;
            _isGpsSelected = false;
            SelectedLocationText.Text = $"✅ Đã chọn địa điểm: {_selectedLocationName}";
            UpdateFooterLocationSummary();
        }
        else if (!string.IsNullOrEmpty(args.QueryText))
        {
            var results = await _locationService.SearchLocationsAsync(args.QueryText);
            if (results != null && results.Count > 0)
            {
                var first = results[0];
                _selectedLocationName = $"{first.Name}, {first.Country}";
                _selectedLatitude = first.Latitude;
                _selectedLongitude = first.Longitude;
                _isGpsSelected = false;
                SelectedLocationText.Text = $"✅ Đã chọn địa điểm: {_selectedLocationName}";
                UpdateFooterLocationSummary();
            }
        }
    }

    private void ThemeOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string theme)
        {
            _selectedTheme = theme;
            UpdateThemeButtonsVisual();

            // Áp dụng theme tức thì
            ElementTheme elementTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
            ThemeHelper.SetTheme(elementTheme);
        }
    }

    private void UpdateThemeButtonsVisual()
    {
        ThemeLightButton.BorderThickness = new Thickness(_selectedTheme == "Light" ? 2 : 1);
        ThemeDarkButton.BorderThickness = new Thickness(_selectedTheme == "Dark" ? 2 : 1);
        ThemeSystemButton.BorderThickness = new Thickness(_selectedTheme == "System" ? 2 : 1);
    }

    private void SaveAndContinueButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = _settingsService.LoadSettings();

            // 0. Tên người dùng
            settings.UserName = UserNameBox.Text.Trim();

            // 1. Vị trí
            settings.LastLocationName = _selectedLocationName;
            settings.LastLatitude = _selectedLatitude;
            settings.LastLongitude = _selectedLongitude;
            settings.AutoDetectLocationOnStart = _isGpsSelected;

            // 2. Đơn vị
            settings.TemperatureUnit = UnitFahrenheitRadio.IsChecked == true ? "F" : "C";
            settings.WindSpeedUnit = WindMsRadio.IsChecked == true ? "ms" : (WindMphRadio.IsChecked == true ? "mph" : "kmh");
            settings.PressureUnit = PressureMmHgRadio.IsChecked == true ? "mmHg" : "hPa";

            // 3. Thời gian & Múi giờ
            settings.Is24HourFormat = Time24hRadio.IsChecked == true;
            settings.DateFormat = DateMdyRadio.IsChecked == true ? "MM/dd/yyyy" : (DateYmdRadio.IsChecked == true ? "yyyy-MM-dd" : "dd/MM/yyyy");
            settings.FirstDayOfWeek = StartSundayRadio.IsChecked == true ? "Sunday" : "Monday";
            if (TimezoneComboBox.SelectedItem is ComboBoxItem tzItem && tzItem.Tag is string tz)
            {
                settings.TimezoneMode = tz;
            }

            // 4. Giao diện
            settings.ThemeMode = _selectedTheme;

            // 5. Lịch trình đi làm & tan ca (v2.0)
            settings.EnableCommuteAlerts = OnboardingCommuteToggle.IsOn;
            var morningTs = OnboardingMorningTimePicker.Time;
            settings.MorningCommuteTime = $"{morningTs.Hours:D2}:{morningTs.Minutes:D2}";
            var eveningTs = OnboardingEveningTimePicker.Time;
            settings.EveningCommuteTime = $"{eveningTs.Hours:D2}:{eveningTs.Minutes:D2}";
            if (OnboardingLeadTimeComboBox.SelectedItem is ComboBoxItem leadItem && leadItem.Tag is string leadTag && int.TryParse(leadTag, out int leadMins))
            {
                settings.MorningCommuteLeadMinutes = leadMins;
                settings.EveningCommuteLeadMinutes = leadMins;
            }

            // Đánh dấu đã hoàn thành Onboarding & đánh dấu phiên bản v2.2.3
            settings.HasCompletedOnboarding = true;
            settings.LastSeenVersion = "2.2.3";

            _settingsService.SaveSettings(settings);

            // Chuyển trực tiếp vào trang chính (MainPage) an toàn và tức thì
            if (Frame != null)
            {
                Frame.Navigate(typeof(MainPage));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnboardingPage] Save error: {ex.Message}");
            Frame?.Navigate(typeof(MainPage));
        }
    }

    private void StartAppButton_Click(object sender, RoutedEventArgs e)
    {
        // Chuyển tiếp vào trang chính của ứng dụng
        Frame?.Navigate(typeof(MainPage));
    }
}
