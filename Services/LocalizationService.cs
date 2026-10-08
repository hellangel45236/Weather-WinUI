using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace WeatherApp.Services;

/// <summary>
/// Service quản lý đa ngôn ngữ (Tiếng Việt và English) cho toàn bộ ứng dụng Weather App.
/// Hỗ trợ thông báo reactive cho XAML {x:Bind} và giao diện co giãn responsive.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    private string _currentLanguage = "vi-VN";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            string normalized = value?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en-US" : "vi-VN";
            if (_currentLanguage != normalized)
            {
                _currentLanguage = normalized;
                try
                {
                    var culture = new CultureInfo(_currentLanguage);
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                }
                catch { }

                OnPropertyChanged(string.Empty);
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsVietnamese => _currentLanguage.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
    public bool IsEnglish => !IsVietnamese;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
    }

    #region Navigation Bar Strings

    public string NavOverview => IsVietnamese ? "Tổng Quan" : "Overview";
    public string NavFlood => IsVietnamese ? "Triều Cường & Ngập Lụt" : "Tide & Flood Alert";
    public string NavRadar => IsVietnamese ? "Bản Đồ & Radar" : "Map & Radar";
    public string NavLifestyle => IsVietnamese ? "Đời Sống & OOTD" : "Lifestyle & OOTD";
    public string NavCalendar => IsVietnamese ? "Lịch Âm Dương" : "Lunar Calendar";
    public string NavWidget => "Widget Studio";
    public string NavSettings => IsVietnamese ? "Cài Đặt & Giao Diện" : "Settings & Theme";

    #endregion

    #region Top Bar & Header Strings

    public string AppSubtitle => IsVietnamese ? "Dự báo thời tiết & Lời khuyên thông minh" : "Weather Forecast & Smart Insights";
    public string SearchPlaceholder => IsVietnamese ? "Tìm kiếm thành phố (Hà Nội, TP.HCM, Tokyo, Paris...)" : "Search city (Hanoi, HCMC, Tokyo, Paris...)";
    public string RefreshTooltip => IsVietnamese ? "Cập nhật thời tiết mới nhất" : "Refresh latest weather";
    public string UnitTooltip => IsVietnamese ? "Chuyển đổi giữa °C và °F" : "Toggle between °C and °F";
    public string ThemeTooltip => IsVietnamese ? "Chọn chế độ giao diện" : "Select appearance theme";
    public string ThemeLight => IsVietnamese ? "☀️ Sáng" : "☀️ Light";
    public string ThemeDark => IsVietnamese ? "🌙 Tối" : "🌙 Dark";
    public string ThemeSystem => IsVietnamese ? "💻 Hệ thống" : "💻 System";
    public string WidgetBtnText => "Widget";
    public string WidgetTooltip => IsVietnamese ? "Bật hoặc quản lý các Widget tiện ích đa thành phố trên Desktop" : "Enable or manage desktop widgets";
    public string WidgetOpenCurrent => IsVietnamese ? "📍 Mở Widget cho vị trí hiện tại" : "📍 Open Widget for current location";
    public string WidgetOpenCustom => IsVietnamese ? "➕ Tạo Widget mới cho thành phố khác..." : "➕ Create new Widget for other city...";
    public string WidgetCloseAll => IsVietnamese ? "❌ Đóng tất cả Widget đang mở" : "❌ Close all open widgets";
    public string GpsLocation => IsVietnamese ? "Vị trí GPS" : "GPS Location";
    public string GpsTooltip => IsVietnamese ? "Bấm để lấy lại thời tiết tại vị trí GPS thực tế của bạn" : "Click to get weather at your actual GPS location";

    #endregion

    #region Overview Tab Strings

    public string FeelsLike => IsVietnamese ? "Cảm nhận như" : "Feels like";
    public string Humidity => IsVietnamese ? "Độ ẩm" : "Humidity";
    public string Wind => IsVietnamese ? "Gió" : "Wind";
    public string UvIndex => IsVietnamese ? "Chỉ số UV" : "UV Index";
    public string Pressure => IsVietnamese ? "Áp suất" : "Pressure";
    public string Visibility => IsVietnamese ? "Tầm nhìn" : "Visibility";
    public string DewPoint => IsVietnamese ? "Điểm sương" : "Dew point";
    public string Sunrise => IsVietnamese ? "Bình minh" : "Sunrise";
    public string Sunset => IsVietnamese ? "Hoàng hôn" : "Sunset";
    public string AirQuality => IsVietnamese ? "Không khí (AQI)" : "Air Quality (AQI)";
    public string HourlyForecast => IsVietnamese ? "Dự Báo Theo Giờ (24h)" : "Hourly Forecast (24h)";
    public string DailyForecast => IsVietnamese ? "Dự Báo 7 Ngày Tới" : "7-Day Forecast";
    public string Today => IsVietnamese ? "Hôm nay" : "Today";
    public string Tomorrow => IsVietnamese ? "Ngày mai" : "Tomorrow";
    public string RainChance => IsVietnamese ? "Khả năng mưa" : "Rain chance";
    public string Precipitation => IsVietnamese ? "Lượng mưa" : "Precipitation";
    public string WeatherSummaryTitle => IsVietnamese ? "Tóm Tắt Khí Tượng Tự Nhiên" : "Natural Weather Summary";
    public string ShareWeather => IsVietnamese ? "Chia sẻ" : "Share";
    public string ShareTooltip => IsVietnamese ? "Chụp và sao chép ảnh thời tiết tuyệt đẹp vào Clipboard (Ctrl+V)" : "Capture and copy weather image to clipboard (Ctrl+V)";

    public string TodayWeatherDetails => IsVietnamese ? "Chi Tiết Thời Tiết Hôm Nay" : "Today's Weather Details";
    public string EssentialMetrics => IsVietnamese ? "8 Chỉ Số Khí Tượng Thiết Yếu" : "8 Essential Weather Metrics";
    public string RelativeHumidityDesc => IsVietnamese ? "Độ ẩm tương đối không khí" : "Relative air humidity";
    public string WindAndDirection => IsVietnamese ? "Gió & Hướng" : "Wind & Direction";
    public string PrecipProbDesc => IsVietnamese ? "Xác suất kết tủa hôm nay" : "Precipitation probability today";
    public string SeaPressureDesc => IsVietnamese ? "Chuẩn khí quyển biển" : "Sea-level atmospheric standard";
    public string Past24hPrecipDesc => IsVietnamese ? "Lũy kế 24 giờ qua" : "Cumulative over past 24h";
    public string SunAndMoon => IsVietnamese ? "Mặt Trời & Lặn" : "Sun & Sunset";
    public string AqiScaleDesc => IsVietnamese ? "Thang đo chất lượng US-EPA" : "US-EPA quality index scale";

    public string ScrubberHeader => IsVietnamese ? "Tua Nhanh Dự Báo 24 Giờ" : "24-Hour Forecast Scrubber";
    public string ResetScrubber => IsVietnamese ? "Về hiện tại" : "Reset to Now";
    public string ResetScrubberTooltip => IsVietnamese ? "Bấm để quay về xem thời tiết thời gian thực" : "Click to return to real-time weather";

    public string AdviceHeader => IsVietnamese ? "Lời khuyên & Gợi ý thông minh" : "Smart Advice & Recommendations";
    public string AiBriefingHeader => IsVietnamese ? "Dự Báo & Lời Khuyên Khí Tượng Hôm Nay" : "Today's Meteorological Briefing & Advice";
    public string FeedbackTitle => IsVietnamese ? "Thời tiết thực tế có chính xác không?" : "Is the current weather accurate?";
    public string FeedbackAccurate => IsVietnamese ? "Chính xác" : "Accurate";
    public string FeedbackInaccurate => IsVietnamese ? "Chưa đúng" : "Inaccurate";
    public string FeedbackWhatIsActual => IsVietnamese ? "Thực tế ngoài trời tại bạn đang như thế nào?" : "What is the actual weather outside?";
    public string FeedbackClose => IsVietnamese ? "Đóng" : "Close";
    public string FeedbackChangeAgain => IsVietnamese ? "Đổi lại" : "Change";

    #endregion

    #region Lifestyle & OOTD Tab Strings
    public string OotdHeaderTitle => IsVietnamese ? "Hôm Nay Mặc Gì?" : "What to Wear Today?";
    public string OotdHeaderSub => IsVietnamese ? "Hệ thống tư vấn thời trang thông minh OOTD & bảo vệ sức khỏe theo khí tượng" : "AI-driven outfit stylist & wellness recommendations based on real-time weather";
    public string OotdBadge => "OOTD Studio v3.0";
    public string OotdOccasionWork => IsVietnamese ? "Công sở" : "Office";
    public string OotdOccasionSchool => IsVietnamese ? "Đi học" : "Campus";
    public string OotdOccasionCasual => IsVietnamese ? "Dạo phố" : "Casual";
    public string OotdOccasionSport => IsVietnamese ? "Thể thao" : "Sport";
    public string OotdOccasionTravel => IsVietnamese ? "Dã ngoại" : "Travel";
    public string OotdGenderAll => IsVietnamese ? "Tự do" : "Unisex";
    public string OotdGenderMen => IsVietnamese ? "Nam" : "Men";
    public string OotdGenderWomen => IsVietnamese ? "Nữ" : "Women";
    public string OotdCopyBtn => IsVietnamese ? "Sao chép gợi ý" : "Copy Advice";
    public string OotdTopClothing => IsVietnamese ? "Áo & Thân trên" : "Tops & Upper Body";
    public string OotdBottomClothing => IsVietnamese ? "Quần & Thân dưới" : "Bottoms & Pants";
    public string OotdOuterwear => IsVietnamese ? "Áo khoác & Che chắn" : "Outerwear & Layers";
    public string OotdFootwear => IsVietnamese ? "Giày & Dép" : "Footwear & Shoes";
    public string OotdAccessories => IsVietnamese ? "Vật Dụng & Phụ Kiện Nên Mang Theo" : "Essential Accessories & Items";
    public string OotdColorPalette => IsVietnamese ? "Bảng màu đề xuất hôm nay" : "Today's Color Palette";
    public string OotdFabricAdvice => IsVietnamese ? "Chất liệu khuyên dùng" : "Recommended Fabric";
    public string OotdMotorbikeCommute => IsVietnamese ? "Cảnh Báo Di Chuyển Xe Máy & 2 Bánh" : "Two-Wheeler & Motorbike Advice";
    public string OotdRaincoatAdvice => IsVietnamese ? "Trang bị đi mưa" : "Rain Gear Advice";

    public string LifestyleHeader => IsVietnamese ? "Chỉ Số Hoạt Động & Đời Sống" : "Lifestyle & Activity Indices";
    public string LifestyleSub => IsVietnamese ? "Khuyến nghị sinh hoạt & chăm sóc sức khỏe theo điều kiện khí tượng" : "Practical living & health recommendations based on weather conditions";

    public string WorkoutWindowsHeader => IsVietnamese ? "Khung Giờ Vàng Thể Thao & Vận Động" : "Best Workout Windows";
    public string WorkoutWindowsSub => IsVietnamese ? "Thời điểm lý tưởng nhất trong ngày cho chạy bộ, đạp xe & tập luyện" : "Optimal timeframes today for running, cycling & workouts";

    public string AirQualityHeader => IsVietnamese ? "Chất Lượng Không Khí & Bụi Mịn" : "Air Quality & Pollution Tracker";
    public string AirQualitySub => IsVietnamese ? "Thang đo US-AQI và bảo vệ đường hô hấp" : "US-AQI scale and respiratory health protection";
    public string HealthAdviceTitle => IsVietnamese ? "Lời khuyên sức khỏe khí tượng" : "Health & Medical Advice";
    public string SkinDefenseHeader => IsVietnamese ? "Chỉ Số Chống Nắng UV & Bảo Vệ Da" : "UV Defense & Skincare Index";
    public string SkinDefenseSub => IsVietnamese ? "Khuyến nghị SPF kem chống nắng và giới hạn phơi nắng an toàn" : "Sunscreen SPF advice & safe sun exposure limit";
    public string RecommendedSpfLabel => IsVietnamese ? "Chỉ số SPF khuyên dùng" : "Recommended SPF";
    public string SafeSunTimeLabel => IsVietnamese ? "Thời gian nắng an toàn" : "Safe Sun Time";
    public string MaskAdviceLabel => IsVietnamese ? "Khuyến nghị khẩu trang" : "Mask Advice";

    public string Pm25Label => IsVietnamese ? "Bụi mịn PM2.5" : "Fine Dust PM2.5";
    public string Pm10Label => IsVietnamese ? "Bụi thô PM10" : "Inhalable Dust PM10";
    public string OzoneLabel => IsVietnamese ? "Khí Ozone (O₃)" : "Ozone (O₃)";
    public string No2Label => IsVietnamese ? "Khí Nitơ (NO₂)" : "Nitrogen Dioxide (NO₂)";
    #endregion

    #region Settings Tab Strings

    public string SettingsTitle => IsVietnamese ? "Trung Tâm Cài Đặt & Cá Nhân Hóa (Version 3.0.3)" : "Settings & Personalization Center (Version 3.0.3)";
    public string SettingsSubtitle => IsVietnamese ? "Quản lý giao diện, ngôn ngữ, biểu tượng, đơn vị, hình nền và hiệu năng hệ thống" : "Manage appearance, language, icons, units, wallpapers and system performance";
    public string SettingsHeaderTitle => SettingsTitle;
    public string SettingsHeaderSubtitle => SettingsSubtitle;
    public string SaveAndApply => IsVietnamese ? "Lưu & Áp Dụng" : "Save & Apply";
    public string SaveApplyButton => SaveAndApply;
    public string VersionOfficial => IsVietnamese ? "Version 3.0.3 (Chính Thức)" : "Version 3.0.3 (Official)";
    public string VersionBadge => VersionOfficial;

    public string CatPersonalization => IsVietnamese ? "Cá nhân hóa" : "Personalization";
    public string CatAppearance => IsVietnamese ? "Giao diện & Biểu tượng" : "Theme & Icons";
    public string CatTimeUnits => IsVietnamese ? "Thời gian & Đơn vị" : "Time & Units";
    public string CatCityBg => IsVietnamese ? "Hình nền thành phố" : "City Wallpapers";
    public string CatWidget => IsVietnamese ? "Widget Desktop" : "Desktop Widgets";
    public string CatNotifications => IsVietnamese ? "Thông báo & Khay" : "Notifications & Tray";
    public string CatPerformance => IsVietnamese ? "Hiệu năng & Khởi động" : "Performance & Startup";
    public string CatAbout => IsVietnamese ? "Giới thiệu & Changelog" : "About & Changelog";
    public string CatCommute => IsVietnamese ? "Đi làm & Trang phục" : "Commute & Outfit";

    public string PersonalizationHeader => IsVietnamese ? "Cá Nhân Hóa" : "Personalization";
    public string PersonalizationSub => IsVietnamese ? "Thiết lập tên hiển thị, ngôn ngữ và trải nghiệm cá nhân của bạn" : "Configure display name, language, and your personal experience";
    public string UserNameTitle => IsVietnamese ? "Tên của bạn" : "Your Name";
    public string UserNameSub => IsVietnamese ? "Tên hiển thị trong lời chào và các thông báo hàng ngày" : "Name displayed in greetings and daily notifications";
    public string UserNamePlaceholder => IsVietnamese ? "Nhập tên..." : "Enter name...";

    public string SettingsLanguage => IsVietnamese ? "Ngôn ngữ giao diện" : "Interface Language";
    public string SettingsLanguageDesc => IsVietnamese ? "Lựa chọn hiển thị Tiếng Việt hoặc English (Tự động thích ứng co giãn giao diện)" : "Choose Vietnamese or English display (Responsive layout adapts to word lengths)";
    public string LangVietnamese => "🇻🇳 Tiếng Việt";
    public string LangEnglish => "🇬🇧 English";

    public string TimeUnitsHeader => IsVietnamese ? "Thời Gian & Đơn Vị Đo Lường" : "Time & Measurement Units";
    public string TimeUnitsSub => IsVietnamese ? "Tùy chỉnh định dạng giờ, múi giờ và các đơn vị thời tiết" : "Customize time format, timezone, and weather measurement units";
    public string TimezoneTitle => IsVietnamese ? "Múi giờ đồng hồ" : "Clock Timezone";
    public string TimezoneSub => IsVietnamese ? "Chọn múi giờ hiển thị trên đồng hồ thời tiết" : "Select timezone displayed on the weather clock";
    public string HourFormatTitle => IsVietnamese ? "Định dạng giờ" : "Time Format";
    public string HourFormatSub => IsVietnamese ? "Hiển thị theo chuẩn 24 giờ hoặc 12 giờ AM/PM" : "Display in 24-hour format or 12-hour AM/PM";
    public string DateFormatTitle => IsVietnamese ? "Định dạng ngày tháng" : "Date Format";
    public string DateFormatSub => IsVietnamese ? "Kiểu hiển thị ngày tháng trên màn hình" : "Date presentation format on screen";
    public string FirstDayOfWeekTitle => IsVietnamese ? "Ngày bắt đầu tuần" : "First Day of Week";
    public string FirstDayOfWeekSub => IsVietnamese ? "Chọn ngày đầu tiên hiển thị trên Lịch tháng" : "Select first day of the week on monthly calendar";
    public string TempUnitTitle => IsVietnamese ? "Đơn vị nhiệt độ" : "Temperature Unit";
    public string TempUnitSub => IsVietnamese ? "Hiển thị độ Celsius (°C) hoặc Fahrenheit (°F)" : "Display in Celsius (°C) or Fahrenheit (°F)";
    public string WindUnitTitle => IsVietnamese ? "Đơn vị tốc độ gió" : "Wind Speed Unit";
    public string WindUnitSub => IsVietnamese ? "km/h, m/s hoặc mph" : "km/h, m/s, or mph";
    public string PressureUnitTitle => IsVietnamese ? "Đơn vị áp suất khí quyển" : "Atmospheric Pressure Unit";
    public string PressureUnitSub => IsVietnamese ? "hPa hoặc mmHg" : "hPa or mmHg";
    public string PrecipUnitTitle => IsVietnamese ? "Đơn vị lượng mưa" : "Precipitation Unit";
    public string PrecipUnitSub => IsVietnamese ? "Milimét (mm) hoặc Inch (in)" : "Millimeters (mm) or Inches (in)";

    #endregion

    #region Urban Flood Tab Strings

    public string FloodStationHeader => IsVietnamese ? "Trạm Thủy Văn & Thoát Nước Đô Thị" : "Hydro & Urban Drainage Station";
    public string LiveBadge => IsVietnamese ? "TRỰC TIẾP" : "LIVE";
    public string FloodStationSub => IsVietnamese ? "• Giám sát triều cường, lượng mưa & năng lực tiêu thoát" : "• Monitoring tide levels, rainfall & urban drainage capacity";
    public string CurrentTidePod => IsVietnamese ? "Mực Nước Triều" : "Tide Level";
    public string RainIntensityPod => IsVietnamese ? "Vũ Lượng Mưa" : "Rainfall Rate";
    public string NextPeakPod => IsVietnamese ? "Đỉnh Kế Tiếp" : "Next Peak";
    public string TrackedSpotsPod => IsVietnamese ? "Điểm Trũng Theo Dõi" : "Flood Hotspots";
    public string HotspotsCountUnit => IsVietnamese ? "tuyến đường" : "roads";
    public string TideChartHeader => IsVietnamese ? "Đồ Thị Sóng Bán Nhật Triều 24 Giờ (Sông Sài Gòn & Nhà Bè)" : "24-Hour Semidiurnal Tidal Wave Chart (Saigon & Nha Be River)";
    public string TideChartSub => IsVietnamese ? "Mô phỏng chu kỳ biến thiên mực nước thiên văn 2 đỉnh 2 đáy kết hợp lực hút vũ trụ" : "Simulating astronomical tidal fluctuation with 2 highs and 2 lows daily";
    public string MonitoringStation => IsVietnamese ? "TRẠM QUAN TRẮC" : "MONITORING STATION";
    public string CurrentWaterColumn => IsVietnamese ? "Cột Nước Hiện Tại" : "Current Water Level";
    public string MetersUnit => IsVietnamese ? "mét (m)" : "meters (m)";
    public string SafeLegend => IsVietnamese ? "<1.40m: An toàn (Thông thoáng)" : "<1.40m: Safe (Clear)";
    public string Alarm1Legend => IsVietnamese ? "1.40m: Báo động I (Mực nước cao)" : "1.40m: Alert I (High water)";
    public string Alarm2Legend => IsVietnamese ? "1.55m: Báo động II (Ngập mép bờ kè)" : "1.55m: Alert II (Edge flooding)";
    public string Alarm3Legend => IsVietnamese ? "≥1.60m: Báo động III (Ngập tràn đường)" : "≥1.60m: Alert III (Road flooded)";

    public string InlandDrainageHeader => IsVietnamese ? "Hạ Tầng Thủy Lợi & Tiêu Thoát Nước Đô Thị" : "Urban Drainage & Hydraulic Infrastructure";
    public string InlandDrainageSub => IsVietnamese ? "Giám sát năng lực lưu vực sông Tô Lịch, Nhuệ, Kim Ngưu & hệ thống trạm bơm tiêu úng" : "Monitoring basin capacity of To Lich, Nhue, Kim Nguu rivers & pumping systems";
    public string InlandOperatingSmooth => IsVietnamese ? "VẬN HÀNH THÔNG SUỐT" : "OPERATING SMOOTHLY";

    public string MotorbikeAdviceHeader => IsVietnamese ? "Lưu Ý Di Chuyển Xe Máy" : "Motorbike Travel Safety";
    public string MotorbikeSafetyLimit => IsVietnamese ? "Giới hạn an toàn < 20 cm (dưới lọc gió)" : "Safe limit < 20 cm (below air filter)";
    public string CarAdviceHeader => IsVietnamese ? "Lưu Ý Di Chuyển Xe Ô Tô" : "Car Travel Safety";
    public string CarSafetyLimit => "Sedan: <20cm | CUV/SUV: <35cm";
    public string SkillThroughWater => IsVietnamese ? "Kỹ năng khi qua vũng:" : "Technique in water:";
    public string SkillMotorbike => IsVietnamese ? "Đi số 1-2, giữ đều tay ga" : "Low gear 1-2, steady throttle";
    public string WarningAbsolute => IsVietnamese ? "Cảnh báo tuyệt đối:" : "Strict warning:";
    public string WarningMotorbike => IsVietnamese ? "Không giảm ga giữa vũng ngập" : "Never drop throttle mid-flood";
    public string RuleCrucial => IsVietnamese ? "Quy tắc cốt tử:" : "Crucial rule:";
    public string RuleCarAc => IsVietnamese ? "TẮT ĐIỀU HÒA (A/C) ngay" : "TURN OFF A/C immediately";
    public string WhenCarStalls => IsVietnamese ? "Khi xe chết máy:" : "If engine stalls:";
    public string ActionHydrolock => IsVietnamese ? "KHÔNG ĐỀ LẠI - GỌI CỨU HỘ" : "DO NOT RESTART - CALL TOWING";

    public string RoadDirectoryHeader => IsVietnamese ? "Danh Mục Tuyến Đường Nguy Cơ Ngập Úng Đô Thị" : "Urban Road Flood Risk Directory";
    public string RoadDirectorySub => IsVietnamese ? "Dữ liệu điểm ngập lịch sử, trắc địa lòng chảo và phản hồi triều cường mới nhất" : "Historical flood data, basin topography and latest tidal feedback";
    public string DeepFlood => IsVietnamese ? "Ngập sâu:" : "Deep flood:";
    public string ModerateFlood => IsVietnamese ? "Ngập vừa:" : "Moderate flood:";
    public string RoadSearchPlaceholder => IsVietnamese ? "Tìm tên đường (Huỳnh Tấn Phát, Nguyễn Hữu Cảnh, Thái Hà...)" : "Search road (Huynh Tan Phat, Nguyen Huu Canh, Thai Ha...)";
    public string FilterAll => IsVietnamese ? "Tất Cả" : "All";
    public string FilterTide => IsVietnamese ? "Do Triều Cường" : "Tidal Flood";
    public string FilterRain => IsVietnamese ? "Do Mưa Lớn" : "Heavy Rain";
    public string FilterCritical => IsVietnamese ? "Nguy Cơ Cao (≥30cm)" : "Critical (≥30cm)";
    public string ExpandRoads => IsVietnamese ? "Xem tất cả tuyến đường" : "View all roads";
    public string CollapseRoads => IsVietnamese ? "Thu gọn danh sách" : "Collapse list";
    public string HandbookHeader => IsVietnamese ? "Cẩm Nang Kỹ Năng An Toàn & Xử Lý Sự Cố Đô Thị" : "Urban Safety & Emergency Handbook";
    public string HandbookSub => IsVietnamese ? "Quy tắc an toàn giao thông, bảo vệ thiết bị điện và số điện thoại cứu hộ khẩn cấp" : "Traffic safety rules, electrical protection and emergency hotlines";
    public string PocketHandbook => IsVietnamese ? "CẨM NANG BỎ TÚI" : "POCKET GUIDE";

    public string Bento1Title => IsVietnamese ? "Khi Xe Chết Máy Giữa Vũng Ngập" : "When Vehicle Stalls in Water";
    public string Bento1Item1 => IsVietnamese ? "• Tuyệt đối KHÔNG đề nổ lại máy để tránh thủy kích phá máy." : "• NEVER attempt to restart the engine to avoid hydrolock damage.";
    public string Bento1Item2 => IsVietnamese ? "• Tắt chìa khóa điện ngay, dắt xe lên vị trí cao ráo." : "• Turn off ignition immediately, push vehicle to high ground.";
    public string Bento1Item3 => IsVietnamese ? "• Tháo bugi, hong khô lọc gió hoặc gọi cứu hộ giao thông." : "• Remove spark plug, dry air filter or call roadside rescue.";

    public string Bento2Title => IsVietnamese ? "Phòng Tránh Nguy Cơ Điện Giật" : "Preventing Electric Shock Hazards";
    public string Bento2Item1 => IsVietnamese ? "• Tránh xa cột đèn đường, trạm biến áp, biển quảng cáo ngập nước." : "• Stay away from lampposts, transformers, and submerged signboards.";
    public string Bento2Item2 => IsVietnamese ? "• Không chạm vào dây điện chùng võng trên đường." : "• Do not touch sagging or fallen power lines.";
    public string Bento2Item3 => IsVietnamese ? "• Tại gia đình: Ngắt cầu dao (Aptomat) tổng tầng trệt nếu nước tràn." : "• At home: Switch off the ground floor circuit breaker if water enters.";

    public string Bento3Title => IsVietnamese ? "Bảo Vệ Đồ Đạc & Chống Tràn" : "Protecting Belongings & Anti-Backflow";
    public string Bento3Item1 => IsVietnamese ? "• Lắp đặt tấm chắn kim loại hoặc bao cát chặn trước giờ đỉnh triều." : "• Set up flood barriers or sandbags before peak tide hour.";
    public string Bento3Item2 => IsVietnamese ? "• Kê cao thiết bị điện tử, tủ lạnh, máy giặt lên ít nhất 40-50 cm." : "• Elevate electronics, refrigerators, and washers at least 40-50 cm.";
    public string Bento3Item3 => IsVietnamese ? "• Bịt chặt các miệng cống thoát sàn tầng 1 chống nước trào ngược." : "• Seal ground floor drains securely to prevent sewage backflow.";

    public string Bento4Title => IsVietnamese ? "Đường Dây Nóng Cứu Hộ Khẩn Cấp" : "Emergency Rescue Hotlines";
    public string Bento4Item1 => IsVietnamese ? "• 114: Cảnh sát PCCC & Tìm kiếm cứu nạn khẩn cấp." : "• 114: Fire Fighting & Search and Rescue Emergency.";
    public string Bento4Item2 => IsVietnamese ? "• 115: Cấp cứu Y tế phục vụ vận chuyển bệnh nhân." : "• 115: Medical Emergency ambulance service.";
    public string Bento4Item3 => IsVietnamese ? "• 1022: Tổng đài phản ánh sự cố hạ tầng kỹ thuật đô thị." : "• 1022: Urban Technical Infrastructure Hotline.";

    #endregion

    #region Radar Tab Strings

    public string RadarHeader => IsVietnamese ? "Bản Đồ Thời Tiết & Giám Sát Radar Trực Quan" : "Visual Weather Map & Radar Monitoring";
    public string RadarSub => IsVietnamese ? "Quan sát mây vệ tinh, radar mưa phản hồi, hướng gió và lớp ngập úng đô thị" : "Satellite cloud cover, rain radar reflectivity, wind stream and urban flood overlay";
    public string RadarLiveBadge => IsVietnamese ? "THỜI GIAN THỰC" : "REAL-TIME";
    public string MapLayersTitle => IsVietnamese ? "Lớp Dữ Liệu Thời Tiết" : "Weather Data Layers";
    public string LayerClouds => IsVietnamese ? "Mây Vệ Tinh" : "Satellite Clouds";
    public string LayerRadar => IsVietnamese ? "Radar Mưa" : "Rain Radar";
    public string LayerWind => IsVietnamese ? "Gió Bề Mặt" : "Surface Wind";
    public string LayerTemp => IsVietnamese ? "Nhiệt Độ" : "Temperature";
    public string LayerFlood => IsVietnamese ? "Độ Sâu Ngập" : "Flood Depth";
    public string LayerPumps => IsVietnamese ? "Trạm Bơm & Cống" : "Pumping Stations";
    public string LayerRoads => IsVietnamese ? "Đường Đang Ngập" : "Flooded Roads";
    public string CityJumpTitle => IsVietnamese ? "Di Chuyển Nhanh Thành Phố" : "Quick City Navigation";

    public string RadarDopplerTitle => IsVietnamese ? "Đài Radar Doppler Khí Tượng 360°" : "360° Doppler Meteorological Radar";
    public string RadarDopplerSub => IsVietnamese ? "Hệ thống quét mây đối lưu thời gian thực & phân tích vi khí hậu đa tầng" : "Real-time convective cloud scanning & multi-layer microclimate analysis";
    public string RadarPause => IsVietnamese ? "Tạm dừng" : "Pause";
    public string RadarResume => IsVietnamese ? "Tiếp tục" : "Resume";
    public string RadarPauseTooltip => IsVietnamese ? "Bấm để tạm dừng hoặc tiếp tục vòng quét radar 360°" : "Click to pause or resume 360° radar sweep";
    public string RadarSpeedTooltip => IsVietnamese ? "Đổi tốc độ chùm tia quét (1x, 2x, 0.5x)" : "Change sweep beam speed (1x, 2x, 0.5x)";
    public string RadarPausedStatus => IsVietnamese ? "Đã Tạm Dừng" : "Paused";
    public string RadarScanningStatus => IsVietnamese ? "Đang Quét 360°" : "Scanning 360°";
    public string RadarSweepSpeed => IsVietnamese ? "Tốc độ: " : "Speed: ";
    public string RadarRadius => IsVietnamese ? "Bán kính:" : "Radius:";
    public string RadarObsLayer => IsVietnamese ? "Tầng quan trắc:" : "Layer:";
    public string RadarLayerPrecip => IsVietnamese ? "Mây Mưa (dBZ)" : "Rain Cloud (dBZ)";
    public string RadarLayerWind => IsVietnamese ? "Véc-tơ Gió" : "Wind Vectors";
    public string RadarLayerLightning => IsVietnamese ? "Tâm Dông & Sét" : "Storm & Lightning";
    public string RadarLayerThermal => IsVietnamese ? "Trường Nhiệt" : "Thermal Field";
    public string RadarScaleDbz => IsVietnamese ? "Thang phản hồi đối lưu dBZ:" : "Convective dBZ scale:";
    public string RadarScaleLight => IsVietnamese ? "10-25 dBZ (Mây mỏng)" : "10-25 dBZ (Light cloud)";
    public string RadarScaleModerate => IsVietnamese ? "30-45 dBZ (Mưa rào)" : "30-45 dBZ (Showers)";
    public string RadarScaleSevere => IsVietnamese ? "50-65+ dBZ (Dông sét / Mưa đá)" : "50-65+ dBZ (Storm / Hail)";
    public string RadarWindBeaufort => IsVietnamese ? "Gió Beaufort" : "Beaufort Wind";
    public string RadarAtmPressure => IsVietnamese ? "Áp Suất Khí Quyển" : "Atmospheric Pressure";
    public string RadarPressureStatus => IsVietnamese ? "Trạng thái cân bằng khí quyển" : "Atmospheric equilibrium state";
    public string RadarMaxReflectivity => IsVietnamese ? "Phản Hồi Cực Đại" : "Max Reflectivity";
    public string RadarDisasterRisk => IsVietnamese ? "Rủi Ro Thiên Tai" : "Disaster Risk";

    #endregion
}
