using WeatherApp.Models;

namespace WeatherApp.Helpers;

public static class WeatherCodeHelper
{
    public static (string description, string iconGlyph) GetConditionInfo(int weatherCode, bool isDay = true)
    {
        return weatherCode switch
        {
            0 => isDay 
                ? ("Trời quang đãng, nắng đẹp", "\uf185")  // fa-sun
                : ("Đêm quang mây, trăng sáng", "\uf186"), // fa-moon

            1 => isDay 
                ? ("Trời ít mây, nắng nhẹ", "\uf6c4")      // fa-cloud-sun
                : ("Đêm ít mây", "\uf6c3"),                // fa-cloud-moon

            2 => isDay
                ? ("Mây rải rác", "\uf6c4")                // fa-cloud-sun
                : ("Mây rải rác", "\uf6c3"),               // fa-cloud-moon

            3 => ("Nhiều mây u ám", "\uf0c2"),             // fa-cloud

            45 => ("Có sương mù dày", "\uf75f"),           // fa-smog
            48 => ("Sương muối bám đọng", "\uf75f"),       // fa-smog

            51 => ("Mưa phùn hạt nhỏ", "\uf73d"),          // fa-cloud-rain
            53 => ("Mưa phùn rải rác", "\uf73d"),          // fa-cloud-rain
            55 => ("Mưa phùn dày hạt", "\uf73d"),          // fa-cloud-rain

            56 or 57 => ("Mưa phùn giá buốt", "\uf2dc"),  // fa-snowflake

            61 => ("Mưa nhỏ nhẹ hạt", "\uf73d"),           // fa-cloud-rain
            63 => ("Mưa vừa", "\uf73d"),                   // fa-cloud-rain
            65 => ("Mưa to diện rộng", "\uf740"),          // fa-cloud-showers-heavy

            66 or 67 => ("Mưa lạnh đóng băng", "\uf2dc"), // fa-snowflake

            71 => ("Tuyết rơi nhẹ", "\uf2dc"),             // fa-snowflake
            73 => ("Tuyết rơi vừa", "\uf2dc"),             // fa-snowflake
            75 => ("Tuyết rơi dày đặc", "\uf2dc"),         // fa-snowflake
            77 => ("Hạt tuyết li ti", "\uf2dc"),           // fa-snowflake

            80 => ("Mưa rào nhẹ", "\uf73d"),               // fa-cloud-rain
            81 => ("Mưa rào vừa", "\uf73d"),               // fa-cloud-rain
            82 => ("Mưa rào xối xả", "\uf740"),            // fa-cloud-showers-heavy

            85 or 86 => ("Mưa rào kèm tuyết", "\uf2dc"),   // fa-snowflake

            95 => ("Dông sét, mưa dông", "\uf76c"),        // fa-cloud-bolt
            96 => ("Mưa dông kèm mưa đá nhỏ", "\uf76c"),   // fa-cloud-bolt
            99 => ("Mưa dông bão mạnh kèm mưa đá", "\uf76c"), // fa-cloud-bolt

            _ => ("Thời tiết bình thường", "\uf0c2")       // fa-cloud
        };
    }

    public static string GetSvgFileName(int weatherCode, bool isDay)
    {
        return weatherCode switch
        {
            0 => isDay ? "clear-day.svg" : "clear-night.svg",
            1 => isDay ? "cloudy-1-day.svg" : "cloudy-1-night.svg",
            2 => isDay ? "cloudy-2-day.svg" : "cloudy-2-night.svg",
            3 => "cloudy.svg",
            45 or 48 => isDay ? "fog-day.svg" : "fog-night.svg",
            51 or 53 or 55 => isDay ? "rainy-1-day.svg" : "rainy-1-night.svg",
            56 or 57 => "rain-and-sleet-mix.svg",
            61 or 63 => isDay ? "rainy-2-day.svg" : "rainy-2-night.svg",
            65 => isDay ? "rainy-3-day.svg" : "rainy-3-night.svg",
            66 or 67 => "rain-and-snow-mix.svg",
            71 or 73 or 75 or 77 => isDay ? "snowy-2-day.svg" : "snowy-2-night.svg",
            80 or 81 => isDay ? "rainy-2-day.svg" : "rainy-2-night.svg",
            82 => isDay ? "rainy-3-day.svg" : "rainy-3-night.svg",
            85 or 86 => isDay ? "snowy-3-day.svg" : "snowy-3-night.svg",
            95 => isDay ? "isolated-thunderstorms-day.svg" : "isolated-thunderstorms-night.svg",
            96 or 99 => "severe-thunderstorm.svg",
            _ => isDay ? "cloudy-1-day.svg" : "cloudy-1-night.svg"
        };
    }

