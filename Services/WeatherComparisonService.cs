using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ phân tích và so sánh thời tiết đối đầu giữa 2 thành phố (Weather Duel Comparison)
/// </summary>
public class WeatherComparisonService
{
    private readonly IWeatherService _weatherService;

    public WeatherComparisonService(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>
    /// Danh sách các thành phố gợi ý so sánh nhanh phổ biến trong nước và quốc tế
    /// </summary>
    public List<QuickCompareCityItem> GetPresetCities(bool isVietnamese)
    {
        return new List<QuickCompareCityItem>
        {
            new() { Name = "Hà Nội", Subtitle = isVietnamese ? "Thủ đô" : "Capital", Latitude = 21.0285, Longitude = 105.8542, IconGlyph = "\uf1ad" },
            new() { Name = "TP. Hồ Chí Minh", Subtitle = isVietnamese ? "Miền Nam" : "South", Latitude = 10.8231, Longitude = 106.6297, IconGlyph = "\uf1ad" },
            new() { Name = "Đà Nẵng", Subtitle = isVietnamese ? "Miền Trung" : "Central", Latitude = 16.0544, Longitude = 108.2022, IconGlyph = "\uf773" },
            new() { Name = "Đà Lạt", Subtitle = isVietnamese ? "Cao nguyên mát mẻ" : "Highland", Latitude = 11.9404, Longitude = 108.4583, IconGlyph = "\uf1bb" },
            new() { Name = "Nha Trang", Subtitle = isVietnamese ? "Thành phố biển" : "Coastal City", Latitude = 12.2388, Longitude = 109.1967, IconGlyph = "\uf5ca" },
            new() { Name = "Sa Pa", Subtitle = isVietnamese ? "Vùng cao Tây Bắc" : "Mountain City", Latitude = 22.3364, Longitude = 103.8438, IconGlyph = "\uf2dc" },
            new() { Name = "Hải Phòng", Subtitle = isVietnamese ? "Thành phố Cảng" : "Port City", Latitude = 20.8449, Longitude = 106.6881, IconGlyph = "\uf21a" },
            new() { Name = "Cần Thơ", Subtitle = isVietnamese ? "Tây Nam Bộ" : "Mekong Delta", Latitude = 10.0452, Longitude = 105.7469, IconGlyph = "\uf773" },
            new() { Name = "Tokyo", Subtitle = isVietnamese ? "Nhật Bản" : "Japan", Latitude = 35.6762, Longitude = 139.6503, IconGlyph = "\uf0ac" },
            new() { Name = "Seoul", Subtitle = isVietnamese ? "Hàn Quốc" : "South Korea", Latitude = 37.5665, Longitude = 126.9780, IconGlyph = "\uf0ac" },
            new() { Name = "Bangkok", Subtitle = isVietnamese ? "Thái Lan" : "Thailand", Latitude = 13.7563, Longitude = 100.5018, IconGlyph = "\uf0ac" },
            new() { Name = "Singapore", Subtitle = isVietnamese ? "Đảo quốc Sư tử" : "Singapore", Latitude = 1.3521, Longitude = 103.8198, IconGlyph = "\uf0ac" },
            new() { Name = "Paris", Subtitle = isVietnamese ? "Pháp" : "France", Latitude = 48.8566, Longitude = 2.3522, IconGlyph = "\uf0ac" },
            new() { Name = "New York", Subtitle = isVietnamese ? "Hoa Kỳ" : "USA", Latitude = 40.7128, Longitude = -74.0060, IconGlyph = "\uf0ac" },
            new() { Name = "London", Subtitle = isVietnamese ? "Vương quốc Anh" : "UK", Latitude = 51.5074, Longitude = -0.1278, IconGlyph = "\uf0ac" }
        };
    }

    /// <summary>
    /// Thực hiện đối chiếu so sánh toàn diện 2 thành phố
    /// </summary>
    public async Task<WeatherComparisonData> CompareWeatherAsync(
        CurrentWeatherDisplay cityA,
        double latB,
        double lonB,
        string nameB,
        AppSettings settings,
        bool isVietnamese)
    {
        var result = new WeatherComparisonData
        {
            CityA = cityA
        };

        // 1. Tải dữ liệu thời tiết và chất lượng không khí cho Thành phố B
        var weatherDataB = await _weatherService.GetWeatherDataAsync(latB, lonB);
        var airQualityB = await _weatherService.GetAirQualityAsync(latB, lonB);

        if (weatherDataB != null)
        {
            result.CityB = _weatherService.CreateCurrentWeatherDisplay(weatherDataB, nameB, settings, airQualityB);
        }
        else
        {
            result.CityB = new CurrentWeatherDisplay
            {
                LocationName = nameB,
                TemperatureText = "--°C",
                ConditionText = isVietnamese ? "Không thể tải dữ liệu" : "Unable to load data"
            };
            return result;
        }

        // 2. So sánh chênh lệch nhiệt độ
        double tempA = cityA.TemperatureValue;
        double tempB = result.CityB.TemperatureValue;
        double tempDiff = Math.Round(tempB - tempA, 1);
        string tempUnit = settings.TemperatureUnit == "F" ? "°F" : "°C";

        if (tempDiff > 0.5)
        {
            result.TempComparisonBadge = isVietnamese ? $"+{tempDiff}{tempUnit} Nóng hơn" : $"+{tempDiff}{tempUnit} Warmer";
            result.TempDiffText = isVietnamese 
                ? $"{result.CityB.LocationName} đang ấm hơn {cityA.LocationName} +{tempDiff}{tempUnit}"
                : $"{result.CityB.LocationName} is warmer than {cityA.LocationName} by +{tempDiff}{tempUnit}";
        }
        else if (tempDiff < -0.5)
        {
            result.TempComparisonBadge = isVietnamese ? $"{tempDiff}{tempUnit} Mát hơn" : $"{tempDiff}{tempUnit} Cooler";
            result.TempDiffText = isVietnamese 
                ? $"{result.CityB.LocationName} đang mát hơn {cityA.LocationName} {Math.Abs(tempDiff)}{tempUnit}"
                : $"{result.CityB.LocationName} is cooler than {cityA.LocationName} by {Math.Abs(tempDiff)}{tempUnit}";
        }
        else
        {
            result.TempComparisonBadge = isVietnamese ? "Nhiệt độ tương đương" : "Similar Temp";
            result.TempDiffText = isVietnamese 
                ? $"Hai thành phố có mức nhiệt xấp xỉ nhau (~{Math.Round(tempA)}{tempUnit})"
                : $"Both cities have approximately equal temperature (~{Math.Round(tempA)}{tempUnit})";
        }

        // 3. Xây dựng các hàng chỉ số so sánh chi tiết
        var rows = new List<MetricComparisonRow>();

        // Chỉ số 1: Nhiệt độ & Cảm giác thực tế
        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Nhiệt Độ & Cảm Nhận" : "Temperature & Real Feel",
            IconGlyph = "\uf76c",
            IconColor = "#F59E0B",
            ValueA = cityA.TemperatureText,
            SubValueA = cityA.FeelsLikeText,
            ValueB = result.CityB.TemperatureText,
            SubValueB = result.CityB.FeelsLikeText,
            DifferenceBadge = tempDiff == 0 
                ? (isVietnamese ? "Ngang nhau" : "Equal")
                : (tempDiff > 0 ? $"+{tempDiff}{tempUnit}" : $"{tempDiff}{tempUnit}"),
            DifferenceBadgeColor = Math.Abs(tempDiff) > 5 ? "#EF4444" : "#F59E0B",
            AdvantageCity = tempDiff == 0 ? "Equal" : (Math.Abs(tempA - 24) < Math.Abs(tempB - 24) ? "A" : "B")
        });

