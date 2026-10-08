using WeatherApp.Models;

namespace WeatherApp.Services;

public class WeatherAdviceService : IWeatherAdviceService
{
    public List<WeatherAdvice> GenerateAdvice(CurrentWeatherDto current, DailyWeatherDto? daily)
    {
        var adviceList = new List<WeatherAdvice>();
        bool isVi = LocalizationService.Instance.IsVietnamese;

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

        // 1. GỢI Ý TRANG PHỤC / CLOTHING
        var clothing = new WeatherAdvice { Category = isVi ? "TRANG PHỤC" : "CLOTHING" };
        if (temp <= 12)
        {
            clothing.Title = isVi ? "Mặc ấm nhiều lớp, áo phao dày" : "Layer up warmly, thick down coat";
            clothing.Description = isVi
                ? $"Nhiệt độ hiện tại {temp:F1}°C (Cảm giác: {feelsLike:F1}°C) rất lạnh. Hãy mặc áo len dày, áo khoác ấm, quàng khăn và đeo găng tay khi ra ngoài."
                : $"Current temperature {temp:F1}°C (Feels like: {feelsLike:F1}°C) is cold. Wear a warm sweater, down coat, scarf, and gloves outside.";
            clothing.IconGlyph = "\uf553"; // FontAwesome fa-shirt
            clothing.Severity = AdviceSeverity.Warning;
        }
        else if (temp <= 19)
        {
            clothing.Title = isVi ? "Áo khoác gió mỏng hoặc áo len dệt" : "Light windbreaker or cardigan";
            clothing.Description = isVi
                ? $"Thời tiết se lạnh ({temp:F1}°C). Một chiếc áo dài tay kết hợp áo khoác nhẹ hoặc cardigan sẽ giúp bạn thoải mái cả ngày."
                : $"Cool weather ({temp:F1}°C). A long-sleeve shirt paired with a light jacket or cardigan will keep you comfortable all day.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else if (temp <= 27)
        {
            clothing.Title = isVi ? "Trang phục nhẹ nhàng, thoải mái" : "Lightweight, comfortable casual";
            clothing.Description = isVi
                ? $"Nhiệt độ mát mẻ lý tưởng ({temp:F1}°C). Bạn có thể tự do diện đồ thường ngày như áo phông, sơ mi hoặc váy nhẹ."
                : $"Pleasant temperature ({temp:F1}°C). Feel free to wear casual t-shirts, light shirts, dresses, or standard workwear.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else if (temp <= 33)
        {
            clothing.Title = isVi ? "Trang phục cotton thoáng mát" : "Breathable cotton or linen clothes";
            clothing.Description = isVi
                ? $"Thời tiết khá oi bức ({temp:F1}°C). Nên chọn chất liệu cotton hoặc lanh thấm hút mồ hôi tốt và màu sắc tươi sáng."
                : $"Warm conditions ({temp:F1}°C). Opt for moisture-wicking cotton or linen materials with light colors.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Info;
        }
        else
        {
            clothing.Title = isVi ? "Chống nắng tối đa, vải che chắn tốt" : "Sun-protective wear and wide hat";
            clothing.Description = isVi
                ? $"Nắng nóng gay gắt ({temp:F1}°C, cảm giác {feelsLike:F1}°C). Bắt buộc mặc áo khoác chống nắng, đội nón rộng vành và hạn chế quần áo tối màu."
                : $"Intense heat ({temp:F1}°C, feels like {feelsLike:F1}°C). Wear UV-protective jackets, wide-brim hats, and avoid dark colors.";
            clothing.IconGlyph = "\uf553";
            clothing.Severity = AdviceSeverity.Alert;
        }
        adviceList.Add(clothing);

        // 2. VẬT DỤNG CẦN THIẾT / ESSENTIAL GEAR
        var gear = new WeatherAdvice { Category = isVi ? "VẬT DỤNG NÊN MANG" : "ESSENTIAL GEAR" };
        if (isThunderstorm)
        {
            gear.Title = isVi ? "Mang áo mưa bộ hoặc ô cỡ lớn" : "Raincoat suit or heavy-duty umbrella";
            gear.Description = isVi
                ? "Thời tiết có dông sét nguy hiểm. Luôn chuẩn bị sẵn áo mưa trong xe, tránh dùng ô kim loại khi có sét đánh."
                : "Dangerous thunderstorm conditions. Keep rain gear in your vehicle and avoid metallic umbrellas during lightning strikes.";
            gear.IconGlyph = "\uf0e9"; // FontAwesome fa-umbrella
            gear.Severity = AdviceSeverity.Alert;
        }
        else if (isRaining)
        {
            gear.Title = isVi ? "Đừng quên ô (dù) và áo mưa" : "Don't forget an umbrella & raincoat";
            gear.Description = isVi
                ? $"Xác suất mưa hôm nay khoảng {rainProb}%. Hãy mang theo ô gập hoặc áo mưa để không bị ướt bất ngờ."
                : $"Chance of rain today is {rainProb}%. Keep a compact umbrella or raincoat handy to stay dry.";
            gear.IconGlyph = "\uf0e9"; // FontAwesome fa-umbrella
            gear.Severity = AdviceSeverity.Warning;
        }
        else if (uvMax >= 7)
        {
            gear.Title = isVi ? "Kính râm và kem chống nắng" : "Sunglasses and SPF 50+ sunscreen";
            gear.Description = isVi
                ? $"Chỉ số UV đạt mức cao ({uvMax:F1}). Đừng quên mang kính râm chống tia UV, thoa kem chống nắng SPF 50+ và khẩu trang chống nắng."
                : $"UV index is very high ({uvMax:F1}). Don't forget UV sunglasses, broad-spectrum sunscreen, and protective face coverings.";
            gear.IconGlyph = "\uf185"; // FontAwesome fa-sun
            gear.Severity = AdviceSeverity.Warning;
        }
        else
        {
            gear.Title = isVi ? "Thời tiết tạnh ráo, thuận tiện" : "Dry weather, travel light";
            gear.Description = isVi
                ? "Không có dấu hiệu mưa đáng kể. Bạn có thể yên tâm di chuyển gọn nhẹ mà không cần mang theo áo mưa cồng kềnh."
                : "No significant rain detected. You can comfortably commute light without bulky rain gear.";
            gear.IconGlyph = "\uf185";
            gear.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(gear);

        // 3. HOẠT ĐỘNG NGOÀI TRỜI / OUTDOOR ACTIVITIES
        var outdoor = new WeatherAdvice { Category = isVi ? "HOẠT ĐỘNG NGOÀI TRỜI" : "OUTDOOR ACTIVITIES" };
        if (isThunderstorm)
        {
            outdoor.Title = isVi ? "Hạn chế ra ngoài, đề phòng dông lốc" : "Stay indoors, avoid high winds & storms";
            outdoor.Description = isVi
                ? "Có dông lốc và sét nguy hiểm. Nên ở trong nhà kiên cố, đóng kín cửa sổ và tránh xa các gốc cây to hoặc cột biển báo."
                : "Dangerous storm conditions. Stay inside secure buildings, close windows, and keep away from large trees.";
            outdoor.IconGlyph = "\uf76c"; // FontAwesome fa-cloud-bolt
            outdoor.Severity = AdviceSeverity.Alert;
        }
        else if (isRaining && (code >= 63 || rainProb >= 70))
        {
            outdoor.Title = isVi ? "Ưu tiên các hoạt động trong nhà" : "Prioritize indoor activities";
            outdoor.Description = isVi
                ? "Mưa có thể ảnh hưởng đến lịch trình ngoài trời. Nên dời các buổi thể thao dã ngoại sang các hoạt động rạp phim, quán cà phê hoặc thể thao trong nhà."
                : "Heavy rain may disrupt outdoor plans. Reschedule open-air sports to cafes, cinema, or indoor gym facilities.";
            outdoor.IconGlyph = "\uf73d"; // FontAwesome fa-cloud-rain
            outdoor.Severity = AdviceSeverity.Warning;
        }
        else if (temp >= 35)
        {
            outdoor.Title = isVi ? "Tránh vận động mạnh giờ trưa" : "Avoid intense midday outdoor exercise";
            outdoor.Description = isVi
                ? "Khung giờ 11h - 15h nắng rất gắt, dễ gây say nắng sốc nhiệt. Nếu muốn tập thể dục, hãy chọn sáng sớm hoặc sau 17h30 chiều."
                : "Intense sun between 11 AM - 3 PM can cause heat exhaustion. Schedule workouts for early morning or after 5:30 PM.";
            outdoor.IconGlyph = "\uf185"; // FontAwesome fa-sun
            outdoor.Severity = AdviceSeverity.Warning;
        }
        else
        {
            outdoor.Title = isVi ? "Thời tiết tuyệt vời cho dã ngoại & thể thao" : "Great weather for workouts & outings";
            outdoor.Description = isVi
                ? "Khí trời rất dễ chịu và khô ráo! Rất thích hợp để chạy bộ công viên, đạp xe, dạo phố hoặc gặp gỡ bạn bè ngoài trời."
                : "Pleasant and dry air! Ideal for park jogging, cycling, city strolls, or meeting friends outdoors.";
            outdoor.IconGlyph = "\uf6c4"; // FontAwesome fa-cloud-sun
            outdoor.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(outdoor);

        // 4. AN TOÀN GIAO THÔNG / COMMUTE & TRAFFIC
        var traffic = new WeatherAdvice { Category = isVi ? "LÁI XE & DI CHUYỂN" : "COMMUTE & TRAFFIC" };
        if (isFog)
        {
            traffic.Title = isVi ? "Bật đèn sương mù, giữ khoảng cách" : "Use fog lights, maintain safe distance";
            traffic.Description = isVi
                ? "Sương mù làm tầm nhìn giảm thấp. Hãy bật đèn chiếu gần hoặc đèn sương mù, đi chậm và giữ cự ly an toàn với xe phía trước."
                : "Dense fog impairs road visibility. Turn on low beams or fog lamps, slow down, and keep a safe following distance.";
            traffic.IconGlyph = "\uf1b9"; // FontAwesome fa-car
            traffic.Severity = AdviceSeverity.Warning;
        }
        else if (isRaining)
        {
            traffic.Title = isVi ? "Mặt đường trơn trượt, giảm tốc độ" : "Slippery roads, reduce driving speed";
            traffic.Description = isVi
                ? "Mưa làm giảm độ bám của lốp xe và hạn chế tầm nhìn kính lái. Chú ý đi chậm, tránh phanh gấp và cẩn thận tại các đoạn ngập nước."
                : "Rain reduces tire traction and windshield visibility. Drive moderately, avoid abrupt braking, and watch for flooded spots.";
            traffic.IconGlyph = "\uf1b9";
            traffic.Severity = AdviceSeverity.Warning;
        }
        else if (wind >= 28)
        {
            traffic.Title = isVi ? "Gió giật mạnh trên cầu và đường cao tốc" : "Strong crosswinds on bridges & highways";
            traffic.Description = isVi
                ? $"Tốc độ gió {wind:F0} km/h có thể tạo lực cản lớn khi lái xe máy. Hãy vững tay lái, nhất là khi đi qua cầu vượt hay khu vực trống gió."
                : $"Wind speeds of {wind:F0} km/h create strong resistance for two-wheelers. Maintain firm handlebar control on elevated bridges.";
            traffic.IconGlyph = "\uf72e"; // FontAwesome fa-wind
            traffic.Severity = AdviceSeverity.Warning;
        }
        else
        {
            traffic.Title = isVi ? "Đường xá khô ráo, tầm nhìn thoáng" : "Dry roads, clear driving visibility";
            traffic.Description = isVi
                ? "Điều kiện giao thông rất thuận tiện. Tầm nhìn xa tốt, mặt đường khô ráo giúp việc lái xe an toàn."
                : "Commute conditions are optimal. Clear visibility and dry pavement ensure safe, comfortable travel.";
            traffic.IconGlyph = "\uf1b9";
            traffic.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(traffic);

        // 5. CHĂM SÓC SỨC KHỎE / HEALTH & WELLNESS
        var health = new WeatherAdvice { Category = isVi ? "SỨC KHỎE & THỂ TRẠNG" : "HEALTH & WELLNESS" };
        if (uvMax >= 8)
        {
            health.Title = isVi ? "Bảo vệ da & mắt khỏi tia cực tím cực mạnh" : "Protect skin & eyes from intense UV rays";
            health.Description = isVi
                ? $"Chỉ số UV lên tới {uvMax:F1} (Rất cao). Tiếp xúc trực tiếp có thể gây bỏng rát da và hại mắt. Hãy che chắn kỹ và uống đủ nước lọc."
                : $"UV index reaches {uvMax:F1} (Very High). Direct exposure causes sunburn and eye strain. Cover up and stay hydrated.";
            health.IconGlyph = "\uf21e"; // FontAwesome fa-heart-pulse
            health.Severity = AdviceSeverity.Alert;
        }
        else if (humidity >= 85)
        {
            health.Title = isVi ? "Độ ẩm cao, giữ nhà cửa khô ráo" : "High humidity, keep living spaces airy";
            health.Description = isVi
                ? $"Độ ẩm đạt {humidity:F0}%, dễ tạo điều kiện cho vi khuẩn và nấm mốc phát triển. Người có bệnh viêm mũi, xoang cần chú ý vệ sinh sạch sẽ."
                : $"Humidity reaches {humidity:F0}%, promoting mold and allergen growth. Keep indoor areas airy and maintain good respiratory hygiene.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else if (humidity <= 40)
        {
            health.Title = isVi ? "Không khí khô, bổ sung 2L nước và dưỡng ẩm" : "Dry air, drink 2L water and moisturize";
            health.Description = isVi
                ? $"Độ ẩm xuống thấp ({humidity:F0}%), da dễ bị khô nẻ và khô họng. Hãy uống nhiều nước ấm, ăn nhiều hoa quả và sử dụng kem dưỡng ẩm."
                : $"Low humidity ({humidity:F0}%) can cause dry skin and throat irritation. Drink ample water, eat fruit, and use moisturizers.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else if (temp >= 33)
        {
            health.Title = isVi ? "Bổ sung nước thường xuyên, tránh sốc nhiệt" : "Stay hydrated often, avoid heat exhaustion";
            health.Description = isVi
                ? "Cơ thể dễ mất nước nhanh qua mồ hôi. Hãy chủ động uống nước cách quãng 30 phút, bổ sung thêm khoáng chất hoặc nước dừa tươi."
                : "The body loses fluids rapidly through sweat. Drink fluids every 30 minutes and consider replenishing electrolytes.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        else
        {
            health.Title = isVi ? "Thời tiết cân bằng, thể trạng tốt" : "Balanced weather, optimal wellness";
            health.Description = isVi
                ? "Nhiệt độ và độ ẩm ở mức hài hòa, rất tốt cho hệ hô hấp và tim mạch. Hãy duy trì lối sống lành mạnh và vận động đều đặn."
                : "Harmonious temperature and humidity support cardiovascular and respiratory comfort. Keep up regular physical wellness habits.";
            health.IconGlyph = "\uf21e";
            health.Severity = AdviceSeverity.Info;
        }
        adviceList.Add(health);

        return adviceList;
    }
}