    public static string GetMeteoconsFileName(int weatherCode, bool isDay)
    {
        return weatherCode switch
        {
            0 => isDay ? "clear-day.svg" : "clear-night.svg",
            1 => isDay ? "partly-cloudy-day.svg" : "partly-cloudy-night.svg",
            2 => isDay ? "partly-cloudy-day.svg" : "partly-cloudy-night.svg",
            3 => isDay ? "overcast-day.svg" : "overcast-night.svg",
            45 or 48 => isDay ? "fog-day.svg" : "fog-night.svg",
            51 or 53 or 55 => isDay ? "partly-cloudy-day-drizzle.svg" : "partly-cloudy-night-drizzle.svg",
            56 or 57 => "sleet.svg",
            61 or 63 => isDay ? "partly-cloudy-day-rain.svg" : "partly-cloudy-night-rain.svg",
            65 => "extreme-day-rain.svg",
            66 or 67 => "sleet.svg",
            71 or 73 or 75 or 77 => isDay ? "partly-cloudy-day-snow.svg" : "partly-cloudy-night-snow.svg",
            80 or 81 => isDay ? "partly-cloudy-day-rain.svg" : "partly-cloudy-night-rain.svg",
            82 => "extreme-day-rain.svg",
            85 or 86 => "extreme-snow.svg",
            95 => isDay ? "thunderstorms-day-rain.svg" : "thunderstorms-night-rain.svg",
            96 or 99 => "extreme-thunderstorms-day.svg",
            _ => isDay ? "partly-cloudy-day.svg" : "partly-cloudy-night.svg"
        };
    }

    public static string GetWeatherIconPath(int weatherCode, bool isDay, string? iconPack = null)
    {
        iconPack ??= "Meteocons";
        if (iconPack == "FontAwesome") return string.Empty;

        if (iconPack == "Fluent3D")
        {
            string amFile = GetSvgFileName(weatherCode, isDay);
            return $"ms-appx:///Assets/weather-icons-main/static/{amFile}";
        }

        // Mặc định: Meteocons Animated
        string meteoFile = GetMeteoconsFileName(weatherCode, isDay);
        return $"ms-appx:///Assets/Meteocons/{meteoFile}";
    }

    public static string GetWeatherIconFullPath(int weatherCode, bool isDay, string? iconPack = null)
    {
        iconPack ??= "Meteocons";
        if (iconPack == "FontAwesome") return string.Empty;

        string subFolder = iconPack == "Fluent3D" ? System.IO.Path.Combine("weather-icons-main", "static") : "Meteocons";
        string fileName = iconPack == "Fluent3D" ? GetSvgFileName(weatherCode, isDay) : GetMeteoconsFileName(weatherCode, isDay);

        string[] possibleDirs =
        {
            System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", subFolder),
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", subFolder),
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", subFolder)
        };

        foreach (var dir in possibleDirs)
        {
            string p = System.IO.Path.Combine(dir, fileName);
            if (File.Exists(p)) return p;
        }

        return string.Empty;
    }

