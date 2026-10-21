namespace WeatherApp.Helpers;

/// <summary>
/// Quản lý toàn bộ Glyph Icon đặc trưng của ngôn ngữ thiết kế Nothing OS
/// Được nạp tự host từ font Ndot77JPExtended.ttf (Plane 15 Supplementary PUA A: 0xF1900 - 0xF19A3).
/// </summary>
public static class NothingGlyphs
{
    // ==================== 1. THỜI TIẾT (WEATHER CONDITIONS) ====================
    /// <summary>Trời quang đãng / Nắng đẹp (Sun with 4 corner dot rays - U+F1964)</summary>
    public static readonly string Sun = char.ConvertFromUtf32(0xF1964);

    /// <summary>Đêm quang mây / Trăng lưỡi liềm (Crescent Moon - U+F193A)</summary>
    public static readonly string Moon = char.ConvertFromUtf32(0xF193A);

    /// <summary>Nắng nhẹ / Mây rải rác / Bình minh (Sun rising with dot rays - U+F1915)</summary>
    public static readonly string PartlyCloudyDay = char.ConvertFromUtf32(0xF1915);

    /// <summary>Nhiều mây u ám (Dot-Matrix Cloud - U+F196F)</summary>
    public static readonly string Cloud = char.ConvertFromUtf32(0xF196F);

    /// <summary>Sương mù / Luồng gió (Stream wavy lines - U+F191D)</summary>
    public static readonly string Mist = char.ConvertFromUtf32(0xF191D);

    /// <summary>Mưa phùn / Giọt mưa (Rain drop arrow - U+F1941)</summary>
    public static readonly string RainDrop = char.ConvertFromUtf32(0xF1941);

    /// <summary>Mưa to xối xả / Chùm mưa (Falling rain drops cluster - U+F194B)</summary>
    public static readonly string RainCluster = char.ConvertFromUtf32(0xF194B);

    /// <summary>Chiếc ô / Lượng mưa (Precipitation Umbrella - U+F1970)</summary>
    public static readonly string Umbrella = char.ConvertFromUtf32(0xF1970);

    /// <summary>Tuyết rơi / Bông tuyết 8 cánh (Asterisk Snowflake - U+F1926)</summary>
    public static readonly string Snowflake = char.ConvertFromUtf32(0xF1926);

    /// <summary>Dông sét bão / Tia sét (Lightning bolt box - U+F1948)</summary>
    public static readonly string LightningBolt = char.ConvertFromUtf32(0xF1948);


    // ==================== 2. THANH ĐIỀU HƯỚNG CHÍNH (NAVIGATION TABS) ====================
    /// <summary>Trang chủ / Tổng quan (Dot-matrix House - U+F196D)</summary>
    public static readonly string House = char.ConvertFromUtf32(0xF196D);

    /// <summary>Triều cường & Ngập lụt (Ripple Water Waves - U+F196A)</summary>
    public static readonly string Waves = char.ConvertFromUtf32(0xF196A);

    /// <summary>Radar Doppler 360 (Circular Radar Target - U+F191C)</summary>
    public static readonly string RadarTarget = char.ConvertFromUtf32(0xF191C);

    /// <summary>Đời sống & Hoạt động (Smiley Face - U+F1983)</summary>
    public static readonly string Smiley = char.ConvertFromUtf32(0xF1983);

    /// <summary>Lịch âm & Tiết khí (Clock dial with hands - U+F196B)</summary>
    public static readonly string Clock = char.ConvertFromUtf32(0xF196B);

    /// <summary>Mini Widget Desktop (Widget Robot Capsule - U+F1919)</summary>
    public static readonly string WidgetRobot = char.ConvertFromUtf32(0xF1919);

    /// <summary>Cài đặt cấu hình (Concentric Setting Dials - U+F197C)</summary>
    public static readonly string SettingsDial = char.ConvertFromUtf32(0xF197C);


    // ==================== 3. THANH TIÊU ĐỀ & NÚT THAO TÁC (HEADER & ACTIONS) ====================
    /// <summary>Định vị GPS / Điểm trạm (Crosshair Target Pin - U+F1930)</summary>
    public static readonly string GpsTarget = char.ConvertFromUtf32(0xF1930);

    /// <summary>Làm mới dữ liệu (Fast forward chevrons >> - U+F1909)</summary>
    public static readonly string AdvanceRefresh = char.ConvertFromUtf32(0xF1909);

    /// <summary>Đơn vị nhiệt độ / Tỷ lệ % (Dot-matrix percent - U+F197B)</summary>
    public static readonly string Percent = char.ConvertFromUtf32(0xF197B);

    /// <summary>Chế độ Zen tối giản (Dot-matrix Sun - U+F1964)</summary>
    public static readonly string ZenSun = char.ConvertFromUtf32(0xF1964);

    /// <summary>Dropdown widget ngoài Desktop (Widget Capsule - U+F1919)</summary>
    public static readonly string WidgetDropdown = char.ConvertFromUtf32(0xF1919);


    // ==================== 4. THÔNG SỐ KHÍ TƯỢNG (TELEMETRY METRICS) ====================
    /// <summary>Độ ẩm không khí / Điểm sương (Water Drop - U+F191A)</summary>
    public static readonly string WaterDrop = char.ConvertFromUtf32(0xF191A);

    /// <summary>Gió & Tốc độ gió (Wind Streams - U+F191D)</summary>
    public static readonly string WindStreams = char.ConvertFromUtf32(0xF191D);

    /// <summary>Chỉ số UV tia cực tím (Sun with 4 dots - U+F1964)</summary>
    public static readonly string UvSun = char.ConvertFromUtf32(0xF1964);

    /// <summary>Áp suất khí quyển / Đồng hồ đo (Barometer Gauge - U+F1930)</summary>
    public static readonly string BarometerGauge = char.ConvertFromUtf32(0xF1930);

    /// <summary>Chất lượng không khí AQI (Signal Bars / Telemetry - U+F195A)</summary>
    public static readonly string SignalBars = char.ConvertFromUtf32(0xF195A);

    /// <summary>Tầm nhìn xa (Eye Lens Aperture - U+F1987)</summary>
    public static readonly string EyeVisibility = char.ConvertFromUtf32(0xF1987);

    /// <summary>Địa điểm yêu thích (Minimalist Heart - U+F194E)</summary>
    public static readonly string Heart = char.ConvertFromUtf32(0xF194E);


    /// <summary>
    /// Chuyển đổi mã WMO Weather Code sang Nothing Dot-Matrix Glyph tương ứng
    /// </summary>
    public static string GetWeatherGlyph(int weatherCode, bool isDay = true)
    {
        return weatherCode switch
        {
            0 => isDay ? Sun : Moon,
            1 or 2 => isDay ? PartlyCloudyDay : Moon,
            3 => Cloud,
            45 or 48 => Mist,
            51 or 53 or 55 => RainDrop,
            56 or 57 => Snowflake,
            61 or 63 => RainDrop,
            65 => RainCluster,
            66 or 67 => Snowflake,
            71 or 73 or 75 or 77 => Snowflake,
            80 or 81 => RainDrop,
            82 => RainCluster,
            85 or 86 => Snowflake,
            95 or 96 or 99 => LightningBolt,
            _ => Cloud
        };
    }
}