        // Chỉ số 2: Tình trạng mưa & Lượng mưa
        double rainProbA = cityA.RainProbabilityValue;
        double rainProbB = result.CityB.RainProbabilityValue;
        double rainDiff = rainProbB - rainProbA;

        string rainBadge = isVietnamese
            ? (rainDiff > 15 ? $"{result.CityB.LocationName} dễ mưa hơn" : (rainDiff < -15 ? $"{cityA.LocationName} dễ mưa hơn" : "Tương đương"))
            : (rainDiff > 15 ? $"{result.CityB.LocationName} rainier" : (rainDiff < -15 ? $"{cityA.LocationName} rainier" : "Similar"));

        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Xác Suất Mưa & Lượng Mưa" : "Rain Probability & Precip",
            IconGlyph = "\uf73d",
            IconColor = "#38BDF8",
            ValueA = cityA.RainProbabilityText,
            SubValueA = cityA.PrecipitationText,
            ValueB = result.CityB.RainProbabilityText,
            SubValueB = result.CityB.PrecipitationText,
            DifferenceBadge = rainBadge,
            DifferenceBadgeColor = Math.Max(rainProbA, rainProbB) > 50 ? "#38BDF8" : "#10B981",
            AdvantageCity = rainProbA < rainProbB ? "A" : (rainProbB < rainProbA ? "B" : "Equal")
        });

        // Chỉ số 3: Chất lượng không khí (AQI)
        int aqiA = cityA.AqiValue > 0 ? cityA.AqiValue : 50;
        int aqiB = result.CityB.AqiValue > 0 ? result.CityB.AqiValue : 50;
        int aqiDiff = aqiB - aqiA;

        string aqiBadge = isVietnamese
            ? (aqiDiff < -20 ? $"{result.CityB.LocationName} trong lành hơn" : (aqiDiff > 20 ? $"{cityA.LocationName} trong lành hơn" : "Tương đương"))
            : (aqiDiff < -20 ? $"{result.CityB.LocationName} cleaner air" : (aqiDiff > 20 ? $"{cityA.LocationName} cleaner air" : "Similar"));

        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Chất Lượng Không Khí (AQI)" : "Air Quality Index (AQI)",
            IconGlyph = "\uf75f",
            IconColor = aqiA <= 50 && aqiB <= 50 ? "#10B981" : "#F59E0B",
            ValueA = $"AQI {aqiA}",
            SubValueA = cityA.AqiDescription,
            ValueB = $"AQI {aqiB}",
            SubValueB = result.CityB.AqiDescription,
            DifferenceBadge = aqiBadge,
            DifferenceBadgeColor = aqiA < aqiB ? "#10B981" : "#38BDF8",
            AdvantageCity = aqiA < aqiB ? "A" : (aqiB < aqiA ? "B" : "Equal")
        });

        // Chỉ số 4: Chỉ số bức xạ tia cực tím (UV)
        double uvA = cityA.UvIndexValue;
        double uvB = result.CityB.UvIndexValue;
        double uvDiff = Math.Round(uvB - uvA, 1);

        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Chỉ Số Tia Cực Tím (UV)" : "UV Radiation Index",
            IconGlyph = "\uf185",
            IconColor = Math.Max(uvA, uvB) >= 8 ? "#EF4444" : "#F59E0B",
            ValueA = cityA.UvIndexText,
            SubValueA = cityA.UvIndexDescription,
            ValueB = result.CityB.UvIndexText,
            SubValueB = result.CityB.UvIndexDescription,
            DifferenceBadge = Math.Abs(uvDiff) < 1.0 
                ? (isVietnamese ? "Mức UV như nhau" : "Similar UV")
                : (uvDiff > 0 ? (isVietnamese ? $"{result.CityB.LocationName} gắt hơn" : $"{result.CityB.LocationName} harsher") : (isVietnamese ? $"{cityA.LocationName} gắt hơn" : $"{cityA.LocationName} harsher")),
            DifferenceBadgeColor = Math.Max(uvA, uvB) >= 8 ? "#EF4444" : "#F59E0B",
            AdvantageCity = uvA < uvB ? "A" : (uvB < uvA ? "B" : "Equal")
        });

        // Chỉ số 5: Độ ẩm không khí
        double humA = cityA.HumidityValue;
        double humB = result.CityB.HumidityValue;

        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Độ Ẩm Không Khí" : "Relative Humidity",
            IconGlyph = "\uf043",
            IconColor = "#0284C7",
            ValueA = cityA.HumidityText,
            SubValueA = isVietnamese ? (humA > 80 ? "Ẩm ướt" : (humA < 45 ? "Khô hanh" : "Dễ chịu")) : (humA > 80 ? "Humid" : "Comfortable"),
            ValueB = result.CityB.HumidityText,
            SubValueB = isVietnamese ? (humB > 80 ? "Ẩm ướt" : (humB < 45 ? "Khô hanh" : "Dễ chịu")) : (humB > 80 ? "Humid" : "Comfortable"),
            DifferenceBadge = Math.Abs(humB - humA) < 8 
                ? (isVietnamese ? "Độ ẩm gần bằng" : "Similar")
                : (humB > humA ? (isVietnamese ? $"+{Math.Round(humB - humA)}% Ẩm hơn" : $"+{Math.Round(humB - humA)}% Humid") : (isVietnamese ? $"{Math.Round(humB - humA)}% Khô hơn" : $"{Math.Round(humB - humA)}% Drier")),
            DifferenceBadgeColor = "#0284C7",
            AdvantageCity = Math.Abs(humA - 60) < Math.Abs(humB - 60) ? "A" : "B"
        });

        // Chỉ số 6: Sức gió & Hướng gió
        rows.Add(new MetricComparisonRow
        {
            MetricName = isVietnamese ? "Tốc Độ Gió & Hướng Gió" : "Wind Speed & Direction",
            IconGlyph = "\uf72e",
            IconColor = "#10B981",
            ValueA = cityA.WindText,
            SubValueA = cityA.WindDirectionText,
            ValueB = result.CityB.WindText,
            SubValueB = result.CityB.WindDirectionText,
            DifferenceBadge = isVietnamese ? "Gió tự nhiên" : "Natural breeze",
            DifferenceBadgeColor = "#10B981",
            AdvantageCity = "Equal"
        });

        result.MetricRows = rows;

        // 4. Tính toán Phán quyết độ dễ chịu tổng thể (Comfort Verdict)
        // Điểm đánh giá (càng cao càng dễ chịu):
        // Nhiệt độ gần 24-25°C là lý tưởng
        double scoreA = 100 - Math.Abs(tempA - 24) * 3 - (rainProbA * 0.4) - (aqiA > 100 ? (aqiA - 100) * 0.3 : 0);
        double scoreB = 100 - Math.Abs(tempB - 24) * 3 - (rainProbB * 0.4) - (aqiB > 100 ? (aqiB - 100) * 0.3 : 0);

        if (scoreB > scoreA + 12)
        {
            result.VerdictTitle = isVietnamese 
                ? $"{result.CityB.LocationName} có thời tiết dễ chịu hơn hôm nay!"
                : $"{result.CityB.LocationName} has more pleasant weather today!";
            result.VerdictDescription = isVietnamese
                ? $"{result.CityB.LocationName} đạt điểm số khí hậu thuận lợi hơn nhờ nhiệt độ êm dịu ({result.CityB.TemperatureText}) và môi trường ngoài trời thoáng mát hơn."
                : $"{result.CityB.LocationName} scores higher for climate comfort with pleasant temperature ({result.CityB.TemperatureText}) and comfortable outdoor environment.";
            result.VerdictBadgeText = isVietnamese ? $"{result.CityB.LocationName} Thắng" : $"{result.CityB.LocationName} Wins";
            result.VerdictBadgeColor = "#10B981";
        }
        else if (scoreA > scoreB + 12)
        {
            result.VerdictTitle = isVietnamese 
                ? $"{cityA.LocationName} có thời tiết dễ chịu hơn hôm nay!"
                : $"{cityA.LocationName} has more pleasant weather today!";
            result.VerdictDescription = isVietnamese
                ? $"{cityA.LocationName} có điều kiện thời tiết ổn định hơn ({cityA.TemperatureText}), ít biến động bất lợi so với {result.CityB.LocationName}."
                : $"{cityA.LocationName} offers more comfortable and stable conditions ({cityA.TemperatureText}) compared to {result.CityB.LocationName}.";
            result.VerdictBadgeText = isVietnamese ? $"{cityA.LocationName} Thắng" : $"{cityA.LocationName} Wins";
            result.VerdictBadgeColor = "#38BDF8";
        }
        else
        {
            result.VerdictTitle = isVietnamese 
                ? "Hai thành phố có mức độ thoải mái tương đương nhau"
                : "Both cities offer similar comfort levels today";
            result.VerdictDescription = isVietnamese
                ? "Cả 2 địa điểm đều có ưu và nhược điểm khí hậu bù trừ cho nhau, thích hợp cho kế hoạch sinh hoạt hàng ngày."
                : "Both locations balance distinct climatic traits, suitable for everyday routines.";
            result.VerdictBadgeText = isVietnamese ? "Hòa / Cân Bằng" : "Tie / Balanced";
            result.VerdictBadgeColor = "#F59E0B";
        }

        // 5. Tạo Lời khuyên di chuyển & Hành lý (Travel & Packing Advisor)
        result.TravelAdviceTitle = isVietnamese 
            ? $"Lộ Trình: {cityA.LocationName} ➔ {result.CityB.LocationName}"
            : $"Route: {cityA.LocationName} ➔ {result.CityB.LocationName}";

        var tips = new List<string>();

        if (tempDiff <= -6)
        {
            tips.Add(isVietnamese 
                ? $"❄️ Nhiệt độ giảm sâu ({tempDiff}{tempUnit}): Chuẩn bị áo khoác dày, khăn ấm và áo giữ nhiệt khi đến {result.CityB.LocationName}."
                : $"❄️ Temperature drops sharply ({tempDiff}{tempUnit}): Pack warm coat, scarf, and thermal wear for {result.CityB.LocationName}.");
        }
        else if (tempDiff >= 6)
        {
            tips.Add(isVietnamese 
                ? $"☀️ Nhiệt độ tăng cao (+{tempDiff}{tempUnit}): Mang trang phục cotton mỏng, thoáng mát, kính mát và bổ sung đủ nước."
                : $"☀️ Temperature rises significantly (+{tempDiff}{tempUnit}): Pack light breathable cotton clothes, sunglasses, and stay hydrated.");
        }

        if (rainProbB >= 60)
        {
            tips.Add(isVietnamese 
                ? $"🌧️ {result.CityB.LocationName} có nguy cơ mưa cao ({result.CityB.RainProbabilityText}): Luôn để sẵn ô gấp gọn hoặc áo mưa trong hành lý xách tay."
                : $"🌧️ High rain chance in {result.CityB.LocationName} ({result.CityB.RainProbabilityText}): Keep a compact umbrella or raincoat handy in your daypack.");
        }
        else if (rainProbA >= 60 && rainProbB < 30)
        {
            tips.Add(isVietnamese
                ? $"🌤️ Đến {result.CityB.LocationName} thời tiết sẽ khô ráo và tạnh hơn điểm khởi hành, rất thuận lợi cho lịch trình du lịch."
                : $"🌤️ Arriving in {result.CityB.LocationName} promises drier weather, perfect for outdoor exploration.");
        }

        if (uvB >= 7)
        {
            tips.Add(isVietnamese 
                ? $"🛡️ Tia UV tại {result.CityB.LocationName} ở mức cao: Mang theo kem chống nắng SPF50+, mũ rộng vành để bảo vệ da."
                : $"🛡️ High UV index in {result.CityB.LocationName}: Carry SPF50+ sunscreen and a wide-brim hat to shield your skin.");
        }

        if (aqiB >= 150)
        {
            tips.Add(isVietnamese 
                ? $"😷 Không khí tại {result.CityB.LocationName} có cảnh báo ô nhiễm: Trang bị khẩu trang lọc bụi mịn N95 khi di chuyển ngoài phố."
                : $"😷 Unhealthy air quality in {result.CityB.LocationName}: Wear N95 filtration masks when moving outdoors.");
        }

        if (tips.Count == 0)
        {
            tips.Add(isVietnamese 
                ? "✨ Khí hậu hai bên khá tương đồng, bạn chỉ cần mang trang phục thường ngày thoải mái và năng động."
                : "✨ Weather between both destinations is pleasant and similar; regular comfortable attire is sufficient.");
        }

        result.PackingTips = string.Join("\n\n", tips);

        return result;
    }
}