    public static WeatherEffectType GetWeatherEffect(int weatherCode, bool isDay, double uvIndex)
    {
        // 1. Dông bão / sấm sét
        if (weatherCode >= 95)
        {
            return WeatherEffectType.Thunderstorm;
        }

        // 2. Mưa to
        if (weatherCode == 65 || weatherCode == 82)
        {
            return WeatherEffectType.HeavyRain;
        }

        // 3. Mưa vừa / mưa rào
        if (weatherCode == 61 || weatherCode == 63 || weatherCode == 80 || weatherCode == 81)
        {
            return WeatherEffectType.ModerateRain;
        }

        // 4. Mưa phùn nhẹ hạt
        if (weatherCode >= 51 && weatherCode <= 57)
        {
            return WeatherEffectType.LightRain;
        }

        // 5. Sương mù
        if (weatherCode == 45 || weatherCode == 48)
        {
            return WeatherEffectType.Fog;
        }

        // 6. Tuyết
        if ((weatherCode >= 71 && weatherCode <= 77) || weatherCode == 85 || weatherCode == 86)
        {
            return WeatherEffectType.Snow;
        }

        // 7. Ban ngày
        if (isDay)
        {
            // Nếu nhiều mây u ám (Overcast code 3) -> Luôn là Cloudy, tuyệt đối không bị gán nhầm thành HighUvSunny
            if (weatherCode == 3)
            {
                return WeatherEffectType.Cloudy;
            }

            // Nếu mây rải rác (Partly cloudy code 2)
            if (weatherCode == 2)
            {
                return WeatherEffectType.PartlyCloudy;
            }

            // Chỉ khi trời quang / ít mây (code 0 hoặc 1) và vào khung giờ trưa (11h-14h30) có tia UV thực sự cao thì mới là HighUvSunny
            int hour = DateTime.Now.Hour;
            if (weatherCode <= 1 && uvIndex >= 8.0 && hour >= 11 && hour <= 14)
            {
                return WeatherEffectType.HighUvSunny; // Nắng gắt tia UV cao giữa trưa
            }

            if (weatherCode <= 1)
            {
                return WeatherEffectType.ClearSunny; // Nắng vàng ấm / trời trong xanh
            }

            return WeatherEffectType.Cloudy;
        }

        // 8. Ban đêm
        if (weatherCode <= 1)
        {
            return WeatherEffectType.ClearNight;
        }
        return WeatherEffectType.Cloudy;
    }

    public static (string badgeText, string badgeColor) GetWeatherAlert(WeatherEffectType effect, double temp, double uvIndex)
    {
        return effect switch
        {
            WeatherEffectType.Thunderstorm => ("⚡ CẢNH BÁO DÔNG SÉT & MƯA LỚN", "#DC2626"),
            WeatherEffectType.HeavyRain => ("🌧️ MƯA TO DIỆN RỘNG", "#0284C7"),
            WeatherEffectType.ModerateRain => ("🌧️ TRỜI ĐANG CÓ MƯA", "#0284C7"),
            WeatherEffectType.LightRain => ("🌦️ MƯA PHÙN RẢI RÁC", "#0EA5E9"),
            WeatherEffectType.HighUvSunny => ($"🔥 NẮNG GẮT • CHỈ SỐ UV {uvIndex:F1} (RẤT CAO)", "#EA580C"),
            WeatherEffectType.ClearSunny => (temp >= 35 ? "☀️ TRỜI NẮNG NÓNG" : "", "#F59E0B"),
            WeatherEffectType.Fog => ("🌫️ SƯƠNG MÙ GIẢM TẦM NHÌN", "#64748B"),
            WeatherEffectType.Snow => ("❄️ TUYẾT RƠI", "#38BDF8"),
            _ => ("", "#00000000") // Với mây u ám (Cloudy), mây rải rác hoặc trời bình thường, không hiện badge cảnh báo thừa
        };
    }

