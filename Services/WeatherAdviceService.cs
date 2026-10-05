using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherAdviceService : IWeatherAdviceService
{
    public List<WeatherAdvice> GenerateAdvice(CurrentWeatherDto current, DailyWeatherDto? daily)
    {
        var adviceList = new List<WeatherAdvice>();

        double temp = current.Temperature;
        double feelsLike = current.ApparentTemperature;
        int code = current.WeatherCode;
        double humidity = current.RelativeHumidity;
        double wind = current.WindSpeed;
        double uvMax = (daily?.UvIndexMax != null && daily.UvIndexMax.Count > 0) ? daily.UvIndexMax[0] : 0;
        int rainProb = (daily?.PrecipitationProbabilityMax != null && daily.PrecipitationProbabilityMax.Count > 0) ? daily.PrecipitationProbabilityMax[0] : 0;
        bool isRaining = code >= 51 || current.Precipitation > 0 || rainProb >= 40;
        bool isThunderstorm = code >= 95;
        bool isFog = code == 45 || code == 48;

        // 1. GỢI Ý TRANG PHỤC
        var clothing = new WeatherAdvice { Category = "TRANG PHỤC" };
        if (temp <= 12)
        {
            clothing.Title = "Mặc ấm nhiều lớp, áo phao dày";
            clothing.Description = $"Nhiệt độ hiện tại {temp:F1}°C (Cảm giác: {feelsLike:F1}°C) rất lạnh. Hãy mặc áo len dày, áo khoác ấm, quàng khăn và đeo găng tay khi ra ngoài.";
            clothing.IconGlyph = "\uf553"; // FontAwesome fa-shirt
            clothing.Severity = AdviceSeverity.Warning;
        }
        else if (temp <= 19)
        {
            clothing.Title = "Áo khoác gió mỏng hoặc áo len dệt";
            clothing.Description = $"Thời tiết se lạnh ({temp:F1}°C). Một chiếc áo dài tay kết hợp áo khoác nhẹ hoặc cardigan sẽ giúp bạn thoải mái cả ngày.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else if (temp <= 27)
        {
            clothing.Title = "Trang phục nhẹ nhàng, thoải mái";
            clothing.Description = $"Nhiệt độ mát mẻ lý tưởng ({temp:F1}°C). Bạn có thể tự do diện đồ thường ngày như áo phông, sơ mi hoặc váy nhẹ.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else if (temp <= 33)
        {
            clothing.Title = "Trang phục cotton thoáng mát";
            clothing.Description = $"Thời tiết khá oi bức ({temp:F1}°C). Nên chọn chất liệu cotton hoặc lanh thấm hút mồ hôi tốt và màu sắc tươi sáng.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else
        {
            clothing.Title = "Chống nắng tối đa, vải che chắn tốt";
            clothing.Description = $"Nắng nóng gay gắt ({temp:F1}°C, cảm giác {feelsLike:F1}°C). Bắt buộc mặc áo khoác chống nắng, đội nón rộng vành và hạn chế quần áo tối màu.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Alert;
        }
        adviceList.Add(clothing);

        // 2. VẬT DỤNG CẦN THIẾT
        var gear = new WeatherAdvice { Category = "VẬT DỤNG NÊN MANG" };
        if (isThunderstorm)
        {
            gear.Title = "Mang áo mưa bộ hoặc ô cỡ lớn";
            gear.Description = "Thời tiết có dông sét nguy hiểm. Luôn chuẩn bị sẵn áo mưa trong xe, tránh dùng ô kim loại khi có sét đánh.";
            gear.IconGlyph = "\uf0e9"; // FontAwesome fa-umbrella
            gear.Severity = AdviceSeverity.Alert;
        }
        else if (isRaining)
        {
            gear.Title = "Đừng quên ô (dù) và áo mưa";
            gear.Description = $"Xác suất mưa hôm nay khoảng {rainProb}%. Hãy mang theo ô gập hoặc áo mưa để không bị ướt bất ngờ.";
            gear.IconGlyph = "\uf0e9"; // FontAwesome fa-umbrella
            gear.Severity = AdviceSeverity.Warning;
        }
        else if (uvMax >= 7)
        {
            gear.Title = "Kính râm và kem chống nắng";
            gear.Description = $"Chỉ số UV đạt mức cao ({uvMax:F1}). Đừng quên mang kính râm chống tia UV, thoa kem chống nắng SPF 50+ và khẩu trang chống nắng.";
            gear.IconGlyph = "\uf185"; // FontAwesome fa-sun
            gear.Severity = AdviceSeverity.Warning;
        }
        else
        {
            gear.Title = "Thời tiết tạnh ráo, thuận tiện";
            gear.Description = "Không có dấu hiệu mưa đáng kể. Bạn có thể yên tâm di chuyển gọn nhẹ mà không cần mang theo áo mưa cồng kềnh.";
            gear.IconGlyph = "\uf185";
            gear.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(gear);

        // 3. HOẠT ĐỘNG NGOÀI TRỜI
        var outdoor = new WeatherAdvice { Category = "HOẠT ĐỘNG NGOÀI TRỜI" };
        if (isThunderstorm)
        {
            outdoor.Title = "Hạn chế ra ngoài, đề phòng dông lốc";
            outdoor.Description = "Có dông lốc và sét nguy hiểm. Nên ở trong nhà kiên cố, đóng kín cửa sổ và tránh xa các gốc cây to hoặc cột biển báo.";
            outdoor.IconGlyph = "\uf76c"; // FontAwesome fa-cloud-bolt
            outdoor.Severity = AdviceSeverity.Alert;
        }
        else if (isRaining && (code >= 63 || rainProb >= 70))
        {
            outdoor.Title = "Ưu tiên các hoạt động trong nhà";
            outdoor.Description = "Mưa có thể ảnh hưởng đến lịch trình ngoài trời. Nên dời các buổi thể thao dã ngoại sang các hoạt động rạp phim, quán cà phê hoặc thể thao trong nhà.";
            outdoor.IconGlyph = "\uf73d"; // FontAwesome fa-cloud-rain
            outdoor.Severity = AdviceSeverity.Warning;
        }
        else if (temp >= 35)
        {
            outdoor.Title = "Tránh vận động mạnh giờ trưa";
            outdoor.Description = "Khung giờ 11h - 15h nắng rất gắt, dễ gây say nắng sốc nhiệt. Nếu muốn tập thể dục, hãy chọn sáng sớm hoặc sau 17h30 chiều.";
            outdoor.IconGlyph = "\uf185"; // FontAwesome fa-sun
            outdoor.Severity = AdviceSeverity.Warning;
        }
        else
        {
            outdoor.Title = "Thời tiết tuyệt vời cho dã ngoại & thể thao";
            outdoor.Description = "Khí trời rất dễ chịu và khô ráo! Rất thích hợp để chạy bộ công viên, đạp xe, dạo phố hoặc gặp gỡ bạn bè ngoài trời.";
            outdoor.IconGlyph = "\uf6c4"; // FontAwesome fa-cloud-sun
            outdoor.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(outdoor);

        // 4. AN TOÀN GIAO THÔNG
        var traffic = new WeatherAdvice { Category = "LÁI XE & DI CHUYỂN" };
        if (isFog)
        {
            traffic.Title = "Bật đèn sương mù, giữ khoảng cách";
            traffic.Description = "Sương mù làm tầm nhìn giảm thấp. Hãy bật đèn chiếu gần hoặc đèn sương mù, đi chậm và giữ cự ly an toàn với xe phía trước.";
            traffic.IconGlyph = "\uf1b9"; // FontAwesome fa-car
            traffic.Severity = AdviceSeverity.Warning;
        }
        else if (isRaining)
        {
            traffic.Title = "Mặt đường trơn trượt, giảm tốc độ";
            traffic.Description = "Mưa làm giảm độ bám của lốp xe và hạn chế tầm nhìn kính lái. Chú ý đi chậm, tránh phanh gấp và cẩn thận tại các đoạn ngập nước.";
            traffic.IconGlyph = "\uf1b9";
            traffic.Severity = AdviceSeverity.Warning;
        }
        else if (wind >= 28)
        {
            traffic.Title = "Gió giật mạnh trên cầu và đường cao tốc";
            traffic.Description = $"Tốc độ gió {wind:F0} km/h có thể tạo lực cản lớn khi lái xe máy. Hãy vững tay lái, nhất là khi đi qua cầu vượt hay khu vực trống gió.";
            traffic.IconGlyph = "\uf72e"; // FontAwesome fa-wind
            traffic.Severity = AdviceSeverity.Warning;
        }
        else
        {
            traffic.Title = "Đường xá khô ráo, tầm nhìn thoáng";
            traffic.Description = "Điều kiện giao thông rất thuận tiện. Tầm nhìn xa tốt, mặt đường khô ráo giúp việc lái xe an toàn.";
            traffic.IconGlyph = "\uf1b9";
            traffic.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(traffic);

        // 5. CHĂM SÓC SỨC KHỎE
        var health = new WeatherAdvice { Category = "SỨC KHỎE & THỂ TRẠNG" };
        if (uvMax >= 8)
        {
            health.Title = "Bảo vệ da & mắt khỏi tia cực tím cực mạnh";
            health.Description = $"Chỉ số UV lên tới {uvMax:F1} (Rất cao). Tiếp xúc trực tiếp có thể gây bỏng rát da và hại mắt. Hãy che chắn kỹ và uống đủ nước lọc.";
            health.IconGlyph = "\uf21e"; // FontAwesome fa-heart-pulse
            health.Severity = AdviceSeverity.Alert;
        }
        else if (humidity >= 85)
        {
            health.Title = "Độ ẩm cao, giữ nhà cửa khô ráo";
            health.Description = $"Độ ẩm đạt {humidity:F0}%, dễ tạo điều kiện cho vi khuẩn và nấm mốc phát triển. Người có bệnh viêm mũi, xoang cần chú ý vệ sinh sạch sẽ.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else if (humidity <= 40)
        {
            health.Title = "Không khí khô, bổ sung 2L nước và dưỡng ẩm";
            health.Description = $"Độ ẩm xuống thấp ({humidity:F0}%), da dễ bị khô nẻ và khô họng. Hãy uống nhiều nước ấm, ăn nhiều hoa quả và sử dụng kem dưỡng ẩm.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else if (temp >= 33)
        {
            health.Title = "Bổ sung nước thường xuyên, tránh sốc nhiệt";
            health.Description = "Cơ thể dễ mất nước nhanh qua mồ hôi. Hãy chủ động uống nước cách quãng 30 phút, bổ sung thêm khoáng chất hoặc nước dừa tươi.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else
        {
            health.Title = "Thời tiết cân bằng, thể trạng tốt";
            health.Description = "Nhiệt độ và độ ẩm ở mức hài hòa, rất tốt cho hệ hô hấp và tim mạch. Hãy duy trì lối sống lành mạnh và vận động đều đặn.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(health);

        return adviceList;
    }
}
