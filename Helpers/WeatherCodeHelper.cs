using WeatherApp.Models;
using WeatherApp.Services;

namespace WeatherApp.Helpers;

public static class WeatherCodeHelper
{
    public static (string description, string iconGlyph) GetConditionInfo(int weatherCode, bool isDay = true)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        return weatherCode switch
        {
            0 => isDay 
                ? (isVi ? "Trời quang đãng, nắng đẹp" : "Clear sky, sunny", "\uf185")  // fa-sun
                : (isVi ? "Đêm quang mây, trăng sáng" : "Clear night, starry", "\uf186"), // fa-moon

            1 => isDay 
                ? (isVi ? "Trời ít mây, nắng nhẹ" : "Mainly clear, mild sun", "\uf6c4")      // fa-cloud-sun
                : (isVi ? "Đêm ít mây" : "Mainly clear night", "\uf6c3"),                // fa-cloud-moon

            2 => isDay
                ? (isVi ? "Mây rải rác" : "Partly cloudy", "\uf6c4")                // fa-cloud-sun
                : (isVi ? "Mây rải rác" : "Partly cloudy", "\uf6c3"),               // fa-cloud-moon

            3 => (isVi ? "Nhiều mây u ám" : "Overcast", "\uf0c2"),             // fa-cloud

            45 => (isVi ? "Có sương mù dày" : "Dense fog", "\uf75f"),           // fa-smog
            48 => (isVi ? "Sương muối bám đọng" : "Depositing rime fog", "\uf75f"),       // fa-smog

            51 => (isVi ? "Mưa phùn hạt nhỏ" : "Light drizzle", "\uf73d"),          // fa-cloud-rain
            53 => (isVi ? "Mưa phùn rải rác" : "Moderate drizzle", "\uf73d"),          // fa-cloud-rain
            55 => (isVi ? "Mưa phùn dày hạt" : "Dense drizzle", "\uf73d"),          // fa-cloud-rain

            56 or 57 => (isVi ? "Mưa phùn giá buốt" : "Freezing drizzle", "\uf2dc"),  // fa-snowflake

            61 => (isVi ? "Mưa nhỏ nhẹ hạt" : "Slight rain", "\uf73d"),           // fa-cloud-rain
            63 => (isVi ? "Mưa vừa" : "Moderate rain", "\uf73d"),                   // fa-cloud-rain
            65 => (isVi ? "Mưa to diện rộng" : "Heavy rain", "\uf740"),          // fa-cloud-showers-heavy

            66 or 67 => (isVi ? "Mưa lạnh đóng băng" : "Freezing rain", "\uf2dc"), // fa-snowflake

            71 => (isVi ? "Tuyết rơi nhẹ" : "Slight snowfall", "\uf2dc"),             // fa-snowflake
            73 => (isVi ? "Tuyết rơi vừa" : "Moderate snowfall", "\uf2dc"),             // fa-snowflake
            75 => (isVi ? "Tuyết rơi dày đặc" : "Heavy snowfall", "\uf2dc"),         // fa-snowflake
            77 => (isVi ? "Hạt tuyết li ti" : "Snow grains", "\uf2dc"),           // fa-snowflake

            80 => (isVi ? "Mưa rào nhẹ" : "Slight rain showers", "\uf73d"),               // fa-cloud-rain
            81 => (isVi ? "Mưa rào vừa" : "Moderate rain showers", "\uf73d"),               // fa-cloud-rain
            82 => (isVi ? "Mưa rào xối xả" : "Violent rain showers", "\uf740"),            // fa-cloud-showers-heavy

            85 or 86 => (isVi ? "Mưa rào kèm tuyết" : "Snow showers", "\uf2dc"),   // fa-snowflake

            95 => (isVi ? "Dông sét, mưa dông" : "Thunderstorm", "\uf76c"),        // fa-cloud-bolt
            96 => (isVi ? "Mưa dông kèm mưa đá nhỏ" : "Thunderstorm with slight hail", "\uf76c"),   // fa-cloud-bolt
            99 => (isVi ? "Mưa dông bão mạnh kèm mưa đá" : "Thunderstorm with heavy hail", "\uf76c"), // fa-cloud-bolt

            _ => (isVi ? "Thời tiết bình thường" : "Normal weather", "\uf0c2")       // fa-cloud
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
        bool isVi = LocalizationService.Instance.IsVietnamese;
        return effect switch
        {
            WeatherEffectType.Thunderstorm => (isVi ? "⚡ CẢNH BÁO DÔNG SÉT & MƯA LỚN" : "⚡ THUNDERSTORM & HEAVY RAIN ALERT", "#DC2626"),
            WeatherEffectType.HeavyRain => (isVi ? "🌧️ MƯA TO DIỆN RỘNG" : "🌧️ EXTENSIVE HEAVY RAIN", "#0284C7"),
            WeatherEffectType.ModerateRain => (isVi ? "🌧️ TRỜI ĐANG CÓ MƯA" : "🌧️ CURRENTLY RAINING", "#0284C7"),
            WeatherEffectType.LightRain => (isVi ? "🌦️ MƯA PHÙN RẢI RÁC" : "🌦️ SCATTERED DRIZZLE", "#0EA5E9"),
            WeatherEffectType.HighUvSunny => (isVi ? $"🔥 NẮNG GẮT • CHỈ SỐ UV {uvIndex:F1} (RẤT CAO)" : $"🔥 INTENSE SUN • UV {uvIndex:F1} (VERY HIGH)", "#EA580C"),
            WeatherEffectType.ClearSunny => (temp >= 35 ? (isVi ? "☀️ TRỜI NẮNG NÓNG" : "☀️ HOT & SUNNY") : "", "#F59E0B"),
            WeatherEffectType.Fog => (isVi ? "🌫️ SƯƠNG MÙ GIẢM TẦM NHÌN" : "🌫️ FOG REDUCING VISIBILITY", "#64748B"),
            WeatherEffectType.Snow => (isVi ? "❄️ TUYẾT RƠI" : "❄️ SNOWFALL", "#38BDF8"),
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
        bool isVi = LocalizationService.Instance.IsVietnamese;
        string[] viDirs = { "Bắc", "Đông Bắc", "Đông", "Đông Nam", "Nam", "Tây Nam", "Tây", "Tây Bắc" };
        string[] enDirs = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };
        int index = (int)Math.Round(((degrees % 360) / 45)) % 8;
        return isVi ? viDirs[index] : enDirs[index];
    }

