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
    public string FindingLocation => IsVietnamese ? "Đang tìm vị trí..." : "Locating position...";
    public string ShareSuccessTitle => IsVietnamese ? "📸 Đã chụp & sao chép ảnh thời tiết vào Clipboard!" : "📸 Weather image captured & copied to clipboard!";
    public string ShareSuccessMessage => IsVietnamese ? "Bạn có thể nhấn Ctrl + V để dán ảnh thời tiết cực đẹp này gửi cho bạn bè." : "Press Ctrl + V to paste and share this weather card with friends.";
    public string ErrorTitle => IsVietnamese ? "Không thể cập nhật" : "Unable to Update";

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
    public string AdviceEmpty => IsVietnamese ? "Thời tiết ôn hòa, thuận lợi cho mọi hoạt động ngoài trời." : "Mild and pleasant weather, suitable for all outdoor activities.";
    public string FeedbackTitle => IsVietnamese ? "Thời tiết thực tế có chính xác không?" : "Is the current weather accurate?";
    public string FeedbackAccurate => IsVietnamese ? "Chính xác" : "Accurate";
    public string FeedbackInaccurate => IsVietnamese ? "Chưa đúng" : "Inaccurate";
    public string FeedbackWhatIsActual => IsVietnamese ? "Thực tế ngoài trời tại bạn đang như thế nào?" : "What is the actual weather outside?";
    public string FeedbackClose => IsVietnamese ? "Đóng" : "Close";
    public string FeedbackChangeAgain => IsVietnamese ? "Đổi lại" : "Change";
    public string FeedbackPillTooltip => IsVietnamese ? "Góp ý thời tiết thực tế để cải thiện độ chính xác" : "Submit real-world weather feedback";

    public string CompassNorth => IsVietnamese ? "B" : "N";
    public string CompassSouth => IsVietnamese ? "N" : "S";
    public string CompassEast => IsVietnamese ? "Đ" : "E";
    public string CompassWest => IsVietnamese ? "T" : "W";

    public string UvLevelSafe => IsVietnamese ? "An toàn" : "Safe";
    public string UvLevelCaution => IsVietnamese ? "Cần che chắn" : "Use protection";
    public string UvLevelExtreme => IsVietnamese ? "Nguy hại" : "Extreme";
    public string UnifiedTimelineDesc => IsVietnamese ? "Kéo thanh trượt hoặc nhấp vào từng giờ để xem chi tiết" : "Drag slider or click an hour to preview details";

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

    public string SettingsTitle => IsVietnamese ? "Trung Tâm Cài Đặt & Cá Nhân Hóa (Version 3.0.6)" : "Settings & Personalization Center (Version 3.0.6)";
    public string SettingsSubtitle => IsVietnamese ? "Quản lý giao diện, ngôn ngữ, biểu tượng, đơn vị, hình nền và hiệu năng hệ thống" : "Manage appearance, language, icons, units, wallpapers and system performance";
    public string SettingsHeaderTitle => SettingsTitle;
    public string SettingsHeaderSubtitle => SettingsSubtitle;
    public string SaveAndApply => IsVietnamese ? "Lưu & Áp Dụng" : "Save & Apply";
    public string SaveApplyButton => SaveAndApply;
    public string VersionOfficial => IsVietnamese ? "Version 3.0.6 (Chính Thức)" : "Version 3.0.6 (Official)";
    public string VersionBadge => VersionOfficial;
    public string AppDescription => IsVietnamese ? "Ứng dụng thời tiết hiện đại, siêu nhẹ, tương thích hoàn hảo Windows 10 & 11" : "Modern, lightweight weather application, perfectly compatible with Windows 10 & 11";

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
    public string InlandPod1Title => IsVietnamese ? "LƯU VỰC SÔNG NỘI ĐÔ" : "INNER CITY RIVER BASIN";
    public string InlandPod1Name => IsVietnamese ? "Sông Tô Lịch & Sông Nhuệ" : "To Lich & Nhue Rivers";
    public string InlandPod1Desc => IsVietnamese ? "Lòng dẫn thông thoáng, mực nước đang dưới mức báo động 1, tiếp tục tự chảy tiêu úng tốt." : "Channels are clear, water level below Alert 1, gravity drainage operating effectively.";
    public string InlandPod2Title => IsVietnamese ? "TRẠM BƠM YÊN SỞ" : "YEN SO PUMPING STATION";
    public string InlandPod2Name => IsVietnamese ? "Công Suất 95 m³/s" : "Capacity 95 m³/s";
    public string InlandPod2Desc => IsVietnamese ? "Các tổ máy bơm cưỡng bức ra sông Hồng sẵn sàng kích hoạt ngay khi xuất hiện mưa lớn." : "Forced drainage pumps to Red River are ready to engage immediately when heavy rain occurs.";
    public string InlandPod3Title => IsVietnamese ? "HỒ ĐIỀU HÒA ĐÔ THỊ" : "URBAN RETENTION LAKES";
    public string InlandPod3Name => IsVietnamese ? "Hồ Tây & Linh Đàm" : "West Lake & Linh Dam Lake";
    public string InlandPod3Desc => IsVietnamese ? "Mực nước đệm được hạ thấp để dành dung tích trữ nước tối đa ứng phó mưa xối xả." : "Buffer water levels are lowered to maximize retention capacity against intense downpours.";

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

    #region Calendar Tab Strings
    public string CalendarTitle => IsVietnamese ? "Lịch Vạn Niên & Thời Tiết" : "Lunisolar Calendar & Weather";
    public string CalendarSubtitle => IsVietnamese ? "Tra cứu âm dương, can chi, tiết khí, hoàng đạo và lập kế hoạch theo thời tiết" : "Lunisolar dates, heavenly stems & earthly branches, zodiac hours & weather-aware planning";
    public string CalendarBadge => "Vạn Niên v3.0 Pro";
    public string CalendarSelectMonth => IsVietnamese ? "Chọn tháng" : "Select month";
    public string CalendarSelectYear => IsVietnamese ? "Chọn năm" : "Select year";
    public string CalendarGoToToday => IsVietnamese ? "Hôm nay" : "Today";
    public string CalendarPrevMonth => IsVietnamese ? "Tháng trước" : "Prev Month";
    public string CalendarNextMonth => IsVietnamese ? "Tháng sau" : "Next Month";
    public string CalendarLegend1st15th => IsVietnamese ? "Mùng 1 & Rằm" : "1st & 15th Lunar";
    public string CalendarLegendHoliday => IsVietnamese ? "Ngày Lễ" : "Holidays";
    public string CalendarLegendGoal => IsVietnamese ? "Mục tiêu" : "Daily Goal";
    public string CalendarLegendNotes => IsVietnamese ? "Ghi chú" : "Events";
    public string CalendarLegendWeatherConflict => IsVietnamese ? "Cảnh báo thời tiết" : "Weather Alert";
    public string CalendarClickHint => IsVietnamese ? "💡 Bấm vào ô ngày để xem chi tiết, giờ hoàng đạo & ghi chú" : "💡 Click a day cell to inspect details, zodiac hours & events";

    public string CalendarDayDetailHeader => IsVietnamese ? "Chi Tiết Ngày & Giờ Hoàng Đạo" : "Day Details & Auspicious Hours";
    public string CalendarTodayTag => IsVietnamese ? "Hôm nay" : "Today";
    public string CalendarSolarDateLabel => IsVietnamese ? "Dương lịch" : "Solar Date";
    public string CalendarLunarDateLabel => IsVietnamese ? "Âm lịch" : "Lunar Date";
    public string CalendarCanChiLabel => IsVietnamese ? "Can Chi Ba Trụ" : "Three Pillars (Year, Month, Day)";
    public string CalendarSolarTermLabel => IsVietnamese ? "Tiết Khí" : "Solar Term";
    public string CalendarAuspiciousDayLabel => IsVietnamese ? "Trực Ngày" : "Zodiac Status";
    public string CalendarAuspiciousHoursHeader => IsVietnamese ? "Giờ Hoàng Đạo Khởi Sự" : "Auspicious Zodiac Hours";
    public string CalendarAuspiciousHoursSub => IsVietnamese ? "Khung giờ vượng khí, thuận lợi xuất hành, ký hợp đồng & khai trương" : "Optimal golden hours for travel, business & celebrations";
    public string CalendarMoonPhaseHeader => IsVietnamese ? "Tuần Trăng & Độ Sáng" : "Moon Phase & Illumination";

    public string CalendarWeatherForecastHeader => IsVietnamese ? "Dự Báo Khí Tượng Chi Tiết" : "Detailed Weather Forecast";
    public string CalendarWeatherRainProb => IsVietnamese ? "Khả năng mưa" : "Rain Probability";
    public string CalendarWeatherUv => IsVietnamese ? "Chỉ số UV" : "UV Index";
    public string CalendarWeatherOutOfRange => IsVietnamese ? "Dữ liệu thời tiết chi tiết áp dụng cho 7 ngày tới. Khi ngày này đến gần sẽ tự động cập nhật." : "Detailed forecast available for the next 7 days. Will update automatically as date approaches.";

    public string CalendarDailyGoalHeader => IsVietnamese ? "Mục Tiêu Cá Nhân Trong Ngày" : "Daily Focus Goal";
    public string CalendarDailyGoalHasGoal => IsVietnamese ? "Đã đặt mục tiêu" : "Goal active";
    public string CalendarDailyGoalPlaceholder => IsVietnamese ? "Đặt mục tiêu cho ngày này..." : "Set goal for this day...";
    public string CalendarSaveGoal => IsVietnamese ? "Lưu" : "Save";

    public string CalendarUserEventsHeader => IsVietnamese ? "Kế Hoạch & Ghi Chú Cá Nhân" : "Personal Schedule & Events";
    public string CalendarNewEventPlaceholder => IsVietnamese ? "Nhập tên sự kiện / việc cần làm..." : "Enter event or task name...";
    public string CalendarCategoryOutdoor => IsVietnamese ? "🌲 Ngoài trời / Dã ngoại" : "🌲 Outdoor / Picnic";
    public string CalendarCategoryWork => IsVietnamese ? "💼 Công việc / Học tập" : "💼 Work / Study";
    public string CalendarCategoryFamily => IsVietnamese ? "👥 Gia đình / Bạn bè" : "👥 Family / Friends";
    public string CalendarCategorySport => IsVietnamese ? "🏃 Thể thao / Rèn luyện" : "🏃 Sports / Fitness";
    public string CalendarCategoryCeremony => IsVietnamese ? "🎉 Kỷ niệm / Tiệc tùng" : "🎉 Celebration / Party";
    public string CalendarCategorySpiritual => IsVietnamese ? "🏮 Cúng lễ / Tâm linh" : "🏮 Traditional / Spiritual";
    public string CalendarIsOutdoorLabel => IsVietnamese ? "Ngoài trời" : "Outdoor";
    public string CalendarHasTimeLabel => IsVietnamese ? "Có giờ" : "Has Time";
    public string CalendarSavePlanBtn => IsVietnamese ? "Lưu Kế Hoạch 📌" : "Save Plan 📌";
    public string CalendarCopyDayInfoBtn => IsVietnamese ? "Sao chép ngày âm & giờ tốt" : "Copy Date & Auspicious Hours";

    public string CalendarCountdownHeader => IsVietnamese ? "Đếm Ngược Sự Kiện & Lễ Hội Cổ Truyền" : "Upcoming Festivals & Cultural Events";
    public string CalendarConverterHeader => IsVietnamese ? "Bộ Tra Cứu & Đổi Ngày Âm ↔ Dương Nhanh" : "Lunisolar Date Converter";
    public string CalendarConverterSub => IsVietnamese ? "Tra cứu đối soát hai chiều chính xác giữa Dương lịch và Âm lịch Việt Nam" : "Bidirectional accurate conversion between Solar and Vietnamese Lunar calendar";
    public string CalendarConvertSolarToLunarBtn => IsVietnamese ? "Dương sang Âm" : "Solar to Lunar";
    public string CalendarConvertLunarToSolarBtn => IsVietnamese ? "Âm sang Dương" : "Lunar to Solar";
    public string CalendarSolarToLunarTitle => IsVietnamese ? "Dương Lịch ➔ Âm Lịch" : "Solar ➔ Lunar";
    public string CalendarLunarToSolarTitle => IsVietnamese ? "Âm Lịch ➔ Dương Lịch" : "Lunar ➔ Solar";
    public string CalendarPlaceholderDay => IsVietnamese ? "Ngày (1-30)" : "Day (1-30)";
    public string CalendarPlaceholderMonth => IsVietnamese ? "Tháng (1-12)" : "Month (1-12)";
    public string CalendarPlaceholderYear => IsVietnamese ? "Năm" : "Year";
    public string CalendarLeapMonthLabel => IsVietnamese ? "Tháng nhuận" : "Leap Month";
    public string CalendarQuickGoalExercise => IsVietnamese ? "Tập thể dục 🏃" : "Exercise 🏃";
    public string CalendarQuickGoalVegetarian => IsVietnamese ? "Ăn chay 🥗" : "Vegetarian 🥗";
    public string CalendarQuickGoalReading => IsVietnamese ? "Đọc sách 📖" : "Reading 📖";
    public string CalendarOutdoorActivityTag => IsVietnamese ? "🌲 Hoạt động ngoài trời" : "🌲 Outdoor activity";
    public string CalendarReminderNone => IsVietnamese ? "Không nhắc" : "No reminder";
    public string CalendarReminder15m => IsVietnamese ? "Trước 15p" : "15m before";
    public string CalendarReminder30m => IsVietnamese ? "Trước 30p" : "30m before";
    public string CalendarReminder1h => IsVietnamese ? "Trước 1h" : "1h before";

    #endregion

    #region Ambient Flyout & Weather Feedback Strings

    public string AmbientFlyoutTitle => IsVietnamese ? "Âm Thanh Thư Giãn" : "Relaxing Sounds";
    public string AmbientChooseSpace => IsVietnamese ? "Chọn không gian âm thanh thiên nhiên:" : "Choose nature soundscape:";
    public string SoundAutoTitle => IsVietnamese ? "🌐 Tự động theo thời tiết" : "🌐 Auto by weather";
    public string SoundAutoDesc => IsVietnamese ? "Tự khớp tiếng mưa hoặc gió theo thời tiết" : "Matches rain or wind to live weather";
    public string SoundRainTitle => IsVietnamese ? "🌧️ Mưa rào mùa hạ" : "🌧️ Summer rain";
    public string SoundRainDesc => IsVietnamese ? "Tiếng mưa rơi lộp độp êm dịu, dễ ngủ" : "Gentle patter of raindrops for sleep";
    public string SoundThunderTitle => IsVietnamese ? "⛈️ Sấm chớp đêm mưa" : "⛈️ Stormy night";
    public string SoundThunderDesc => IsVietnamese ? "Mưa rào nặng hạt kèm sấm rền từ xa" : "Heavy rainfall with rolling thunder";
    public string SoundPineWindTitle => IsVietnamese ? "🌲 Gió rừng thông" : "🌲 Pine forest wind";
    public string SoundPineWindDesc => IsVietnamese ? "Gió thoảng vi vu qua rặng thông đại ngàn" : "Breeze whispering through pine trees";
    public string SoundOceanTitle => IsVietnamese ? "🌊 Sóng biển dạt dào" : "🌊 Ocean waves";
    public string SoundOceanDesc => IsVietnamese ? "Từng đợt sóng dạt dào xô bờ cát thư thái" : "Relaxing waves lapping against the shore";
    public string SoundCafeRainTitle => IsVietnamese ? "☕ Mưa quán cà phê" : "☕ Rain at cafe";
    public string SoundCafeRainDesc => IsVietnamese ? "Tiếng mưa trầm ấm như ngồi bên hiên cà phê" : "Cozy rain sound from a porch cafe";

    public string FeedbackOptionSunny => IsVietnamese ? "Nắng đẹp" : "Sunny";
    public string FeedbackOptionHighUv => IsVietnamese ? "Nắng gắt UV" : "Intense UV";
    public string FeedbackOptionMildSun => IsVietnamese ? "Nắng nhẹ" : "Mild Sun";
    public string FeedbackOptionCloudy => IsVietnamese ? "Nhiều mây" : "Cloudy";
    public string FeedbackOptionDrizzle => IsVietnamese ? "Mưa phùn" : "Drizzle";
    public string FeedbackOptionRaining => IsVietnamese ? "Đang mưa" : "Raining";
    public string FeedbackOptionHeavyRain => IsVietnamese ? "Mưa rất to" : "Heavy Rain";
    public string FeedbackOptionThunder => IsVietnamese ? "Dông sét" : "Thunderstorm";
    public string FeedbackOptionFog => IsVietnamese ? "Sương mù" : "Foggy";
    public string FeedbackOptionWindy => IsVietnamese ? "Gió to" : "Windy";
    public string FeedbackOptionCold => IsVietnamese ? "Rét buốt" : "Freezing Cold";

    // Overview Tooltips & Buttons
    public string Toggle1224HoursTooltip => IsVietnamese ? "Nhấp để chuyển đổi giữa định dạng 24 Giờ và 12 Giờ (AM/PM)" : "Click to toggle between 24-Hour and 12-Hour (AM/PM) format";
    public string AirQualityTooltip => IsVietnamese ? "Chất lượng không khí (US-AQI)" : "Air Quality Index (US-AQI)";
    public string FeedbackAccurateTooltip => IsVietnamese ? "Xác nhận thời tiết đang đúng với thực tế" : "Confirm weather matches actual conditions";
    public string FeedbackInaccurateTooltip => IsVietnamese ? "Báo cáo thời tiết thực tế khác với dự báo" : "Report actual weather differs from forecast";
    public string FeedbackReselectTooltip => IsVietnamese ? "Bấm để chọn lại" : "Click to select again";
    public string ExpandAdviceTooltip => IsVietnamese ? "Bấm để xem hoặc thu gọn chi tiết 5 mục lời khuyên" : "Click to view or collapse detailed 5 advice categories";
    public string AdviceCollapse => IsVietnamese ? "Thu gọn" : "Collapse";
    public string AdviceDetails => IsVietnamese ? "Xem chi tiết" : "View details";

    // Calendar Pod Labels
    public string CalendarYearPodLabel => IsVietnamese ? "NĂM (YEAR)" : "YEAR";
    public string CalendarMonthPodLabel => IsVietnamese ? "THÁNG (MONTH)" : "MONTH";
    public string CalendarDayPodLabel => IsVietnamese ? "NGÀY (DAY)" : "DAY";

    // Settings Tab - Performance Panel
    public string PerfHeader => IsVietnamese ? "Hiệu Năng & Khởi Động Cùng Windows" : "Performance & Windows Startup";
    public string PerfSub => IsVietnamese ? "Tối ưu hóa tài nguyên phần cứng, tiết kiệm pin Laptop và khởi động tự động" : "Optimize hardware resources, laptop battery saving, and auto-startup";
    public string AutoStartTitle => IsVietnamese ? "Khởi động cùng Windows" : "Launch on Windows Startup";
    public string AutoStartSub => IsVietnamese ? "Tự động chạy ngầm Weather WinUI khi mở máy tính" : "Automatically run Weather WinUI in background on boot";
    public string MinimizeToTrayTitle => IsVietnamese ? "Thu nhỏ xuống Khay hệ thống (System Tray)" : "Minimize to System Tray";
    public string MinimizeToTraySub => IsVietnamese ? "Khi bấm nút đóng [X], ứng dụng sẽ thu nhỏ xuống khay thay vì thoát hẳn" : "When closing [X], minimize to system tray instead of exiting";
    public string AutoRefreshTitle => IsVietnamese ? "Tự động cập nhật thời tiết" : "Auto-Refresh Weather";
    public string AutoRefreshSub => IsVietnamese ? "Chu kỳ tự động làm mới dữ liệu khí tượng định kỳ" : "Periodic interval for refreshing weather data";
    public string WeatherEffectsTitle => IsVietnamese ? "Hiệu ứng khí quyển động" : "Dynamic Weather Effects";
    public string WeatherEffectsSub => IsVietnamese ? "Mưa rơi, sấm sét, vầng nắng (Tắt để tiết kiệm pin/RAM)" : "Raindrops, lightning, sun rays (Turn off to save battery/RAM)";
    public string BatterySaverTitle => IsVietnamese ? "Tối ưu tiết kiệm pin cho Laptop (Smart Battery Saver)" : "Laptop Smart Battery Saver";
    public string BatterySaverSub => IsVietnamese ? "Tự động tạm dừng hiệu ứng mưa/sấm sét động và giãn tài nguyên khi máy tính dùng pin hoặc bật Tiết kiệm pin của Windows" : "Auto-pause dynamic weather effects when running on battery or Windows battery saver";
    public string RamUsageTitle => IsVietnamese ? "Mức chiếm dụng RAM hiện tại" : "Current RAM Usage";
    public string RamUsageSub => IsVietnamese ? "Đã tối ưu Native Engine, không chạy WebView2 Chrome nền" : "Optimized Native Engine, no Chromium/WebView2 background overhead";

    // Settings Tab - About Panel
    public string AboutHeader => IsVietnamese ? "Giới Thiệu & Nhật Ký Phiên Bản" : "About & Version Changelog";
    public string AboutSub => IsVietnamese ? "Thông tin phiên bản, bản quyền và nhật ký thay đổi" : "Version info, copyright, and release changelog";
    public string CheckUpdatesTitle => IsVietnamese ? "Cập Nhật Ứng Dụng (GitHub Releases)" : "App Updates (GitHub Releases)";
    public string CheckUpdatesBtn => IsVietnamese ? "Kiểm tra ngay" : "Check Now";
    public string NewVersionFound => IsVietnamese ? "🎉 Phiên bản mới:" : "🎉 New Version:";
    public string DownloadNowBtn => IsVietnamese ? "Tải về ngay" : "Download Now";
    public string AutoCheckUpdatesCheck => IsVietnamese ? "Tự động kiểm tra bản cập nhật mới mỗi khi mở ứng dụng" : "Automatically check for updates on startup";
    public string ViewChangelogBtn => IsVietnamese ? "📜 Xem Nhật Ký Thay Đổi Các Phiên Bản (Changelog)" : "📜 View Version Changelog";
    public string ReopenOnboardingBtn => IsVietnamese ? "🚀 Mở lại trang hướng dẫn ban đầu (Onboarding)" : "🚀 Reopen Getting Started Guide (Onboarding)";

    // Settings Tab - Appearance & Icon Packs
    public string IconPacksHeader => IsVietnamese ? "Bộ Biểu Tượng Thời Tiết (Icon Pack)" : "Weather Icon Packs";
    public string IconPacksSub => IsVietnamese ? "Chọn bộ biểu tượng yêu thích để hiển thị trên màn hình chính, thanh dự báo và widget desktop:" : "Select your preferred icon set for main view, forecasts, and desktop widgets:";
    public string BadgeExclusiveAnimated => IsVietnamese ? "✨ ĐỘC QUYỀN & SỐNG ĐỘNG" : "✨ EXCLUSIVE & ANIMATED";
    public string BadgeInUse => IsVietnamese ? "Đang dùng" : "In Use";
    public string MeteoconsDesc => IsVietnamese ? "Bộ icon vector thời tiết hoạt họa mượt mà, sắc nét đỉnh cao từ Bas Milius (GitHub basmilius/meteocons). Hiển thị sống động mọi sắc thái nắng mưa." : "Smooth animated vector weather icons by Bas Milius. Vividly renders every weather nuance.";
    public string Fluent3DDesc => IsVietnamese ? "Bộ icon 3D sắc màu phong phú với chiều sâu bóng đổ, mang lại cảm giác hiện đại và trực quan." : "Rich 3D icons with realistic depth and lighting, offering a sleek and modern look.";
    public string FontAwesomeDesc => IsVietnamese ? "Biểu tượng glyph phẳng tối giản, tải cực nhanh, tương thích 100% mọi độ phân giải màn hình." : "Minimalist flat glyph icons, ultra-fast loading, 100% compatible with all resolutions.";
    public string Badge3DModern => IsVietnamese ? "🎨 3D HIỆN ĐẠI" : "🎨 3D MODERN";
    public string BadgeMinimalist => IsVietnamese ? "⚡ TỐI GIẢN" : "⚡ MINIMALIST";
    public string AppThemeTitle => IsVietnamese ? "Chủ đề hiển thị ứng dụng" : "App Appearance Theme";
    public string AppThemeSub => IsVietnamese ? "Chế độ màu Sáng, Tối hoặc Tự động thích ứng Windows" : "Light, Dark, or Match Windows System mode";

    // Settings Tab - City Picture
    public string CityBgHeader => IsVietnamese ? "Hình Nền Thành Phố (City Picture)" : "City Wallpapers";
    public string CityBgSub => IsVietnamese ? "Hiện ảnh nền mờ nghệ thuật phía sau thẻ thời tiết hiện tại" : "Display scenic blurred wallpaper behind the current weather card";
    public string CityBgBlurTitle => IsVietnamese ? "Hình nền mờ theo địa điểm" : "Location-based blurred wallpaper";
    public string CityBgBlurSub => IsVietnamese ? "Tự động áp dụng ảnh thành phố tương ứng với vị trí đang xem hoặc ảnh mẫu tùy thích" : "Auto-applies scenic city wallpaper matching viewed location or selected presets";
    public string DynamicDayNightTitle => IsVietnamese ? "Đổi ảnh nền theo Ngày & Đêm (Dynamic Day & Night)" : "Dynamic Day & Night Wallpaper";
    public string DynamicDayNightSub => IsVietnamese ? "Tự động chuyển sang ảnh thành phố ban đêm lên đèn lung linh khi trời tối theo giờ mặt trời lặn" : "Automatically switches to illuminated night skyline after sunset";
    public string CityModeAuto => IsVietnamese ? "🌐 Tự động theo địa điểm thời tiết" : "🌐 Auto by weather location";
    public string CityModePreset => IsVietnamese ? "🌆 Chọn trong danh sách ảnh mẫu" : "🌆 Select from preset gallery";
    public string CityModeCustom => IsVietnamese ? "📁 Tự chọn hình ảnh từ máy tính..." : "📁 Custom image from PC...";
    public string CustomImageNone => IsVietnamese ? "Chưa chọn tệp ảnh nào" : "No image selected";
    public string CustomImageChange => IsVietnamese ? "Đổi ảnh khác..." : "Change image...";
    public string PreviewCardTitle => IsVietnamese ? "Xem Trước Thẻ Thời Tiết Có Hình Nền" : "Weather Card Wallpaper Preview";
    public string PreviewModeAuto => IsVietnamese ? "Tự động" : "Auto";

    // Settings Tab - Mini Desktop Widget
    public string WidgetHeader => IsVietnamese ? "Widget Mini Desktop" : "Desktop Mini Widgets";
    public string WidgetSub => IsVietnamese ? "Tùy chỉnh tiện ích thời tiết mini nổi trên màn hình Desktop" : "Customize floating desktop weather mini-widget";
    public string WidgetStyleTitle => IsVietnamese ? "Kiểu dáng Widget" : "Widget Style";
    public string WidgetStyleSub => IsVietnamese ? "Glass Card đầy đủ hoặc Compact Bar thanh gọn" : "Full Glass Card or sleek Compact Bar";
    public string WidgetStyleDynamic => IsVietnamese ? "✨ Dribbble Dynamic (Đổi màu)" : "✨ Dynamic Color Changing";
    public string WidgetStyleGlass => IsVietnamese ? "🪟 Glass Card (Đầy đủ)" : "🪟 Glass Card (Full)";
    public string WidgetStyleCompact => IsVietnamese ? "➖ Compact Bar (Gọn)" : "➖ Compact Bar (Slim)";
    public string WidgetStyleIsland => IsVietnamese ? "💊 Mini Island (Viên thuốc)" : "💊 Mini Island";
    public string WidgetOpacityTitle => IsVietnamese ? "Độ trong suốt Widget (Opacity)" : "Widget Transparency (Opacity)";
    public string WidgetOpacitySub => IsVietnamese ? "Tự động sáng rõ 100% khi rê chuột vào" : "Automatically dims and highlights 100% on mouse hover";

    // Settings Tab - Notifications & Tray
    public string NotifHeader => IsVietnamese ? "Thông Báo & Khay Hệ Thống" : "Notifications & System Tray";
    public string NotifSub => IsVietnamese ? "Quản lý thông báo Windows Toast và hành vi khay Taskbar" : "Manage Windows Toast notifications and Taskbar tray behavior";
    public string ToastTitle => IsVietnamese ? "Cảnh báo Windows Toast" : "Windows Toast Alerts";
    public string ToastSub => IsVietnamese ? "Gửi cảnh báo mưa, nắng gắt và tóm tắt đầu ngày" : "Send alerts for impending rain, high UV, and daily summaries";
    public string ToastRainCheck => IsVietnamese ? "🌧️ Cảnh báo mưa sắp đến trong 20-30 phút tới" : "🌧️ Rain incoming alert within 20-30 minutes";
    public string ToastUvCheck => IsVietnamese ? "🔥 Cảnh báo tia cực tím UV rất cao buổi trưa (11h-14h)" : "🔥 Extreme UV radiation warning at midday (11am-2pm)";
    public string ToastMorningCheck => IsVietnamese ? "☀️ Tóm tắt dự báo đầu ngày (buổi sáng 07h00)" : "☀️ Morning weather briefing (07:00 AM)";
    public string ToastTestBtn => IsVietnamese ? "Thử gửi thông báo kiểm tra ngay" : "Send test notification now";
    public string CommuteAlertTitle => IsVietnamese ? "Nhắc nhở thời tiết Đi làm & Tan ca" : "Commute Weather Reminder";
    public string CommuteAlertSub => IsVietnamese ? "Cảnh báo thời tiết trước giờ di chuyển hàng ngày" : "Proactive weather warnings before your daily commute";
    public string MorningCommuteTitle => IsVietnamese ? "Giờ đi làm / đi học sáng:" : "Morning departure time:";
    public string EveningCommuteTitle => IsVietnamese ? "Giờ tan ca buổi chiều:" : "Evening commute time:";
    public string CommuteLeadTimeTitle => IsVietnamese ? "Thông báo trước:" : "Notify ahead by:";
    public string CommuteDaysTitle => IsVietnamese ? "Áp dụng vào các ngày:" : "Active on days:";
    public string TestCommuteAlertBtn => IsVietnamese ? "🔔 Thử gửi thông báo lịch trình ngay" : "🔔 Send test commute notification now";
    public string StartMinimizedTitle => IsVietnamese ? "Khởi động thu nhỏ vào khay hệ thống" : "Start minimized to system tray";
    public string StartMinimizedSub => IsVietnamese ? "Tự động ẩn ứng dụng vào khay Taskbar khi bật máy tính, không bung to cửa sổ làm phiền" : "Silently hide app to taskbar tray on computer startup";
    public string CloseToTrayTitle => IsVietnamese ? "Đóng ứng dụng về khay hệ thống" : "Close to system tray";
    public string CloseToTraySub => IsVietnamese ? "Bấm nút X sẽ ẩn app về khay thay vì thoát hẳn" : "Clicking [X] hides app to tray instead of exiting";
    public string AppearanceHeader => IsVietnamese ? "Giao Diện & Biểu Tượng" : "Appearance & Icons";
    public string AppearanceSub => IsVietnamese ? "Tùy biến bộ icon thời tiết trực quan và phong cách hiển thị của toàn bộ ứng dụng" : "Customize visual weather icons and application appearance theme";
    public string CityPresetHeader => IsVietnamese ? "Danh sách 10 thành phố mẫu (Bấm để chọn nhanh làm hình nền):" : "10 preset cities (Click to set as wallpaper):";

    #endregion

    #region Widget Studio Tab & Widget Window Strings

    public string WidgetStudioHeader => IsVietnamese ? "Studio Thiết Kế Desktop Widget (Bộ Sưu Tập Trực Quan)" : "Desktop Widget Studio (Visual Gallery)";
    public string WidgetStudioSub => IsVietnamese ? "Chọn kiểu dáng trực quan, tinh chỉnh độ mờ Acrylic và ghim ra màn hình Desktop" : "Select visual styles, fine-tune acrylic transparency and pin to your desktop";
    public string WidgetStudioBadge => "WinUI 3 Glass Studio";
    public string WidgetStudioSelected => IsVietnamese ? "✓ Đang chọn" : "✓ Selected";
    public string WidgetStudioBryanCTitle => "✨ Bryan C Dynamic";
    public string WidgetStudioBryanCDesc => IsVietnamese ? "Tự động biến đổi nền bầu trời, hình học mặt trời & vầng trăng theo điều kiện thực tế" : "Dynamically changes sky canvas, sun aura & moon arc based on real-time weather";
    public string WidgetStudioGlassTitle => "🪟 Fluent Glass Card";
    public string WidgetStudioGlassDesc => IsVietnamese ? "Thẻ kính mờ hiệu ứng Mica/Acrylic đầy đủ chi tiết độ ẩm, gió và chất lượng không khí" : "Frosted acrylic glass card with full details: humidity, wind, and air quality";
    public string WidgetStudioCompactTitle => "➖ Compact Bar";
    public string WidgetStudioCompactDesc => IsVietnamese ? "Thanh ngang dài thanh mảnh, chiếm diện tích tối thiểu, đặt sát góc trên màn hình" : "Ultra-slim horizontal bar taking minimal desktop space, ideal for screen edges";
    public string WidgetStudioIslandTitle => "💊 Dynamic Island Pill";
    public string WidgetStudioIslandDesc => IsVietnamese ? "Viên thuốc bo tròn tinh tế, tối giản sang trọng, luôn nổi nhẹ nhàng trên màn hình" : "Sleek floating capsule pill with minimalist luxury, always gently present";
    public string WidgetStudio4DayForecast => IsVietnamese ? "Dự báo 4 ngày" : "4-Day Forecast";
    public string WidgetStudioAqiGood => IsVietnamese ? "AQI Chuẩn" : "AQI Good";
    public string WidgetStudioControlsHeader => IsVietnamese ? "Tùy Biến & Điều Khiển Widget" : "Widget Controls & Customization";
    public string WidgetStudioStyleLabel => IsVietnamese ? "Danh sách kiểu dáng Widget:" : "Widget Style:";
    public string WidgetStudioOpacityLabel => IsVietnamese ? "Độ trong suốt Widget:" : "Widget Opacity:";
    public string WidgetStudioAlwaysOnTop => IsVietnamese ? "Luôn nổi trên cùng (Always on Top)" : "Always on top of other windows";
    public string WidgetStudioAlwaysOnTopSub => IsVietnamese ? "Giữ Widget luôn hiển thị phía trên các cửa sổ ứng dụng khác" : "Keep widget visible above all other running application windows";
    public string WidgetStudioPinCurrentBtn => IsVietnamese ? "📌 Ghim Ra Desktop" : "📌 Pin to Desktop";
    public string WidgetStudioOpenOtherBtn => IsVietnamese ? "➕ Mở Thành Phố Khác..." : "➕ Add Other City...";
    public string WidgetStudioCloseAllBtn => IsVietnamese ? "❌ Đóng Tất Cả Widget" : "❌ Close All Widgets";
    public string WidgetStudioCityDialogTitle => IsVietnamese ? "➕ Mở Widget Cho Thành Phố Khác" : "➕ Open Widget for Another City";
    public string WidgetStudioCityDialogPlaceholder => IsVietnamese ? "Nhập tên thành phố (VD: Đà Nẵng, Tokyo, Paris, New York...)" : "Enter city name (e.g. Da Nang, Tokyo, Paris, New York...)";
    public string WidgetStudioCityDialogCreate => IsVietnamese ? "Tạo Widget" : "Create Widget";
    public string WidgetStudioCityDialogCancel => IsVietnamese ? "Hủy" : "Cancel";

    public string WidgetWindowCityTooltip => IsVietnamese ? "Bấm để đổi thành phố hoặc mở thêm Widget" : "Click to change city or add another widget";
    public string WidgetWindowAddTooltip => IsVietnamese ? "Mở thêm một Widget mới cho thành phố khác" : "Open another widget for a different city";
    public string WidgetWindowStyleTooltip => IsVietnamese ? "Đổi kiểu Widget (Bryan C / GlassCard / Compact / Island)" : "Switch widget style (Bryan C / GlassCard / Compact / Island)";
    public string WidgetWindowRefreshTooltip => IsVietnamese ? "Cập nhật thời tiết mới nhất" : "Refresh latest weather";
    public string WidgetWindowPinTooltipActive => IsVietnamese ? "Đang ghim trên cùng (Bấm để bỏ ghim)" : "Currently pinned on top (Click to unpin)";
    public string WidgetWindowPinTooltipInactive => IsVietnamese ? "Ghim trên cùng (Always on top)" : "Pin on top (Always on top)";
    public string WidgetWindowCloseTooltip => IsVietnamese ? "Đóng widget này" : "Close this widget";
    public string WidgetWindowCityPickerTitle => IsVietnamese ? "📍 Thành Phố Cho Widget" : "📍 City for Widget";
    public string WidgetWindowCityPickerCurrent => IsVietnamese ? "Đang hiển thị:" : "Currently showing:";
    public string WidgetWindowCityPickerSearchPlaceholder => IsVietnamese ? "Tìm thành phố..." : "Search city...";
    public string WidgetWindowCityPickerQuick => IsVietnamese ? "Hoặc chọn nhanh:" : "Or quick select:";

    #endregion
}
