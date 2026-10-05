using System;
using System.Collections.Generic;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ gợi ý trang phục thông minh "Hôm Nay Mặc Gì?" (OOTD - Outfit Of The Day)
/// Phân tích nhiệt độ cảm nhận (FeelsLike), mưa, gió, bức xạ UV, chất lượng không khí (AQI),
/// kết hợp với mục đích di chuyển (Công sở, Đi học, Dạo phố) và đặc thù đi xe máy tại Việt Nam.
/// </summary>
public class OutfitAdvisorService
{
    public OutfitAdvice GenerateOutfitAdvice(CurrentWeatherDisplay current, OpenMeteoResponse? rawData, string occasion = "Work")
    {
        double temp = current.TemperatureValue;
        double feelsLike = temp;

        // Trích xuất nhiệt độ cảm nhận nếu có dạng số
        if (!string.IsNullOrEmpty(current.FeelsLikeText))
        {
            string clean = current.FeelsLikeText.Replace("Cảm giác:", "").Replace("Cảm giác như", "").Replace("°C", "").Replace("°", "").Trim();
            if (double.TryParse(clean, out double parsedFeels))
            {
                feelsLike = parsedFeels;
            }
        }

        // Kiểm tra trời mưa
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

        // Chỉ số UV
        double uv = 0;
        if (!string.IsNullOrEmpty(current.UvIndexText))
        {
            double.TryParse(current.UvIndexText.Trim(), out uv);
        }

        // Tốc độ gió
        double wind = 10;
        if (!string.IsNullOrEmpty(current.WindText))
        {
            string wStr = current.WindText.Replace("km/h", "").Replace("m/s", "").Trim();
            double.TryParse(wStr, out wind);
        }

        int aqi = current.AqiValue;

        var advice = new OutfitAdvice
        {
            Occasion = occasion
        };

        // 1. Tiêu đề dịp & Tình trạng nhiệt
        string thermalHeadline;
        string thermalNotice;
        string thermalColor;

        if (feelsLike < 16)
        {
            thermalHeadline = "Trời Rét Lạnh • Cần Giữ Ấm Kỹ";
            thermalNotice = $"Cảm giác {feelsLike:F0}°C khá buốt lạnh. Hãy mặc đủ ấm để bảo vệ cổ họng và lồng ngực.";
            thermalColor = "#3B82F6"; // Xanh dương
        }
        else if (feelsLike < 22)
        {
            thermalHeadline = "Khí Trời Se Lạnh • Dễ Chịu";
            thermalNotice = $"Cảm giác {feelsLike:F0}°C mát mẻ và se lạnh. Rất thích hợp diện áo khoác nhẹ hoặc cardigan.";
            thermalColor = "#06B6D4"; // Xanh ngọc
        }
        else if (feelsLike <= 28)
        {
            thermalHeadline = "Thời Tiết Lý Tưởng • Mát Mẻ";
            thermalNotice = $"Cảm giác {feelsLike:F0}°C cực kỳ ôn hòa, dễ chịu. Thoải mái phối trang phục yêu thích.";
            thermalColor = "#10B981"; // Xanh lá
        }
        else if (feelsLike <= 33)
        {
            thermalHeadline = "Khí Hậu Khá Nóng & Oi Nhẹ";
            thermalNotice = $"Cảm giác {feelsLike:F0}°C hơi bức bối vào giữa trưa. Ưu tiên chất vải cotton mỏng thấm mồ hôi.";
            thermalColor = "#F59E0B"; // Vàng cam
        }
        else
        {
            thermalHeadline = "Nắng Nóng Gay Gắt • Đề Phòng Sốc Nhiệt";
            thermalNotice = $"Cảm giác {feelsLike:F0}°C rất oi bức. Bắt buộc chống nắng toàn diện và uống nhiều nước.";
            thermalColor = "#EF4444"; // Đỏ
        }

        advice.ThermalComfortNotice = $"{thermalHeadline} — {thermalNotice}";
        advice.ThermalTagColor = thermalColor;

        // 2. Gợi ý trang phục theo từng dịp
        switch (occasion)
        {
            case "School":
                advice.OccasionTitle = "🎒 Đi học / Sinh viên";
                if (feelsLike < 16)
                {
                    advice.Headline = "Đồng phục + Áo nỉ hoodie lót bông & Giày ấm";
                    advice.TopClothing = "Áo thun/sơ mi đồng phục + Áo nỉ hoodie lót bông hoặc sweater dày";
                    advice.BottomClothing = "Quần jean dày hoặc quần tây đồng phục ấm áp";
                    advice.Outerwear = "Áo khoác gió bomber hoặc áo phao mũ lông";
                    advice.Footwear = "Giày thể thao sneaker cổ cao kèm tất len ấm";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = "Đồng phục trẻ trung + Áo cardigan / khoác bomber nhẹ";
                    advice.TopClothing = "Áo polo đồng phục hoặc áo thun dài tay năng động";
                    advice.BottomClothing = "Quần jean co giãn hoặc quần kaki ống suông";
                    advice.Outerwear = "Áo khoác gió dù nhẹ hoặc áo cardigan mỏng";
                    advice.Footwear = "Giày thể thao sneaker êm chân, thoải mái di chuyển";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = "Áo thun polo năng động + Quần jean thoải mái";
                    advice.TopClothing = "Áo thun đồng phục cotton hoặc áo phông graphic thoáng mát";
                    advice.BottomClothing = "Quần jean năng động hoặc chân váy xếp ly dài";
                    advice.Outerwear = "Áo sơ mi caro khoác ngoài tạo điểm nhấn";
                    advice.Footwear = "Giày sneaker thể thao hoặc giày lười búp bê";
                }
                else if (feelsLike <= 33)
                {
                    advice.Headline = "Trang phục cotton mát nhẹ + Nón lưỡi trai";
                    advice.TopClothing = "Áo thun oversize 100% cotton mỏng nhẹ, thấm hút mồ hôi";
                    advice.BottomClothing = "Quần kaki mỏng hoặc quần vải suông thoáng khí";
                    advice.Outerwear = "Áo khoác chống nắng có mũ che gáy khi đi đường";
                    advice.Footwear = "Giày sneaker vải lưới thoáng khí hoặc sandal học sinh";
                }
                else
                {
                    advice.Headline = "Đồ thun mỏng mát tối đa + Áo khoác chống tia UV";
                    advice.TopClothing = "Áo phông cộc tay mỏng nhẹ, màu sáng (trắng, kem, be)";
                    advice.BottomClothing = "Quần vải đũi mỏng hoặc quần vải thun thoáng khí";
                    advice.Outerwear = "Áo khoác chống nắng UPF 50+ bao trùm bàn tay và gáy";
                    advice.Footwear = "Giày lười vải nhẹ hoặc sandal quai hậu thoáng mát";
                }
                break;

            case "Casual":
                advice.OccasionTitle = "🏃 Thể thao & Dạo phố";
                if (feelsLike < 16)
                {
                    advice.Headline = "Set nỉ thể thao ấm + Áo gió 2 lớp cản gió lạnh";
                    advice.TopClothing = "Áo thun giữ nhiệt heattech + Áo nỉ sweater thể thao";
                    advice.BottomClothing = "Quần jogger nỉ co giãn dày dặn hoặc legging lót lông";
                    advice.Outerwear = "Áo khoác gió thể thao trượt nước chống gió buốt";
                    advice.Footwear = "Giày chạy bộ thể thao đế bám, chống trượt";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = "Áo hoodie trẻ trung + Quần jogger hoặc kaki túi hộp";
                    advice.TopClothing = "Áo nỉ mỏng, hoodie trơn hoặc thun dài tay cổ tròn";
                    advice.BottomClothing = "Quần jogger bo gấu hoặc quần túi hộp dạo phố thời trang";
                    advice.Outerwear = "Áo khoác bóng chày varsity hoặc áo gió thể thao mỏng";
                    advice.Footwear = "Giày sneaker thời trang hoặc giày chạy êm ái";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = "Áo phông oversize + Quần short hoặc quần suông thời thượng";
                    advice.TopClothing = "Áo phông cotton thoáng khí hoặc áo ba lỗ thể thao khỏe khoắn";
                    advice.BottomClothing = "Quần short thể thao năng động hoặc quần cargo dạo phố";
                    advice.Outerwear = "Áo sơ mi denim mở cúc hoặc không cần áo khoác";
                    advice.Footwear = "Giày chạy thể thao đệm khí hoặc sandal phong cách";
                }
                else if (feelsLike <= 33)
                {
                    advice.Headline = "Đồ thể thao Dry-Fit siêu thoáng mát + Kính râm";
                    advice.TopClothing = "Áo thun dry-fit thể thao co giãn 4 chiều, nhanh khô";
                    advice.BottomClothing = "Quần short đùi chạy bộ hoặc quần lửng thoáng khí";
                    advice.Outerwear = "Áo gió siêu mỏng che nắng khi đạp xe / chạy bộ";
                    advice.Footwear = "Giày chạy bộ mặt lưới thoáng khí, vớ ngắn cotton";
                }
                else
                {
                    advice.Headline = "Trang phục siêu nhẹ thoát nhiệt + Che chắn nắng tuyệt đối";
                    advice.TopClothing = "Áo sát nách / ba lỗ thể thao hoặc thun lụa băng làm mát";
                    advice.BottomClothing = "Quần short dù mỏng nhẹ, tối ưu giải nhiệt cơ thể";
                    advice.Outerwear = "Áo chống nắng thể thao chuyên dụng làm mát tức thì";
                    advice.Footwear = "Giày thể thao siêu nhẹ hoặc dép quai chéo dạo mát";
                }
                break;

            case "Work":
            default:
                advice.OccasionTitle = "💼 Công sở / Đi làm";
                if (feelsLike < 16)
                {
                    advice.Headline = "Sơ mi + Áo len dệt kim + Áo dạ / Blazer dày trang nhã";
                    advice.TopClothing = "Áo sơ mi dài tay phối áo len dệt kim cổ V hoặc cổ tròn";
                    advice.BottomClothing = "Quần âu/tây dày giữ ấm hoặc chân váy dạ dài qua gối";
                    advice.Outerwear = "Áo khoác măng tô dạ dáng dài hoặc blazer dạ ép thanh lịch";
                    advice.Footwear = "Giày da oxford/derby kín cổ hoặc boots da gót vuông";
                }
                else if (feelsLike < 22)
                {
                    advice.Headline = "Sơ mi dài tay + Blazer công sở thanh lịch & lịch lãm";
                    advice.TopClothing = "Áo sơ mi dài tay chất liệu cotton chống nhăn hoặc lụa cao cấp";
                    advice.BottomClothing = "Quần tây ống đứng hoặc chân váy bút chì công sở";
                    advice.Outerwear = "Áo vest blazer mỏng nhẹ hoặc cardigan len tăm";
                    advice.Footwear = "Giày da bóng hoặc giày cao gót mũi nhọn bọc kín";
                }
                else if (feelsLike <= 28)
                {
                    advice.Headline = "Sơ mi công sở nhẹ nhàng + Quần âu thoáng khí";
                    advice.TopClothing = "Sơ mi cotton thoáng mát hoặc áo polo công sở gọn gàng";
                    advice.BottomClothing = "Quần âu co giãn nhẹ hoặc chân váy chữ A thanh thoát";
                    advice.Outerwear = "Áo blazer mỏng dự phòng khi ngồi văn phòng điều hòa";
                    advice.Footwear = "Giày da lười (loafer) hoặc giày cao gót đế êm";
                }
                else if (feelsLike <= 33)
                {
                    advice.Headline = "Sơ mi lụa/đũi thoáng mát + Quần âu mỏng thấm hút";
                    advice.TopClothing = "Áo sơ mi cộc tay chất liệu lụa modal, đũi mát hoặc cotton lạnh";
                    advice.BottomClothing = "Quần tây vải mỏng nhẹ hoặc chân váy xếp ly thoáng mát";
                    advice.Outerwear = "Mang sẵn áo khoác mỏng tại bàn làm việc tránh lạnh máy lạnh";
                    advice.Footwear = "Giày da đục lỗ thoáng khí hoặc sandal công sở kín ngón";
                }
                else
                {
                    advice.Headline = "Sơ mi màu sáng siêu mát + Áo chống nắng khi ra đường";
                    advice.TopClothing = "Sơ mi mỏng màu sáng (trắng, xanh nhạt) giải nhiệt tối đa";
                    advice.BottomClothing = "Quần âu vải lanh/poly-rayon mỏng nhẹ, không ôm sát";
                    advice.Outerwear = "Áo khoác chống nắng toàn thân khi di chuyển, blazer trong phòng họp";
                    advice.Footwear = "Giày lười da mềm thoáng chân, mang tất ngắn hút ẩm";
                }
                break;
        }

        // 3. Phụ kiện & Vật dụng cần mang (Checklist)
        var accessories = new List<OutfitAccessoryItem>();

        // Mưa hoặc xác suất mưa cao
        if (isRaining)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Áo mưa bộ cao cấp",
                Reason = "Trời có mưa, để sẵn áo mưa bộ trong cốp xe máy",
                IconGlyph = "\uf73d",
                AccentColor = "#38BDF8",
                IsEssential = true
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Ô (Dù) gấp gọn",
                Reason = "Tiện lợi khi đi bộ từ bãi đỗ xe vào tòa nhà",
                IconGlyph = "\uf0e9",
                AccentColor = "#60A5FA",
                IsEssential = true
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Túi bọc giày / Bọc balo",
                Reason = "Bảo vệ tài liệu, laptop và giày không bị ướt bẩn",
                IconGlyph = "\uf549",
                AccentColor = "#818CF8",
                IsEssential = false
            });
        }
        else if (rainProb >= 25)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Ô (Dù) gấp dự phòng",
                Reason = $"Xác suất mưa khoảng {rainProb}%, nên mang theo phòng ngừa",
                IconGlyph = "\uf0e9",
                AccentColor = "#38BDF8",
                IsEssential = false
            });
        }

        // Chỉ số UV cao
        if (uv >= 6.0)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Kính râm chống tia UV400",
                Reason = $"Chỉ số UV {uv:F1} (Rất Cao), bảo vệ võng mạc mắt khi lái xe",
                IconGlyph = "\uf000",
                AccentColor = "#F59E0B",
                IsEssential = true
            });
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Kem chống nắng SPF 50+",
                Reason = "Thoa trước khi ra ngoài 20 phút để chống sạm da và tia tử ngoại",
                IconGlyph = "\uf185",
                AccentColor = "#FBBF24",
                IsEssential = true
            });
        }
        else if (uv >= 3.0 && current.IsDay)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Kính râm / Nón rộng vành",
                Reason = $"Chỉ số UV {uv:F1}, chống chói mắt khi lưu thông ban ngày",
                IconGlyph = "\uf000",
                AccentColor = "#F59E0B",
                IsEssential = false
            });
        }

        // Không khí ô nhiễm / Bụi mịn AQI
        if (aqi >= 150)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Khẩu trang N95 chống bụi mịn",
                Reason = $"AQI {aqi} mức Xấu: Bụi mịn PM2.5 cao, bắt buộc đeo N95 khi ra đường",
                IconGlyph = "\uf6cf",
                AccentColor = "#EF4444",
                IsEssential = true
            });
        }
        else if (aqi >= 100)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Khẩu trang y tế / kháng khuẩn",
                Reason = $"AQI {aqi} cảnh báo ô nhiễm nhẹ, hạn chế hít khói bụi đường phố",
                IconGlyph = "\uf6cf",
                AccentColor = "#F97316",
                IsEssential = false
            });
        }
        else
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Khẩu trang vải thoáng khí",
                Reason = "Không khí trong lành, đeo khẩu trang nhẹ cản bụi đường",
                IconGlyph = "\uf6cf",
                AccentColor = "#10B981",
                IsEssential = false
            });
        }

        // Cần bình nước khi nóng hoặc hanh khô
        if (feelsLike >= 30)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Bình nước cá nhân",
                Reason = "Thời tiết nóng bức, uống đều đặn để tránh mất nước & say nắng",
                IconGlyph = "\uf57b",
                AccentColor = "#06B6D4",
                IsEssential = false
            });
        }

        // Gió to
        if (wind >= 22)
        {
            accessories.Add(new OutfitAccessoryItem
            {
                Name = "Áo khoác gió cản gió",
                Reason = $"Gió giật {wind:F0} km/h, áo khoác gió giúp ấm ngực khi chạy xe",
                IconGlyph = "\uf72e",
                AccentColor = "#6366F1",
                IsEssential = false
            });
        }

        advice.Accessories = accessories;

        // 4. Cảnh báo đi xe máy tại Việt Nam
        if (isRaining)
        {
            advice.MotorbikeWarning = "⚠️ Đường trơn trượt & bùn văng: Nên dùng áo mưa bộ, tránh mặc áo mưa cánh dơi trùm đầu xe máy vì dễ bị gió tạt làm loạng choạng và cuốn vào nan hoa bánh xe. Giữ khoảng cách phanh xa gấp đôi.";
            advice.MotorbikeWarningIcon = "\uf73d";
            advice.MotorbikeWarningColor = "#EF4444";
        }
        else if (wind >= 25)
        {
            advice.MotorbikeWarning = $"⚠️ Gió giật mạnh ({wind:F0} km/h): Cẩn thận nguy cơ cản gió mạnh khi qua các cầu cao (cầu Chương Dương, Thuận Phước, cầu Sài Gòn...) hoặc đi cạnh xe buýt, xe tải lớn. Hãy giữ thật chắc tay lái!";
            advice.MotorbikeWarningIcon = "\uf72e";
            advice.MotorbikeWarningColor = "#F59E0B";
        }
        else if (feelsLike >= 33)
        {
            advice.MotorbikeWarning = "⚠️ Cảnh báo sốc nhiệt máy lạnh: Chênh lệch nhiệt độ lớn giữa văn phòng máy lạnh (22-24°C) và mặt đường nhựa (có thể lên tới 40°C). Hãy nghỉ chân ở sảnh 1-2 phút và mặc áo dài tay trước khi phóng xe ra đường.";
            advice.MotorbikeWarningIcon = "\uf185";
            advice.MotorbikeWarningColor = "#F59E0B";
        }
        else if (feelsLike < 16)
        {
            advice.MotorbikeWarning = "⚠️ Gió lạnh tạt buốt khi lái xe máy: Tốc độ xe làm nhiệt độ cảm nhận giảm thêm 3-5°C. Hãy cài kín cổ áo khoác, đeo găng tay và bịt kín tai để không bị cảm lạnh.";
            advice.MotorbikeWarningIcon = "\uf2dc";
            advice.MotorbikeWarningColor = "#3B82F6";
        }
        else
        {
            advice.MotorbikeWarning = "✅ Thời tiết lý tưởng: Đường sá khô ráo, gió mát dịu. Rất thuận tiện để di chuyển xe máy, dạo phố hoặc đi lại trong ngày.";
            advice.MotorbikeWarningIcon = "\uf21c";
            advice.MotorbikeWarningColor = "#10B981";
        }

        // Tóm tắt nhanh
        advice.QuickSummary = $"{advice.TopClothing} • {advice.BottomClothing}. {accessories[0].Name}";

        return advice;
    }
}
