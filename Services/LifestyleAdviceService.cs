using System;
using System.Collections.Generic;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ phân tích dữ liệu khí tượng nâng cao để tính toán các chỉ số sinh hoạt đời sống thiết thực,
/// khung giờ vàng thể thao & vận động, và trung tâm bảo vệ da & hô hấp. Hỗ trợ song ngữ Tiếng Việt & English.
/// </summary>
public class LifestyleAdviceService
{
    public List<LifestyleIndexItem> GenerateLifestyleIndices(CurrentWeatherDisplay current, OpenMeteoResponse? rawData)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        var result = new List<LifestyleIndexItem>();

        double temp = current.TemperatureValue;
        int aqi = current.AqiValue;
        bool isRaining = current.WeatherEffect is WeatherEffectType.LightRain
            or WeatherEffectType.ModerateRain
            or WeatherEffectType.HeavyRain
            or WeatherEffectType.Thunderstorm;

        // Parse UV & Độ ẩm
        double.TryParse(current.UvIndexText, out double uv);
        int humidity = 60;
        if (!string.IsNullOrEmpty(current.HumidityText))
        {
            int.TryParse(current.HumidityText.Replace("%", "").Trim(), out humidity);
        }

        // ==================== 1. THỂ THAO & CHẠY BỘ ====================
        string workoutRating;
        string workoutColor;
        string workoutAdvice;
        double workoutScore;
        string workoutBadge;

