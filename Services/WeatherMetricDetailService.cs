using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WeatherApp.Helpers;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherMetricDetailService
{
    private static WeatherMetricDetailService? _instance;
    public static WeatherMetricDetailService Instance => _instance ??= new WeatherMetricDetailService();

    public WeatherMetricDetailPopupData GenerateDetailData(
        WeatherMetricType type,
        CurrentWeatherDisplay? current,
        OpenMeteoResponse? rawData,
        AirQualityData? airQuality,
        AppSettings? settings,
        LocalizationService loc)
    {
        bool isVi = loc.IsVietnamese;
        settings ??= new AppSettings();
        current ??= new CurrentWeatherDisplay();

        var result = new WeatherMetricDetailPopupData
        {
            MetricType = type
        };

        switch (type)
        {
            case WeatherMetricType.UvIndex:
                BuildUvIndexDetail(result, current, rawData, isVi);
                break;
            case WeatherMetricType.AirQuality:
                BuildAirQualityDetail(result, current, rawData, airQuality, isVi);
                break;
            case WeatherMetricType.Wind:
                BuildWindDetail(result, current, rawData, settings, isVi);
                break;
            case WeatherMetricType.Humidity:
                BuildHumidityDetail(result, current, rawData, isVi);
                break;
            case WeatherMetricType.Rain:
                BuildRainDetail(result, current, rawData, isVi);
                break;
            case WeatherMetricType.Pressure:
                BuildPressureDetail(result, current, rawData, isVi);
                break;
            case WeatherMetricType.SunMoon:
                BuildSunMoonDetail(result, current, rawData, isVi);
                break;
            case WeatherMetricType.Pollutants:
                BuildPollutantsDetail(result, current, rawData, airQuality, isVi);
                break;
        }

        return result;
    }

    #region 1. CHỈ SỐ UV (UV INDEX)
    private void BuildUvIndexDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, bool isVi)
    {
        double uv = cur.UvIndexValue;
        data.Title = isVi ? "Chỉ Số Tia Cực Tím (UV)" : "Ultraviolet (UV) Index";
        data.Subtitle = isVi ? "Bức xạ mặt trời & Nguy cơ tổn thương biểu bì da" : "Solar UV radiation & Skin damage risk";
        data.IconGlyph = "\uf185";
        data.IconColor = "#F59E0B";
        data.PrimaryValueText = uv.ToString("F1", CultureInfo.InvariantCulture);
        data.UnitText = "UV";
        data.ScaleStandardName = isVi ? "Thang đo chỉ số tử ngoại chuẩn WHO & WMO" : "WHO / WMO Global Solar UV Index Standard";
        data.GaugeMinLabel = "0 (An toàn)";
        data.GaugeMidLabel = "5.5 (Vừa)";
        data.GaugeMaxLabel = "11+ (Cực độ)";
        data.GaugeGradientStart = "#10B981";
        data.GaugeGradientEnd = "#A855F7";
        data.GaugePercent = Math.Clamp(uv / 11.0, 0.05, 1.0);

        string badgeText;
        string badgeColor;
        if (uv <= 2.9)
        {
            badgeText = isVi ? "Thấp • An toàn" : "Low • Safe";
            badgeColor = "#10B981";
        }
        else if (uv <= 5.9)
        {
            badgeText = isVi ? "Trung bình • Cần che chắn" : "Moderate • Use Protection";
            badgeColor = "#FBBF24";
        }
        else if (uv <= 7.9)
        {
            badgeText = isVi ? "Cao • Nguy cơ cháy nắng" : "High • Sunburn Risk";
            badgeColor = "#F97316";
        }
        else if (uv <= 10.9)
        {
            badgeText = isVi ? "Rất cao • Nguy hiểm cho da" : "Very High • Hazardous";
            badgeColor = "#EF4444";
        }
        else
        {
            badgeText = isVi ? "Cực độ • Nguy hại nghiêm trọng" : "Extreme • Severe Hazard";
            badgeColor = "#A855F7";
        }

        data.StatusBadgeText = badgeText;
        data.StatusBadgeColor = badgeColor;
        data.SecondaryInfoText = isVi
            ? $"Giới hạn phơi nắng an toàn: {(uv > 7 ? "15-20 phút" : uv > 4 ? "30-45 phút" : "> 60 phút")} • Đỉnh bức xạ: 11:30 - 14:00"
            : $"Safe sun exposure time: {(uv > 7 ? "15-20 mins" : uv > 4 ? "30-45 mins" : "> 60 mins")} • Peak radiation: 11:30 - 14:00";

        data.ScientificExplanation = isVi
            ? "Chỉ số UV đo lường cường độ bức xạ cực tím gây cháy nắng tại mặt đất. Tia UVA gây lão hóa, nám da trong khi UVB gây bỏng rát và biến đổi tế bào biểu bì. Tia cực tím vẫn có thể xuyên qua mây mỏng và phản xạ mạnh trên mặt nước, kính hoặc bê tông."
            : "The UV Index measures the level of skin-damaging ultraviolet radiation reaching Earth's surface. UVA penetrates deep causing premature aging while UVB causes acute sunburn. Clouds only partially block UV rays, and reflections from glass and water increase exposure.";

        // Smart Advice Items
        if (uv >= 6.0)
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf57b",
                IconColor = "#EF4444",
                Category = isVi ? "Hành động tức thì" : "Immediate Action",
                Title = isVi ? "Thoa kem chống nắng phổ rộng SPF 50+ PA++++" : "Apply Broad-Spectrum Sunscreen SPF 50+",
                Description = isVi
                    ? "Thoa trước khi ra ngoài 20 phút và dặm lại sau mỗi 2-3 giờ. Đeo kính râm chống tia UV400 bảo vệ giác mạc và mặc áo dài tay vải dệt sợi chống nắng UPF 50+."
                    : "Apply 20 minutes prior to outdoor exposure and reapply every 2 hours. Wear UV400 sunglasses and UPF 50+ protective outerwear.",
                BadgeText = isVi ? "Bắt buộc" : "Mandatory",
                BadgeBackground = "#EF4444"
            });
        }
        else
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf57b",
                IconColor = "#10B981",
                Category = isVi ? "Chăm sóc da nhẹ nhàng" : "Mild Skincare",
                Title = isVi ? "Chống nắng nhẹ nhàng SPF 30+" : "Light Sunscreen SPF 30+",
                Description = isVi
                    ? "Mức bức xạ hiện tại ở ngưỡng an toàn vừa phải. Chỉ cần thoa một lớp kem dưỡng ẩm chống nắng nhẹ và đội mũ khi hoạt động ngoài trời lâu."
                    : "Current UV level is relatively safe. A light moisturizing sunscreen and cap will suffice for everyday exposure.",
                BadgeText = isVi ? "Khuyên dùng" : "Recommended",
                BadgeBackground = "#10B981"
            });
        }

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf017",
            IconColor = "#F59E0B",
            Category = isVi ? "Khung giờ vàng & Cảnh báo" : "Time Window & Caution",
            Title = isVi ? "Hạn chế tiếp xúc nắng gắt từ 10:00 - 15:30" : "Avoid Direct Sun Exposure 10:00 - 15:30",
            Description = isVi
                ? "Hơn 70% tổng lượng tia UV trong ngày tập trung vào khung giờ trưa. Khung giờ sáng sớm trước 08:30 là thời điểm tốt nhất để tắm nắng tổng hợp Vitamin D tự nhiên."
                : "Over 70% of daily UV radiation is concentrated around solar noon. Early morning before 08:30 AM is optimal for natural Vitamin D synthesis.",
            BadgeText = isVi ? "Lưu ý giờ" : "Peak Hours",
            BadgeBackground = "#F59E0B"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf06e",
            IconColor = "#38BDF8",
            Category = isVi ? "Bảo vệ mắt & Trẻ nhỏ" : "Eye & Sensitive Groups",
            Title = isVi ? "Bảo vệ thị lực và làn da nhạy cảm" : "Protect Eyes & Vulnerable Skin",
            Description = isVi
                ? "Tia UV có thể làm đục thủy tinh thể và bỏng giác mạc sau thời gian dài. Trẻ nhỏ và người có làn da nhạy cảm nên luôn che chắn bằng mũ rộng vành và che ô khi qua khu vực trống."
                : "Prolonged UV exposure accelerates cataract risk. Children and individuals with sensitive skin should wear wide-brimmed hats and seek shade.",
            BadgeText = isVi ? "Sức khỏe" : "Wellness",
            BadgeBackground = "#0284C7"
        });

        // 7-Day Forecast for UV
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            double dayUv = (raw?.Daily?.UvIndexMax != null && idx < raw.Daily.UvIndexMax.Count)
                ? raw.Daily.UvIndexMax[idx]
                : uv;

            string status = dayUv switch
            {
                <= 2.9 => isVi ? "Thấp" : "Low",
                <= 5.9 => isVi ? "Vừa" : "Moderate",
                <= 7.9 => isVi ? "Cao" : "High",
                <= 10.9 => isVi ? "Rất cao" : "Very High",
                _ => isVi ? "Nguy hại" : "Extreme"
            };

            string col = dayUv switch
            {
                <= 2.9 => "#10B981",
                <= 5.9 => "#FBBF24",
                <= 7.9 => "#F97316",
                <= 10.9 => "#EF4444",
                _ => "#A855F7"
            };

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{dayUv:F1} UV",
                NumericValue = dayUv,
                BarPercent = Math.Clamp(dayUv / 11.0, 0.08, 1.0),
                BarColor = col,
                StatusBadge = status,
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Chỉ số UV cực đại {dayUv:F1}" : $"Peak UV index {dayUv:F1}"
            };
        }, isVi);
    }
    #endregion

    #region 2. CHẤT LƯỢNG KHÔNG KHÍ (AIR QUALITY AQI)
    private void BuildAirQualityDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, AirQualityData? aqiData, bool isVi)
    {
        int aqi = cur.AqiValue > 0 ? cur.AqiValue : (aqiData?.Aqi ?? 42);
        data.Title = isVi ? "Chất Lượng Không Khí (US-AQI)" : "Air Quality Index (US-AQI)";
        data.Subtitle = isVi ? "Mức độ ô nhiễm khí quyển và bụi mịn PM2.5 / PM10" : "Atmospheric pollution & Fine particulate matter PM2.5 / PM10";
        data.IconGlyph = "\uf75f";
        data.IconColor = cur.AqiColor;
        data.PrimaryValueText = aqi.ToString();
        data.UnitText = "US-AQI";
        data.StatusBadgeText = cur.AqiDescription;
        data.StatusBadgeColor = cur.AqiColor;
        data.ScaleStandardName = isVi ? "Thang đo chỉ số chất lượng không khí US-EPA Quốc Tế" : "US-EPA International Air Quality Standards";
        data.GaugeMinLabel = "0 (Trong lành)";
        data.GaugeMidLabel = "100 (Trung bình)";
        data.GaugeMaxLabel = "300+ (Nguy hại)";
        data.GaugeGradientStart = "#10B981";
        data.GaugeGradientEnd = "#7C3AED";
        data.GaugePercent = Math.Clamp(aqi / 300.0, 0.05, 1.0);

        data.SecondaryInfoText = isVi
            ? $"Bụi mịn PM2.5: {cur.Pm25Text} • Bụi thô PM10: {cur.Pm10Text} • Ozone: {cur.OzoneText}"
            : $"Fine dust PM2.5: {cur.Pm25Text} • Particulate PM10: {cur.Pm10Text} • Ozone: {cur.OzoneText}";

        data.ScientificExplanation = isVi
            ? "Chỉ số US-AQI phản ánh độ tinh sạch của không khí. Hạt bụi mịn PM2.5 với đường kính dưới 2.5 micromet có thể xâm nhập sâu qua phế nang vào tuần hoàn máu. Khi AQI trên 100, nguy cơ kích ứng xoang mũi và hệ hô hấp tăng cao rõ rệt."
            : "US-AQI standardizes airborne pollutants. PM2.5 fine particles (diameter <2.5 micrometers) bypass airway filters into lung alveoli and bloodstream. AQI levels above 100 present respiratory irritations for sensitive and healthy individuals alike.";

        // Smart Advices
        if (aqi > 100)
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf6cf",
                IconColor = "#EF4444",
                Category = isVi ? "Bảo vệ đường hô hấp" : "Respiratory Defense",
                Title = isVi ? "Đeo khẩu trang lọc bụi chuẩn N95 / KF94" : "Wear N95 / KF94 Filter Mask",
                Description = isVi
                    ? "Khẩu trang vải hoặc y tế thông thường không ngăn được bụi mịn PM2.5. Nên trang bị khẩu trang đạt chuẩn ôm khít mặt khi lưu thông trên các trục đường lớn."
                    : "Standard surgical masks do not effectively filter PM2.5. Wear a snug-fitting N95 or KF94 respirator when traveling outdoors.",
                BadgeText = isVi ? "Cần thiết" : "Essential",
                BadgeBackground = "#EF4444"
            });

            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf015",
                IconColor = "#F59E0B",
                Category = isVi ? "Không gian trong nhà" : "Indoor Living",
                Title = isVi ? "Đóng cửa đón gió ô nhiễm & Bật máy lọc không khí" : "Close Windows & Run HEPA Purifier",
                Description = isVi
                    ? "Hạn chế mở cửa sổ hướng ra mặt đường xe cộ vào giờ cao điểm. Bật máy lọc không khí màng HEPA và vệ sinh màng lọc thô định kỳ."
                    : "Keep street-facing windows shut during rush hours. Run HEPA air purifiers to clean indoor air.",
                BadgeText = isVi ? "Nhà ở" : "Home",
                BadgeBackground = "#F59E0B"
            });
        }
        else
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf2f1",
                IconColor = "#10B981",
                Category = isVi ? "Không khí trong lành" : "Fresh Air",
                Title = isVi ? "Mở cửa đón gió tươi tự nhiên" : "Open Windows for Natural Ventilation",
                Description = isVi
                    ? "Không khí ngoài trời đang ở mức trong lành, thích hợp mở cửa thông gió đón gió mát tự nhiên và làm mới không gian phòng làm việc."
                    : "Outdoor air quality is clean and crisp. Ideal time to ventilate living spaces with fresh outdoor air.",
                BadgeText = isVi ? "Rất tốt" : "Great",
                BadgeBackground = "#10B981"
            });

            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf70c",
                IconColor = "#38BDF8",
                Category = isVi ? "Thể thao & Vận động" : "Outdoor Fitness",
                Title = isVi ? "Lý tưởng cho chạy bộ & thể thao ngoài trời" : "Perfect for Outdoor Running & Workouts",
                Description = isVi
                    ? "Hô hấp thông thoáng, nồng độ oxy và độ sạch của không khí rất lý tưởng cho các buổi tập cardio, đạp xe hoặc chạy bộ công viên."
                    : "Clean airway breathing conditions. Excellent for cycling, park running, and cardio fitness sessions.",
                BadgeText = isVi ? "Lý tưởng" : "Optimal",
                BadgeBackground = "#38BDF8"
            });
        }

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf48b",
            IconColor = "#0284C7",
            Category = isVi ? "Sức khỏe & Dị ứng" : "Health & Allergies",
            Title = isVi ? "Nhỏ mắt & Vệ sinh mũi bằng nước muối sinh lý" : "Saline Nasal & Eye Rinsing",
            Description = isVi
                ? "Sau khi di chuyển ngoài đường nhiều khói bụi, nên rửa mũi bằng bình rửa xoang nước muối sinh lý 0.9% để loại bỏ dị nguyên và bụi lắng đọng."
                : "Rinse nasal passages and eyes with sterile 0.9% saline solution after commuting to flush out trapped fine particulate matter.",
            BadgeText = isVi ? "Vệ sinh" : "Hygiene",
            BadgeBackground = "#0284C7"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            // Dự báo xu hướng AQI theo mưa và gió
            int rainP = (raw?.Daily?.PrecipitationProbabilityMax != null && idx < raw.Daily.PrecipitationProbabilityMax.Count)
                ? raw.Daily.PrecipitationProbabilityMax[idx]
                : 20;

            // Mưa làm sạch không khí, khô hanh bụi tăng
            int estAqi = Math.Clamp((int)(aqi * (rainP > 50 ? 0.72 : (rainP > 25 ? 0.88 : 1.05)) + (idx * 2 - 3)), 18, 220);
            var (desc, color, _) = WeatherCodeHelper.GetAqiInterpretation(estAqi);

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{estAqi} AQI",
                NumericValue = estAqi,
                BarPercent = Math.Clamp(estAqi / 250.0, 0.08, 1.0),
                BarColor = color,
                StatusBadge = desc,
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"PM2.5 ước tính: {(estAqi * 0.28):F1} µg/m³" : $"Est. PM2.5: {(estAqi * 0.28):F1} µg/m³"
            };
        }, isVi);
    }
    #endregion

    #region 3. GIÓ & HƯỚNG GIÓ (WIND & DIRECTION)
    private void BuildWindDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, AppSettings settings, bool isVi)
    {
        double windKm = raw?.Current?.WindSpeed ?? 15.0;
        double gustsKm = raw?.Current?.WindGusts ?? (windKm * 1.35);
        double dirDeg = cur.WindDirectionDegrees;

        data.Title = isVi ? "Gió & Hướng Gió" : "Wind Speed & Direction";
        data.Subtitle = isVi ? "Tốc độ luồng khí, gió giật và góc phương vị" : "Velocity, sudden gusts & azimuth bearing";
        data.IconGlyph = "\uf72e";
        data.IconColor = "#10B981";
        data.PrimaryValueText = cur.WindText;
        data.UnitText = "";
        data.ScaleStandardName = isVi ? "Thang sức gió Beaufort Khí tượng Hải văn Quốc tế" : "International Beaufort Wind Force Scale";
        data.GaugeMinLabel = "0 (Lặng gió)";
        data.GaugeMidLabel = "30 km/h (Cấp 5)";
        data.GaugeMaxLabel = "60+ km/h (Bão)";
        data.GaugeGradientStart = "#10B981";
        data.GaugeGradientEnd = "#EF4444";
        data.GaugePercent = Math.Clamp(windKm / 60.0, 0.05, 1.0);

        string beaufortText;
        string bColor;
        if (windKm < 6) { beaufortText = isVi ? "Cấp 1 • Gió nhẹ hiu hiu" : "Beaufort 1 • Light air"; bColor = "#10B981"; }
        else if (windKm < 19) { beaufortText = isVi ? "Cấp 2-3 • Gió nhẹ dễ chịu" : "Beaufort 2-3 • Gentle breeze"; bColor = "#10B981"; }
        else if (windKm < 29) { beaufortText = isVi ? "Cấp 4 • Gió vừa phải" : "Beaufort 4 • Moderate breeze"; bColor = "#38BDF8"; }
        else if (windKm < 39) { beaufortText = isVi ? "Cấp 5 • Gió khá mạnh" : "Beaufort 5 • Fresh breeze"; bColor = "#FBBF24"; }
        else if (windKm < 50) { beaufortText = isVi ? "Cấp 6 • Gió mạnh, cành cây đu đưa" : "Beaufort 6 • Strong breeze"; bColor = "#F97316"; }
        else { beaufortText = isVi ? "Cấp 7+ • Gió giật nguy hiểm" : "Beaufort 7+ • Near Gale / Hazardous"; bColor = "#EF4444"; }

        data.StatusBadgeText = beaufortText;
        data.StatusBadgeColor = bColor;
        data.SecondaryInfoText = isVi
            ? $"Hướng thổi: {cur.WindDirectionText} ({dirDeg:F0}°) • Gió giật lên tới: {gustsKm:F0} km/h"
            : $"Direction: {cur.WindDirectionText} ({dirDeg:F0}°) • Peak gusts up to: {gustsKm:F0} km/h";

        data.ScientificExplanation = isVi
            ? "Tốc độ gió được quan trắc tại độ cao chuẩn 10m trên bề mặt địa hình trống. Chênh lệch áp suất khí quyển càng lớn thì tốc độ gió càng mạnh. Gió kết hợp với nhiệt độ thấp làm gia tăng hiệu ứng lạnh buốt (Wind Chill)."
            : "Wind speed is measured at a standard height of 10 meters above open terrain. Stronger barometric gradients generate higher velocities. Wind combined with cold air intensifies the apparent wind chill factor.";

        // Smart Advices
        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf21c",
            IconColor = windKm > 25 ? "#F59E0B" : "#10B981",
            Category = isVi ? "Di chuyển & Phương tiện" : "Transportation",
            Title = windKm > 25
                ? (isVi ? "Cẩn trọng khi lái xe máy qua cầu cao & ngã tư" : "Caution Riding Two-Wheelers on Bridges")
                : (isVi ? "Điều kiện di chuyển xe 2 bánh rất an toàn" : "Optimal Two-Wheeler Riding Conditions"),
            Description = windKm > 25
                ? (isVi ? "Gió tạt ngang và gió giật mạnh có thể làm lạng tay lái. Giữ chắc tay lái, giảm tốc độ và không mặc áo mưa cánh dơi trùm đầu." : "Crosswinds and gusts can push two-wheelers. Maintain firm grip, reduce speed, and avoid loose flapping rain ponchos.")
                : (isVi ? "Gió nhẹ, tầm nhìn thoáng, rất thuận lợi cho việc lái xe máy, đạp xe và đi bộ ngoài trời." : "Light pleasant breeze, excellent for motorcycling, cycling and walking."),
            BadgeText = windKm > 25 ? (isVi ? "Cảnh giác" : "Caution") : (isVi ? "An toàn" : "Safe"),
            BadgeBackground = windKm > 25 ? "#F59E0B" : "#10B981"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf0ac",
            IconColor = "#38BDF8",
            Category = isVi ? "Thể thao & Drone / Flycam" : "Outdoor Sports & Drone",
            Title = isVi ? "Khuyến nghị hoạt động ngoài trời & Thiết bị bay" : "Outdoor Sports & Drone Advisory",
            Description = windKm > 28
                ? (isVi ? "Không nên bay flycam/drone vì dễ bị mất kiểm soát hoặc trôi gimbal. Cầu lông ngoài trời bị ảnh hưởng nhiều." : "Do not operate drones as wind exceeds safe flight envelope. Badminton significantly affected.")
                : (isVi ? "Vận tốc gió trong ngưỡng cho phép bay flycam an toàn. Chạy bộ và đạp xe rất mát mẻ." : "Wind speed is within safe drone flight thresholds. Excellent for outdoor jogging."),
            BadgeText = isVi ? "Thể thao" : "Sports",
            BadgeBackground = "#38BDF8"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf015",
            IconColor = "#8B5CF6",
            Category = isVi ? "Nhà cửa & Ban công" : "Home & Balcony",
            Title = isVi ? "Cố định chậu cây & Giàn phơi ban công" : "Secure Balcony Plants & Drying Racks",
            Description = isVi
                ? "Gió giật có thể làm rơi đổ các chậu cây cảnh hoặc làm bay quần áo trên giàn phơi tầng cao. Kiểm tra chốt cài cửa sổ khi ra khỏi nhà."
                : "Sudden gusts can topple unsecured balcony plant pots or dislodge drying clothes. Ensure high-rise window latches are secured.",
            BadgeText = isVi ? "Nhà ở" : "Home",
            BadgeBackground = "#8B5CF6"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            double daySpeed = (raw?.Daily?.WindSpeedMax != null && idx < raw.Daily.WindSpeedMax.Count)
                ? raw.Daily.WindSpeedMax[idx]
                : windKm;

            double dayDir = (raw?.Daily?.WindDirectionDominant != null && idx < raw.Daily.WindDirectionDominant.Count)
                ? raw.Daily.WindDirectionDominant[idx]
                : dirDeg;

            string dirName = WeatherCodeHelper.GetWindDirection(dayDir);

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{Math.Round(daySpeed)} km/h",
                NumericValue = daySpeed,
                BarPercent = Math.Clamp(daySpeed / 50.0, 0.08, 1.0),
                BarColor = daySpeed > 30 ? "#EF4444" : (daySpeed > 20 ? "#F59E0B" : "#10B981"),
                StatusBadge = $"{dirName}",
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Hướng gió chủ đạo {dirName} ({dayDir:F0}°)" : $"Dominant wind {dirName} ({dayDir:F0}°)"
            };
        }, isVi);
    }
    #endregion

    #region 4. ĐỘ ẨM & ĐIỂM SƯƠNG (HUMIDITY & DEW POINT)
    private void BuildHumidityDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, bool isVi)
    {
        double hum = cur.HumidityValue;
        data.Title = isVi ? "Độ Ẩm Không Khí & Điểm Sương" : "Air Humidity & Dew Point";
        data.Subtitle = isVi ? "Hơi ẩm tương đối và hiện tượng ngưng tụ nồm ẩm" : "Relative humidity & Condensation index";
        data.IconGlyph = "\uf043";
        data.IconColor = "#0284C7";
        data.PrimaryValueText = cur.HumidityText;
        data.UnitText = "";
        data.ScaleStandardName = isVi ? "Chỉ số độ ẩm tương đối RH (Relative Humidity)" : "Relative Air Humidity Standard (RH)";
        data.GaugeMinLabel = "0% (Khô hanh)";
        data.GaugeMidLabel = "50% (Lý tưởng)";
        data.GaugeMaxLabel = "100% (Bão hòa)";
        data.GaugeGradientStart = "#F59E0B";
        data.GaugeGradientEnd = "#0284C7";
        data.GaugePercent = Math.Clamp(hum / 100.0, 0.05, 1.0);

        string humStatus;
        string hColor;
        if (hum < 40) { humStatus = isVi ? "Khô hanh • Khát nước" : "Dry • Dehydrated"; hColor = "#F59E0B"; }
        else if (hum <= 65) { humStatus = isVi ? "Lý tưởng • Dễ chịu nhất" : "Ideal • Optimum Comfort"; hColor = "#10B981"; }
        else if (hum <= 80) { humStatus = isVi ? "Ẩm cao • Hơi oi bí" : "High • Slightly Muggy"; hColor = "#38BDF8"; }
        else { humStatus = isVi ? "Rất ẩm • Nguy cơ nồm ẩm sàn" : "Very High • Sticky & Humid"; hColor = "#0284C7"; }

        data.StatusBadgeText = humStatus;
        data.StatusBadgeColor = hColor;
        data.SecondaryInfoText = isVi
            ? $"Điểm sương: {cur.DewPointText} • Cảm nhận nhiệt: {cur.FeelsLikeText}"
            : $"Dew point: {cur.DewPointText} • Apparent feel: {cur.FeelsLikeText}";

        data.ScientificExplanation = isVi
            ? "Độ ẩm tương đối phản ánh tỷ lệ phần trăm hơi nước hiện có so với mức bão hòa ở cùng nhiệt độ. Khi độ ẩm vượt quá 80%, mồ hôi trên da khó bay hơi khiến cơ thể cảm thấy nóng bức và bí bách hơn nhiệt độ thực tế. Điểm sương là nhiệt độ mà tại đó hơi nước bắt đầu ngưng tụ thành hạt sương."
            : "Relative humidity (RH) represents the moisture content relative to saturated air at the same temperature. Above 80% RH, perspiration cannot evaporate efficiently, intensifying perceived heat. Dew point is the temperature at which condensation begins.";

        // Smart Advices
        if (hum > 80)
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf0eb",
                IconColor = "#0284C7",
                Category = isVi ? "Chống nồm ẩm & Nấm mốc" : "Dehumidification & Mold",
                Title = isVi ? "Đóng kín cửa & Bật điều hòa chế độ hút ẩm (Dry)" : "Close Windows & Run AC Dry Mode",
                Description = isVi
                    ? "Không nên mở cửa đón gió ẩm vào nhà vì sẽ làm ướt đọng sàn gạch. Bật máy hút ẩm hoặc chế độ Dry của máy lạnh để giữ không khí phòng khô ráo."
                    : "Avoid opening windows to humid breezes which cause slippery floor condensation. Run dehumidifier or AC Dry mode to prevent mold.",
                BadgeText = isVi ? "Chống ẩm" : "Moisture Care",
                BadgeBackground = "#0284C7"
            });
        }
        else if (hum < 40)
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf043",
                IconColor = "#F59E0B",
                Category = isVi ? "Bảo vệ da & Hô hấp" : "Skin Hydration & Throat",
                Title = isVi ? "Bổ sung nước & Dưỡng ẩm niêm mạc mũi" : "Hydrate Body & Use Humidifier",
                Description = isVi
                    ? "Độ ẩm thấp gây khô rát cổ họng, nứt nẻ môi và khô mắt. Uống đủ 2 lít nước mỗi ngày và bật máy phun sương tạo ẩm trong phòng ngủ."
                    : "Dry air causes throat irritation, chapped lips, and dry eyes. Drink ample water and consider ultrasonic humidifiers in bedrooms.",
                BadgeText = isVi ? "Cấp nước" : "Hydration",
                BadgeBackground = "#F59E0B"
            });
        }
        else
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf14a",
                IconColor = "#10B981",
                Category = isVi ? "Điều kiện lý tưởng" : "Optimal Balance",
                Title = isVi ? "Độ ẩm vàng cho sức khỏe con người (40% - 60%)" : "Golden Humidity Range for Wellness (40% - 60%)",
                Description = isVi
                    ? "Ngưỡng độ ẩm này giúp da duy trì độ ẩm tự nhiên hoàn hảo, ngăn ngừa tối đa vi khuẩn và nấm mốc phát triển trong nhà."
                    : "This humidity range minimizes indoor viral survivability and allergen proliferation while optimizing skin hydration.",
                BadgeText = isVi ? "Cực tốt" : "Superb",
                BadgeBackground = "#10B981"
            });
        }

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf553",
            IconColor = "#38BDF8",
            Category = isVi ? "Bảo quản đồ dùng" : "Electronics & Fabrics",
            Title = isVi ? "Bảo quản thiết bị điện tử & Giặt phơi" : "Protect Electronics & Laundry",
            Description = isVi
                ? (hum > 75 ? "Quần áo giặt phơi lâu khô, nên dùng máy sấy nhiệt. Máy ảnh và ống kính nên cất trong hộp chống ẩm 45%." : "Quần áo giặt phơi mau khô tự nhiên. Thiết bị điện tử hoạt động bền bỉ, an toàn.")
                : (hum > 75 ? "Clothes take longer to dry. Store camera lenses in dry cabinets below 50% RH." : "Laundry dries quickly and naturally. Electronics operate safely."),
            BadgeText = isVi ? "Bảo quản" : "Care",
            BadgeBackground = "#0284C7"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf70c",
            IconColor = "#10B981",
            Category = isVi ? "Vận động & Giấc ngủ" : "Sleep & Fitness",
            Title = isVi ? "Chất lượng giấc ngủ & Điều hòa nhiệt cơ thể" : "Deep Sleep Comfort & Thermal Homeostasis",
            Description = isVi
                ? "Nhiệt độ phòng ngủ 24-26°C kết hợp độ ẩm 50-60% giúp cơ thể hô hấp sâu, không bị nghẹt mũi hay đổ mồ hôi trộm khi ngủ."
                : "Bedroom temperature around 24-26°C with 50-60% humidity fosters restorative deep REM sleep cycle.",
            BadgeText = isVi ? "Giấc ngủ" : "Sleep",
            BadgeBackground = "#10B981"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            // Tính độ ẩm trung bình của ngày từ hourly nếu có
            double dayHum = hum;
            if (raw?.Hourly?.RelativeHumidity != null && raw.Hourly.RelativeHumidity.Count >= (idx + 1) * 24)
            {
                var slice = raw.Hourly.RelativeHumidity.Skip(idx * 24).Take(24);
                if (slice.Any()) dayHum = Math.Round(slice.Average());
            }

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{dayHum:F0}%",
                NumericValue = dayHum,
                BarPercent = Math.Clamp(dayHum / 100.0, 0.08, 1.0),
                BarColor = dayHum > 80 ? "#0284C7" : (dayHum < 45 ? "#F59E0B" : "#10B981"),
                StatusBadge = dayHum > 80 ? (isVi ? "Rất ẩm" : "Very Humid") : (dayHum < 45 ? (isVi ? "Khô" : "Dry") : (isVi ? "Lý tưởng" : "Ideal")),
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Độ ẩm tương đối trung bình {dayHum:F0}%" : $"Mean relative humidity {dayHum:F0}%"
            };
        }, isVi);
    }
    #endregion

    #region 5. KHẢ NĂNG MƯA & LƯỢNG MƯA (RAIN CHANCE & PRECIPITATION)
    private void BuildRainDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, bool isVi)
    {
        double rainProb = cur.RainProbabilityValue;
        double rainMm = raw?.Current?.Precipitation ?? 0.0;

        data.Title = isVi ? "Khả Năng Mưa & Lượng Mưa" : "Rain Probability & Precipitation";
        data.Subtitle = isVi ? "Xác suất kết tủa và lượng mưa tích lũy dự báo" : "Probability of precipitation & predicted volume";
        data.IconGlyph = "\uf73d";
        data.IconColor = "#0EA5E9";
        data.PrimaryValueText = cur.RainProbabilityText;
        data.UnitText = "";
        data.ScaleStandardName = isVi ? "Thang xác suất kết tủa PoP (Probability of Precipitation) chuẩn WMO" : "WMO Probability of Precipitation (PoP) Index";
        data.GaugeMinLabel = "0% (Tạnh)";
        data.GaugeMidLabel = "50% (Có thể mưa)";
        data.GaugeMaxLabel = "100% (Mưa chắc chắn)";
        data.GaugeGradientStart = "#10B981";
        data.GaugeGradientEnd = "#0284C7";
        data.GaugePercent = Math.Clamp(rainProb / 100.0, 0.0, 1.0);

        string rainStatus;
        string rColor;
        if (rainProb < 20) { rainStatus = isVi ? "Tạnh ráo • Ít khả năng mưa" : "Dry • Minimal Rain Chance"; rColor = "#10B981"; }
        else if (rainProb < 50) { rainStatus = isVi ? "Có thể có mưa nhẹ rải rác" : "Scattered Showers Possible"; rColor = "#38BDF8"; }
        else if (rainProb < 75) { rainStatus = isVi ? "Khả năng mưa cao • Nên mang ô" : "High Rain Probability • Bring Umbrella"; rColor = "#0284C7"; }
        else { rainStatus = isVi ? "Chắc chắn có mưa • Cần áo mưa" : "Definite Rain • Raincoat Required"; rColor = "#2563EB"; }

        data.StatusBadgeText = rainStatus;
        data.StatusBadgeColor = rColor;
        data.SecondaryInfoText = isVi
            ? $"Lượng mưa tích lũy: {cur.PrecipitationText} • Tình trạng: {(rainProb >= 50 ? "Mưa ẩm" : "Khô ráo")}"
            : $"Accumulated precipitation: {cur.PrecipitationText} • Status: {(rainProb >= 50 ? "Wet road conditions" : "Dry pavements")}";

        data.ScientificExplanation = isVi
            ? "Chỉ số xác suất mưa PoP (Probability of Precipitation) tính toán khả năng có mưa từ 0.1mm trở lên tại một điểm bất kỳ trong khu vực dự báo. Lượng mưa (mm) thể hiện độ dày của lớp nước nếu mặt đất hoàn toàn bằng phẳng và nước không bị ngấm hay thoát đi."
            : "PoP indicates the likelihood of receiving at least 0.1 mm of rainfall at any point within the forecast region. Rain depth (mm) represents the vertical thickness of accumulated rainwater over an impermeable flat surface.";

        // Smart Advices
        if (rainProb >= 50)
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf73d",
                IconColor = "#0EA5E9",
                Category = isVi ? "Trang bị cá nhân" : "Essential Gear",
                Title = isVi ? "Luôn mang theo ô (dù) gấp gọn hoặc áo mưa" : "Carry Compact Umbrella or Raincoat",
                Description = isVi
                    ? "Mưa có thể xuất hiện bất chợt trong ngày. Đặt sẵn một chiếc ô gấp nhỏ trong túi xách hoặc để áo mưa bộ trong cốp xe máy."
                    : "Precipitation is likely. Keep a foldable umbrella in your backpack or storage compartment.",
                BadgeText = isVi ? "Cần mang" : "Pack Now",
                BadgeBackground = "#0EA5E9"
            });

            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf5e4",
                IconColor = "#EF4444",
                Category = isVi ? "An toàn giao thông" : "Traffic Safety",
                Title = isVi ? "Đường trơn trượt & Tránh phanh gấp" : "Slippery Roadway & Anti-Skid Caution",
                Description = isVi
                    ? "Nước mưa hòa cùng bụi dầu trên mặt đường làm giảm độ bám của lốp xe tới 40%. Tăng khoảng cách an toàn với xe phía trước và giảm tốc độ khi vào cua."
                    : "Rainwater mixes with road grime reducing tire traction by up to 40%. Extend following distance and avoid abrupt braking.",
                BadgeText = isVi ? "Lái xe" : "Drive Safe",
                BadgeBackground = "#EF4444"
            });
        }
        else
        {
            data.Advices.Add(new MetricAdviceItem
            {
                IconGlyph = "\uf185",
                IconColor = "#10B981",
                Category = isVi ? "Sinh hoạt tạnh ráo" : "Pleasant Weather",
                Title = isVi ? "Thời tiết tạnh ráo thuận lợi đi lại & Giặt giũ" : "Dry Weather Favors Commuting & Outdoor Tasks",
                Description = isVi
                    ? "Khả năng mưa rất thấp, thích hợp cho việc phơi chăn màn, rửa xe hoặc tổ chức các buổi gặp gỡ, dã ngoại ngoài trời."
                    : "Very low rain chance. Favorable for washing cars, doing heavy laundry drying, and attending outdoor gatherings.",
                BadgeText = isVi ? "Thuận lợi" : "Favorable",
                BadgeBackground = "#10B981"
            });
        }

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf549",
            IconColor = "#F59E0B",
            Category = isVi ? "Trang phục & Giày dép" : "Footwear & Shoes",
            Title = isVi ? "Lựa chọn giày dép & Phụ kiện chống nước" : "Footwear & Water-Resistant Choices",
            Description = isVi
                ? (rainProb >= 50 ? "Ưu tiên giày da tối màu hoặc giày bốt có lớp chống thấm nước GORE-TEX. Tránh đi giày vải trắng sáng màu." : "Thoải mái diện giày vải thể thao hoặc giày da công sở mà không lo bị ướt bẩn.")
                : (rainProb >= 50 ? "Wear water-resistant footwear or leather boots. Avoid white canvas sneakers." : "Feel free to wear canvas sneakers or formal dress shoes without splash concerns."),
            BadgeText = isVi ? "Trang phục" : "OOTD",
            BadgeBackground = "#F59E0B"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            int dayProb = (raw?.Daily?.PrecipitationProbabilityMax != null && idx < raw.Daily.PrecipitationProbabilityMax.Count)
                ? raw.Daily.PrecipitationProbabilityMax[idx]
                : (int)rainProb;

            double daySum = (raw?.Daily?.PrecipitationSum != null && idx < raw.Daily.PrecipitationSum.Count)
                ? raw.Daily.PrecipitationSum[idx]
                : rainMm;

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{dayProb}% • {daySum:F1}mm",
                NumericValue = dayProb,
                BarPercent = Math.Clamp(dayProb / 100.0, 0.05, 1.0),
                BarColor = dayProb >= 60 ? "#0284C7" : (dayProb >= 30 ? "#38BDF8" : "#10B981"),
                StatusBadge = dayProb >= 60 ? (isVi ? "Mưa to" : "Rain") : (dayProb >= 30 ? (isVi ? "Mưa rải rác" : "Scattered") : (isVi ? "Tạnh ráo" : "Dry")),
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Lượng mưa tích lũy {daySum:F1} mm" : $"Precipitation sum {daySum:F1} mm"
            };
        }, isVi);
    }
    #endregion

    #region 6. ÁP SUẤT KHÍ QUYỂN (ATMOSPHERIC PRESSURE)
    private void BuildPressureDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, bool isVi)
    {
        double press = raw?.Current?.SurfacePressure ?? 1013.25;

        data.Title = isVi ? "Áp Suất Khí Quyển" : "Atmospheric Barometric Pressure";
        data.Subtitle = isVi ? "Áp lực không khí quy chuẩn mực nước biển và xu hướng thời tiết" : "Sea-level air pressure & Synoptic weather trend";
        data.IconGlyph = "\uf14e";
        data.IconColor = "#8B5CF6";
        data.PrimaryValueText = cur.PressureText;
        data.UnitText = "";
        data.ScaleStandardName = isVi ? "Hệ đơn vị Hectopascal (hPa) chuẩn Khí quyển Quốc tế (ISA)" : "Standard International Atmospheric Pressure (ISA / hPa)";
        data.GaugeMinLabel = "985 (Áp thấp/Bão)";
        data.GaugeMidLabel = "1013 (Chuẩn mực biển)";
        data.GaugeMaxLabel = "1035 (Áp cao)";
        data.GaugeGradientStart = "#38BDF8";
        data.GaugeGradientEnd = "#8B5CF6";
        data.GaugePercent = Math.Clamp((press - 985.0) / 50.0, 0.05, 0.95);

        string pressStatus;
        string pColor;
        if (press < 1005) { pressStatus = isVi ? "Áp suất thấp • Dễ có mưa dông" : "Low Pressure • Storm Risk"; pColor = "#EF4444"; }
        else if (press <= 1020) { pressStatus = isVi ? "Áp suất chuẩn • Thời tiết ổn định" : "Normal Pressure • Stable Weather"; pColor = "#10B981"; }
        else { pressStatus = isVi ? "Áp suất cao • Trời quang tạnh ráo" : "High Pressure • Clear & Fair Skies"; pColor = "#8B5CF6"; }

        data.StatusBadgeText = pressStatus;
        data.StatusBadgeColor = pColor;
        data.SecondaryInfoText = isVi
            ? $"Chuẩn mực biển: 1013.25 hPa • Xu hướng: {cur.PressureTrendText}"
            : $"Sea-level standard: 1013.25 hPa • Trend: {cur.PressureTrendText}";

        data.ScientificExplanation = isVi
            ? "Áp suất khí quyển là trọng lượng của cột không khí tác dụng lên một đơn vị diện tích bề mặt. Sự giảm nhanh của áp suất khí quyển (giảm trên 3-4 hPa trong vài giờ) là dấu hiệu kinh điển cho thấy một vùng áp thấp nhiệt đới, dông bão hoặc không khí lạnh đang áp sát."
            : "Atmospheric pressure is the force exerted by the weight of the air column above. A steep barometric drop (falling >3 hPa over a 3-hour period) reliably indicates incoming squall lines, cold fronts, or storm systems.";

        // Smart Advices
        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf0eb",
            IconColor = "#8B5CF6",
            Category = isVi ? "Dự báo thời tiết tức thì" : "Synoptic Barometer Insight",
            Title = press < 1005
                ? (isVi ? "Dấu hiệu vùng khí áp thấp: Đề phòng mưa dông bất ngờ" : "Low Pressure Zone: Prepare for Squalls")
                : (isVi ? "Áp suất ổn định: Thời tiết giữ trạng thái đẹp" : "Stable Pressure: Fair Weather Continuation"),
            Description = press < 1005
                ? (isVi ? "Khí áp thấp kích hoạt luồng không khí thăng hoa bốc hơi, dễ hình thành mây đối lưu dông sét vào buổi chiều." : "Low air pressure fuels ascending moist convection, frequently generating afternoon convective storms.")
                : (isVi ? "Khí áp cao đẩy luồng khí chìm xuống, ức chế mây phát triển, mang lại bầu trời trong xanh và khô ráo." : "High air pressure promotes subsiding air, suppressing cloud formation and yielding dry pleasant conditions."),
            BadgeText = isVi ? "Khí quyển" : "Synoptic",
            BadgeBackground = "#8B5CF6"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf0f8",
            IconColor = "#F59E0B",
            Category = isVi ? "Sức khỏe & Đau nửa đầu" : "Migraine & Sinus Sensitivity",
            Title = isVi ? "Nhạy cảm khí áp & Đau đầu xoang" : "Barometric Headaches & Joint Sensitivity",
            Description = isVi
                ? "Sự dao động áp suất làm thay đổi áp lực nội mô trong các xoang và màng tai. Người có tiền sử đau nửa đầu Migraine nên nghỉ ngơi, uống trà gừng và hạn chế làm việc căng thẳng."
                : "Barometric shifts alter hydrostatic pressure inside sinus cavities. Individuals prone to migraines should stay hydrated and pace workload.",
            BadgeText = isVi ? "Sức khỏe" : "Wellness",
            BadgeBackground = "#F59E0B"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf578",
            IconColor = "#10B981",
            Category = isVi ? "Câu cá & Sinh học" : "Fishing & Wildlife",
            Title = isVi ? "Hoạt động thủy sinh & Thời điểm câu cá" : "Aquatic Activity & Best Fishing Window",
            Description = isVi
                ? "Khí áp ổn định hoặc tăng nhẹ kích thích cá và sinh vật dưới nước nổi lên tầng giữa săn mồi tích cực; ngược lại khi khí áp tụt sâu cá thường chìm xuống đáy bùn."
                : "Stable or slightly rising barometric pressure stimulates active fish feeding behavior, whereas sharp pressure drops cause fish to retreat to deep beds.",
            BadgeText = isVi ? "Dã ngoại" : "Outdoors",
            BadgeBackground = "#10B981"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            // Lấy từ Hourly.SurfacePressure nếu có
            double dayPress = press;
            if (raw?.Hourly?.SurfacePressure != null && raw.Hourly.SurfacePressure.Count >= (idx + 1) * 24)
            {
                var slice = raw.Hourly.SurfacePressure.Skip(idx * 24).Take(24);
                if (slice.Any()) dayPress = Math.Round(slice.Average(), 1);
            }
            else
            {
                dayPress = Math.Round(press + (Math.Sin(idx * 0.9) * 4.2), 1);
            }

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{dayPress:F1} hPa",
                NumericValue = dayPress,
                BarPercent = Math.Clamp((dayPress - 990.0) / 40.0, 0.1, 0.95),
                BarColor = dayPress < 1005 ? "#EF4444" : (dayPress > 1020 ? "#8B5CF6" : "#10B981"),
                StatusBadge = dayPress < 1005 ? (isVi ? "Thấp" : "Low") : (dayPress > 1020 ? (isVi ? "Cao" : "High") : (isVi ? "Chuẩn" : "Normal")),
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Áp suất trung bình {dayPress:F1} hPa" : $"Mean pressure {dayPress:F1} hPa"
            };
        }, isVi);
    }
    #endregion

    #region 7. VÒNG CUNG MẶT TRỜI & MẶT TRĂNG (SUN & MOON ARC)
    private void BuildSunMoonDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, bool isVi)
    {
        string sunrise = cur.SunriseText;
        string sunset = cur.SunsetText;

        // Tính thời lượng ban ngày
        string dayLengthStr = "12h 15m";
        try
        {
            if (DateTime.TryParse(sunrise, out var sr) && DateTime.TryParse(sunset, out var ss))
            {
                var diff = ss - sr;
                if (diff.TotalMinutes > 0)
                {
                    dayLengthStr = $"{(int)diff.TotalHours}h {diff.Minutes}m";
                }
            }
        }
        catch { }

        data.Title = isVi ? "Mặt Trời & Hoàng Hôn (Sun & Moon Tracker)" : "Solar Arc & Sun Tracker";
        data.Subtitle = isVi ? "Chu kỳ nhật quang, bình minh, hoàng hôn và nhịp sinh học" : "Solar photoperiod, golden hours & circadian rhythm";
        data.IconGlyph = "\uf185";
        data.IconColor = "#FACC15";
        data.PrimaryValueText = $"{sunrise} - {sunset}";
        data.UnitText = "";
        data.StatusBadgeText = cur.SunStatusText;
        data.StatusBadgeColor = "#38BDF8";
        data.ScaleStandardName = isVi ? "Hệ tọa độ góc cao Mặt Trời (Solar Elevation & Photoperiod)" : "Solar Elevation Angle & Celestial Photoperiod Standards";
        data.GaugeMinLabel = $"Bình minh ({sunrise})";
        data.GaugeMidLabel = "Chính ngọ (12:00)";
        data.GaugeMaxLabel = $"Hoàng hôn ({sunset})";
        data.GaugeGradientStart = "#F59E0B";
        data.GaugeGradientEnd = "#38BDF8";
        data.GaugePercent = cur.SunProgressPercent;

        data.SecondaryInfoText = isVi
            ? $"Thời lượng ban ngày: {dayLengthStr} • Vị trí hiện tại: {cur.SunStatusText}"
            : $"Daylight duration: {dayLengthStr} • Solar progress: {cur.SunStatusText}";

        data.ScientificExplanation = isVi
            ? "Đường đi của Mặt Trời trên bầu trời quyết định chu kỳ chiếu sáng tự nhiên (Photoperiod) và nhiệt độ bề mặt. Ánh sáng tự nhiên buổi sớm ức chế melatonin và thúc đẩy hormone cortisol giúp cơ thể tỉnh táo, trong khi bóng tối hoàng hôn kích hoạt quá trình tái tạo tế bào và chuẩn bị cho giấc ngủ sâu."
            : "The solar arc governs the natural photoperiod and diurnal temperature variation. Morning natural sunlight suppresses melatonin and stimulates cortisol for alertness, while dusk triggers melatonin synthesis preparing the body for deep rest.";

        // Smart Advices
        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf030",
            IconColor = "#F59E0B",
            Category = isVi ? "Nhiếp ảnh & Khung giờ Vàng" : "Photography & Golden Hour",
            Title = isVi ? "Giờ Vàng (Golden Hour) & Giờ Xanh (Blue Hour)" : "Golden Hour & Blue Hour Windows",
            Description = isVi
                ? $"Khung giờ vàng sáng sớm (ngay sau {sunrise}) và chiều tà (khoảng 1 tiếng trước {sunset}) có ánh sáng ấm áp, bóng đổ mềm mại, là thời điểm đẹp nhất để chụp ảnh chân dung và phong cảnh."
                : $"Morning golden hour (shortly after {sunrise}) and evening twilight (1 hour prior to {sunset}) feature warm directional light ideal for portraiture.",
            BadgeText = isVi ? "Nhiếp ảnh" : "Photo",
            BadgeBackground = "#F59E0B"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf185",
            IconColor = "#10B981",
            Category = isVi ? "Sức khỏe & Nhịp sinh học" : "Circadian Health",
            Title = isVi ? "Tắm nắng buổi sớm tổng hợp Vitamin D" : "Morning Natural Light Immersion",
            Description = isVi
                ? "Dành 15-20 phút tiếp xúc với ánh sáng mặt trời lúc 07:00 - 08:30 giúp tăng cường hấp thu canxi cho xương, cải thiện tâm trạng và đồng bộ nhịp sinh học tự nhiên của não bộ."
                : "Receiving 15-20 minutes of early morning sunlight between 07:00 - 08:30 AM optimizes bone health, enhances mood, and sets the circadian master clock.",
            BadgeText = isVi ? "Sức khỏe" : "Health",
            BadgeBackground = "#10B981"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf0eb",
            IconColor = "#38BDF8",
            Category = isVi ? "Hiệu suất năng lượng" : "Solar Power Yield",
            Title = isVi ? "Đỉnh hiệu suất pin quang điện mặt trời" : "Peak Photovoltaic Power Generation",
            Description = isVi
                ? "Các tấm pin năng lượng mặt trời đạt công suất cực đại trong khoảng 10:30 đến 14:00 khi góc nghiêng tia nắng vuông góc nhất với mặt đất."
                : "Rooftop solar panels reach peak yield capacity between 10:30 AM and 2:00 PM when solar elevation approaches zenith.",
            BadgeText = isVi ? "Năng lượng" : "Solar",
            BadgeBackground = "#0284C7"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            string sr = (raw?.Daily?.Sunrise != null && idx < raw.Daily.Sunrise.Count)
                ? FormatTimeString(raw.Daily.Sunrise[idx])
                : sunrise;

            string ss = (raw?.Daily?.Sunset != null && idx < raw.Daily.Sunset.Count)
                ? FormatTimeString(raw.Daily.Sunset[idx])
                : sunset;

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{sr} - {ss}",
                NumericValue = 12.0,
                BarPercent = 0.5 + (idx * 0.04),
                BarColor = "#FACC15",
                StatusBadge = isVi ? "Nhật quang" : "Daylight",
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"Mọc: {sr} • Lặn: {ss}" : $"Rise: {sr} • Set: {ss}"
            };
        }, isVi);
    }
    #endregion

    #region 8. CHI TIẾT Ô NHIỄM & BỤI MỊN (POLLUTANTS BREAKDOWN)
    private void BuildPollutantsDetail(WeatherMetricDetailPopupData data, CurrentWeatherDisplay cur, OpenMeteoResponse? raw, AirQualityData? aqiData, bool isVi)
    {
        data.Title = isVi ? "Báo Cáo Bụi Mịn & Các Khí Ô Nhiễm" : "Particulates & Gas Pollutants Breakdown";
        data.Subtitle = isVi ? "Nồng độ chi tiết PM2.5, PM10, Ozone (O₃), NO₂, SO₂, CO" : "Detailed concentration of PM2.5, PM10, Ozone, NO2, SO2, CO";
        data.IconGlyph = "\uf132";
        data.IconColor = "#10B981";
        data.PrimaryValueText = cur.Pm25Text;
        data.UnitText = "";
        data.StatusBadgeText = cur.Pm25Status;
        data.StatusBadgeColor = cur.AqiColor;
        data.ScaleStandardName = isVi ? "Ngưỡng hướng dẫn chất lượng không khí toàn cầu WHO 2021" : "WHO 2021 Global Air Quality Guidelines";
        data.GaugeMinLabel = "0 µg/m³ (WHO)";
        data.GaugeMidLabel = "35 µg/m³ (Bình thường)";
        data.GaugeMaxLabel = "100+ µg/m³ (Nguy hại)";
        data.GaugeGradientStart = "#10B981";
        data.GaugeGradientEnd = "#EF4444";

        double pm25Val = aqiData?.Pm25 ?? 15.0;
        data.GaugePercent = Math.Clamp(pm25Val / 100.0, 0.05, 1.0);

        data.SecondaryInfoText = isVi
            ? $"PM10: {cur.Pm10Text} • Ozone: {cur.OzoneText} • NO₂: {cur.No2Text} • SO₂: {cur.So2Text}"
            : $"PM10: {cur.Pm10Text} • Ozone: {cur.OzoneText} • NO2: {cur.No2Text} • SO2: {cur.So2Text}";

        data.ScientificExplanation = isVi
            ? "Bụi mịn PM2.5 là những hạt chất rắn hoặc lỏng lơ lửng có đường kính nhỏ hơn 2.5 µm (bằng 1/30 sợi tóc). Khí NO2 sinh ra từ khí xả xe cộ và nhà máy nhiệt điện; Ozone tầng mặt đất (O3) là chất oxy hóa mạnh hình thành từ phản ứng quang hóa dưới ánh nắng gắt."
            : "PM2.5 particles are atmospheric aerosols under 2.5 µm in diameter (1/30th the width of a human hair). Nitrogen dioxide (NO2) stems from automotive fuel combustion; ground-level ozone (O3) forms via photochemical reactions in bright sunshine.";

        // Smart Advices
        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf6cf",
            IconColor = pm25Val > 35 ? "#EF4444" : "#10B981",
            Category = isVi ? "Chăm sóc đường thở" : "Airway Protection",
            Title = isVi ? "Bảo vệ phế nang & Thanh quản trước bụi mịn" : "Defend Alveoli & Airway Tissues",
            Description = isVi
                ? "Bụi PM2.5 dễ lắng đọng tại cuống phổi gây kích ứng ho khan. Người hay di chuyển ngoài đường nên dùng khẩu trang có màng lọc than hoạt tính hoặc N95."
                : "PM2.5 settles deep inside lung tissues causing coughs and wheezing. Regular commuters should wear activated carbon or N95 masks.",
            BadgeText = isVi ? "Khẩu trang" : "Respirator",
            BadgeBackground = pm25Val > 35 ? "#EF4444" : "#10B981"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf1bb",
            IconColor = "#10B981",
            Category = isVi ? "Thanh lọc không gian sống" : "Indoor Botanical Purification",
            Title = isVi ? "Cây xanh thanh lọc khí ô nhiễm trong nhà" : "Indoor Air-Purifying Houseplants",
            Description = isVi
                ? "Các loài cây như Lưỡi Hổ, Lan Ý, Trầu Bà và Kim Phát Tài có khả năng hấp thụ khí CO2, Formaldehyde và phát tán khí oxy ion âm tươi mát vào ban đêm."
                : "Plants like Snake Plant, Peace Lily, and Golden Pothos absorb indoor VOCs and generate fresh negative ions.",
            BadgeText = isVi ? "Cây xanh" : "Greenery",
            BadgeBackground = "#10B981"
        });

        data.Advices.Add(new MetricAdviceItem
        {
            IconGlyph = "\uf017",
            IconColor = "#38BDF8",
            Category = isVi ? "Khung giờ lưu thông" : "Rush Hour Dispersion",
            Title = isVi ? "Tránh tập thể dục cạnh mặt đường giờ cao điểm" : "Avoid Exercise Near Major Traffic Corridors",
            Description = isVi
                ? "Nồng độ khí NO2 và khói xe đạt đỉnh lúc 07:00 - 08:30 và 17:00 - 18:30. Nên chọn công viên nhiều cây xanh cách xa mặt đường chính để đi bộ hoặc chạy bộ."
                : "NO2 and exhaust levels peak during morning and evening rush hours. Choose interior park grounds sheltered by tree canopies for cardio exercises.",
            BadgeText = isVi ? "Lưu ý" : "Corridors",
            BadgeBackground = "#0284C7"
        });

        // 7-Day Trend
        Populate7DayTrend(data, raw, (idx, dayName, dateStr, weatherCode) =>
        {
            int rainP = (raw?.Daily?.PrecipitationProbabilityMax != null && idx < raw.Daily.PrecipitationProbabilityMax.Count)
                ? raw.Daily.PrecipitationProbabilityMax[idx]
                : 20;

            double dayPm25 = Math.Clamp(pm25Val * (rainP > 50 ? 0.68 : (rainP > 25 ? 0.85 : 1.08)) + (idx * 1.5 - 2), 5.0, 110.0);
            string pmStatus = WeatherCodeHelper.GetPm25Status(dayPm25);

            return new MetricDailyTrendItem
            {
                DayName = dayName,
                DateDisplay = dateStr,
                ValueText = $"{dayPm25:F1} µg/m³",
                NumericValue = dayPm25,
                BarPercent = Math.Clamp(dayPm25 / 75.0, 0.08, 1.0),
                BarColor = dayPm25 > 55 ? "#EF4444" : (dayPm25 > 35 ? "#F59E0B" : "#10B981"),
                StatusBadge = pmStatus,
                IconGlyph = WeatherCodeHelper.GetConditionInfo(weatherCode, true).iconGlyph,
                ConditionText = WeatherCodeHelper.GetConditionInfo(weatherCode, true).description,
                ExtraInfo = isVi ? $"PM2.5 dự kiến: {dayPm25:F1} µg/m³" : $"Forecast PM2.5: {dayPm25:F1} µg/m³"
            };
        }, isVi);
    }
    #endregion

    #region Helper Methods
    private void Populate7DayTrend(
        WeatherMetricDetailPopupData data,
        OpenMeteoResponse? raw,
        Func<int, string, string, int, MetricDailyTrendItem> itemFactory,
        bool isVi)
    {
        int count = 7;
        if (raw?.Daily?.Time != null && raw.Daily.Time.Count > 0)
        {
            count = Math.Min(7, raw.Daily.Time.Count);
        }

        DateTime today = DateTime.Today;

        for (int i = 0; i < count; i++)
        {
            DateTime dayDate = today.AddDays(i);
            string dayName;
            if (i == 0)
            {
                dayName = isVi ? "Hôm nay" : "Today";
            }
            else if (i == 1)
            {
                dayName = isVi ? "Ngày mai" : "Tomorrow";
            }
            else
            {
                dayName = isVi
                    ? dayDate.ToString("ddd", new CultureInfo("vi-VN"))
                    : dayDate.ToString("ddd", CultureInfo.InvariantCulture);
            }

            string dateStr = dayDate.ToString("dd/MM");
            int weatherCode = (raw?.Daily?.WeatherCode != null && i < raw.Daily.WeatherCode.Count)
                ? raw.Daily.WeatherCode[i]
                : 0;

            var item = itemFactory(i, dayName, dateStr, weatherCode);
            data.DailyTrends.Add(item);
        }
    }

    private static string FormatTimeString(string isoTime)
    {
        if (DateTime.TryParse(isoTime, out var dt))
        {
            return dt.ToString("HH:mm");
        }
        return isoTime;
    }
    #endregion
}
