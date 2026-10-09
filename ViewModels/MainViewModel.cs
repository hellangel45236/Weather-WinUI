using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using WeatherApp.Helpers;
using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IWeatherService _weatherService;
    private readonly ILocationService _locationService;
    private readonly IWeatherAdviceService _adviceService;
    private readonly SettingsService _settingsService;

    private double _currentLatitude;
    private double _currentLongitude;
    public double CurrentLatitude => _currentLatitude;
    public double CurrentLongitude => _currentLongitude;
    private OpenMeteoResponse? _rawWeatherData;
    private AirQualityData _lastAirQuality = new();

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private CurrentWeatherDisplay _currentWeather = new();

    [ObservableProperty]
    private ObservableCollection<WeatherAdvice> _adviceList = new();

    [ObservableProperty]
    private string _topAdviceSummary = string.Empty;

    [ObservableProperty]
    private bool _isAdviceExpanded = false;

    public string AdviceExpandButtonText => IsAdviceExpanded ? Loc.AdviceCollapse : Loc.AdviceDetails;
    public string AdviceExpandIconGlyph => IsAdviceExpanded ? "\uf077" : "\uf078";

    [RelayCommand]
    private void ToggleAdviceExpanded()
    {
        IsAdviceExpanded = !IsAdviceExpanded;
        OnPropertyChanged(nameof(AdviceExpandButtonText));
        OnPropertyChanged(nameof(AdviceExpandIconGlyph));
    }

    [ObservableProperty]
    private ObservableCollection<HourlyForecastItem> _hourlyForecast = new();

    [ObservableProperty]
    private ObservableCollection<DailyForecastItem> _dailyForecast = new();

    [ObservableProperty]
    private ObservableCollection<GeocodingItem> _searchResults = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _locationTitle = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _unitButtonText = "°C";

    [ObservableProperty]
    private int _selectedThemeIndex = 2; // 0: Light, 1: Dark, 2: System

    [ObservableProperty]
    private bool _feedbackSubmitted;

    [ObservableProperty]
    private string _feedbackResponseText = string.Empty;

    [ObservableProperty]
    private string _feedbackStatus = string.Empty;

    [ObservableProperty]
    private bool _showInaccurateOptions;

    public string FeedbackButtonLabel => FeedbackSubmitted
        ? (Services.LocalizationService.Instance.IsVietnamese ? "Đã góp ý ✓" : "Feedback Sent ✓")
        : (Services.LocalizationService.Instance.IsVietnamese ? "Góp ý thời tiết" : "Weather Feedback");

    partial void OnFeedbackSubmittedChanged(bool value) => OnPropertyChanged(nameof(FeedbackButtonLabel));

    [ObservableProperty]
    private string _currentTimeDisplay = "--:--:--";

    [ObservableProperty]
    private string _currentDateDisplay = "--";

    [ObservableProperty]
    private string _timeFormatLabel = "24h";

    [ObservableProperty]
    private string _ramUsageDisplay = "42 MB";

    // Tính năng mới v1.7, v1.9 & v2.0
    private readonly LifestyleAdviceService _lifestyleService = new();
    public readonly AmbientSoundService AmbientSoundService = new();
    private readonly CalendarEventService _calendarEventService = new();
    private List<CalendarUserEvent> _allUserEvents = new();
    private List<CalendarDayGoal> _allDailyGoals = new();
    private DateTime _lastMorningCommuteDate = DateTime.MinValue;
    private DateTime _lastEveningCommuteDate = DateTime.MinValue;

    // Tính năng mới v2.1: Gợi ý trang phục thông minh OOTD "Hôm nay mặc gì?"
    private readonly OutfitAdvisorService _outfitService = new();
    private OpenMeteoResponse? _lastRawWeatherData;

    [ObservableProperty]
    private OutfitAdvice? _currentOutfitAdvice;

    [ObservableProperty]
    private string _selectedOutfitOccasion = "Work"; // "Work", "School", "Casual", "Sport", "Travel"

    [ObservableProperty]
    private string _selectedOutfitGender = "All"; // "All", "Men", "Women"

    [ObservableProperty]
    private ObservableCollection<WorkoutWindowItem> _workoutWindows = new();

    [ObservableProperty]
    private SkinDefenseModel? _skinDefense = new();

    [ObservableProperty]
    private bool _isOotdCopiedOpen = false;

    [ObservableProperty]
    private string _ootdCopiedMessage = string.Empty;

    public bool IsWorkOccasionSelected => SelectedOutfitOccasion == "Work";
    public bool IsSchoolOccasionSelected => SelectedOutfitOccasion == "School";
    public bool IsCasualOccasionSelected => SelectedOutfitOccasion == "Casual";
    public bool IsSportOccasionSelected => SelectedOutfitOccasion == "Sport";
    public bool IsTravelOccasionSelected => SelectedOutfitOccasion == "Travel";

    public bool IsGenderAllSelected => SelectedOutfitGender == "All";
    public bool IsGenderMenSelected => SelectedOutfitGender == "Men";
    public bool IsGenderWomenSelected => SelectedOutfitGender == "Women";

    // Tính năng mới v3.0.1: Kiểm tra cập nhật GitHub & Quản lý tiết kiệm pin Laptop
    private readonly UpdateCheckService _updateCheckService = new();
    private readonly PowerManagementService _powerService = new();

    [ObservableProperty]
    private string _appVersionDisplay = "v3.0.6 Official";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GpsButtonBackground))]
    [NotifyPropertyChangedFor(nameof(GpsButtonBorderBrush))]
    [NotifyPropertyChangedFor(nameof(GpsButtonFontWeight))]
    private bool _isGpsLocationSelected = true;

    public string GpsButtonBackground => IsGpsLocationSelected ? "#3538BDF8" : "#1AFFFFFF";
    public string GpsButtonBorderBrush => IsGpsLocationSelected ? "#38BDF8" : "#3038BDF8";
    public string GpsButtonFontWeight => IsGpsLocationSelected ? "Bold" : "SemiBold";

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private string _updateCheckStatusText = string.Empty;

    [ObservableProperty]
    private bool _hasAvailableUpdate;

    [ObservableProperty]
    private string _latestVersionText = string.Empty;

    [ObservableProperty]
    private string _latestReleaseTitle = string.Empty;

    [ObservableProperty]
    private string _latestReleaseUrl = "https://github.com/hellangel45236/Weather-WinUI/releases";

    [ObservableProperty]
    private string _latestDownloadUrl = string.Empty;

    [ObservableProperty]
    private string _latestChangelog = string.Empty;

    [ObservableProperty]
    private bool _isBatterySavingActive;

    // Tính năng mới v2.2.3: Cảnh báo ngập úng & Triều cường đô thị (Urban Flood & Tide Alert)
    private readonly UrbanFloodService _floodService = new();

    [ObservableProperty]
    private UrbanFloodWarning _urbanFloodWarning = new();

    [ObservableProperty]
    private ObservableCollection<FloodHotspotRoad> _displayedFloodRoads = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableFloodDistricts = new();

    [ObservableProperty]
    private string _selectedFloodDistrict = "Tất cả";

    [ObservableProperty]
    private bool _isFloodRoadsExpanded = false;

    public LocalizationService Loc => LocalizationService.Instance;

    public string FloodRoadsExpandButtonText => IsFloodRoadsExpanded ? Loc.CollapseRoads : Loc.ExpandRoads;
    public string FloodRoadsExpandIconGlyph => IsFloodRoadsExpanded ? "\uf077" : "\uf078";

    [ObservableProperty]
    private ObservableCollection<FavoriteLocationItem> _favoriteLocations = new();

    [ObservableProperty]
    private ObservableCollection<QuickLocationChipItem> _quickChips = new();

    [ObservableProperty]
    private string _calendarLocationSubtitle = string.Empty;

    public static readonly (string Name, double Lat, double Lon)[] PopularCityPresets = new[]
    {
        ("Hà Nội", 21.0285, 105.8542),
        ("TP. Hồ Chí Minh", 10.8230, 106.6296),
        ("Đà Nẵng", 16.0544, 108.2022),
        ("Đà Lạt", 11.9404, 108.4583),
        ("Nha Trang", 12.2388, 109.1967),
        ("Hải Phòng", 20.8449, 106.6881),
        ("Cần Thơ", 10.0452, 105.7469),
        ("Sa Pa", 22.3364, 103.8438),
        ("Huế", 16.4637, 107.5909),
        ("Vũng Tàu", 10.3460, 107.0843),
        ("Quy Nhơn", 13.7830, 109.2197),
        ("Phú Quốc", 10.2899, 103.9840)
    };

    [ObservableProperty]
    private bool _isCurrentFavorite;

    [ObservableProperty]
    private ObservableCollection<LifestyleIndexItem> _lifestyleIndices = new();

    [ObservableProperty]
    private bool _isAmbientSoundPlaying;

    public string FavoriteIconGlyph => IsCurrentFavorite ? "\uf004" : "\uf08a"; // Heart solid vs regular
    public string FavoriteButtonToolTip => IsCurrentFavorite 
        ? (Loc.IsVietnamese ? "Bỏ yêu thích địa điểm này" : "Remove from favorites") 
        : (Loc.IsVietnamese ? "Lưu địa điểm này vào danh sách yêu thích" : "Add to favorite locations");

    private readonly DispatcherTimer _clockTimer = new();
    private readonly DispatcherTimer _autoRefreshTimer = new();
    private readonly DispatcherTimer _ramMonitorTimer = new();

    public MainViewModel(
        IWeatherService weatherService, 
        ILocationService locationService, 
        IWeatherAdviceService adviceService,
        SettingsService? settingsService = null)
    {
        _weatherService = weatherService;
        _locationService = locationService;
        _adviceService = adviceService;
        _settingsService = settingsService ?? new SettingsService();

        // Nạp cài đặt người dùng
        _settings = _settingsService.LoadSettings();
        if (!string.IsNullOrEmpty(_settings.AppLanguage))
        {
            LocalizationService.Instance.CurrentLanguage = _settings.AppLanguage;
        }
        _topAdviceSummary = Loc.AdviceEmpty;
        _locationTitle = Loc.FindingLocation;
        LocalizationService.Instance.LanguageChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Loc));
            OnPropertyChanged(nameof(WelcomeGreetingText));
            UpdateTopAdviceSummary();
            OnPropertyChanged(nameof(TopAdviceSummary));
            if (LocationTitle == "Đang tìm vị trí..." || LocationTitle == "Locating position...")
            {
                LocationTitle = Loc.FindingLocation;
            }
            OnPropertyChanged(nameof(AdviceExpandButtonText));
            OnPropertyChanged(nameof(UnitButtonText));
            OnPropertyChanged(nameof(FloodRoadsExpandButtonText));
            OnPropertyChanged(nameof(UrbanFloodWarning));
            OnPropertyChanged(nameof(FavoriteButtonToolTip));
            OnPropertyChanged(nameof(AmbientSoundButtonText));
            OnPropertyChanged(nameof(AmbientSoundButtonToolTip));
            OnPropertyChanged(nameof(FeedbackButtonLabel));
            UpdateClock();
            UpdateQuickChips();
            if (_rawWeatherData != null)
            {
                UpdateFloodWarningData(_rawWeatherData, LocationTitle);
            }
            UpdateDisplayedFloodRoads();
            UpdateAvailableMonths();
            GenerateCalendar();
            if (_rawWeatherData != null)
            {
                UpdateDisplaysFromRawData(_rawWeatherData, LocationTitle);
            }
        };

        SyncSettingsToProperties();

        // Nạp danh sách địa điểm yêu thích
        FavoriteLocations.Clear();
        foreach (var fav in _settings.FavoriteLocations)
        {
            FavoriteLocations.Add(fav);
        }
        UpdateQuickChips();

        StartClock();
        SetupAutoRefreshTimer();
        StartRamMonitor();
        InitializeCalendar();

        try
        {
            var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _powerService.PowerSavingStateChanged += (s, active) =>
            {
                dq?.TryEnqueue(() =>
                {
                    IsBatterySavingActive = active;
                    ApplyPowerSavingOptimization();
                });
            };
            IsBatterySavingActive = _powerService.IsPowerSavingActive;
            ApplyPowerSavingOptimization();
        }
        catch { }

        // Tự động kiểm tra bản cập nhật mới khi mở ứng dụng nếu được bật
        if (_settings.AutoCheckForUpdates)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(4000);
                var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                dq?.TryEnqueue(async () =>
                {
                    await CheckForUpdatesAsync(isManual: false);
                });
            });
        }
    }

    public void SyncSettingsToProperties()
    {
        UnitButtonText = Settings.TemperatureUnit == "F" ? "°F" : "°C";
        SelectedThemeIndex = Settings.ThemeMode switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => 2
        };
        if (!string.IsNullOrWhiteSpace(Settings.SelectedOutfitOccasion))
        {
            SelectedOutfitOccasion = Settings.SelectedOutfitOccasion;
        }
        if (!string.IsNullOrWhiteSpace(Settings.SelectedOutfitGender))
        {
            SelectedOutfitGender = Settings.SelectedOutfitGender;
        }
    }

    public void UpdateOutfitAdvice()
    {
        if (CurrentWeather == null) return;
        try
        {
            CurrentOutfitAdvice = _outfitService.GenerateOutfitAdvice(CurrentWeather, _lastRawWeatherData, SelectedOutfitOccasion, SelectedOutfitGender);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] UpdateOutfitAdvice error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void SelectOutfitOccasion(string occasion)
    {
        if (string.IsNullOrWhiteSpace(occasion)) return;
        SelectedOutfitOccasion = occasion;
        Settings.SelectedOutfitOccasion = occasion;
        _settingsService.SaveSettings(Settings);
        OnPropertyChanged(nameof(IsWorkOccasionSelected));
        OnPropertyChanged(nameof(IsSchoolOccasionSelected));
        OnPropertyChanged(nameof(IsCasualOccasionSelected));
        OnPropertyChanged(nameof(IsSportOccasionSelected));
        OnPropertyChanged(nameof(IsTravelOccasionSelected));
        UpdateOutfitAdvice();
    }

    [RelayCommand]
    public void SelectOutfitGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return;
        SelectedOutfitGender = gender;
        Settings.SelectedOutfitGender = gender;
        _settingsService.SaveSettings(Settings);
        OnPropertyChanged(nameof(IsGenderAllSelected));
        OnPropertyChanged(nameof(IsGenderMenSelected));
        OnPropertyChanged(nameof(IsGenderWomenSelected));
        UpdateOutfitAdvice();
    }

    [RelayCommand]
    public void CopyOotdAdvice()
    {
        if (CurrentOutfitAdvice == null) return;
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            string textToCopy = !string.IsNullOrWhiteSpace(CurrentOutfitAdvice.ShareableSummaryText)
                ? CurrentOutfitAdvice.ShareableSummaryText
                : $"{CurrentOutfitAdvice.Headline} - {CurrentOutfitAdvice.ThermalComfortNotice}";
            dataPackage.SetText(textToCopy);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            OotdCopiedMessage = LocalizationService.Instance.IsVietnamese
                ? "Đã sao chép gợi ý OOTD vào Clipboard!"
                : "OOTD advice copied to clipboard!";
            IsOotdCopiedOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] CopyOotdAdvice error: {ex.Message}");
        }
    }

    // ==================== v3.0 BETA: 24H INTERACTIVE TIME SCRUBBER ====================
    [ObservableProperty]
    private bool _isTimeScrubbingActive = false;

    [ObservableProperty]
    private double _scrubbedSliderValue = 0;

    [ObservableProperty]
    private string _scrubbedTimeLabel = string.Empty;

    [ObservableProperty]
    private string _scrubbedTempText = string.Empty;

    [ObservableProperty]
    private string _scrubbedConditionText = string.Empty;

    [ObservableProperty]
    private string _scrubbedRainText = string.Empty;

    [ObservableProperty]
    private string _scrubbedGradientStart = "#1E293B";

    [ObservableProperty]
    private string _scrubbedGradientEnd = "#0F172A";

    [ObservableProperty]
    private string _scrubbedIconGlyph = "\uf185";

    [ObservableProperty]
    private string _scrubbedSvgIconPath = "ms-appx:///Assets/weather-icons-main/production/fill/svg/clear-day.svg";

    public void ScrubToHour(int hour)
    {
        if (HourlyForecast == null || HourlyForecast.Count == 0) return;
        var item = HourlyForecast.FirstOrDefault(h => h.HourNumber == hour) ?? HourlyForecast.FirstOrDefault();
        if (item == null) return;

        IsTimeScrubbingActive = true;
        ScrubbedSliderValue = hour;
        ScrubbedTimeLabel = $"{hour:D2}:00 ({item.TimeDisplay})";
        ScrubbedTempText = item.TempDisplay;
        ScrubbedConditionText = item.ConditionText;
        ScrubbedRainText = item.RainProbabilityText;
        ScrubbedIconGlyph = item.IconGlyph;
        ScrubbedSvgIconPath = item.SvgIconPath;

        // Dynamic Chromatic Sky colors based on hour & condition
        if (hour >= 5 && hour < 7)
        {
            ScrubbedGradientStart = "#D97706"; // Rạng đông cam đào
            ScrubbedGradientEnd = "#7C3AED";   // Tím rạng đông
        }
        else if (hour >= 7 && hour < 16)
        {
            ScrubbedGradientStart = "#0284C7"; // Ban ngày xanh biếc
            ScrubbedGradientEnd = "#0369A1";
        }
        else if (hour >= 16 && hour < 19)
        {
            ScrubbedGradientStart = "#EA580C"; // Hoàng hôn hổ phách
            ScrubbedGradientEnd = "#4C1D95";   // Tím chiều
        }
        else
        {
            ScrubbedGradientStart = "#0F172A"; // Đêm tím than
            ScrubbedGradientEnd = "#1E1B4B";   // Đêm huyền ảo
        }
    }

    [RelayCommand]
    public void ResetTimeScrubbing()
    {
        IsTimeScrubbingActive = false;
        ScrubbedSliderValue = DateTime.Now.Hour;
    }

    // ==================== v3.0.6: DEEP-DIVE METRIC DETAIL MODAL POPUP ====================
    [ObservableProperty]
    private WeatherMetricDetailPopupData? _selectedMetricDetail;

    [ObservableProperty]
    private bool _isMetricDetailOpen = false;

    [RelayCommand]
    public void OpenMetricDetail(string metricKey)
    {
        if (Enum.TryParse<WeatherMetricType>(metricKey, true, out var type))
        {
            SelectedMetricDetail = WeatherMetricDetailService.Instance.GenerateDetailData(
                type,
                CurrentWeather,
                _rawWeatherData,
                _lastAirQuality,
                Settings,
                LocalizationService.Instance);
            IsMetricDetailOpen = true;
        }
    }

    [RelayCommand]
    public void CloseMetricDetail()
    {
        IsMetricDetailOpen = false;
    }

    // ==================== v3.0 BETA: FLUENT NAVIGATION STATE ====================
    [ObservableProperty]
    private string _currentNavTag = "overview";

    public bool IsOverviewVisible => CurrentNavTag == "overview";
    public bool IsFloodVisible => CurrentNavTag == "flood";
    public bool IsRadarVisible => CurrentNavTag == "radar";
    public bool IsLifestyleVisible => CurrentNavTag == "lifestyle";
    public bool IsCalendarVisible => CurrentNavTag == "calendar";
    public bool IsWidgetVisible => CurrentNavTag == "widget";
    public bool IsSettingsVisible => CurrentNavTag == "settings";

    public void SwitchNav(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        CurrentNavTag = tag.ToLowerInvariant();
        OnPropertyChanged(nameof(IsOverviewVisible));
        OnPropertyChanged(nameof(IsFloodVisible));
        OnPropertyChanged(nameof(IsRadarVisible));
        OnPropertyChanged(nameof(IsLifestyleVisible));
        OnPropertyChanged(nameof(IsCalendarVisible));
        OnPropertyChanged(nameof(IsWidgetVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
    }


    [RelayCommand]

    public void ToggleFloodRoadsExpanded()
    {
        IsFloodRoadsExpanded = !IsFloodRoadsExpanded;
        OnPropertyChanged(nameof(FloodRoadsExpandButtonText));
        OnPropertyChanged(nameof(FloodRoadsExpandIconGlyph));
        UpdateDisplayedFloodRoads();
    }

    public void FilterFloodRoadsByDistrict(string district)
    {
        SelectedFloodDistrict = district;
        UpdateDisplayedFloodRoads();
    }

    public void UpdateFloodWarningData(OpenMeteoResponse? data, string locationName)
    {
        if (data == null) return;
        try
        {
            UrbanFloodWarning = _floodService.EvaluateFloodRisk(_currentLatitude, _currentLongitude, locationName, data);

            // Cập nhật danh sách các quận
            AvailableFloodDistricts.Clear();
            string allDistrictLabel = LocalizationService.Instance.IsVietnamese ? "Tất cả" : "All";
            AvailableFloodDistricts.Add(allDistrictLabel);
            if (UrbanFloodWarning?.HotspotRoads != null)
            {
                var districts = UrbanFloodWarning.HotspotRoads
                    .Select(r => r.District)
                    .Distinct()
                    .OrderBy(d => d);
                foreach (var d in districts)
                {
                    AvailableFloodDistricts.Add(d);
                }
            }
            SelectedFloodDistrict = allDistrictLabel;
            UpdateDisplayedFloodRoads();

            // Kích hoạt thông báo đẩy nếu nguy cơ ngập hoặc triều cường cao
            if (Settings.EnableToastNotifications && UrbanFloodWarning != null && UrbanFloodWarning.RiskLevel >= 2)
            {
                NotificationService.TriggerFloodWarningToast(UrbanFloodWarning, locationName);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] UpdateFloodWarningData error: {ex.Message}");
        }
    }

    [ObservableProperty]
    private string _floodStreetSearchQuery = string.Empty;

    partial void OnFloodStreetSearchQueryChanged(string value)
    {
        UpdateDisplayedFloodRoads();
    }

    private void UpdateDisplayedFloodRoads()
    {
        DisplayedFloodRoads.Clear();
        if (UrbanFloodWarning?.HotspotRoads == null) return;

        var query = UrbanFloodWarning.HotspotRoads.AsEnumerable();
        if (!string.IsNullOrEmpty(SelectedFloodDistrict) && SelectedFloodDistrict != "Tất cả" && SelectedFloodDistrict != "All")
        {
            query = query.Where(r => r.District == SelectedFloodDistrict);
        }

        if (!string.IsNullOrWhiteSpace(FloodStreetSearchQuery))
        {
            string q = FloodStreetSearchQuery.Trim().ToLowerInvariant();
            query = query.Where(r => r.StreetName.ToLowerInvariant().Contains(q) || r.District.ToLowerInvariant().Contains(q));
        }
        else if (!IsFloodRoadsExpanded)
        {
            query = query.Take(6);
        }

        foreach (var road in query)
        {
            DisplayedFloodRoads.Add(road);
        }
    }

    private void StartClock()
    {
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private void StartRamMonitor()
    {
        _ramMonitorTimer.Interval = TimeSpan.FromSeconds(15);
        _ramMonitorTimer.Tick += (s, e) =>
        {
            MemoryOptimizer.TrimMemory();
            UpdateRamUsage();
        };
        _ramMonitorTimer.Start();
        MemoryOptimizer.TrimMemory();
        UpdateRamUsage();
    }

    public void UpdateRamUsage()
    {
        try
        {
            long bytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            double mb = bytes / (1024.0 * 1024.0);
            RamUsageDisplay = LocalizationService.Instance.IsVietnamese ? $"{mb:F1} MB (Tối ưu)" : $"{mb:F1} MB (Optimized)";
        }
        catch { }
    }

    private void SetupAutoRefreshTimer()
    {
        _autoRefreshTimer.Stop();
        if (Settings.AutoRefreshIntervalMinutes > 0)
        {
            _autoRefreshTimer.Interval = TimeSpan.FromMinutes(Settings.AutoRefreshIntervalMinutes);
            _autoRefreshTimer.Tick += async (s, e) =>
            {
                if (!IsLoading)
                {
                    await RefreshAsync();
                }
            };
            _autoRefreshTimer.Start();
        }
    }

    private void UpdateClock()
    {
        DateTime now;
        if (Settings.TimezoneMode == "GMT+7")
            now = DateTime.UtcNow.AddHours(7);
        else if (Settings.TimezoneMode == "GMT+8")
            now = DateTime.UtcNow.AddHours(8);
        else if (Settings.TimezoneMode == "GMT+9")
            now = DateTime.UtcNow.AddHours(9);
        else if (Settings.TimezoneMode == "GMT+0")
            now = DateTime.UtcNow;
        else if (Settings.TimezoneMode == "GMT-5")
            now = DateTime.UtcNow.AddHours(-5);
        else
            now = DateTime.Now;

        if (Settings.Is24HourFormat)
        {
            CurrentTimeDisplay = now.ToString("HH:mm:ss");
            TimeFormatLabel = "24h";
        }
        else
        {
            CurrentTimeDisplay = now.ToString("hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
            TimeFormatLabel = "12h";
        }

        string dayOfWeekStr = Loc.IsVietnamese ? (now.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => now.ToString("dddd")
        }) : now.ToString("dddd", System.Globalization.CultureInfo.InvariantCulture);

        string formattedDate = Settings.DateFormat switch
        {
            "MM/dd/yyyy" => now.ToString("MM/dd/yyyy"),
            "yyyy-MM-dd" => now.ToString("yyyy-MM-dd"),
            _ => now.ToString("dd/MM/yyyy")
        };

        CurrentDateDisplay = $"{dayOfWeekStr}, {formattedDate}";

        if (now.Second == 0)
        {
            CheckEventReminders(now);
            CheckCommuteAlerts(now);
        }
    }

    private void CheckEventReminders(DateTime now)
    {
        if (_allUserEvents == null || _allUserEvents.Count == 0) return;

        foreach (var ev in _allUserEvents.Where(e => e.Date.Date == now.Date && !e.IsReminded && e.ReminderMinutesBefore >= 0))
        {
            try
            {
                if (TimeSpan.TryParse(ev.EventTime, out TimeSpan eventTime))
                {
                    DateTime targetAlertTime = now.Date.Add(eventTime).AddMinutes(-ev.ReminderMinutesBefore);
                    if (now >= targetAlertTime && now < targetAlertTime.AddMinutes(5))
                    {
                        ev.IsReminded = true;
                        _calendarEventService.UpdateEvent(ev);

                        string weatherInfo = CurrentWeather != null 
                            ? $"{CurrentWeather.TemperatureText} - {CurrentWeather.ConditionText}" 
                            : "";
                        string advice = ev.IsOutdoor ? "⚠️ Hoạt động ngoài trời: chú ý thời tiết trước khi xuất phát!" : "";

                        NotificationService.ShowEventReminderToast(ev.Title, ev.TimeDisplay, ev.Category, weatherInfo, advice);
                    }
                }
            }
            catch { }
        }
    }

    private void CheckCommuteAlerts(DateTime now)
    {
        if (!Settings.EnableCommuteAlerts) return;

        bool isCommuteDay = now.DayOfWeek switch
        {
            DayOfWeek.Monday => Settings.CommuteMon,
            DayOfWeek.Tuesday => Settings.CommuteTue,
            DayOfWeek.Wednesday => Settings.CommuteWed,
            DayOfWeek.Thursday => Settings.CommuteThu,
            DayOfWeek.Friday => Settings.CommuteFri,
            DayOfWeek.Saturday => Settings.CommuteSat,
            DayOfWeek.Sunday => Settings.CommuteSun,
            _ => false
        };
        if (!isCommuteDay) return;

        // 1. Giờ đi sáng
        if (_lastMorningCommuteDate.Date != now.Date && TimeSpan.TryParse(Settings.MorningCommuteTime, out TimeSpan morningTime))
        {
            TimeSpan lead = TimeSpan.FromMinutes(Math.Max(5, Settings.MorningCommuteLeadMinutes));
            TimeSpan alertTime = morningTime - lead;
            if (now.TimeOfDay >= alertTime && now.TimeOfDay < alertTime.Add(TimeSpan.FromMinutes(5)))
            {
                _lastMorningCommuteDate = now.Date;
                TriggerCommuteToast("Đi Làm / Đi Học", Settings.MorningCommuteTime, morningTime.Hours);
            }
        }

        // 2. Giờ tan ca chiều
        if (_lastEveningCommuteDate.Date != now.Date && TimeSpan.TryParse(Settings.EveningCommuteTime, out TimeSpan eveningTime))
        {
            TimeSpan lead = TimeSpan.FromMinutes(Math.Max(5, Settings.EveningCommuteLeadMinutes));
            TimeSpan alertTime = eveningTime - lead;
            if (now.TimeOfDay >= alertTime && now.TimeOfDay < alertTime.Add(TimeSpan.FromMinutes(5)))
            {
                _lastEveningCommuteDate = now.Date;
                TriggerCommuteToast("Tan Ca Về Nhà", Settings.EveningCommuteTime, eveningTime.Hours);
            }
        }
    }

    public void TriggerCommuteToast(string commuteType, string timeText, int targetHour)
    {
        string condition = CurrentWeather?.ConditionText ?? "Thời tiết ổn định";
        string temp = CurrentWeather?.TemperatureText ?? "28°C";
        string advice = "Thuận lợi di chuyển, chúc bạn một chuyến đi an toàn!";

        if (HourlyForecast != null && HourlyForecast.Count > 0)
        {
            var matchHour = HourlyForecast.FirstOrDefault(h => h.TimeDisplay.StartsWith($"{targetHour:D2}:") || h.TimeDisplay.StartsWith($"{targetHour}:"));
            if (matchHour != null)
            {
                condition = matchHour.ConditionText;
                temp = matchHour.TempDisplay;
            }
        }

        if (condition.Contains("mưa", StringComparison.OrdinalIgnoreCase) || condition.Contains("dông", StringComparison.OrdinalIgnoreCase))
        {
            advice = "Dự báo có mưa, nhớ chuẩn bị sẵn áo mưa hoặc ô che bạn nhé!";
        }
        else if (temp.Contains("3") && int.TryParse(temp.Replace("°", "").Replace("C", "").Trim(), out int t) && t >= 35)
        {
            advice = "Trời nắng nóng gay gắt, nhớ trang bị áo khoác chống nắng và kính râm!";
        }

        NotificationService.ShowCommuteToast(commuteType, timeText, condition, temp, advice);
    }

    [RelayCommand]
    public void TestCommuteNotification()
    {
        TriggerCommuteToast("Đi Làm / Đi Học", Settings.MorningCommuteTime, 8);
    }

    [RelayCommand]
    public void ToggleTimeFormat()
    {
        Settings.Is24HourFormat = !Settings.Is24HourFormat;
        _settingsService.SaveSettings(Settings);
        UpdateClock();
        if (_rawWeatherData != null)
        {
            UpdateDisplaysFromRawData(_rawWeatherData, LocationTitle);
        }
    }

    public async Task InitializeAsync()
    {
        // Áp dụng theme lưu trữ
        ChangeTheme(Settings.ThemeMode);

        if (!Settings.AutoDetectLocationOnStart && Settings.LastLatitude != 0 && !string.IsNullOrEmpty(Settings.LastLocationName))
        {
            _currentLatitude = Settings.LastLatitude;
            _currentLongitude = Settings.LastLongitude;
            LocationTitle = Settings.LastLocationName;
            await FetchWeatherForCoordinatesAsync(_currentLatitude, _currentLongitude, LocationTitle);
        }
        else
        {
            await LoadCurrentLocationWeatherAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (_currentLatitude != 0 || _currentLongitude != 0)
        {
            await FetchWeatherForCoordinatesAsync(_currentLatitude, _currentLongitude, LocationTitle);
        }
        else
        {
            await LoadCurrentLocationWeatherAsync();
        }
    }

    [RelayCommand]
    public async Task LoadCurrentLocationWeatherAsync()
    {
        IsGpsLocationSelected = true;
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        LocationTitle = "Đang xác định vị trí...";

        try
        {
            var location = await _locationService.GetCurrentLocationAsync();
            _currentLatitude = location.Latitude;
            _currentLongitude = location.Longitude;
            string city = string.IsNullOrWhiteSpace(location.City) ? "Vị trí của bạn" : location.DisplayName;
            LocationTitle = city;

            Settings.LastLatitude = location.Latitude;
            Settings.LastLongitude = location.Longitude;
            Settings.LastLocationName = city;
            _settingsService.SaveSettings(Settings);

            await FetchWeatherForCoordinatesAsync(location.Latitude, location.Longitude, city);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = Loc.IsVietnamese 
                ? $"Không thể xác định vị trí tự động: {ex.Message}" 
                : $"Cannot determine location automatically: {ex.Message}";
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchLocationsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        var results = await _locationService.SearchLocationsAsync(query);
        SearchResults.Clear();
        foreach (var item in results)
        {
            SearchResults.Add(item);
        }
    }

    [RelayCommand]
    public async Task SelectLocationAsync(GeocodingItem item)
    {
        if (item == null) return;

        SearchResults.Clear();
        SearchText = string.Empty;
        IsGpsLocationSelected = false;

        string displayName = item.DisplayText;
        LocationTitle = displayName;
        _currentLatitude = item.Latitude;
        _currentLongitude = item.Longitude;

        Settings.LastLatitude = item.Latitude;
        Settings.LastLongitude = item.Longitude;
        Settings.LastLocationName = displayName;
        _settingsService.SaveSettings(Settings);

        await FetchWeatherForCoordinatesAsync(item.Latitude, item.Longitude, displayName);
    }

    [RelayCommand]
    public async Task SelectQuickCityAsync(string cityData)
    {
        var parts = cityData.Split('|');
        if (parts.Length == 3 && 
            double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
            double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
        {
            string cityName = parts[0];
            LocationTitle = cityName;
            _currentLatitude = lat;
            _currentLongitude = lon;

            Settings.LastLatitude = lat;
            Settings.LastLongitude = lon;
            Settings.LastLocationName = cityName;
            _settingsService.SaveSettings(Settings);

            await FetchWeatherForCoordinatesAsync(lat, lon, cityName);
        }
    }

    [RelayCommand]
    public void ToggleFavoriteCurrentLocation()
    {
        if (string.IsNullOrWhiteSpace(LocationTitle) || _currentLatitude == 0) return;

        var existing = Settings.FavoriteLocations.FirstOrDefault(f => 
            f.Name.Equals(LocationTitle, StringComparison.OrdinalIgnoreCase) ||
            (Math.Abs(f.Latitude - _currentLatitude) < 0.05 && Math.Abs(f.Longitude - _currentLongitude) < 0.05));

        if (existing != null)
        {
            Settings.FavoriteLocations.Remove(existing);
            FavoriteLocations.Remove(existing);
            IsCurrentFavorite = false;
        }
        else
        {
            var newFav = new FavoriteLocationItem
            {
                Name = LocationTitle,
                Country = "Việt Nam",
                Latitude = _currentLatitude,
                Longitude = _currentLongitude
            };
            Settings.FavoriteLocations.Add(newFav);
            FavoriteLocations.Add(newFav);
            IsCurrentFavorite = true;
        }

        _settingsService.SaveSettings(Settings);
        UpdateIsCurrentFavorite();
        UpdateQuickChips();
        OnPropertyChanged(nameof(FavoriteIconGlyph));
        OnPropertyChanged(nameof(FavoriteButtonToolTip));
    }

    [RelayCommand]
    public async Task SelectFavoriteLocationAsync(FavoriteLocationItem item)
    {
        if (item == null) return;
        IsGpsLocationSelected = false;
        _currentLatitude = item.Latitude;
        _currentLongitude = item.Longitude;
        LocationTitle = item.Name;
        Settings.LastLatitude = item.Latitude;
        Settings.LastLongitude = item.Longitude;
        Settings.LastLocationName = item.Name;
        _settingsService.SaveSettings(Settings);
        UpdateIsCurrentFavorite();
        UpdateQuickChips();
        await FetchWeatherForCoordinatesAsync(item.Latitude, item.Longitude, item.Name);
    }

    [RelayCommand]
    public void RemoveFavoriteLocation(FavoriteLocationItem item)
    {
        if (item == null) return;
        Settings.FavoriteLocations.Remove(item);
        FavoriteLocations.Remove(item);
        _settingsService.SaveSettings(Settings);
        UpdateIsCurrentFavorite();
        UpdateQuickChips();
    }

    public void UpdateIsCurrentFavorite()
    {
        if (string.IsNullOrWhiteSpace(LocationTitle))
        {
            IsCurrentFavorite = false;
        }
        else
        {
            IsCurrentFavorite = Settings.FavoriteLocations.Any(f => 
                f.Name.Equals(LocationTitle, StringComparison.OrdinalIgnoreCase) ||
                (Math.Abs(f.Latitude - _currentLatitude) < 0.05 && Math.Abs(f.Longitude - _currentLongitude) < 0.05));
        }

        OnPropertyChanged(nameof(FavoriteIconGlyph));
        OnPropertyChanged(nameof(FavoriteButtonToolTip));
    }

    public void UpdateQuickChips()
    {
        QuickChips.Clear();
        var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Thêm các địa điểm yêu thích của người dùng trước (❤️)
        foreach (var fav in FavoriteLocations)
        {
            bool isSelected = !string.IsNullOrEmpty(LocationTitle) &&
                (fav.Name.Equals(LocationTitle, StringComparison.OrdinalIgnoreCase) ||
                 (Math.Abs(fav.Latitude - _currentLatitude) < 0.05 && Math.Abs(fav.Longitude - _currentLongitude) < 0.05));

            QuickChips.Add(new QuickLocationChipItem
            {
                Name = fav.Name,
                Latitude = fav.Latitude,
                Longitude = fav.Longitude,
                IsFavorite = true,
                IsSelected = isSelected
            });
            addedNames.Add(fav.Name);

            if (fav.Name.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase))
            {
                addedNames.Add("TP. Hồ Chí Minh");
                addedNames.Add("Thành phố Hồ Chí Minh");
                addedNames.Add("TP.HCM");
            }
        }

        // 2. Thêm các thành phố phổ biến (📍) nếu CHƯA có trong danh sách yêu thích
        foreach (var preset in PopularCityPresets)
        {
            if (addedNames.Contains(preset.Name)) continue;
            if (preset.Name.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) && 
                addedNames.Any(n => n.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase))) continue;

            bool isSelected = !string.IsNullOrEmpty(LocationTitle) &&
                (preset.Name.Equals(LocationTitle, StringComparison.OrdinalIgnoreCase) ||
                 (Math.Abs(preset.Lat - _currentLatitude) < 0.05 && Math.Abs(preset.Lon - _currentLongitude) < 0.05));

            QuickChips.Add(new QuickLocationChipItem
            {
                Name = preset.Name,
                Latitude = preset.Lat,
                Longitude = preset.Lon,
                IsFavorite = false,
                IsSelected = isSelected
            });
            addedNames.Add(preset.Name);
        }

        bool hasSelectedChip = QuickChips.Any(c => c.IsSelected);
        if (hasSelectedChip)
        {
            IsGpsLocationSelected = false;
        }
    }

    [RelayCommand]
    public async Task SelectQuickChipAsync(QuickLocationChipItem chip)
    {
        if (chip == null) return;
        IsGpsLocationSelected = false;
        _currentLatitude = chip.Latitude;
        _currentLongitude = chip.Longitude;
        LocationTitle = chip.Name;

        Settings.LastLatitude = chip.Latitude;
        Settings.LastLongitude = chip.Longitude;
        Settings.LastLocationName = chip.Name;
        _settingsService.SaveSettings(Settings);

        UpdateIsCurrentFavorite();
        UpdateQuickChips();
        await FetchWeatherForCoordinatesAsync(chip.Latitude, chip.Longitude, chip.Name);
    }

    [RelayCommand]
    public void ToggleAmbientSound()
    {
        if (AmbientSoundService.IsPlaying)
        {
            AmbientSoundService.Stop();
            IsAmbientSoundPlaying = false;
        }
        else
        {
            PlaySelectedAmbientSound();
        }
        OnPropertyChanged(nameof(AmbientSoundButtonToolTip));
        OnPropertyChanged(nameof(AmbientSoundIconGlyph));
        OnPropertyChanged(nameof(AmbientSoundButtonText));
    }

    public void PlaySelectedAmbientSound()
    {
        if (Settings.SelectedAmbientSound == "Auto")
        {
            AmbientSoundService.PlayForWeather(CurrentWeather?.WeatherEffect ?? WeatherEffectType.ClearSunny, Settings.AmbientSoundVolume);
        }
        else
        {
            var type = AmbientSoundService.ParseType(Settings.SelectedAmbientSound);
            AmbientSoundService.SetVolume(Settings.AmbientSoundVolume);
            AmbientSoundService.Play(type);
        }
        IsAmbientSoundPlaying = true;
        OnPropertyChanged(nameof(AmbientSoundButtonToolTip));
        OnPropertyChanged(nameof(AmbientSoundIconGlyph));
        OnPropertyChanged(nameof(AmbientSoundButtonText));
    }

    public void ChangeAmbientSound(string soundType)
    {
        Settings.SelectedAmbientSound = soundType;
        _settingsService.SaveSettings(Settings);
        if (IsAmbientSoundPlaying)
        {
            PlaySelectedAmbientSound();
        }
        OnPropertyChanged(nameof(AmbientSoundButtonToolTip));
        OnPropertyChanged(nameof(AmbientSoundButtonText));
    }

    public void ChangeAmbientSoundVolume(double volume)
    {
        Settings.AmbientSoundVolume = Math.Clamp(volume, 0.0, 1.0);
        AmbientSoundService.SetVolume(Settings.AmbientSoundVolume);
        _settingsService.SaveSettings(Settings);
    }

    public string AmbientSoundIconGlyph => IsAmbientSoundPlaying ? "\uf028" : "\uf025";
    public string AmbientSoundButtonText => IsAmbientSoundPlaying 
        ? (Loc.IsVietnamese ? "Đang phát" : "Playing") 
        : (Loc.IsVietnamese ? "Thư giãn" : "Ambient");

    public string AmbientSoundButtonToolTip => IsAmbientSoundPlaying
        ? (Loc.IsVietnamese 
            ? $"Đang phát: {GetAmbientSoundDisplayName(Settings.SelectedAmbientSound)} - Bấm để dừng hoặc tùy chỉnh"
            : $"Playing: {GetAmbientSoundDisplayName(Settings.SelectedAmbientSound)} - Click to pause or adjust")
        : (Loc.IsVietnamese 
            ? "Bật âm thanh thiên nhiên thư giãn (mưa rào, sóng biển, gió thông, sấm chớp, cà phê)"
            : "Play ambient nature sounds (summer rain, ocean waves, pine wind, thunder, rainy cafe)");

    public static string GetAmbientSoundDisplayName(string key)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        return key switch
        {
            "Rain" => isVi ? "Mưa rào mùa hạ" : "Summer Rain",
            "Thunderstorm" => isVi ? "Sấm chớp đêm mưa" : "Night Thunderstorm",
            "PineWind" => isVi ? "Gió rừng thông" : "Pine Forest Wind",
            "OceanWaves" => isVi ? "Sóng biển Nha Trang" : "Ocean Waves",
            "CafeRain" => isVi ? "Mưa quán cà phê" : "Rainy Cafe",
            _ => isVi ? "Tự động theo thời tiết" : "Auto Weather-based"
        };
    }

    [RelayCommand]
    public void ToggleUnit()
    {
        Settings.TemperatureUnit = Settings.TemperatureUnit == "F" ? "C" : "F";
        UnitButtonText = Settings.TemperatureUnit == "F" ? "°F" : "°C";
        _settingsService.SaveSettings(Settings);

        if (_rawWeatherData != null)
        {
            UpdateDisplaysFromRawData(_rawWeatherData, LocationTitle);
        }
    }

    [RelayCommand]
    public void ChangeTheme(string themeMode)
    {
        Settings.ThemeMode = themeMode;
        switch (themeMode?.ToLowerInvariant())
        {
            case "light":
                SelectedThemeIndex = 0;
                ThemeHelper.SetTheme(ElementTheme.Light);
                break;
            case "dark":
                SelectedThemeIndex = 1;
                ThemeHelper.SetTheme(ElementTheme.Dark);
                break;
            default:
                SelectedThemeIndex = 2;
                ThemeHelper.SetTheme(ElementTheme.Default);
                break;
        }
        _settingsService.SaveSettings(Settings);
    }

    [RelayCommand]
    public void SetStartup(bool enabled)
    {
        Settings.LaunchAtStartup = enabled;
        StartupService.SetStartupEnabled(enabled, Settings.StartMinimizedToTray);
        _settingsService.SaveSettings(Settings);
    }

    public void SetStartMinimizedToTray(bool enabled)
    {
        Settings.StartMinimizedToTray = enabled;
        if (Settings.LaunchAtStartup)
        {
            StartupService.SetStartupEnabled(true, enabled);
        }
        _settingsService.SaveSettings(Settings);
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync(bool isManual = true)
    {
        if (IsCheckingForUpdates) return;

        IsCheckingForUpdates = true;
        UpdateCheckStatusText = "Đang kết nối GitHub để kiểm tra phiên bản mới...";

        try
        {
            var info = await _updateCheckService.CheckForUpdatesAsync();

            HasAvailableUpdate = info.HasUpdate;
            LatestVersionText = info.LatestVersion;
            LatestReleaseTitle = info.ReleaseTitle;
            LatestReleaseUrl = info.ReleaseUrl;
            LatestDownloadUrl = info.DownloadUrl;
            LatestChangelog = info.Changelog;

            if (info.HasUpdate)
            {
                UpdateCheckStatusText = Loc.IsVietnamese 
                    ? $"🚀 Đã có phiên bản mới {info.LatestVersion}! Bấm để tải về ngay." 
                    : $"🚀 New version {info.LatestVersion} available! Click to download.";
            }
            else if (info.IsCheckingSuccess)
            {
                UpdateCheckStatusText = Loc.IsVietnamese 
                    ? $"✨ Bạn đang sử dụng phiên bản mới nhất ({UpdateCheckService.CurrentAppVersion})." 
                    : $"✨ You are using the latest version ({UpdateCheckService.CurrentAppVersion}).";
            }
            else
            {
                UpdateCheckStatusText = isManual
                    ? (Loc.IsVietnamese ? $"⚠️ Không thể kiểm tra: {info.ErrorMessage ?? "Vui lòng kiểm tra lại kết nối mạng"}" : $"⚠️ Check failed: {info.ErrorMessage ?? "Please verify network connection"}")
                    : string.Empty;
            }
        }
        catch (Exception ex)
        {
            if (isManual)
            {
                UpdateCheckStatusText = Loc.IsVietnamese ? $"⚠️ Lỗi kiểm tra: {ex.Message}" : $"⚠️ Check error: {ex.Message}";
            }
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    public async Task OpenLatestReleaseUrlAsync()
    {
        try
        {
            string url = !string.IsNullOrEmpty(LatestDownloadUrl) ? LatestDownloadUrl : LatestReleaseUrl;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
        }
        catch { }
    }

    public void ApplyPowerSavingOptimization()
    {
        try
        {
            if (Settings.EnableBatterySaverOptimization && IsBatterySavingActive)
            {
                _ramMonitorTimer.Interval = TimeSpan.FromSeconds(10);
            }
            else
            {
                _ramMonitorTimer.Interval = TimeSpan.FromSeconds(2);
            }
        }
        catch { }
    }

    [RelayCommand]
    public void ApplySettings()
    {
        try
        {
            if (!string.IsNullOrEmpty(Settings.AppLanguage))
            {
                LocalizationService.Instance.CurrentLanguage = Settings.AppLanguage;
            }
            _settingsService.SaveSettings(Settings);
            SetupAutoRefreshTimer();
            UpdateClock();
            SyncSettingsToProperties();
            OnPropertyChanged(nameof(WelcomeGreetingText));
            UpdateCityBackground();
            GenerateCalendar();

            if (_rawWeatherData != null)
            {
                UpdateDisplaysFromRawData(_rawWeatherData, LocationTitle);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] ApplySettings error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void SubmitFeedback(string type)
    {
        if (type == "accurate")
        {
            FeedbackSubmitted = true;
            ShowInaccurateOptions = false;
            FeedbackStatus = "accurate";
            FeedbackResponseText = "🎉 Cảm ơn bạn! Đã ghi nhận thời tiết thực tế khớp với dự báo.";
        }
        else if (type == "inaccurate")
        {
            ShowInaccurateOptions = true;
            FeedbackStatus = "inaccurate";
        }
    }

    [RelayCommand]
    public void SubmitActualCondition(string conditionName)
    {
        try
        {
            bool isVi = LocalizationService.Instance.IsVietnamese;
            FeedbackSubmitted = true;
            ShowInaccurateOptions = false;
            FeedbackResponseText = isVi
                ? $"🙏 Cảm ơn bạn! Đã ghi nhận thời tiết thực tế là: \"{conditionName}\" tại {LocationTitle}."
                : $"🙏 Thank you! Recorded actual weather condition as: \"{conditionName}\" at {LocationTitle}.";

            // Cập nhật ngay lập tức khung thời tiết hiện tại và hiệu ứng hình ảnh tương ứng!
            if (CurrentWeather != null)
            {
                WeatherEffectType effect;
                string svgFile;
                string badgeText;
                string badgeColor;
                string glyph;

                string normalized = (conditionName ?? "").ToLowerInvariant();

                if (normalized.Contains("gắt") || normalized.Contains("uv") || normalized.Contains("chói") || normalized.Contains("intense"))
                {
                    effect = WeatherEffectType.HighUvSunny;
                    svgFile = CurrentWeather.IsDay ? "clear-day.svg" : "clear-night.svg";
                    badgeText = isVi ? "🔥 NẮNG GẮT & UV CAO (THỰC TẾ)" : "🔥 INTENSE UV & SUN (ACTUAL)";
                    badgeColor = "#EA580C";
                    glyph = "\uf185";
                }
                else if (normalized.Contains("nhẹ") || normalized.Contains("hửng") || normalized.Contains("ít mây") || normalized.Contains("mild"))
                {
                    effect = WeatherEffectType.PartlyCloudy;
                    svgFile = CurrentWeather.IsDay ? "cloudy-1-day.svg" : "cloudy-1-night.svg";
                    badgeText = isVi ? "🌤️ NẮNG NHẸ & ÍT MÂY (THỰC TẾ)" : "🌤️ MILD SUN & PARTLY CLOUDY (ACTUAL)";
                    badgeColor = "#0284C7";
                    glyph = "\uf6c4";
                }
                else if (normalized.Contains("nắng") || normalized.Contains("quang") || normalized.Contains("sunny") || normalized.Contains("clear"))
                {
                    effect = WeatherEffectType.ClearSunny;
                    svgFile = CurrentWeather.IsDay ? "clear-day.svg" : "clear-night.svg";
                    badgeText = isVi ? "☀️ TRỜI NẮNG ĐẸP (THỰC TẾ)" : "☀️ SUNNY & CLEAR (ACTUAL)";
                    badgeColor = "#F59E0B";
                    glyph = "\uf185";
                }
                else if (normalized.Contains("dông") || normalized.Contains("sét") || normalized.Contains("sấm") || normalized.Contains("thunder"))
                {
                    effect = WeatherEffectType.Thunderstorm;
                    svgFile = CurrentWeather.IsDay ? "isolated-thunderstorms-day.svg" : "isolated-thunderstorms-night.svg";
                    badgeText = isVi ? "⚡ DÔNG SÉT & MƯA LỚN (THỰC TẾ)" : "⚡ THUNDERSTORM & RAIN (ACTUAL)";
                    badgeColor = "#DC2626";
                    glyph = "\uf76c";
                }
                else if (normalized.Contains("rất to") || normalized.Contains("mưa to") || normalized.Contains("mưa lớn") || normalized.Contains("xối xả") || normalized.Contains("heavy"))
                {
                    effect = WeatherEffectType.HeavyRain;
                    svgFile = CurrentWeather.IsDay ? "rainy-3-day.svg" : "rainy-3-night.svg";
                    badgeText = isVi ? "🌊 MƯA RẤT TO (THỰC TẾ)" : "🌊 HEAVY RAINFALL (ACTUAL)";
                    badgeColor = "#1D4ED8";
                    glyph = "\uf73d";
                }
                else if (normalized.Contains("phùn") || normalized.Contains("mưa nhỏ") || normalized.Contains("mưa bay") || normalized.Contains("drizzle"))
                {
                    effect = WeatherEffectType.LightRain;
                    svgFile = CurrentWeather.IsDay ? "rainy-1-day.svg" : "rainy-1-night.svg";
                    badgeText = isVi ? "🌦️ MƯA PHÙN / MƯA NHỎ (THỰC TẾ)" : "🌦️ LIGHT DRIZZLE / RAIN (ACTUAL)";
                    badgeColor = "#0284C7";
                    glyph = "\uf73d";
                }
                else if (normalized.Contains("mưa") || normalized.Contains("rain"))
                {
                    effect = WeatherEffectType.ModerateRain;
                    svgFile = CurrentWeather.IsDay ? "rainy-2-day.svg" : "rainy-2-night.svg";
                    badgeText = isVi ? "🌧️ ĐANG CÓ MƯA (THỰC TẾ)" : "🌧️ RAINING (ACTUAL)";
                    badgeColor = "#0369A1";
                    glyph = "\uf73d";
                }
                else if (normalized.Contains("sương") || normalized.Contains("bụi") || normalized.Contains("fog") || normalized.Contains("haze"))
                {
                    effect = WeatherEffectType.Fog;
                    svgFile = CurrentWeather.IsDay ? "fog-day.svg" : "fog-night.svg";
                    badgeText = isVi ? "🌫️ SƯƠNG MÙ DÀY (THỰC TẾ)" : "🌫️ DENSE FOG (ACTUAL)";
                    badgeColor = "#78716C";
                    glyph = "\uf75f";
                }
                else if (normalized.Contains("rét") || normalized.Contains("lạnh") || normalized.Contains("buốt") || normalized.Contains("cold"))
                {
                    effect = WeatherEffectType.Cloudy;
                    svgFile = CurrentWeather.IsDay ? "snowy-1-day.svg" : "snowy-1-night.svg";
                    badgeText = isVi ? "❄️ RÉT BUỐT & LẠNH GIÁ (THỰC TẾ)" : "❄️ FREEZING COLD (ACTUAL)";
                    badgeColor = "#0284C7";
                    glyph = "\uf2dc";
                }
                else if (normalized.Contains("mây") || normalized.Contains("cloud") || normalized.Contains("u ám"))
                {
                    effect = WeatherEffectType.Cloudy;
                    svgFile = CurrentWeather.IsDay ? "cloudy-1-day.svg" : "cloudy-1-night.svg";
                    badgeText = isVi ? "☁️ NHIỀU MÂY (THỰC TẾ)" : "☁️ CLOUDY (ACTUAL)";
                    badgeColor = "#64748B";
                    glyph = "\uf0c2";
                }
                else if (normalized.Contains("gió") || normalized.Contains("wind"))
                {
                    effect = WeatherEffectType.HeavyRain;
                    svgFile = "wind.svg";
                    badgeText = isVi ? "💨 GIÓ TO CẤP CAO (THỰC TẾ)" : "💨 HIGH WIND (ACTUAL)";
                    badgeColor = "#E11D48";
                    glyph = "\uf72e";
                }
                else
                {
                    effect = WeatherEffectType.ClearSunny;
                    svgFile = CurrentWeather.IsDay ? "clear-day.svg" : "clear-night.svg";
                    badgeText = isVi ? $"🌦️ {conditionName} (THỰC TẾ)" : $"🌦️ {conditionName} (ACTUAL)";
                    badgeColor = "#0284C7";
                    glyph = "\uf6c4";
                }

                CurrentWeather.ConditionText = isVi ? $"{conditionName} (Theo thực tế)" : $"{conditionName} (Reported actual)";
                CurrentWeather.WeatherEffect = effect;
                CurrentWeather.WeatherAlertBadgeText = badgeText;
                CurrentWeather.WeatherAlertBadgeColor = badgeColor;
                CurrentWeather.IconGlyph = glyph;

                var (startColor, endColor) = WeatherCodeHelper.GetHeroGradientsByEffect(effect);
                CurrentWeather.HeroGradientStart = startColor;
                CurrentWeather.HeroGradientEnd = endColor;

                // Lấy icon theo bộ biểu tượng đã chọn (Meteocons / Fluent3D / FontAwesome)
                string iconPack = Settings.SelectedIconPack ?? "Meteocons";
                int code = effect switch
                {
                    WeatherEffectType.Thunderstorm => 95,
                    WeatherEffectType.HeavyRain => 65,
                    WeatherEffectType.ModerateRain => 61,
                    WeatherEffectType.LightRain => 51,
                    WeatherEffectType.Fog => 45,
                    WeatherEffectType.Snow => 71,
                    WeatherEffectType.ClearSunny => 0,
                    WeatherEffectType.HighUvSunny => 0,
                    WeatherEffectType.PartlyCloudy => 1,
                    WeatherEffectType.Cloudy => 3,
                    _ => 0
                };
                CurrentWeather.SvgIconPath = WeatherCodeHelper.GetWeatherIconPath(code, CurrentWeather.IsDay, iconPack);
                string fullPath = WeatherCodeHelper.GetWeatherIconFullPath(code, CurrentWeather.IsDay, iconPack);
                CurrentWeather.SvgIconFullPath = fullPath;
                if (!string.IsNullOrEmpty(fullPath))
                {
                    CurrentWeather.SvgIconUri = new Uri(fullPath);
                }

                // Cập nhật lại danh sách Lời khuyên thông minh dựa theo tình trạng thực tế người dùng vừa báo cáo
                UpdateAdviceForReportedCondition(conditionName ?? "", effect);

                OnPropertyChanged(nameof(CurrentWeather));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SubmitActualCondition: {ex.Message}");
        }
    }

    public void UpdateIconPack(string iconPack)
    {
        Settings.SelectedIconPack = iconPack;
        _settingsService.SaveSettings(Settings);

        if (_rawWeatherData != null)
        {
            UpdateDisplaysFromRawData(_rawWeatherData, LocationTitle, _lastAirQuality);
        }
        else if (CurrentWeather != null)
        {
            int code = CurrentWeather.WeatherEffect switch
            {
                WeatherEffectType.Thunderstorm => 95,
                WeatherEffectType.HeavyRain => 65,
                WeatherEffectType.ModerateRain => 61,
                WeatherEffectType.LightRain => 51,
                WeatherEffectType.Fog => 45,
                WeatherEffectType.Snow => 71,
                WeatherEffectType.ClearSunny => 0,
                WeatherEffectType.HighUvSunny => 0,
                WeatherEffectType.PartlyCloudy => 1,
                WeatherEffectType.Cloudy => 3,
                _ => 0
            };
            CurrentWeather.SvgIconPath = WeatherCodeHelper.GetWeatherIconPath(code, CurrentWeather.IsDay, iconPack);
            string fullPath = WeatherCodeHelper.GetWeatherIconFullPath(code, CurrentWeather.IsDay, iconPack);
            CurrentWeather.SvgIconFullPath = fullPath;
            if (!string.IsNullOrEmpty(fullPath))
            {
                CurrentWeather.SvgIconUri = new Uri(fullPath);
            }
            OnPropertyChanged(nameof(CurrentWeather));
        }

        // Cập nhật các widget đang mở
        foreach (var widget in WidgetWindow.ActiveWidgets)
        {
            widget.UpdateWidgetData();
        }
    }

    private void UpdateAdviceForReportedCondition(string conditionName, WeatherEffectType effect)
    {
        try
        {
            if (_rawWeatherData?.Current != null)
            {
                var generated = _adviceService.GenerateAdvice(_rawWeatherData.Current, _rawWeatherData.Daily);
                AdviceList.Clear();
                foreach (var item in generated)
                {
                    AdviceList.Add(item);
                }
                UpdateTopAdviceSummary();
            }
        }
        catch { }
    }

    private void UpdateTopAdviceSummary()
    {
        if (AdviceList.Count == 0)
        {
            TopAdviceSummary = Loc.AdviceEmpty;
            return;
        }

        // Ưu tiên cảnh báo nguy hiểm trước (Alert)
        var alert = AdviceList.FirstOrDefault(a => a.Severity == AdviceSeverity.Alert);
        if (alert != null)
        {
            TopAdviceSummary = $"⚠️ {alert.Category}: {alert.Title}";
            return;
        }

        // Sau đó đến lưu ý thời tiết (Warning)
        var warning = AdviceList.FirstOrDefault(a => a.Severity == AdviceSeverity.Warning);
        if (warning != null)
        {
            TopAdviceSummary = $"💡 {warning.Category}: {warning.Title}";
            return;
        }

        // Mặc định kết hợp trang phục và hoạt động
        var cloth = AdviceList.FirstOrDefault(a => a.Category.Contains("TRANG PHỤC") || a.Category.Contains("CLOTHING"));
        var outdoor = AdviceList.FirstOrDefault(a => a.Category.Contains("HOẠT ĐỘNG") || a.Category.Contains("OUTDOOR"));
        if (cloth != null && outdoor != null)
        {
            TopAdviceSummary = $"{cloth.Title} • {outdoor.Title}";
        }
        else
        {
            TopAdviceSummary = AdviceList[0].Title;
        }
    }


    [RelayCommand]
    public void ResetFeedback()
    {
        FeedbackSubmitted = false;
        ShowInaccurateOptions = false;
        FeedbackStatus = string.Empty;
        FeedbackResponseText = string.Empty;
    }

    private async Task FetchWeatherForCoordinatesAsync(double lat, double lon, string locationName)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        ResetFeedback();

        try
        {
            var weatherTask = _weatherService.GetWeatherDataAsync(lat, lon);
            var aqiTask = _weatherService.GetAirQualityAsync(lat, lon);

            await Task.WhenAll(weatherTask, aqiTask);

            var data = await weatherTask;
            _lastAirQuality = await aqiTask;

            if (data == null || data.Current == null)
            {
                HasError = true;
                ErrorMessage = Loc.IsVietnamese 
                    ? "Không thể tải dữ liệu thời tiết. Vui lòng kiểm tra lại kết nối mạng." 
                    : "Unable to load weather data. Please check your network connection.";
                IsLoading = false;
                return;
            }

            _rawWeatherData = data;
            UpdateDisplaysFromRawData(data, locationName, _lastAirQuality);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = Loc.IsVietnamese ? $"Đã xảy ra lỗi: {ex.Message}" : $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            MemoryOptimizer.TrimMemory();
            UpdateRamUsage();
        }
    }

    private void UpdateDisplaysFromRawData(OpenMeteoResponse data, string locationName, AirQualityData? airQuality = null)
    {
        try
        {
            if (airQuality != null)
                _lastAirQuality = airQuality;

            CurrentWeather = _weatherService.CreateCurrentWeatherDisplay(data, locationName, Settings, _lastAirQuality);

            // Cập nhật hình nền thành phố
            UpdateCityBackground();

            // Kiểm tra và gửi thông báo cảnh báo thông minh nếu đủ điều kiện
            try
            {
                NotificationService.CheckAndTriggerAlerts(CurrentWeather, Settings, data.Daily, data.Hourly);
            }
            catch (Exception exAlert)
            {
                System.Diagnostics.Debug.WriteLine($"[Alerts] Error: {exAlert.Message}");
            }

            // Cập nhật khay hệ thống (System Tray)
            try
            {
                TrayService?.UpdateWeather(CurrentWeather.TemperatureText, CurrentWeather.ConditionText, locationName);
            }
            catch (Exception exTray)
            {
                System.Diagnostics.Debug.WriteLine($"[TrayService] Error: {exTray.Message}");
            }

            // Lời khuyên thời tiết thông minh
            if (data.Current != null)
            {
                var advice = _adviceService.GenerateAdvice(data.Current, data.Daily);
                AdviceList.Clear();
                foreach (var item in advice)
                {
                    AdviceList.Add(item);
                }
                UpdateTopAdviceSummary();
            }

            // Dự báo theo giờ
            var hourly = _weatherService.CreateHourlyForecast(data, Settings);
            HourlyForecast.Clear();
            foreach (var item in hourly)
            {
                HourlyForecast.Add(item);
            }
            OnPropertyChanged(nameof(HourlyForecast));
            HourlyForecastUpdated?.Invoke();

            // Dự báo 7 ngày
            var daily = _weatherService.CreateDailyForecast(data, Settings);
            DailyForecast.Clear();
            foreach (var item in daily)
            {
                DailyForecast.Add(item);
            }
            OnPropertyChanged(nameof(DailyForecast));

            // Cập nhật lại toàn bộ Lịch Tháng với dữ liệu thời tiết của thành phố mới
            CalendarLocationSubtitle = LocationTitle;
            GenerateCalendar();
            UpdateQuickChips();

            // Tính năng mới v1.7 & v3.0.3: Chỉ số đời sống, Khung giờ vận động & Bảo vệ da
            try
            {
                var lifestyle = _lifestyleService.GenerateLifestyleIndices(CurrentWeather, data);
                LifestyleIndices.Clear();
                foreach (var l in lifestyle)
                {
                    LifestyleIndices.Add(l);
                }

                var workouts = _lifestyleService.GenerateWorkoutWindows(CurrentWeather, data);
                WorkoutWindows.Clear();
                foreach (var w in workouts)
                {
                    WorkoutWindows.Add(w);
                }

                SkinDefense = _lifestyleService.GenerateSkinDefenseAdvice(CurrentWeather);
            }
            catch { }

            // Tính năng mới v2.1: Gợi ý trang phục OOTD "Hôm nay mặc gì?"
            try
            {
                _lastRawWeatherData = data;
                UpdateOutfitAdvice();
            }
            catch { }

            // Tính năng mới v2.2.3: Cảnh báo ngập úng & Triều cường đô thị (TP.HCM, Hà Nội & Đô thị)
            try
            {
                UpdateFloodWarningData(data, locationName);
            }
            catch { }

            // Tính năng mới v1.7: Cập nhật trạng thái yêu thích của địa điểm hiện tại
            UpdateIsCurrentFavorite();

            // Tính năng mới v1.7: Cập nhật âm thanh thư giãn nếu đang phát
            if (IsAmbientSoundPlaying)
            {
                if (Settings.SelectedAmbientSound == "Auto")
                {
                    AmbientSoundService.PlayForWeather(CurrentWeather.WeatherEffect, Settings.AmbientSoundVolume);
                }
            }

            UpdateRamUsage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] UpdateDisplaysFromRawData error: {ex.Message}");
        }
    }

    public void UpdateCityBackground()
    {
        if (CurrentWeather != null)
        {
            bool isDay = CurrentWeather.IsDay;
            string? path = CityBackgroundHelper.ResolveImagePath(LocationTitle, Settings, isDay);
            CurrentWeather.CityImagePath = path;
            OnPropertyChanged(nameof(CurrentWeather));
        }
    }

    public string WelcomeGreetingText
    {
        get
        {
            bool isVi = Loc.IsVietnamese;
            if (string.IsNullOrWhiteSpace(Settings.UserName))
            {
                return isVi ? "Thời Tiết WinUI" : "Weather WinUI";
            }
            return isVi ? $"Xin chào, {Settings.UserName}! 👋" : $"Hello, {Settings.UserName}! 👋";
        }
    }

    public readonly NotificationService NotificationService = new();
    public TrayIconService? TrayService { get; set; }
    public event Action? HourlyForecastUpdated;

    [RelayCommand]
    public void TestToastNotification()
    {
        string title = "🔔 Kiểm Tra Thông Báo Windows";
        string msg = $"Thời Tiết WinUI kết nối thành công! Thời tiết hiện tại: {CurrentWeather?.TemperatureText} • {CurrentWeather?.ConditionText} tại {LocationTitle}.";
        NotificationService.ShowToast(title, msg);
        TrayService?.ShowBalloon(title, msg);
    }

    #region Lịch Tháng Vạn Niên & Thời Tiết (Monthly Calendar) v1.8

    private int _calendarYear = DateTime.Today.Year;
    private int _calendarMonth = DateTime.Today.Month;

    public int CalendarYear
    {
        get => _calendarYear;
        set
        {
            if (SetProperty(ref _calendarYear, value))
            {
                SelectedYear = value;
                GenerateCalendar();
            }
        }
    }

    public int CalendarMonth
    {
        get => _calendarMonth;
        set
        {
            if (SetProperty(ref _calendarMonth, value))
            {
                SelectedMonthIndex = value - 1;
                GenerateCalendar();
            }
        }
    }

    [ObservableProperty]
    private string _calendarMonthTitle = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _calendarDayHeaders = new();

    [ObservableProperty]
    private ObservableCollection<CalendarDayItem> _calendarDays = new();

    [ObservableProperty]
    private CalendarDayItem? _selectedCalendarDay;

    [ObservableProperty]
    private bool _isDayDetailOpen = false;

    [ObservableProperty]
    private int _selectedMonthIndex = DateTime.Today.Month - 1;

    partial void OnSelectedMonthIndexChanged(int value)
    {
        if (value >= 0 && value < 12 && _calendarMonth != value + 1)
        {
            _calendarMonth = value + 1;
            GenerateCalendar();
        }
    }

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    partial void OnSelectedYearChanged(int value)
    {
        if (value >= 1950 && value <= 2100 && _calendarYear != value)
        {
            _calendarYear = value;
            GenerateCalendar();
        }
    }

    public List<int> AvailableYears { get; } = Enumerable.Range(1950, 151).ToList();
    public ObservableCollection<string> AvailableMonths { get; } = new();

    public void UpdateAvailableMonths()
    {
        int prevIndex = SelectedMonthIndex >= 0 ? SelectedMonthIndex : (_calendarMonth - 1);
        AvailableMonths.Clear();
        bool isVi = LocalizationService.Instance.IsVietnamese;
        for (int i = 1; i <= 12; i++)
        {
            if (isVi)
                AvailableMonths.Add($"Tháng {i}");
            else
                AvailableMonths.Add(new DateTime(2026, i, 1).ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture));
        }
        SelectedMonthIndex = Math.Clamp(prevIndex, 0, 11);
    }

    // ==================== TÍNH NĂNG MỚI v3.0.4: LỊCH VẠN NIÊN & ĐẾM NGƯỢC LỄ HỘI & CHUYỂN ĐỔI ÂM DƯƠNG ====================
    [ObservableProperty]
    private ObservableCollection<CalendarFestivalCountdown> _festivalCountdowns = new();

    [ObservableProperty]
    private DateTimeOffset _converterSolarDate = DateTimeOffset.Now;

    [ObservableProperty]
    private int _converterLunarDay = 1;

    [ObservableProperty]
    private int _converterLunarMonth = 1;

    [ObservableProperty]
    private int _converterLunarYear = DateTime.Today.Year;

    [ObservableProperty]
    private bool _converterIsLunarLeap = false;

    [ObservableProperty]
    private string _converterSolarToLunarResult = string.Empty;

    [ObservableProperty]
    private string _converterLunarToSolarResult = string.Empty;

    [ObservableProperty]
    private bool _isCalendarCopiedOpen = false;

    [ObservableProperty]
    private string _calendarCopiedMessage = string.Empty;

    public void InitializeCalendar()
    {
        _calendarYear = DateTime.Today.Year;
        _calendarMonth = DateTime.Today.Month;
        SelectedYear = _calendarYear;
        SelectedMonthIndex = _calendarMonth - 1;
        UpdateAvailableMonths();
        GenerateCalendar();
    }

    [RelayCommand]
    public void PrevMonth()
    {
        if (_calendarMonth == 1)
        {
            _calendarMonth = 12;
            _calendarYear--;
        }
        else
        {
            _calendarMonth--;
        }
        SelectedYear = _calendarYear;
        SelectedMonthIndex = _calendarMonth - 1;
        GenerateCalendar();
    }

    [RelayCommand]
    public void NextMonth()
    {
        if (_calendarMonth == 12)
        {
            _calendarMonth = 1;
            _calendarYear++;
        }
        else
        {
            _calendarMonth++;
        }
        SelectedYear = _calendarYear;
        SelectedMonthIndex = _calendarMonth - 1;
        GenerateCalendar();
    }

    [RelayCommand]
    public void GoToToday()
    {
        _calendarYear = DateTime.Today.Year;
        _calendarMonth = DateTime.Today.Month;
        SelectedYear = _calendarYear;
        SelectedMonthIndex = _calendarMonth - 1;
        GenerateCalendar();
    }

    [RelayCommand]
    public void OpenDayDetail(CalendarDayItem? day)
    {
        if (day == null) return;
        SelectedCalendarDay = day;
        EvaluateWeatherConflict(SelectedCalendarDay);
        IsDayDetailOpen = true;
    }

    [RelayCommand]
    public void CloseDayDetail()
    {
        IsDayDetailOpen = false;
    }

    [RelayCommand]
    public void CopySelectedDayInfo()
    {
        if (SelectedCalendarDay == null) return;
        try
        {
            var day = SelectedCalendarDay;
            bool isVi = LocalizationService.Instance.IsVietnamese;
            string text = isVi
                ? $"{day.DayOfWeekName}, {day.SolarDay:D2}/{day.SolarMonth:D2}/{day.SolarYear}\n" +
                  $"Âm lịch: Ngày {day.LunarDay:D2} tháng {day.LunarMonth:D2} năm {day.CanChiYear}\n" +
                  $"Can Chi: Ngày {day.CanChiDay}, Tháng {day.CanChiMonth}\n" +
                  $"Tiết khí: {day.SolarTerm}\n" +
                  $"Trực ngày: {day.AuspiciousDayName}\n" +
                  $"Giờ Hoàng Đạo: {day.AuspiciousHoursFormatted}\n" +
                  $"Tuần trăng: {day.MoonPhaseIcon} {day.MoonPhaseName}"
                : $"{day.DayOfWeekName}, {day.SolarDay:D2}/{day.SolarMonth:D2}/{day.SolarYear}\n" +
                  $"Lunar: Day {day.LunarDay:D2}/{day.LunarMonth:D2} Year {day.CanChiYear}\n" +
                  $"Stems & Branches: Day {day.CanChiDay}, Month {day.CanChiMonth}\n" +
                  $"Solar Term: {day.SolarTerm}\n" +
                  $"Zodiac Status: {day.AuspiciousDayName}\n" +
                  $"Auspicious Hours: {day.AuspiciousHoursFormatted}\n" +
                  $"Moon Phase: {day.MoonPhaseIcon} {day.MoonPhaseName}";

            if (day.HasHoliday)
            {
                text += isVi ? $"\nNgày lễ: {day.HolidayName} ({day.HolidayDescription})" : $"\nHoliday: {day.HolidayName} ({day.HolidayDescription})";
            }
            if (day.HasWeatherForecast)
            {
                text += isVi ? $"\nDự báo: {day.WeatherCondition}, {day.TempRangeText}" : $"\nForecast: {day.WeatherCondition}, {day.TempRangeText}";
            }

            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            dp.SetText(text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

            CalendarCopiedMessage = isVi
                ? "Đã sao chép chi tiết ngày & giờ hoàng đạo vào Clipboard!"
                : "Day details & auspicious hours copied to clipboard!";
            IsCalendarCopiedOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] CopySelectedDayInfo error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void ConvertSolarToLunar()
    {
        try
        {
            bool isVi = LocalizationService.Instance.IsVietnamese;
            DateTime dt = ConverterSolarDate.DateTime;
            var l = VietnameseLunarHelper.ConvertSolarToLunar(dt, isVi);
            ConverterSolarToLunarResult = isVi
                ? $"Âm lịch: Ngày {l.Day:D2}/{l.Month:D2}/{l.Year} ({l.CanChiYear})\nCan Chi Ngày: {l.CanChiDay} • Tiết {l.SolarTerm} • {l.AuspiciousDayName}"
                : $"Lunar: Day {l.Day:D2}/{l.Month:D2}/{l.Year} ({l.CanChiYear})\nDay Branch: {l.CanChiDay} • Term {l.SolarTerm} • {l.AuspiciousDayName}";
        }
        catch (Exception ex)
        {
            ConverterSolarToLunarResult = LocalizationService.Instance.IsVietnamese ? $"Lỗi: {ex.Message}" : $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ConvertLunarToSolar()
    {
        try
        {
            bool isVi = LocalizationService.Instance.IsVietnamese;
            DateTime? solar = VietnameseLunarHelper.ConvertLunarToSolar(ConverterLunarDay, ConverterLunarMonth, ConverterLunarYear, ConverterIsLunarLeap);
            if (solar.HasValue)
            {
                var l = VietnameseLunarHelper.ConvertSolarToLunar(solar.Value, isVi);
                ConverterLunarToSolarResult = isVi
                    ? $"Dương lịch: {solar.Value:dddd, dd/MM/yyyy}\nCan Chi: Ngày {l.CanChiDay}, Năm {l.CanChiYear} • {l.AuspiciousDayName}"
                    : $"Solar: {solar.Value:dddd, dd/MM/yyyy}\nBranches: Day {l.CanChiDay}, Year {l.CanChiYear} • {l.AuspiciousDayName}";
            }
            else
            {
                ConverterLunarToSolarResult = isVi
                    ? "Không tìm thấy ngày Dương lịch phù hợp cho ngày Âm này."
                    : "No matching Solar date found for this Lunar date.";
            }
        }
        catch (Exception ex)
        {
            ConverterLunarToSolarResult = LocalizationService.Instance.IsVietnamese ? $"Lỗi: {ex.Message}" : $"Error: {ex.Message}";
        }
    }

    public void GenerateCalendar()
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (isVi)
        {
            CalendarMonthTitle = $"Tháng {_calendarMonth:D2}, {_calendarYear}";
        }
        else
        {
            CalendarMonthTitle = new DateTime(_calendarYear, _calendarMonth, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        bool startMonday = (Settings?.FirstDayOfWeek ?? "Monday").Equals("Monday", StringComparison.OrdinalIgnoreCase);

        CalendarDayHeaders.Clear();
        if (isVi)
        {
            if (startMonday)
            {
                CalendarDayHeaders.Add("T2");
                CalendarDayHeaders.Add("T3");
                CalendarDayHeaders.Add("T4");
                CalendarDayHeaders.Add("T5");
                CalendarDayHeaders.Add("T6");
                CalendarDayHeaders.Add("T7");
                CalendarDayHeaders.Add("CN");
            }
            else
            {
                CalendarDayHeaders.Add("CN");
                CalendarDayHeaders.Add("T2");
                CalendarDayHeaders.Add("T3");
                CalendarDayHeaders.Add("T4");
                CalendarDayHeaders.Add("T5");
                CalendarDayHeaders.Add("T6");
                CalendarDayHeaders.Add("T7");
            }
        }
        else
        {
            if (startMonday)
            {
                CalendarDayHeaders.Add("Mon");
                CalendarDayHeaders.Add("Tue");
                CalendarDayHeaders.Add("Wed");
                CalendarDayHeaders.Add("Thu");
                CalendarDayHeaders.Add("Fri");
                CalendarDayHeaders.Add("Sat");
                CalendarDayHeaders.Add("Sun");
            }
            else
            {
                CalendarDayHeaders.Add("Sun");
                CalendarDayHeaders.Add("Mon");
                CalendarDayHeaders.Add("Tue");
                CalendarDayHeaders.Add("Wed");
                CalendarDayHeaders.Add("Thu");
                CalendarDayHeaders.Add("Fri");
                CalendarDayHeaders.Add("Sat");
            }
        }

        DateTime firstDayOfMonth;
        try
        {
            firstDayOfMonth = new DateTime(_calendarYear, _calendarMonth, 1);
        }
        catch
        {
            firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        int dayOfWeekInt = (int)firstDayOfMonth.DayOfWeek;
        int offsetDays = startMonday
            ? (dayOfWeekInt == 0 ? 6 : dayOfWeekInt - 1)
            : dayOfWeekInt;

        DateTime gridStartDate = firstDayOfMonth.AddDays(-offsetDays);

        CalendarDays.Clear();
        DateTime today = DateTime.Today;
        _allUserEvents = _calendarEventService.LoadEvents();
        _allDailyGoals = _calendarEventService.LoadGoals();

        // Cập nhật danh sách đếm ngược lễ hội truyền thống
        try
        {
            var countList = VietnameseLunarHelper.GetUpcomingFestivals(today, isVi);
            FestivalCountdowns.Clear();
            foreach (var c in countList)
            {
                FestivalCountdowns.Add(new CalendarFestivalCountdown
                {
                    Title = c.Title,
                    LunarDateText = c.LunarDateText,
                    SolarDateText = c.SolarDateText,
                    DaysRemaining = c.DaysRemaining,
                    DaysRemainingText = c.DaysRemainingText,
                    IconGlyph = c.IconGlyph,
                    AccentColor = c.AccentColor
                });
            }
        }
        catch { }

        for (int i = 0; i < 42; i++)
        {
            DateTime curDate = gridStartDate.AddDays(i);
            var lunar = VietnameseLunarHelper.ConvertSolarToLunar(curDate, isVi);
            var holiday = VietnameseHolidayHelper.GetHoliday(curDate, lunar.Day, lunar.Month, lunar.IsLeap, isVi);
            var dayEvents = _allUserEvents.Where(e => e.Date.Date == curDate.Date).ToList();

            string dateKey = curDate.ToString("yyyy-MM-dd");
            var goal = _allDailyGoals.FirstOrDefault(g => g.DateKey == dateKey);

            string lunarDisplay = (lunar.Day == 1)
                ? $"{lunar.Day}/{lunar.Month}"
                : (lunar.Day == 15 ? "15" : $"{lunar.Day}");

            string dayOfWeekName = isVi
                ? curDate.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"))
                : curDate.ToString("dddd", System.Globalization.CultureInfo.InvariantCulture);
            if (dayOfWeekName.Length > 0)
                dayOfWeekName = char.ToUpper(dayOfWeekName[0]) + dayOfWeekName.Substring(1);

            var item = new CalendarDayItem
            {
                Date = curDate,
                LunarDay = lunar.Day,
                LunarMonth = lunar.Month,
                LunarYear = lunar.Year,
                IsLeapLunar = lunar.IsLeap,
                LunarDisplay = lunarDisplay,
                IsCurrentMonth = curDate.Month == _calendarMonth && curDate.Year == _calendarYear,
                IsToday = curDate.Date == today,
                IsWeekend = curDate.DayOfWeek == DayOfWeek.Saturday || curDate.DayOfWeek == DayOfWeek.Sunday,
                DayOfWeekName = dayOfWeekName,
                CanChiYear = lunar.CanChiYear,
                CanChiMonth = lunar.CanChiMonth,
                CanChiDay = lunar.CanChiDay,
                SolarTerm = lunar.SolarTerm,
                IsAuspiciousDay = lunar.IsAuspiciousDay,
                AuspiciousDayName = lunar.AuspiciousDayName,
                AuspiciousDayColor = lunar.AuspiciousDayColor,
                AuspiciousHoursList = lunar.AuspiciousHoursList,
                AuspiciousHoursFormatted = lunar.AuspiciousHoursFormatted,
                MoonPhaseIcon = lunar.MoonPhaseIcon,
                MoonPhaseName = lunar.MoonPhaseName,
                MoonIllumination = lunar.MoonIllumination,
                HasHoliday = holiday.IsHoliday,
                HolidayName = holiday.Name,
                HolidayBadge = holiday.Badge,
                HolidayDescription = holiday.Description,
                IsOfficialDayOff = holiday.IsOfficialDayOff,
                UserEvents = new ObservableCollection<CalendarUserEvent>(dayEvents),
                HasGoal = goal != null && !string.IsNullOrWhiteSpace(goal.GoalText),
                GoalText = goal?.GoalText ?? "",
                IsGoalCompleted = goal?.IsCompleted ?? false,
                GoalIcon = string.IsNullOrEmpty(goal?.GoalIcon) ? "🎯" : goal.GoalIcon
            };

            AttachWeatherToCalendarDay(item);
            EvaluateWeatherConflict(item);
            CalendarDays.Add(item);
        }

        if (SelectedCalendarDay == null || SelectedCalendarDay.Date.Month != _calendarMonth || SelectedCalendarDay.Date.Year != _calendarYear)
        {
            var targetDay = CalendarDays.FirstOrDefault(d => d.IsToday) ?? CalendarDays.FirstOrDefault(d => d.IsCurrentMonth) ?? CalendarDays.FirstOrDefault();
            if (targetDay != null)
            {
                SelectedCalendarDay = targetDay;
                EvaluateWeatherConflict(SelectedCalendarDay);
            }
        }
    }

    public void AttachWeatherToCalendarDay(CalendarDayItem item)
    {
        if (_rawWeatherData?.Daily?.Time == null)
        {
            item.HasWeatherForecast = false;
            item.NotifyWeatherChanged();
            return;
        }

        string targetDateStr = item.Date.ToString("yyyy-MM-dd");
        int index = _rawWeatherData.Daily.Time.IndexOf(targetDateStr);
        if (index >= 0)
        {
            var daily = _rawWeatherData.Daily;
            item.HasWeatherForecast = true;

            int code = daily.WeatherCode != null && index < daily.WeatherCode.Count ? daily.WeatherCode[index] : 0;
            double maxT = daily.TemperatureMax != null && index < daily.TemperatureMax.Count ? daily.TemperatureMax[index] : 0;
            double minT = daily.TemperatureMin != null && index < daily.TemperatureMin.Count ? daily.TemperatureMin[index] : 0;
            int rainProb = daily.PrecipitationProbabilityMax != null && index < daily.PrecipitationProbabilityMax.Count ? daily.PrecipitationProbabilityMax[index] : 0;

            bool isFahrenheit = Settings.TemperatureUnit == "F";
            double displayMax = isFahrenheit ? (maxT * 9 / 5 + 32) : maxT;
            double displayMin = isFahrenheit ? (minT * 9 / 5 + 32) : minT;
            string unit = isFahrenheit ? "°F" : "°";

            var (condText, glyph) = WeatherCodeHelper.GetConditionInfo(code, true);
            item.WeatherIconGlyph = glyph;
            item.WeatherCondition = condText;
            item.TempMax = displayMax;
            item.TempMin = displayMin;
            item.TempRangeText = $"{Math.Round(displayMin)}{unit}-{Math.Round(displayMax)}{unit}";
            item.RainProbabilityText = $"{rainProb}%";

            if (code >= 51 && code <= 99)
            {
                item.WeatherEffect = "Rain";
            }
            else if (code <= 1)
            {
                item.WeatherEffect = "Sunny";
            }
            else
            {
                item.WeatherEffect = "Normal";
            }

            if (daily.UvIndexMax != null && index < daily.UvIndexMax.Count)
            {
                item.UvText = $"{daily.UvIndexMax[index]:F1}";
            }
        }
        else
        {
            item.HasWeatherForecast = false;
            item.WeatherEffect = "Normal";
            item.TempRangeText = string.Empty;
        }
        item.NotifyWeatherChanged();
    }

    public void EvaluateWeatherConflict(CalendarDayItem? item)
    {
        if (item == null) return;

        if (item.UserEvents == null || item.UserEvents.Count == 0)
        {
            item.HasWeatherConflictWarning = false;
            item.WeatherConflictTitle = string.Empty;
            item.WeatherConflictMessage = string.Empty;
            return;
        }

        var outdoorEvent = item.UserEvents.FirstOrDefault(e => e.IsOutdoor);
        if (outdoorEvent == null)
        {
            item.HasWeatherConflictWarning = false;
            item.WeatherConflictTitle = string.Empty;
            item.WeatherConflictMessage = string.Empty;
            return;
        }

        if (!item.HasWeatherForecast)
        {
            item.HasWeatherConflictWarning = false;
            item.WeatherConflictTitle = string.Empty;
            item.WeatherConflictMessage = string.Empty;
            return;
        }

        // Parse rain probability
        int rainProb = 0;
        if (!string.IsNullOrEmpty(item.RainProbabilityText))
        {
            int.TryParse(item.RainProbabilityText.Replace("%", "").Trim(), out rainProb);
        }

        // Parse UV
        double uv = 0;
        if (!string.IsNullOrEmpty(item.UvText))
        {
            double.TryParse(item.UvText.Trim(), out uv);
        }

        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (rainProb >= 40 || item.IsRainy || item.WeatherCondition.Contains("mưa", StringComparison.OrdinalIgnoreCase) || item.WeatherCondition.Contains("rain", StringComparison.OrdinalIgnoreCase) || item.WeatherCondition.Contains("dông", StringComparison.OrdinalIgnoreCase) || item.WeatherCondition.Contains("storm", StringComparison.OrdinalIgnoreCase))
        {
            item.HasWeatherConflictWarning = true;
            item.WeatherConflictTitle = isVi 
                ? "Cảnh Báo Mưa Cho Kế Hoạch Ngoài Trời" 
                : "Rain Alert for Outdoor Event";
            item.WeatherConflictMessage = isVi
                ? $"Bạn có kế hoạch ngoài trời: \"{outdoorEvent.Title}\", nhưng ngày này dự báo có {item.WeatherCondition.ToLower()} (khả năng mưa {item.RainProbabilityText}). Hãy chuẩn bị ô che hoặc cân nhắc dời lịch!"
                : $"You have an outdoor plan: \"{outdoorEvent.Title}\", but the forecast indicates {item.WeatherCondition} ({item.RainProbabilityText} rain chance). Please prepare an umbrella or consider rescheduling!";
        }
        else if (uv >= 8.0)
        {
            item.HasWeatherConflictWarning = true;
            item.WeatherConflictTitle = isVi 
                ? "Cảnh Báo Nắng Gắt & Tia UV Cao" 
                : "Intense Sun & High UV Warning";
            item.WeatherConflictMessage = isVi
                ? $"Bạn có kế hoạch ngoài trời: \"{outdoorEvent.Title}\", ngày này nắng gắt với chỉ số UV cực cao ({item.UvText}). Cần bôi kem chống nắng và che chắn kỹ khi ra ngoài!"
                : $"You have an outdoor plan: \"{outdoorEvent.Title}\", but the forecast shows extreme UV index ({item.UvText}). Apply sunscreen and wear protective gear!";
        }
        else if (item.TempMax >= 36.0)
        {
            item.HasWeatherConflictWarning = true;
            item.WeatherConflictTitle = isVi 
                ? "Cảnh Báo Nắng Nóng Gay Gắt" 
                : "Extreme Heat Warning";
            item.WeatherConflictMessage = isVi
                ? $"Nhiệt độ dự báo lên đến {Math.Round(item.TempMax)}°. Hoạt động ngoài trời \"{outdoorEvent.Title}\" có nguy cơ say nắng, nên tổ chức vào sáng sớm hoặc chiều mát!"
                : $"Forecast high reaches {Math.Round(item.TempMax)}°. Outdoor activity \"{outdoorEvent.Title}\" has high heatstroke risk, schedule in early morning or late afternoon!";
        }
        else if (item.TempMin <= 12.0 && item.TempMin > -50)
        {
            item.HasWeatherConflictWarning = true;
            item.WeatherConflictTitle = isVi 
                ? "Cảnh Báo Trời Rét Buốt" 
                : "Severe Cold Warning";
            item.WeatherConflictMessage = isVi
                ? $"Nhiệt độ thấp nhất dự báo {Math.Round(item.TempMin)}°, trời rét buốt. Nhớ chuẩn bị áo ấm giữ nhiệt cho: \"{outdoorEvent.Title}\"!"
                : $"Forecast low is {Math.Round(item.TempMin)}°, freezing cold weather. Remember warm thermal layers for: \"{outdoorEvent.Title}\"!";
        }
        else
        {
            item.HasWeatherConflictWarning = false;
            item.WeatherConflictTitle = string.Empty;
            item.WeatherConflictMessage = string.Empty;
        }
    }

    public void AddCalendarUserEvent(string title, string category, bool isOutdoor, bool hasSpecificTime = true, string eventTime = "09:00", int reminderMinutes = 30)
    {
        if (SelectedCalendarDay == null || string.IsNullOrWhiteSpace(title)) return;

        string colorHex = category switch
        {
            "Ngoài trời" or "Outdoor" => "#10B981", // Green
            "Công việc" or "Work" => "#3B82F6",    // Blue
            "Gia đình" or "Family" => "#EC4899",   // Pink
            "Thể thao" or "Sports" => "#F59E0B",   // Amber
            "Kỷ niệm" or "Celebration" or "Anniversary" => "#8B5CF6", // Purple
            "Cúng lễ" or "Spiritual" or "Traditional" => "#DC2626", // Red
            _ => "#6366F1"
        };

        var newEvent = new CalendarUserEvent
        {
            Date = SelectedCalendarDay.Date,
            Title = title.Trim(),
            Category = category,
            IsOutdoor = isOutdoor,
            ColorHex = colorHex,
            HasSpecificTime = hasSpecificTime,
            EventTime = eventTime,
            ReminderMinutesBefore = reminderMinutes,
            IsReminded = false
        };

        _calendarEventService.AddEvent(newEvent);
        _allUserEvents.Add(newEvent);

        SelectedCalendarDay.UserEvents.Add(newEvent);
        SelectedCalendarDay.NotifyEventsChanged();
        EvaluateWeatherConflict(SelectedCalendarDay);

        OnPropertyChanged(nameof(CalendarDays));
    }

    public void SetSelectedDayGoal(string goalText, string goalIcon = "🎯")
    {
        if (SelectedCalendarDay == null) return;
        string dateKey = SelectedCalendarDay.Date.ToString("yyyy-MM-dd");

        _calendarEventService.SetGoal(dateKey, goalText, goalIcon);
        _allDailyGoals = _calendarEventService.LoadGoals();

        var goal = _allDailyGoals.FirstOrDefault(g => g.DateKey == dateKey);
        SelectedCalendarDay.HasGoal = goal != null && !string.IsNullOrWhiteSpace(goal.GoalText);
        SelectedCalendarDay.GoalText = goal?.GoalText ?? "";
        SelectedCalendarDay.IsGoalCompleted = goal?.IsCompleted ?? false;
        SelectedCalendarDay.GoalIcon = string.IsNullOrEmpty(goal?.GoalIcon) ? "🎯" : goal.GoalIcon;
        SelectedCalendarDay.NotifyEventsChanged();

        OnPropertyChanged(nameof(CalendarDays));
    }

    public void ToggleSelectedDayGoal()
    {
        if (SelectedCalendarDay == null) return;
        string dateKey = SelectedCalendarDay.Date.ToString("yyyy-MM-dd");

        bool newState = _calendarEventService.ToggleGoal(dateKey);
        _allDailyGoals = _calendarEventService.LoadGoals();

        SelectedCalendarDay.IsGoalCompleted = newState;
        SelectedCalendarDay.NotifyEventsChanged();
        OnPropertyChanged(nameof(CalendarDays));
    }

    public void DeleteSelectedDayGoal()
    {
        if (SelectedCalendarDay == null) return;
        string dateKey = SelectedCalendarDay.Date.ToString("yyyy-MM-dd");

        _calendarEventService.DeleteGoal(dateKey);
        _allDailyGoals = _calendarEventService.LoadGoals();

        SelectedCalendarDay.HasGoal = false;
        SelectedCalendarDay.GoalText = "";
        SelectedCalendarDay.IsGoalCompleted = false;
        SelectedCalendarDay.NotifyEventsChanged();
        OnPropertyChanged(nameof(CalendarDays));
    }

    public void DeleteCalendarUserEvent(string eventId)
    {
        if (SelectedCalendarDay == null || string.IsNullOrEmpty(eventId)) return;

        _calendarEventService.DeleteEvent(eventId);
        _allUserEvents.RemoveAll(e => e.Id == eventId);

        var existing = SelectedCalendarDay.UserEvents.FirstOrDefault(e => e.Id == eventId);
        if (existing != null)
        {
            SelectedCalendarDay.UserEvents.Remove(existing);
        }
        SelectedCalendarDay.NotifyEventsChanged();
        EvaluateWeatherConflict(SelectedCalendarDay);

        OnPropertyChanged(nameof(CalendarDays));
    }

    #endregion
}