        if (isRaining)
        {
            workoutRating = isVi ? "Nên tập trong nhà" : "Indoor Workout Only";
            workoutColor = "#EF4444";
            workoutScore = 25;
            workoutBadge = isVi ? "Mưa ngoài trời" : "Rainy Outdoor";
            workoutAdvice = isVi
                ? "Thời tiết mưa ẩm hoặc dông sét. Hãy tập yoga, gym hoặc nhảy dây trong nhà để giữ ấm và an toàn."
                : "Wet roads or thunderstorm risk. Choose indoor gym, yoga or skipping for safety and comfort.";
        }
        else if (uv >= 8.0 && current.IsDay)
        {
            workoutRating = isVi ? "Hạn chế do nắng gắt" : "Avoid Peak Sun";
            workoutColor = "#F59E0B";
            workoutScore = 45;
            workoutBadge = $"UV {uv:F1}";
            workoutAdvice = isVi
                ? "Chỉ số UV và bức xạ nhiệt cao. Nếu muốn tập ngoài trời, nên dời lịch sang sau 17h30 chiều mát mẻ."
                : "High UV and heat radiation. Move outdoor cardio to after 17:30 to avoid heat exhaustion.";
        }
        else if (aqi > 150)
        {
            workoutRating = isVi ? "Chất lượng khí xấu" : "Poor Air Quality";
            workoutColor = "#F97316";
            workoutScore = 40;
            workoutBadge = $"AQI {aqi}";
            workoutAdvice = isVi
                ? "Nồng độ bụi mịn PM2.5 cao có thể kích ứng đường thở. Nên chạy bộ trên máy trong phòng kín có lọc khí."
                : "Elevated PM2.5 can cause respiratory irritation. Exercise in indoor filtered environments.";
        }
        else if (temp >= 19 && temp <= 29)
        {
            workoutRating = isVi ? "Rất lý tưởng (9.5/10)" : "Ideal Conditions (9.5/10)";
            workoutColor = "#10B981";
            workoutScore = 95;
            workoutBadge = isVi ? "Ôn hòa mát mẻ" : "Pleasant";
            workoutAdvice = isVi
                ? "Nhiệt độ cực kỳ dễ chịu và thoáng đãng. Rất thích hợp cho chạy bộ đường dài, đạp xe và bóng đá ngoài trời."
                : "Pleasant temperatures and fresh air. Perfect for running, cycling and outdoor sports.";
        }
        else
        {
            workoutRating = isVi ? "Khá thuận lợi" : "Favorable";
            workoutColor = "#3B82F6";
            workoutScore = 75;
            workoutBadge = $"{temp:F0}°C";
            workoutAdvice = isVi
                ? "Điều kiện thời tiết ổn định. Hãy khởi động kỹ khớp chân và uống đủ nước trước khi tập."
                : "Stable weather conditions. Warm up thoroughly and stay hydrated during exercise.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "THỂ THAO & VẬN ĐỘNG" : "SPORTS & FITNESS",
            Title = isVi ? "Chạy Bộ & Hoạt Động Ngoài Trời" : "Running & Outdoor Fitness",
            Rating = workoutRating,
            RatingColor = workoutColor,
            ScoreProgress = workoutScore,
            MetricBadge = workoutBadge,
            Advice = workoutAdvice,
            IconGlyph = "\uf44b"
        });

        // ==================== 2. GIẶT & PHƠI QUẦN ÁO ====================
        string dryingRating;
        string dryingColor;
        string dryingAdvice;
        double dryingScore;
        string dryingBadge;

        if (isRaining)
        {
            dryingRating = isVi ? "Không phơi ngoài trời" : "Do Not Dry Outdoors";
            dryingColor = "#EF4444";
            dryingScore = 20;
            dryingBadge = isVi ? "Mưa ẩm ướt" : "High Rain Risk";
            dryingAdvice = isVi
                ? "Không khí ẩm ướt sắp có mưa. Phơi quần áo trong phòng có quạt thông gió hoặc dùng máy sấy nhiệt."
                : "Humid air and rain incoming. Dry clothes indoors with a fan or tumble dry.";
        }
        else if (humidity < 60 && current.IsDay)
        {
            dryingRating = isVi ? "Khô siêu nhanh (< 2.5h)" : "Fast Drying (< 2.5h)";
            dryingColor = "#10B981";
            dryingScore = 98;
            dryingBadge = isVi ? $"Độ ẩm {humidity}%" : $"Humidity {humidity}%";
            dryingAdvice = isVi
                ? "Nắng ráo, độ ẩm thấp và gió mát giúp đồ giặt khô kiệt, thơm tho chỉ sau 2-3 tiếng phơi."
                : "Low humidity and sunlight allow laundry to dry completely within 2-3 hours.";
        }
        else if (humidity >= 85)
        {
            dryingRating = isVi ? "Lâu khô • Cần thoáng gió" : "Slow Dry • Needs Airflow";
            dryingColor = "#F59E0B";
            dryingScore = 50;
            dryingBadge = isVi ? $"Nồm ẩm {humidity}%" : $"Humid {humidity}%";
            dryingAdvice = isVi
                ? "Không khí nồm ẩm cao khiến đồ lâu khô. Nên giãn cách móc phơi và dùng quạt hỗ trợ lưu thông khí."
                : "Heavy humidity slows drying. Space garments apart and use fan circulation.";
        }
        else
        {
            dryingRating = isVi ? "Phơi thuận lợi" : "Favorable Drying";
            dryingColor = "#3B82F6";
            dryingScore = 80;
            dryingBadge = isVi ? $"Độ ẩm {humidity}%" : $"Humidity {humidity}%";
            dryingAdvice = isVi
                ? "Thời tiết tạnh ráo, thuận lợi phơi chăn màn và quần áo thường ngày trong buổi sáng."
                : "Dry and breezy weather, suitable for drying clothes outdoors during daytime.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "SINH HOẠT HÀNG NGÀY" : "DAILY LIVING",
            Title = isVi ? "Giặt & Phơi Quần Áo" : "Laundry & Clothes Drying",
            Rating = dryingRating,
            RatingColor = dryingColor,
            ScoreProgress = dryingScore,
            MetricBadge = dryingBadge,
            Advice = dryingAdvice,
            IconGlyph = "\uf553"
        });

        // ==================== 3. RỬA XE ====================
        string carWashRating;
        string carWashColor;
        string carWashAdvice;
        double carWashScore;
        string carWashBadge;

        bool willRainSoon = false;
        if (rawData?.Daily?.PrecipitationProbabilityMax != null && rawData.Daily.PrecipitationProbabilityMax.Count > 1)
        {
            int rainTomorrow = rawData.Daily.PrecipitationProbabilityMax[1];
            int rainDayAfter = rawData.Daily.PrecipitationProbabilityMax.Count > 2 ? rawData.Daily.PrecipitationProbabilityMax[2] : 0;
            if (rainTomorrow >= 45 || rainDayAfter >= 55)
            {
                willRainSoon = true;
            }
        }

        if (isRaining || willRainSoon)
        {
            carWashRating = isVi ? "Nên hoãn rửa xe" : "Postpone Car Wash";
            carWashColor = "#EF4444";
            carWashScore = 20;
            carWashBadge = isVi ? "Sắp có mưa" : "Rain Expected";
            carWashAdvice = isVi
                ? "Dự báo khu vực sắp có mưa trong 24-48 giờ tới. Rửa xe lúc này sẽ nhanh chóng bị bám bùn bẩn lại."
                : "Rain predicted in the next 24-48 hours. Freshly washed vehicles will quickly get dirty.";
        }
        else
        {
            carWashRating = isVi ? "Rất thích hợp rửa xe" : "Great for Car Wash";
            carWashColor = "#10B981";
            carWashScore = 92;
            carWashBadge = isVi ? "Khô ráo 2-3 ngày" : "Dry 2-3 Days";
            carWashAdvice = isVi
                ? "Bầu trời tạnh ráo trong 2-3 ngày tới. Rất thích hợp để rửa xe và đánh bóng, xe sẽ giữ sạch được lâu."
                : "Clear skies forecasted for 2-3 days. Ideal time to wash and wax your vehicle.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "BẢO DƯỠNG PHƯƠNG TIỆN" : "VEHICLE CARE",
            Title = isVi ? "Rửa Xe Ô Tô & Xe Máy" : "Car & Motorcycle Wash",
            Rating = carWashRating,
            RatingColor = carWashColor,
            ScoreProgress = carWashScore,
            MetricBadge = carWashBadge,
            Advice = carWashAdvice,
            IconGlyph = "\uf1b9"
        });

        // ==================== 4. SỨC KHỎE & HÔ HẤP ====================
        string healthRating;
        string healthColor;
        string healthAdvice;
        double healthScore;
        string healthBadge;

        if (humidity >= 85)
        {
            healthRating = isVi ? "Nguy cơ muỗi & nấm mốc" : "Mosquito & Mold Alert";
            healthColor = "#F59E0B";
            healthScore = 55;
            healthBadge = isVi ? $"Ẩm {humidity}%" : $"Humidity {humidity}%";
            healthAdvice = isVi
                ? "Độ ẩm cao tạo môi trường cho muỗi và nấm mốc phát triển. Hãy mắc màn khi ngủ và lau khô góc nhà ẩm ướt."
                : "High humidity fosters mold and mosquitoes. Sleep under nets and keep moist corners ventilated.";
        }
        else if (temp >= 34)
        {
            healthRating = isVi ? "Cảnh báo sốc nhiệt" : "Heatstroke Warning";
            healthColor = "#EF4444";
            healthScore = 35;
            healthBadge = $"{temp:F0}°C";
            healthAdvice = isVi
                ? "Nhiệt độ ngoài trời rất oi ả. Cần bù nước liên tục, mang nón rộng vành và tránh thay đổi nhiệt độ đột ngột."
                : "Very hot outdoors. Drink plenty of water and avoid abrupt temperature changes from AC rooms.";
        }
        else if (temp <= 16)
        {
            healthRating = isVi ? "Giữ ấm cổ & phổi" : "Protect Chest & Throat";
            healthColor = "#3B82F6";
            healthScore = 60;
            healthBadge = $"{temp:F0}°C";
            healthAdvice = isVi
                ? "Thời tiết lạnh buốt dễ gây viêm mũi họng. Hãy quàng khăn ấm, uống nước ấm và mang khẩu trang khi ra ngoài."
                : "Chilly weather may trigger respiratory irritation. Wear scarves, drink warm tea and bundle up.";
        }
        else
        {
            healthRating = isVi ? "Sức khỏe tối ưu" : "Optimal Wellness";
            healthColor = "#10B981";
            healthScore = 95;
            healthBadge = isVi ? "Thời tiết êm" : "Comfortable";
            healthAdvice = isVi
                ? "Thời tiết ôn hòa dễ chịu. Thích hợp mở cửa đón gió tự nhiên vào nhà để thanh lọc không khí."
                : "Pleasant mild weather. Open windows to let fresh outdoor air ventilate living spaces.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "CHĂM SÓC SỨC KHỎE" : "HEALTH & WELLNESS",
            Title = isVi ? "Sức Khỏe & Phòng Ngừa Côn Trùng" : "Health & Insect Protection",
            Rating = healthRating,
            RatingColor = healthColor,
            ScoreProgress = healthScore,
            MetricBadge = healthBadge,
            Advice = healthAdvice,
            IconGlyph = "\uf21e"
        });

        // ==================== 5. SỐC NHIỆT & BIÊN ĐỘ NHIỆT NGÀY ĐÊM ====================
        double maxTemp = temp;
        double minTemp = temp;
        if (rawData?.Daily?.TemperatureMax != null && rawData.Daily.TemperatureMax.Count > 0 &&
            rawData?.Daily?.TemperatureMin != null && rawData.Daily.TemperatureMin.Count > 0)
        {
            maxTemp = rawData.Daily.TemperatureMax[0];
            minTemp = rawData.Daily.TemperatureMin[0];
        }
        double deltaTemp = Math.Round(Math.Max(0, maxTemp - minTemp), 1);

        string tempDeltaRating;
        string tempDeltaColor;
        string tempDeltaAdvice;
        double tempDeltaScore;

        if (deltaTemp >= 10.0)
        {
            tempDeltaRating = isVi ? $"Biên độ lớn (Δ{deltaTemp}°C)" : $"High Swing (Δ{deltaTemp}°C)";
            tempDeltaColor = "#EF4444";
            tempDeltaScore = 30;
            tempDeltaAdvice = isVi
                ? $"Ngày và đêm chênh lệch tới {deltaTemp}°C! Trưa oi bức nhưng sáng và đêm lạnh sâu, hãy mang áo khoác nhẹ phòng cảm sốt."
                : $"Day and night differ by {deltaTemp}°C. Chilly mornings and warm afternoons; layer clothes to prevent cold symptoms.";
        }
        else if (deltaTemp >= 7.0)
        {
            tempDeltaRating = isVi ? $"Chênh lệch vừa (Δ{deltaTemp}°C)" : $"Moderate Swing (Δ{deltaTemp}°C)";
            tempDeltaColor = "#F59E0B";
            tempDeltaScore = 65;
            tempDeltaAdvice = isVi
                ? $"Biên độ nhiệt dao động {deltaTemp}°C. Nên giữ ấm phần cổ và ngực khi về khuya hoặc đi sáng sớm."
                : $"Temperature shifts by {deltaTemp}°C. Keep a light jacket handy when returning home late.";
        }
        else
        {
            tempDeltaRating = isVi ? $"Ổn định (Δ{deltaTemp}°C)" : $"Stable Temp (Δ{deltaTemp}°C)";
            tempDeltaColor = "#10B981";
            tempDeltaScore = 95;
            tempDeltaAdvice = isVi
                ? $"Nhiệt độ ngày và đêm ổn định (dao động {deltaTemp}°C), cơ thể dễ thích nghi, ít nguy cơ sốc nhiệt sinh học."
                : $"Consistent temperatures day and night (Δ{deltaTemp}°C), minimal thermal stress on the body.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "CẢNH BÁO NHIỆT ĐỘ" : "THERMAL SWING",
            Title = isVi ? "Sốc Nhiệt & Biên Độ Ngày Đêm" : "Thermal Swing & Heatstroke Risk",
            Rating = tempDeltaRating,
            RatingColor = tempDeltaColor,
            ScoreProgress = tempDeltaScore,
            MetricBadge = $"Δ{deltaTemp:F1}°C",
            Advice = tempDeltaAdvice,
            IconGlyph = "\uf2c7"
        });

        // ==================== 6. CHĂM SÓC DA & TIA UV ====================
        string uvRating;
        string uvColor;
        string uvAdvice;
        double uvScore;

        if (uv >= 8.0)
        {
            uvRating = isVi ? "Bức xạ UV cực cao" : "Extreme UV Radiation";
            uvColor = "#EF4444";
            uvScore = 20;
            uvAdvice = isVi
                ? "Tia UV gây bỏng rát da trong vòng 15-20 phút. Thoa kem chống nắng SPF 50+, đeo kính râm và mặc áo chống nắng UPF 50+."
                : "UV causes sunburn within 15-20 minutes. Apply SPF 50+ sunscreen, wear UV400 sunglasses and UPF clothing.";
        }
        else if (uv >= 4.0)
        {
            uvRating = isVi ? "Bức xạ trung bình" : "Moderate UV";
            uvColor = "#F59E0B";
            uvScore = 60;
            uvAdvice = isVi
                ? "Bức xạ mặt trời ở mức vừa phải. Nên thoa kem chống nắng khi hoạt động ngoài trời quá 30 phút."
                : "Moderate solar radiation. Sunscreen recommended if spending more than 30 minutes outside.";
        }
        else
        {
            uvRating = isVi ? "An toàn cho da" : "Skin Safe";
            uvColor = "#10B981";
            uvScore = 95;
            uvAdvice = isVi
                ? "Chỉ số UV thấp, ít nguy cơ tổn thương tế bào da. Có thể phơi nắng sáng sớm để hấp thụ vitamin D."
                : "Low UV exposure, safe for skin. Great for absorbing morning vitamin D.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = isVi ? "CHĂM SÓC DA & NẮNG" : "SKINCARE & UV DEFENSE",
            Title = isVi ? "Bảo Vệ Làn Da & Tia Cực Tím" : "Skin Defense & UV Protection",
            Rating = uvRating,
            RatingColor = uvColor,
            ScoreProgress = uvScore,
            MetricBadge = $"UV {uv:F1}",
            Advice = uvAdvice,
            IconGlyph = "\uf185"
        });

        return result;
    }

    public List<WorkoutWindowItem> GenerateWorkoutWindows(CurrentWeatherDisplay current, OpenMeteoResponse? rawData)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        var list = new List<WorkoutWindowItem>();

        double temp = current.TemperatureValue;
        bool isRaining = current.WeatherEffect is WeatherEffectType.LightRain
            or WeatherEffectType.ModerateRain
            or WeatherEffectType.HeavyRain
            or WeatherEffectType.Thunderstorm;

        // 1. Khung giờ sáng sớm (05:30 - 08:30)
        string morningStatus = !isRaining && temp < 30 ? (isVi ? "Rất lý tưởng (Giờ vàng)" : "Optimal (Golden Hour)") : (isVi ? "Bình thường" : "Fair");
        string morningColor = !isRaining && temp < 30 ? "#10B981" : "#F59E0B";
        list.Add(new WorkoutWindowItem
        {
            TimeRange = "05:30 - 08:30",
            Label = isVi ? "Sáng Sớm • Giờ Vàng" : "Early Morning • Golden Hour",
            Temperature = $"{Math.Max(18, Math.Round(temp - 3)):F0}°C",
            Condition = isVi ? "Không khí trong lành, ít xe" : "Fresh air, low traffic",
            Status = morningStatus,
            StatusColor = morningColor,
            IconGlyph = "\uf185",
            Advice = isVi ? "Thời điểm tốt nhất trong ngày cho chạy bộ, đạp xe và tập thể dục nhịp điệu." : "Best timeframe today for jogging, cycling and outdoor cardio."
        });

        // 2. Buổi trưa / đầu chiều (11:30 - 14:00)
        list.Add(new WorkoutWindowItem
        {
            TimeRange = "11:30 - 14:00",
            Label = isVi ? "Buổi Trưa • Nắng Đỉnh Điểm" : "Midday • Peak Sun",
            Temperature = $"{Math.Round(temp + 2):F0}°C",
            Condition = isVi ? "Bức xạ UV & nhiệt độ cao" : "High UV & heat radiation",
            Status = isVi ? "Không nên tập ngoài trời" : "Avoid Outdoors",
            StatusColor = "#EF4444",
            IconGlyph = "\uf00d",
            Advice = isVi ? "Dễ bị sốc nhiệt và kiệt sức. Hãy tập gym phòng lạnh hoặc nghỉ ngơi phục hồi." : "High risk of heat exhaustion. Prefer air-conditioned gym or rest."
        });

        // 3. Chiều hoàng hôn (17:00 - 19:30)
        string eveningStatus = !isRaining ? (isVi ? "Rất thích hợp" : "Highly Recommended") : (isVi ? "Có mưa rào" : "Rain Risk");
        string eveningColor = !isRaining ? "#10B981" : "#EF4444";
        list.Add(new WorkoutWindowItem
        {
            TimeRange = "17:00 - 19:30",
            Label = isVi ? "Chiều Mát • Giờ Tan Tầm" : "Late Afternoon • Sunset",
            Temperature = $"{Math.Round(temp - 1):F0}°C",
            Condition = isVi ? "Dịu nắng, gió mát lộng" : "Mild sun, cooling breeze",
            Status = eveningStatus,
            StatusColor = eveningColor,
            IconGlyph = "\uf186",
            Advice = isVi ? "Bầu trời mát dịu sau giờ làm việc. Thích hợp đi dạo bộ, chạy nhẹ hoặc cầu lông." : "Cool twilight breeze after work. Ideal for light running and casual sports."
        });

        // 4. Buổi tối (20:00 - 22:30)
        list.Add(new WorkoutWindowItem
        {
            TimeRange = "20:00 - 22:30",
            Label = isVi ? "Buổi Tối • Thư Giãn" : "Evening • Recovery",
            Temperature = $"{Math.Round(temp - 2):F0}°C",
            Condition = isVi ? "Nhiệt độ ổn định, tĩnh lặng" : "Cool, calm atmosphere",
            Status = isVi ? "Thích hợp dạo bộ / Gym" : "Suitable for Gym / Walk",
            StatusColor = "#3B82F6",
            IconGlyph = "\uf0eb",
            Advice = isVi ? "Thích hợp đi bộ thư giãn tiêu hóa hoặc tập gym trong nhà trước khi ngủ 2 tiếng." : "Great for a post-dinner digestive walk or indoor fitness 2 hours before bed."
        });

        return list;
    }

    public SkinDefenseModel GenerateSkinDefenseAdvice(CurrentWeatherDisplay current)
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        double.TryParse(current.UvIndexText, out double uv);
        int aqi = current.AqiValue;

        var model = new SkinDefenseModel
        {
            UvIndex = uv
        };

        // UV Level & Sunscreen SPF
        if (uv >= 8.0)
        {
            model.UvLevelText = isVi ? "Rất cao • Cực kỳ nguy hại" : "Very High • Severe Exposure";
            model.UvColor = "#EF4444";
            model.RecommendedSpf = "SPF 50+ / PA++++";
            model.MaxSafeSunTime = isVi ? "Dưới 15 phút" : "Under 15 mins";
            model.SkincareAdvice = isVi
                ? "Bắt buộc thoa kem chống nắng phổ rộng trước khi ra ngoài 20 phút. Thoa lại mỗi 2 giờ nếu đổ mồ hôi. Đeo kính râm UV400 và mũ nón rộng vành."
                : "Broad-spectrum sunscreen mandatory 20 mins before heading out. Reapply every 2 hours. Wear UV400 sunglasses and wide-brimmed hats.";
        }
        else if (uv >= 4.0)
        {
            model.UvLevelText = isVi ? "Trung bình • Cần che chắn" : "Moderate • Protection Needed";
            model.UvColor = "#F59E0B";
            model.RecommendedSpf = "SPF 30 - 50, PA+++";
            model.MaxSafeSunTime = isVi ? "30 - 45 phút" : "30 - 45 mins";
            model.SkincareAdvice = isVi
                ? "Thoa kem chống nắng cho mặt và cổ. Mang kính mát cản chói khi lái xe vào buổi trưa."
                : "Apply sunscreen to face and neck. Wear sunglasses to protect against midday glare.";
        }
        else
        {
            model.UvLevelText = isVi ? "Thấp • An toàn cho da" : "Low • Safe Exposure";
            model.UvColor = "#10B981";
            model.RecommendedSpf = "SPF 15 - 30";
            model.MaxSafeSunTime = isVi ? "Trên 60 phút" : "Over 60 mins";
            model.SkincareAdvice = isVi
                ? "Tia cực tím yếu, ít gây hại. Có thể tận hưởng ánh nắng sáng để cơ thể tổng hợp vitamin D tự nhiên."
                : "Weak UV rays, skin safe. Enjoy natural morning sunlight for natural vitamin D synthesis.";
        }

        // Mask Recommendation
        if (aqi > 150)
        {
            model.MaskRecommendation = isVi ? "Bắt buộc khẩu trang N95 / KF94 lọc bụi PM2.5" : "N95 / KF94 Respirator Mask Required";
            model.MaskColor = "#EF4444";
            model.MaskIcon = "\uf6cf";
        }
        else if (aqi > 100)
        {
            model.MaskRecommendation = isVi ? "Khẩu trang y tế kháng khuẩn 4 lớp" : "4-Ply Medical Face Mask";
            model.MaskColor = "#F97316";
            model.MaskIcon = "\uf6cf";
        }
        else
        {
            model.MaskRecommendation = isVi ? "Khẩu trang vải / y tế thông thường" : "Standard Cloth / Medical Mask";
            model.MaskColor = "#10B981";
            model.MaskIcon = "\uf6cf";
        }

        return model;
    }
}