    public static (string startColor, string endColor) GetHeroGradientsByEffect(WeatherEffectType effect)
    {
        return effect switch
        {
            WeatherEffectType.Thunderstorm => ("#0F172A", "#1E1B4B"),  // Bầu trời dông bão sấm chớp tối huyền bí
            WeatherEffectType.HeavyRain => ("#0C1929", "#1E3A5F"),     // Mưa to xanh đen mờ mịt
            WeatherEffectType.ModerateRain => ("#162B44", "#2B5278"),  // Mưa vừa xanh nước đậm
            WeatherEffectType.LightRain => ("#243242", "#384E66"),     // Mưa phùn xám lam êm dịu
            WeatherEffectType.HighUvSunny => ("#9A3412", "#D97706"),   // Nắng gắt vàng cam ấm áp giữa trưa nắng
            WeatherEffectType.ClearSunny => ("#0284C7", "#0369A1"),    // Nắng đẹp trời xanh trong rực rỡ
            WeatherEffectType.PartlyCloudy => ("#1E3A5F", "#2A5298"),  // Nắng nhẹ xen lẫn mây trời xanh dịu
            WeatherEffectType.Cloudy => ("#1E293B", "#334155"),        // Nhiều mây u ám: xám than / slate trầm tĩnh, không bao giờ bị màu cam!
            WeatherEffectType.Fog => ("#334155", "#475569"),           // Sương mù mờ ảo xám khói
            WeatherEffectType.Snow => ("#1E293B", "#2D4263"),          // Tuyết rơi xám lạnh
            WeatherEffectType.ClearNight => ("#0B132B", "#1C2541"),    // Đêm thanh quang mây xanh đêm sâu
            _ => ("#1E293B", "#334155")
        };
    }

    public static string GetWindDirection(double degrees)
    {
        string[] directions = { "Bắc", "Đông Bắc", "Đông", "Đông Nam", "Nam", "Tây Nam", "Tây", "Tây Bắc" };
        int index = (int)Math.Round(((degrees % 360) / 45)) % 8;
        return directions[index];
    }

    public static (string level, string advice) GetUvInterpretation(double uvIndex)
    {
        return uvIndex switch
        {
            < 3 => ("Thấp (0-2)", "An toàn khi ra ngoài."),
            < 6 => ("Trung bình (3-5)", "Nên bôi kem chống nắng và đội mũ."),
            < 8 => ("Cao (6-7)", "Cần bảo vệ mắt và da cẩn thận."),
            < 11 => ("Rất cao (8-10)", "Hạn chế ra ngoài giờ cao điểm."),
            _ => ("Nguy hiểm (11+)", "Tránh ra ngoài trời nắng gắt!")
        };
    }

    public static (string level, string color, string advice) GetAqiInterpretation(int aqi)
    {
        return aqi switch
        {
            <= 50 => ("Tốt", "#10B981", "Chất lượng không khí trong lành, rất lý tưởng cho các hoạt động thể thao ngoài trời và mở cửa thông gió."),
            <= 100 => ("Trung bình", "#F59E0B", "Chất lượng không khí ở mức chấp nhận được. Người cực kỳ nhạy cảm với ô nhiễm nên chú ý."),
            <= 150 => ("Kém (Nhạy cảm)", "#EA580C", "Nhóm người nhạy cảm (trẻ nhỏ, người lớn tuổi, bệnh hô hấp) nên hạn chế vận động mạnh ngoài trời."),
            <= 200 => ("Xấu", "#DC2626", "Không khí có hại cho sức khỏe. Khuyến cáo đeo khẩu trang chống bụi PM2.5 khi di chuyển ngoài đường."),
            <= 300 => ("Rất xấu", "#9333EA", "Cảnh báo ô nhiễm nặng: Hạn chế ra ngoài, nên đóng kín cửa sổ và bật máy lọc không khí trong phòng."),
            _ => ("Nguy hại", "#7F1D1D", "Mức độ khẩn cấp nguy hại nghiêm trọng! Toàn bộ người dân nên ở trong nhà và đóng kín các cửa.")
        };
    }

    public static string GetPm25Status(double pm25)
    {
        if (pm25 <= 15) return "Đạt chuẩn";
        if (pm25 <= 35) return "Chấp nhận được";
        if (pm25 <= 55) return "Kém";
        return "Vượt chuẩn";
    }

    public static string GetPm10Status(double pm10)
    {
        if (pm10 <= 45) return "Đạt chuẩn";
        if (pm10 <= 80) return "Chấp nhận được";
        return "Vượt chuẩn";
    }

    public static string GetOzoneStatus(double o3)
    {
        if (o3 <= 100) return "Tốt";
        if (o3 <= 160) return "Trung bình";
        return "Cao";
    }

    public static string GetNo2Status(double no2)
    {
        if (no2 <= 40) return "Tốt";
        if (no2 <= 80) return "Trung bình";
        return "Cao";
    }
}