    public static (string level, string advice) GetUvInterpretation(double uvIndex)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (isVi)
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
        else
        {
            return uvIndex switch
            {
                < 3 => ("Low (0-2)", "Safe to stay outdoors."),
                < 6 => ("Moderate (3-5)", "Wear sunscreen and a hat."),
                < 8 => ("High (6-7)", "Protect eyes and skin carefully."),
                < 11 => ("Very High (8-10)", "Avoid midday sun exposure."),
                _ => ("Extreme (11+)", "Take full sun protection precautions!")
            };
        }
    }

    public static (string level, string color, string advice) GetAqiInterpretation(int aqi)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (isVi)
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
        else
        {
            return aqi switch
            {
                <= 50 => ("Good", "#10B981", "Air quality is satisfactory and poses little or no risk for outdoor activities."),
                <= 100 => ("Moderate", "#F59E0B", "Air quality is acceptable. Very sensitive individuals should take caution."),
                <= 150 => ("Unhealthy (Sensitive)", "#EA580C", "Members of sensitive groups may experience health effects. General public less affected."),
                <= 200 => ("Unhealthy", "#DC2626", "Some members of the general public may experience health effects. Wear a PM2.5 mask."),
                <= 300 => ("Very Unhealthy", "#9333EA", "Health alert: The risk of health effects is increased for everyone. Keep windows closed."),
                _ => ("Hazardous", "#7F1D1D", "Health warning of emergency conditions: Everyone is more likely to be affected. Stay indoors.")
            };
        }
    }

    public static string GetPm25Status(double pm25)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (pm25 <= 15) return isVi ? "Đạt chuẩn" : "Good";
        if (pm25 <= 35) return isVi ? "Chấp nhận được" : "Moderate";
        if (pm25 <= 55) return isVi ? "Kém" : "Poor";
        return isVi ? "Vượt chuẩn" : "Unhealthy";
    }

    public static string GetPm10Status(double pm10)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (pm10 <= 45) return isVi ? "Đạt chuẩn" : "Good";
        if (pm10 <= 80) return isVi ? "Chấp nhận được" : "Moderate";
        return isVi ? "Vượt chuẩn" : "Unhealthy";
    }

    public static string GetOzoneStatus(double o3)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (o3 <= 100) return isVi ? "Tốt" : "Good";
        if (o3 <= 160) return isVi ? "Trung bình" : "Moderate";
        return isVi ? "Cao" : "High";
    }

    public static string GetNo2Status(double no2)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        if (no2 <= 40) return isVi ? "Tốt" : "Good";
        if (no2 <= 80) return isVi ? "Trung bình" : "Moderate";
        return isVi ? "Cao" : "High";
    }
}
