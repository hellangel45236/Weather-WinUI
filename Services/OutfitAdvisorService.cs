using System;
using System.Collections.Generic;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ gợi ý trang phục thông minh "Hôm Nay Mặc Gì?" (OOTD Studio v3.0 Pro).
/// Phân tích nhiệt độ cảm nhận (FeelsLike), mưa, gió, bức xạ UV, chất lượng không khí (AQI),
/// kết hợp với mục đích di chuyển (Công sở, Đi học, Dạo phố, Thể thao, Dã ngoại), phong cách giới tính (Nam, Nữ, Unisex),
/// và đặc thù giao thông xe máy tại Việt Nam. Hỗ trợ song ngữ Tiếng Việt & English.
/// </summary>
public class OutfitAdvisorService
{
    public OutfitAdvice GenerateOutfitAdvice(CurrentWeatherDisplay current, OpenMeteoResponse? rawData, string occasion = "Work", string gender = "All")
    {
        bool isVi = LocalizationService.Instance.IsVietnamese;
        double temp = current.TemperatureValue;
        double feelsLike = temp;

        if (!string.IsNullOrEmpty(current.FeelsLikeText))
        {
            string clean = current.FeelsLikeText.Replace("Cảm giác:", "").Replace("Cảm giác như", "").Replace("Feels like:", "").Replace("°C", "").Replace("°", "").Trim();
            if (double.TryParse(clean, out double parsedFeels))
            {
                feelsLike = parsedFeels;
            }
        }

        bool isRaining = current.WeatherEffect is WeatherEffectType.LightRain
            or WeatherEffectType.ModerateRain
            or WeatherEffectType.HeavyRain
            or WeatherEffectType.Thunderstorm;

        int rainProb = 0;
        if (!string.IsNullOrEmpty(current.RainProbabilityText))
        {
            int.TryParse(current.RainProbabilityText.Replace("%", "").Trim(), out rainProb);
        }
        if (rainProb >= 40) isRaining = true;

        double uv = 0;
        if (!string.IsNullOrEmpty(current.UvIndexText))
        {
            double.TryParse(current.UvIndexText.Trim(), out uv);
        }

        double wind = 10;
        if (!string.IsNullOrEmpty(current.WindText))
        {
            string wStr = current.WindText.Replace("km/h", "").Replace("m/s", "").Trim();
            double.TryParse(wStr, out wind);
        }

        int aqi = current.AqiValue;

        var advice = new OutfitAdvice
        {
            Occasion = occasion,
            GenderStyle = gender
        };

        // Gán tiêu đề Dịp & Giới tính
        advice.OccasionTitle = occasion switch
        {
            "School" => isVi ? "Đi học / Giảng đường" : "Campus / School",
            "Casual" => isVi ? "Dạo phố & Cà phê" : "Casual / Cafe",
            "Sport" => isVi ? "Thể thao & Vận động" : "Sport & Workout",
            "Travel" => isVi ? "Du lịch & Dã ngoại" : "Travel & Outdoor",
            _ => isVi ? "Công sở / Đi làm" : "Office & Work"
        };

        advice.GenderStyleTitle = gender switch
        {
            "Men" => isVi ? "Nam giới" : "Men's Style",
            "Women" => isVi ? "Nữ giới" : "Women's Style",
            _ => isVi ? "Tự do / Unisex" : "Unisex / Free"
        };

        // 1. Phân tích nhiệt độ cảm nhận & Tiêu đề nhiệt
        string thermalHeadline;
        string thermalNotice;
        string thermalColor;

        if (feelsLike < 16)
        {
            thermalHeadline = isVi ? "Trời Rét Lạnh • Cần Giữ Ấm Kỹ" : "Cold & Chilly • Bundle Up Warmly";
            thermalNotice = isVi
                ? $"Cảm giác {feelsLike:F0}°C buốt lạnh. Hãy mặc áo giữ nhiệt và che chắn cổ ngực."
                : $"Feels like {feelsLike:F0}°C. Wear thermal layers and protect chest & throat.";
            thermalColor = "#3B82F6";
            advice.FabricRecommendation = isVi ? "Len dệt, nỉ bông dày dặn, dạ cừu giữ nhiệt" : "Wool knit, fleece, thermal cotton";
        }
        else if (feelsLike < 22)
        {
            thermalHeadline = isVi ? "Khí Trời Se Lạnh • Dễ Chịu" : "Cool Breeze • Fresh Weather";
            thermalNotice = isVi
                ? $"Cảm giác {feelsLike:F0}°C mát mẻ và se lạnh. Rất thích hợp diện áo khoác mỏng hoặc cardigan."
                : $"Feels like {feelsLike:F0}°C. Perfect weather for light jackets or cardigans.";
            thermalColor = "#06B6D4";
            advice.FabricRecommendation = isVi ? "Cotton pha dệt kim, nỉ da cá, denim mềm" : "Knit cotton, soft denim, terry cloth";
        }
        else if (feelsLike <= 28)
        {
            thermalHeadline = isVi ? "Thời Tiết Lý Tưởng • Mát Mẻ" : "Optimal Comfort • Pleasant Breeze";
            thermalNotice = isVi
                ? $"Cảm giác {feelsLike:F0}°C cực kỳ ôn hòa. Thoải mái diện các bộ phối thời trang yêu thích."
                : $"Feels like {feelsLike:F0}°C. Highly comfortable, ideal for versatile styling.";
            thermalColor = "#10B981";
            advice.FabricRecommendation = isVi ? "Cotton 100%, đũi thoáng mát, vải thun co giãn" : "100% Cotton, airy linen, stretch modal";
        }
        else if (feelsLike <= 33)
        {
            thermalHeadline = isVi ? "Khí Hậu Khá Nóng & Oi Nhẹ" : "Warm & Slightly Humid";
            thermalNotice = isVi
                ? $"Cảm giác {feelsLike:F0}°C khá bức bối vào giữa trưa. Ưu tiên chất vải mỏng nhẹ thấm mồ hôi."
                : $"Feels like {feelsLike:F0}°C. Slightly warm midday. Choose light sweat-absorbing fabrics.";
            thermalColor = "#F59E0B";
            advice.FabricRecommendation = isVi ? "Đũi tự nhiên, cotton mỏng nhẹ, vải sợi tre Bamboo" : "Airy linen, light cotton, breathable bamboo";
        }
        else
        {
            thermalHeadline = isVi ? "Nắng Nóng Gay Gắt • Đề Phòng Sốc Nhiệt" : "Scorching Heat • Sun Protection Crucial";
            thermalNotice = isVi
                ? $"Cảm giác {feelsLike:F0}°C rất oi bức. Bắt buộc chống nắng toàn diện và bù nước liên tục."
                : $"Feels like {feelsLike:F0}°C. Extreme heat. Apply sunscreen and stay hydrated.";
            thermalColor = "#EF4444";
            advice.FabricRecommendation = isVi ? "Sợi mát lạnh Ice-Silk, cotton dệt thưa, UPF 50+" : "Cool-touch Ice Silk, lightweight UPF 50+ fabric";
        }

        advice.ThermalComfortNotice = $"{thermalHeadline} — {thermalNotice}";
        advice.ThermalTagColor = thermalColor;

        // 2. Gợi ý bảng màu trang phục theo thời tiết (Color Palette)
        var palette = new List<OutfitColorItem>();
        if (isRaining)
        {
            palette.Add(new OutfitColorItem { HexColor = "#EA580C", Name = isVi ? "Cam cảnh báo" : "Safety Orange" });
            palette.Add(new OutfitColorItem { HexColor = "#0284C7", Name = isVi ? "Xanh biển sâu" : "Ocean Navy" });
            palette.Add(new OutfitColorItem { HexColor = "#475569", Name = isVi ? "Ghi xám trượt nước" : "Waterproof Slate" });
            palette.Add(new OutfitColorItem { HexColor = "#1E293B", Name = isVi ? "Đen than chì" : "Graphite Black" });
        }
        else if (feelsLike >= 30)
        {
            palette.Add(new OutfitColorItem { HexColor = "#F8FAFC", Name = isVi ? "Trắng tinh khôi" : "Crisp White" });
            palette.Add(new OutfitColorItem { HexColor = "#BAE6FD", Name = isVi ? "Xanh pastel dịu" : "Sky Pastel" });
            palette.Add(new OutfitColorItem { HexColor = "#FEF08A", Name = isVi ? "Vàng kem nhạt" : "Soft Cream" });
            palette.Add(new OutfitColorItem { HexColor = "#D1FAE5", Name = isVi ? "Xanh bạc hà" : "Mint Green" });
        }
        else if (feelsLike < 18)
        {
            palette.Add(new OutfitColorItem { HexColor = "#78350F", Name = isVi ? "Nâu Caramel ấm" : "Warm Caramel" });
            palette.Add(new OutfitColorItem { HexColor = "#1E3A8A", Name = isVi ? "Xanh navy đầm" : "Deep Navy" });
            palette.Add(new OutfitColorItem { HexColor = "#831843", Name = isVi ? "Đỏ mận quý phái" : "Burgundy Red" });
            palette.Add(new OutfitColorItem { HexColor = "#334155", Name = isVi ? "Xám lông chuột" : "Charcoal Grey" });
        }
        else
        {
            palette.Add(new OutfitColorItem { HexColor = "#3B82F6", Name = isVi ? "Xanh dương trẻ trung" : "Royal Blue" });
            palette.Add(new OutfitColorItem { HexColor = "#F1F5F9", Name = isVi ? "Trắng ngà" : "Off-White" });
            palette.Add(new OutfitColorItem { HexColor = "#10B981", Name = isVi ? "Xanh ngọc lục bảo" : "Emerald Green" });
            palette.Add(new OutfitColorItem { HexColor = "#CBD5E1", Name = isVi ? "Ghi sáng thanh lịch" : "Light Slate" });
        }
        advice.SuggestedColors = palette;

        // 3. Phân loại trang phục chi tiết theo Dịp & Giới tính
        ApplyOutfitDetails(advice, feelsLike, isRaining, uv, occasion, gender, isVi);

        // 4. Checklist phụ kiện thiết yếu
        var accessories = new List<OutfitAccessoryItem>();

        // Trời mưa hoặc khả năng mưa cao
        if (isRaining)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Áo mưa bộ / Dù gập" : "Raincoat / Compact Umbrella",
                Reason = isVi ? "Có mưa ngoài trời, che chắn chống ướt áo quần và balo" : "Rain expected, shield clothes and backpack",
                IconGlyph = "\uf73d",
                AccentColor = "#38BDF8",
                IsEssential = true,
                BadgeText = isVi ? "Bắt buộc" : "Essential"
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Bọc giày đi mưa chống nước" : "Waterproof Shoe Covers",
                Reason = isVi ? "Bảo vệ giày da, sneaker không bị ẩm mốc nước bẩn" : "Protect footwear from water puddles",
                IconGlyph = "\uf549",
                AccentColor = "#0284C7",
                IsEssential = true,
                BadgeText = isVi ? "Cần thiết" : "Recommended"
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Túi bọc balo chống thấm" : "Backpack Rain Cover",
                Reason = isVi ? "Bảo vệ laptop, giấy tờ quan trọng trong túi" : "Protect laptop and papers from moisture",
                IconGlyph = "\uf5be",
                AccentColor = "#6366F1",
                IsEssential = false,
                BadgeText = isVi ? "Tiện ích" : "Helpful"
            });
        }
        else if (rainProb >= 30)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Dù gập cầm tay dự phòng" : "Portable Umbrella",
                Reason = isVi ? $"Khả năng mưa {rainProb}%, đề phòng cơn mưa rào bất chợt" : $"Rain chance {rainProb}%, prepare for sudden shower",
                IconGlyph = "\uf0e9",
                AccentColor = "#38BDF8",
                IsEssential = false,
                BadgeText = isVi ? "Dự phòng" : "Standby"
            });
        }

        // Tia UV & Nắng
        if (uv >= 6.0 && current.IsDay)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Kem chống nắng SPF 50+" : "Sunscreen SPF 50+",
                Reason = isVi ? $"Chỉ số UV {uv:F1} nguy hại cao, thoa trước khi ra đường 15 phút" : $"UV Index {uv:F1} very high, apply 15m before going out",
                IconGlyph = "\uf185",
                AccentColor = "#EF4444",
                IsEssential = true,
                BadgeText = isVi ? "Bắt buộc" : "Essential"
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Kính râm chống UV400" : "UV400 Sunglasses",
                Reason = isVi ? "Bảo vệ giác mạc và giảm chói mắt khi lái xe" : "Protect eyes and reduce glare while driving",
                IconGlyph = "\uf000",
                AccentColor = "#F59E0B",
                IsEssential = false,
                BadgeText = isVi ? "Bảo vệ" : "Protection"
            });
        }
        else if (uv >= 3.0 && current.IsDay)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Kính mát / Nón vành" : "Sunglasses / Hat",
                Reason = isVi ? $"Chỉ số UV {uv:F1}, chống chói mắt khi lưu thông" : $"UV {uv:F1}, prevents glare during daytime",
                IconGlyph = "\uf000",
                AccentColor = "#F59E0B",
                IsEssential = false,
                BadgeText = isVi ? "Nên mang" : "Suggested"
            });
        }

        // Bụi mịn AQI & Khẩu trang
        if (aqi >= 150)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Khẩu trang N95 / KF94 lọc bụi mịn" : "N95 / KF94 Respirator Mask",
                Reason = isVi ? $"AQI {aqi} (Xấu): Bụi mịn PM2.5 cao, bắt buộc đeo N95 kín mặt" : $"AQI {aqi} (Unhealthy): PM2.5 high, wear N95 tightly",
                IconGlyph = "\uf6cf",
                AccentColor = "#EF4444",
                IsEssential = true,
                BadgeText = isVi ? "Bắt buộc" : "Essential"
            });
        }
        else if (aqi >= 100)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Khẩu trang y tế kháng khuẩn 4 lớp" : "4-Ply Medical Face Mask",
                Reason = isVi ? $"AQI {aqi} ô nhiễm trung bình, cản khói bụi xe cộ" : $"AQI {aqi} moderate pollution, block traffic dust",
                IconGlyph = "\uf6cf",
                AccentColor = "#F97316",
                IsEssential = false,
                BadgeText = isVi ? "Khuyên dùng" : "Recommended"
            });
        }
        else
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Khẩu trang vải thoáng khí" : "Breathable Cloth Mask",
                Reason = isVi ? "Không khí tản sạch, bảo vệ da mặt chống gió bụi" : "Air quality is good, light barrier against wind",
                IconGlyph = "\uf6cf",
                AccentColor = "#10B981",
                IsEssential = false,
                BadgeText = isVi ? "Cơ bản" : "Basic"
            });
        }

        // Bình nước cá nhân khi nóng
        if (feelsLike >= 30)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = isVi ? "Bình giữ nhiệt cấp nước" : "Insulated Water Bottle",
                Reason = isVi ? "Nhiệt độ oi bức, bổ sung nước đều đặn để tránh mất sức" : "Hot weather, drink frequently to prevent dehydration",
                IconGlyph = "\uf57b",
                AccentColor = "#06B6D4",
                IsEssential = false,
                BadgeText = isVi ? "Sức khỏe" : "Wellness"
            });
        }

        advice.Accessories = accessories;

        // 5. Cảnh báo xe máy & Trang bị đi mưa đặc thù Việt Nam
        if (isRaining)
        {
            advice.RaincoatAdvice = isVi
                ? "Khuyên dùng áo mưa bộ 2 mảnh riêng biệt, chống gió tạt lật tà và an toàn khi lái xe tốc độ cao."
                : "Recommend 2-piece split raincoat for safety against strong side winds and tyre catches.";

            advice.MotorbikeWarning = isVi
                ? "⚠️ Đường trơn trượt & bùn văng: Tránh mặc áo mưa cánh dơi trùm đầu xe máy vì dễ bị gió tạt làm loạng choạng. Cẩn thận các vạch kẻ sơn và nắp cống kim loại, giữ khoảng cách phanh gấp đôi."
                : "⚠️ Slick roads & water spray: Avoid poncho-style raincoats that flap over handlebars. Watch out for road paint lines and metal drain covers, double your braking distance.";
            advice.MotorbikeWarningIcon = "\uf73d";
            advice.MotorbikeWarningColor = "#EF4444";
        }
        else if (wind >= 25)
        {
            advice.RaincoatAdvice = isVi ? "Thời tiết khô ráo, gió giật mạnh." : "Dry conditions, strong gusts.";
            advice.MotorbikeWarning = isVi
                ? $"⚠️ Gió giật mạnh ({wind:F0} km/h): Cẩn thận lực cản gió mạnh khi qua các cầu cao hoặc đi cạnh xe tải lớn. Hãy giữ thật chắc tay lái và giảm tốc độ!"
                : $"⚠️ Strong wind gusts ({wind:F0} km/h): Beware of heavy crosswinds when crossing high bridges or passing large trucks. Grip handlebars firmly!";
            advice.MotorbikeWarningIcon = "\uf72e";
            advice.MotorbikeWarningColor = "#F59E0B";
        }
        else if (feelsLike >= 33)
        {
            advice.RaincoatAdvice = isVi ? "Trời tạnh ráo, không cần mang áo mưa." : "Dry and clear, no raincoat needed.";
            advice.MotorbikeWarning = isVi
                ? "⚠️ Cảnh báo sốc nhiệt máy lạnh: Chênh lệch lớn giữa văn phòng máy lạnh (23°C) và mặt đường nhựa (trên 40°C). Hãy nghỉ chân ở sảnh 1-2 phút và khoác áo dài tay trước khi xuất phát."
                : "⚠️ Heat shock caution: Big gap between AC indoor (23°C) and asphalt road (40°C+). Rest 1-2 minutes in the lobby before riding.";
            advice.MotorbikeWarningIcon = "\uf185";
            advice.MotorbikeWarningColor = "#F59E0B";
        }
        else if (feelsLike < 16)
        {
            advice.RaincoatAdvice = isVi ? "Trời khô lạnh, chuẩn bị áo ấm cản gió." : "Dry and chilly, prepare windproof layers.";
            advice.MotorbikeWarning = isVi
                ? "⚠️ Gió lạnh tạt buốt khi lái xe máy: Tốc độ xe làm nhiệt độ cảm nhận giảm thêm 3-5°C. Cài kín cổ áo khoác, đeo găng tay xe máy và nón bảo hiểm có kính chắn gió."
                : "⚠️ Severe windchill on motorbikes: Moving speed drops perceived temp by 3-5°C. Zip up collar, wear riding gloves and visor helmet.";
            advice.MotorbikeWarningIcon = "\uf2dc";
            advice.MotorbikeWarningColor = "#3B82F6";
        }
        else
        {
            advice.RaincoatAdvice = isVi ? "Thời tiết đẹp, đường sá khô ráo." : "Fine weather, roads are dry.";
            advice.MotorbikeWarning = isVi
                ? "✅ Thời tiết lý tưởng: Đường sá khô thoáng, gió mát dịu. Rất thuận tiện để lái xe máy, dạo phố và di chuyển trong ngày."
                : "✅ Ideal conditions: Clean dry roads, mild breeze. Highly pleasant for motorbike commuting and walking.";
            advice.MotorbikeWarningIcon = "\uf21c";
            advice.MotorbikeWarningColor = "#10B981";
        }

        // 6. Tóm tắt nhanh & Chuỗi chia sẻ (Shareable Summary Text)
        advice.QuickSummary = $"{advice.TopClothing} • {advice.BottomClothing}";

        advice.ShareableSummaryText = isVi
            ? $"✨ [OOTD HÔM NAY - {advice.OccasionTitle.ToUpper()}]\n" +
              $"🌡️ Thời tiết: {current.ConditionText} ({current.TemperatureText}) | Cảm giác: {feelsLike:F0}°C\n" +
              $"👔 Phong cách: {advice.FashionStyleTag}\n" +
              $"• Áo: {advice.TopClothing}\n" +
              $"• Quần: {advice.BottomClothing}\n" +
              $"• Áo khoác: {advice.Outerwear}\n" +
              $"• Giày dép: {advice.Footwear}\n" +
              $"• Phụ kiện: {string.Join(", ", accessories.ConvertAll(a => a.Name))}\n" +
              $"🛵 Lưu ý xe máy: {advice.MotorbikeWarning}"
            : $"✨ [TODAY'S OOTD - {advice.OccasionTitle.ToUpper()}]\n" +
              $"🌡️ Weather: {current.ConditionText} ({current.TemperatureText}) | Feels like: {feelsLike:F0}°C\n" +
              $"👔 Style: {advice.FashionStyleTag}\n" +
              $"• Top: {advice.TopClothing}\n" +
              $"• Bottom: {advice.BottomClothing}\n" +
              $"• Layer: {advice.Outerwear}\n" +
              $"• Shoes: {advice.Footwear}\n" +
              $"• Essentials: {string.Join(", ", accessories.ConvertAll(a => a.Name))}\n" +
              $"🛵 Commute note: {advice.MotorbikeWarning}";

        return advice;
    }

    private void ApplyOutfitDetails(OutfitAdvice advice, double feelsLike, bool isRaining, double uv, string occasion, string gender, bool isVi)
    {
        switch (occasion)
        {
            case "School":
                advice.FashionStyleTag = "Campus Preppy / Youth Casual";
                if (feelsLike < 16)
                {
                    advice.Headline = isVi ? "Đồng phục + Hoodie nỉ lót bông & Sneaker ấm" : "Uniform + Fleece Hoodie & Warm Sneakers";
                    advice.TopClothing = gender switch
                    {
                        "Women" => isVi ? "Áo sơ mi/thun đồng phục + Sweater len dệt cổ tim hoặc nỉ hoodie" : "Uniform shirt + V-neck cable knit sweater or hoodie",
                        "Men" => isVi ? "Áo sơ mi/polo đồng phục + Hoodie nỉ dày có mũ giữ ấm" : "Uniform polo/shirt + Heavy fleece hooded sweatshirt",
                        _ => isVi ? "Áo sơ mi/thun đồng phục + Hoodie nỉ lót bông form rộng thoải mái" : "Uniform shirt + Oversized fleece hoodie"
                    };
                    advice.BottomClothing = gender == "Women" && isVi
                        ? "Quần jean ống đứng dày dặn hoặc chân váy dạ dài kèm tất len cao cổ"
                        : (isVi ? "Quần jean xanh dày hoặc quần tây đồng phục ấm áp" : "Heavy denim jeans or warm uniform trousers");
                    advice.Outerwear = isVi ? "Áo khoác gió bomber 2 lớp hoặc áo phao mũ lông cản gió" : "2-layer bomber jacket or down parka with hood";
                    advice.Footwear = isVi ? "Giày sneaker cổ cao kèm tất len cotton dày" : "High-top sneakers with thick cotton socks";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = isVi ? "Áo polo đồng phục + Cardigan dệt kim trẻ trung" : "Polo Uniform + Knit Cardigan & Loafers";
                    advice.TopClothing = isVi ? "Áo polo đồng phục hoặc áo thun dài tay cổ tròn thanh lịch" : "Polo shirt or long-sleeve crewneck tee";
                    advice.BottomClothing = isVi ? "Quần kaki ống suông hoặc quần jean co giãn nhẹ nhàng" : "Straight-leg khakis or flexible denim jeans";
                    advice.Outerwear = isVi ? "Áo khoác gió dù thể thao mỏng hoặc cardigan len dệt nhẹ" : "Light windbreaker jacket or thin knit cardigan";
                    advice.Footwear = isVi ? "Giày thể thao sneaker êm chân, phù hợp di chuyển nhiều" : "Comfortable walking sneakers or canvas shoes";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = isVi ? "Sơ mi cộc tay / Áo thun mát + Quần suông năng động" : "Short-Sleeve Shirt / Tee + Casual Trousers";
                    advice.TopClothing = gender switch
                    {
                        "Women" => isVi ? "Áo thun cotton graphic nhẹ hoặc sơ mi babydoll thoải mái" : "Graphic cotton tee or relaxed babydoll blouse",
                        _ => isVi ? "Áo thun cotton 100% thoáng khí hoặc áo polo ngắn tay" : "100% Cotton breathable tee or short-sleeve polo"
                    };
                    advice.BottomClothing = isVi ? "Quần jean ống rộng hoặc quần vải tây trẻ trung" : "Wide-leg jeans or casual relaxed trousers";
                    advice.Outerwear = isVi ? "Áo sơ mi kẻ caro khoác ngoài hờ tạo điểm nhấn phong cách" : "Oversized plaid shirt worn as light layer";
                    advice.Footwear = isVi ? "Giày sneaker trắng phong cách hoặc giày lười búp bê" : "Classic white sneakers or casual slip-ons";
                }
                else
                {
                    advice.Headline = isVi ? "Đồ thun mỏng mát tối đa + Áo khoác chống nắng UPF 50+" : "Light Cotton Tee + UPF 50+ Sun Protection Jacket";
                    advice.TopClothing = isVi ? "Áo phông cộc tay mỏng nhẹ, màu sáng (trắng, kem, be dịu)" : "Ultra-light short-sleeve tee, light colors (white, beige)";
                    advice.BottomClothing = isVi ? "Quần đũi suông mỏng hoặc quần vải thun thoáng khí" : "Light linen pants or breathable relaxed trousers";
                    advice.Outerwear = isVi ? "Áo khoác chống nắng UPF 50+ bao trùm bàn tay và gáy khi đi đường" : "UPF 50+ UV jacket covering neck and hands";
                    advice.Footwear = isVi ? "Sandal quai hậu thoáng mát hoặc giày lười vải lưới" : "Breathable sandals or mesh slip-on sneakers";
                }
                break;

            case "Casual":
                advice.FashionStyleTag = "Urban Streetwear / Minimalist";
                if (feelsLike < 16)
                {
                    advice.Headline = isVi ? "Set nỉ ấm thời thượng + Áo khoác măng tô hoặc phao ấm" : "Cozy Sweat Set + Trench Coat or Puffer";
                    advice.TopClothing = isVi ? "Áo thun giữ nhiệt Heattech + Sweater nỉ thêu chữ nổi bật" : "Thermal Heattech inner + Embroidered sweater";
                    advice.BottomClothing = isVi ? "Quần jogger nỉ co giãn bo gấu hoặc quần nhung tăm ấm áp" : "Fleece jogger pants or corduroy trousers";
                    advice.Outerwear = isVi ? "Áo khoác dạ dài măng tô hoặc áo phao phom ngắn thời trang" : "Wool trench coat or trendy cropped puffer";
                    advice.Footwear = isVi ? "Giày boots da lộn ấm hoặc sneaker thể thao đế đệm" : "Suede leather ankle boots or chunky sneakers";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = isVi ? "Áo len mỏng / Sơ mi flannel + Quần ống rộng thời trang" : "Light Knit / Flannel Shirt + Wide-Leg Pants";
                    advice.TopClothing = isVi ? "Áo len mỏng cổ tròn hoặc áo sơ mi flannel phong trần" : "Crewneck thin knit sweater or flannel overshirt";
                    advice.BottomClothing = isVi ? "Quần túi hộp cargo dạo phố hoặc quần jean ống loe nhẹ" : "Cargo utility pants or relaxed wide-leg jeans";
                    advice.Outerwear = isVi ? "Áo khoác bóng chày Varsity hoặc áo khoác denim cá tính" : "Varsity baseball jacket or classic denim jacket";
                    advice.Footwear = isVi ? "Giày sneaker thời trang hoặc giày lười loafer hiện đại" : "Trendy sneakers or contemporary loafers";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = isVi ? "Áo phông oversize + Quần short hoặc quần suông thời thượng" : "Oversized Tee + Shorts or Flowy Trousers";
                    advice.TopClothing = gender == "Women" && isVi
                        ? "Áo croptop / baby tee hoặc váy liền chữ A duyên dáng"
                        : (isVi ? "Áo thun oversize 100% cotton thoáng mát, họa tiết tinh tế" : "Oversized 100% cotton tee with minimal graphics");
                    advice.BottomClothing = isVi ? "Quần short đùi năng động hoặc quần vải suông rủ mềm mại" : "Casual utility shorts or flowing wide trousers";
                    advice.Outerwear = isVi ? "Không cần áo khoác, hoặc sơ mi mỏng khoác vai làm phụ kiện" : "No jacket needed, or drape a light shirt over shoulders";
                    advice.Footwear = isVi ? "Giày sneaker thấp cổ hoặc sandal quai da phong cách" : "Low-top sneakers or stylish leather slide sandals";
                }
                else
                {
                    advice.Headline = isVi ? "Đồ vải đũi / lanh siêu mát + Kính râm dạo phố" : "Linen Set / Breathable Cotton + Sunglasses";
                    advice.TopClothing = isVi ? "Áo sơ mi đũi cộc tay mở khuy hoặc áo ba lỗ tanktop thể thao" : "Short-sleeve linen camp shirt or airy tank top";
                    advice.BottomClothing = isVi ? "Quần short đũi tự nhiên hoặc quần lanh ống rộng siêu mát" : "Linen relaxed shorts or breezy wide-leg pants";
                    advice.Outerwear = isVi ? "Áo choàng mỏng chống nắng khi ra đường" : "Light sun cover-up when outdoors";
                    advice.Footwear = isVi ? "Dép sandal quai chéo hoặc giày lười xỏ ngón êm ái" : "Crisscross strap sandals or ergonomic slides";
                }
                break;

            case "Sport":
                advice.FashionStyleTag = "Athleisure Active / Dry-Fit Tech";
                advice.Headline = isVi ? "Bộ đồ thể thao Dry-Fit co giãn 4 chiều + Giày chạy êm" : "4-Way Stretch Dry-Fit Gear + Running Shoes";
                advice.TopClothing = isVi ? "Áo thun thể thao Dry-Fit công nghệ thoát nhiệt, kháng khuẩn" : "Moisture-wicking Dry-Fit athletic technical tee";
                advice.BottomClothing = isVi ? "Quần short chạy bộ 2 lớp có túi tiện ích hoặc quần legging thể thao" : "2-in-1 running shorts with phone pocket or compression tights";
                advice.Outerwear = feelsLike < 20
                    ? (isVi ? "Áo khoác gió thể thao trượt nước siêu nhẹ, cản gió" : "Ultra-light water-resistant athletic windbreaker")
                    : (isVi ? "Không cần áo khoác, ưu tiên thông thoáng cơ thể" : "No jacket needed, prioritize ventilation");
                advice.Footwear = isVi ? "Giày chạy bộ chuyên dụng đệm khí êm ái, bám đường chống trượt" : "Cushioned running shoes with anti-slip traction";
                break;

            case "Travel":
                advice.FashionStyleTag = "Outdoor Explorer / Techwear";
                advice.Headline = isVi ? "Set đồ đa năng tiện lợi + Áo khoác cản gió chống nước" : "Versatile Utility Outfit + All-Weather Shell";
                advice.TopClothing = isVi ? "Áo polo thể thao hoặc áo thun merino co giãn nhanh khô" : "Quick-dry performance polo or merino wool tee";
                advice.BottomClothing = isVi ? "Quần dài dã ngoại co giãn có túi hộp kháng nước nhẹ" : "Stretch outdoor trekking pants with utility pockets";
                advice.Outerwear = isRaining || feelsLike < 22
                    ? (isVi ? "Áo khoác gió Gore-Tex 3 lớp kháng nước tuyệt đối, có mũ sâu" : "Waterproof 3-layer shell jacket with deep hood")
                    : (isVi ? "Áo khoác dù siêu nhẹ gập gọn vào balo" : "Packable ultra-lightweight wind shell");
                advice.Footwear = isVi ? "Giày trekking dã ngoại đế gai chống trượt hoặc sneaker bám đất" : "Trail running shoes or durable trekking sneakers";
                break;

            default: // Work
                advice.FashionStyleTag = "Smart Casual / Modern Office";
                if (feelsLike < 16)
                {
                    advice.Headline = isVi ? "Sơ mi + Áo len cổ lọ / gile & Áo măng tô dạ lịch lãm" : "Shirt + Turtleneck/Vest & Tailored Overcoat";
                    advice.TopClothing = gender == "Women" && isVi
                        ? "Áo giữ nhiệt + Sơ mi lụa hoặc đầm len ôm thanh lịch"
                        : (isVi ? "Áo sơ mi dài tay + Áo len dệt kim cổ tim/cổ lọ giữ ấm ngực" : "Dress shirt + V-neck or turtleneck wool sweater");
                    advice.BottomClothing = isVi ? "Quần tây dạ dày hoặc quần âu ống suông giữ phom chuẩn" : "Wool blend dress trousers with sharp crease";
                    advice.Outerwear = isVi ? "Áo khoác măng tô dáng dài hoặc áo blazer dạ 2 lớp sang trọng" : "Tailored wool overcoat or structured winter blazer";
                    advice.Footwear = isVi ? "Giày tây Oxford da bóng hoặc giày cao gót mũi nhọn da êm" : "Leather Oxford shoes or warm stylish dress shoes";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = isVi ? "Áo sơ mi công sở + Blazer nhẹ nhàng & Quần âu thanh lịch" : "Dress Shirt + Relaxed Blazer & Chinos";
                    advice.TopClothing = isVi ? "Áo sơ mi dài tay cao cấp vải bamboo hoặc áo blouse trang nhã" : "Premium bamboo dress shirt or elegant silk blouse";
                    advice.BottomClothing = isVi ? "Quần âu may đo hoặc chân váy bút chì công sở chỉn chu" : "Tailored chinos or formal office trousers";
                    advice.Outerwear = isVi ? "Áo khoác blazer mỏng 1 lớp hoặc áo cardigan len dệt công sở" : "Unlined tailored blazer or fine-gauge cardigan";
                    advice.Footwear = isVi ? "Giày lười Loafer da êm hoặc giày búp bê công sở" : "Classic leather loafers or dress slip-ons";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = isVi ? "Sơ mi ngắn tay / Polo công sở + Quần tây thoáng mát" : "Short-Sleeve Shirt / Smart Polo + Light Trousers";
                    advice.TopClothing = isVi ? "Áo polo dệt kim đứng phom hoặc áo sơ mi cộc tay vải sợi sen" : "Knit dress polo or short-sleeve breathable shirt";
                    advice.BottomClothing = isVi ? "Quần tây ống đứng chất vải mát hoặc quần kaki co giãn" : "Lightweight dress slacks or smart stretch khakis";
                    advice.Outerwear = isVi ? "Để sẵn áo khoác cardigan mỏng tại văn phòng phòng máy lạnh" : "Keep a light cardigan at office for cold AC";
                    advice.Footwear = isVi ? "Giày tây công sở nhẹ chân hoặc giày lười da mềm" : "Breathable leather loafers or modern dress shoes";
                }
                else
                {
                    advice.Headline = isVi ? "Sơ mi cộc tay lụa mát + Quần âu mỏng nhẹ & Áo chống nắng" : "Breathable Silk-Cotton Shirt + Slacks & UV Layer";
                    advice.TopClothing = isVi ? "Áo sơ mi ngắn tay chất liệu modal/lụa mát, chống nhăn" : "Wrinkle-resistant short-sleeve modal/silk shirt";
                    advice.BottomClothing = isVi ? "Quần âu mỏng nhẹ, cạp chun thoải mái khi ngồi làm việc" : "Ultra-light formal slacks with flexible waistband";
                    advice.Outerwear = isVi ? "Áo khoác chống nắng UPF 50+ khi đi đường, cởi ra khi vào sảnh" : "UPF 50+ sun jacket for commute, remove indoor";
                    advice.Footwear = isVi ? "Giày lười da có đục lỗ thoáng khí hoặc giày bệt êm ái" : "Perforated leather loafers or cushioned flats";
                }
                break;
        }
    }
}
