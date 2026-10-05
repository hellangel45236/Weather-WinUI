using System;
using System.Collections.Generic;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ phân tích dữ liệu khí tượng để tính toán các chỉ số sinh hoạt đời sống thiết thực:
/// 1. Thể thao & Chạy bộ
/// 2. Giặt & Phơi quần áo
/// 3. Rửa xe
/// 4. Sức khỏe & Cảnh báo côn trùng / thời tiết
/// </summary>
public class LifestyleAdviceService
{
    public List<LifestyleIndexItem> GenerateLifestyleIndices(CurrentWeatherDisplay current, OpenMeteoResponse? rawData)
    {
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

        if (isRaining)
        {
            workoutRating = "Nên tập trong nhà";
            workoutColor = "#EF4444";
            workoutAdvice = "Đang có mưa hoặc dông sét ngoài trời. Hãy tập yoga, nhảy dây hoặc gym trong phòng để đảm bảo an toàn.";
        }
        else if (uv >= 8.0)
        {
            workoutRating = "Hạn chế do nắng gắt";
            workoutColor = "#F59E0B";
            workoutAdvice = "Chỉ số UV cao và nhiệt độ oi bức. Nếu muốn chạy bộ, hãy dời sang sau 17h30 chiều mát mẻ.";
        }
        else if (aqi > 150)
        {
            workoutRating = "Chất lượng khí xấu";
            workoutColor = "#F97316";
            workoutAdvice = "Chỉ số bụi mịn AQI cao có thể ảnh hưởng đường thở. Nên chạy bộ trên máy trong phòng kín lọc khí.";
        }
        else if (temp >= 19 && temp <= 29)
        {
            workoutRating = "Rất lý tưởng (9/10)";
            workoutColor = "#10B981";
            workoutAdvice = "Nhiệt độ dễ chịu và không khí thoáng đãng. Rất thích hợp cho chạy bộ, đạp xe và các môn ngoài trời.";
        }
        else
        {
            workoutRating = "Khá thuận lợi";
            workoutColor = "#3B82F6";
            workoutAdvice = "Điều kiện thời tiết ổn định. Uống đủ nước trước khi khởi động để duy trì thể lực dẻo dai.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = "THỂ THAO & VẬN ĐỘNG",
            Title = "Chạy Bộ & Hoạt Động Ngoài Trời",
            Rating = workoutRating,
            RatingColor = workoutColor,
            Advice = workoutAdvice,
            IconGlyph = "\uf44b" // running icon
        });

        // ==================== 2. GIẶT & PHƠI QUẦN ÁO ====================
        string dryingRating;
        string dryingColor;
        string dryingAdvice;

        if (isRaining)
        {
            dryingRating = "Không nên phơi ngoài trời";
            dryingColor = "#EF4444";
            dryingAdvice = "Độ ẩm cao và sắp có mưa. Phơi quần áo trong nhà có quạt hoặc sử dụng chế độ sấy máy giặt.";
        }
        else if (humidity < 60 && current.IsDay)
        {
            dryingRating = "Khô siêu nhanh (< 3h)";
            dryingColor = "#10B981";
            dryingAdvice = "Nắng tốt, độ ẩm thấp và gió mát giúp áo quần khô kiệt, thơm tho chỉ sau 2-3 tiếng phơi.";
        }
        else if (humidity >= 85)
        {
            dryingRating = "Phơi nơi thoáng gió";
            dryingColor = "#F59E0B";
            dryingAdvice = "Không khí nồm ẩm cao khiến đồ lâu khô. Nên giãn cách móc phơi và bật quạt hỗ trợ.";
        }
        else
        {
            dryingRating = "Phơi thuận lợi";
            dryingColor = "#3B82F6";
            dryingAdvice = "Thời tiết tạnh ráo, thích hợp giặt chăn màn và áo quần hàng ngày trong buổi sáng.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = "SINH HOẠT HÀNG NGÀY",
            Title = "Giặt & Phơi Áo Quần",
            Rating = dryingRating,
            RatingColor = dryingColor,
            Advice = dryingAdvice,
            IconGlyph = "\uf553" // t-shirt / hanger icon
        });

        // ==================== 3. RỬA XE ====================
        string carWashRating;
        string carWashColor;
        string carWashAdvice;

        bool willRainSoon = false;
        if (rawData?.Daily?.PrecipitationProbabilityMax != null && rawData.Daily.PrecipitationProbabilityMax.Count > 1)
        {
            int rainTomorrow = rawData.Daily.PrecipitationProbabilityMax[1];
            int rainDayAfter = rawData.Daily.PrecipitationProbabilityMax.Count > 2 ? rawData.Daily.PrecipitationProbabilityMax[2] : 0;
            if (rainTomorrow >= 50 || rainDayAfter >= 60)
            {
                willRainSoon = true;
            }
        }

        if (isRaining || willRainSoon)
        {
            carWashRating = "Nên hoãn rửa xe";
            carWashColor = "#EF4444";
            carWashAdvice = "Dự báo khu vực sẽ có mưa trong 24-48 giờ tới. Rửa xe lúc này sẽ nhanh chóng bị bám bùn bẩn lại.";
        }
        else
        {
            carWashRating = "Rất thích hợp rửa xe";
            carWashColor = "#10B981";
            carWashAdvice = "Bầu trời tạnh ráo trong 2-3 ngày tới. Rất thích hợp để rửa xe và đánh bóng, xe sẽ giữ sạch được lâu.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = "BẢO DƯỠNG PHƯƠNG TIỆN",
            Title = "Rửa Xe Ô Tô & Xe Máy",
            Rating = carWashRating,
            RatingColor = carWashColor,
            Advice = carWashAdvice,
            IconGlyph = "\uf1b9" // car icon
        });

        // ==================== 4. SỨC KHỎE & PHÒNG CÔN TRÙNG ====================
        string healthRating;
        string healthColor;
        string healthAdvice;

        if (humidity >= 80)
        {
            healthRating = "Nguy cơ muỗi sinh sôi";
            healthColor = "#F59E0B";
            healthAdvice = "Độ ẩm cao tạo môi trường thuận lợi cho muỗi và nấm mốc. Hãy mắc màn khi ngủ và vệ sinh dụng cụ chứa nước đọng.";
        }
        else if (temp >= 34)
        {
            healthRating = "Cảnh báo sốc nhiệt & mất nước";
            healthColor = "#EF4444";
            healthAdvice = "Nhiệt độ ngoài trời oi ả. Cần bù nước liên tục, mang nón rộng vành và hạn chế sốc nhiệt khi từ phòng máy lạnh bước ra ngoài.";
        }
        else if (temp <= 16)
        {
            healthRating = "Giữ ấm cổ & họng";
            healthColor = "#3B82F6";
            healthAdvice = "Thời tiết lạnh buốt dễ gây viêm họng và hen suyễn. Quàng khăn ấm và uống nước ấm đầu ngày.";
        }
        else
        {
            healthRating = "Sức khỏe tối ưu";
            healthColor = "#10B981";
            healthAdvice = "Thời tiết ôn hòa dễ chịu. Thích hợp mở cửa đón gió tự nhiên vào nhà để thanh lọc không khí.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = "CHĂM SÓC SỨC KHỎE",
            Title = "Cảnh Báo Sức Khỏe & Đời Sống",
            Rating = healthRating,
            RatingColor = healthColor,
            Advice = healthAdvice,
            IconGlyph = "\uf21e" // heartbeat icon
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

        if (deltaTemp >= 10.0)
        {
            tempDeltaRating = $"Biên độ lớn (Δ{deltaTemp}°C)";
            tempDeltaColor = "#EF4444";
            tempDeltaAdvice = $"Ngày và đêm chênh lệch tới {deltaTemp}°C! Trưa nắng gắt nhưng đêm và sáng sớm lạnh sâu, hãy mang áo khoác nhẹ đề phòng cảm sốt và viêm họng.";
        }
        else if (deltaTemp >= 7.5)
        {
            tempDeltaRating = $"Chênh lệch vừa (Δ{deltaTemp}°C)";
            tempDeltaColor = "#F59E0B";
            tempDeltaAdvice = $"Biên độ nhiệt ngày đêm dao động {deltaTemp}°C. Cần chú ý giữ ấm phần cổ và ngực khi ra đường vào buổi tối muộn.";
        }
        else
        {
            tempDeltaRating = $"Biên độ ổn định (Δ{deltaTemp}°C)";
            tempDeltaColor = "#10B981";
            tempDeltaAdvice = $"Nhiệt độ ngày và đêm ổn định (dao động {deltaTemp}°C), cơ thể dễ thích nghi, ít nguy cơ sốc nhiệt sinh học.";
        }

        result.Add(new LifestyleIndexItem
        {
            Category = "CẢNH BÁO NHIỆT ĐỘ",
            Title = "Sốc Nhiệt & Biên Độ Ngày Đêm",
            Rating = tempDeltaRating,
            RatingColor = tempDeltaColor,
            Advice = tempDeltaAdvice,
            IconGlyph = "\uf2c7" // thermometer icon
        });

        return result;
    }
}
