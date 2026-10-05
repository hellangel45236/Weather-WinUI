using System;
using System.Collections.Generic;
using System.Linq;
using WeatherApp.Helpers;
using WeatherApp.Models;

namespace WeatherApp.Services;

public class UrbanFloodService
{
    public UrbanFloodWarning EvaluateFloodRisk(double latitude, double longitude, string locationName, OpenMeteoResponse? weatherData, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.Now;
        var warning = new UrbanFloodWarning();

        // 1. Nhận diện khu vực đô thị
        string locLower = (locationName ?? string.Empty).ToLowerInvariant();
        bool isHcm = locLower.Contains("hồ chí minh") || locLower.Contains("ho chi minh") || locLower.Contains("sài gòn") || 
                     locLower.Contains("sai gon") || locLower.Contains("thủ đức") || locLower.Contains("thu duc") ||
                     locLower.Contains("bình thạnh") || locLower.Contains("gò vấp") || locLower.Contains("tân bình") ||
                     locLower.Contains("quận ") || locLower.Contains("district ") ||
                     (latitude >= 10.3 && latitude <= 11.2 && longitude >= 106.3 && longitude <= 107.1);

        bool isHanoi = locLower.Contains("hà nội") || locLower.Contains("ha noi") || locLower.Contains("hanoi") ||
                      locLower.Contains("ba đình") || locLower.Contains("hoàn kiếm") || locLower.Contains("đống đa") ||
                      locLower.Contains("cầu giấy") || locLower.Contains("thanh xuân") || locLower.Contains("hà đông") ||
                      locLower.Contains("nam từ liêm") || locLower.Contains("bắc từ liêm") || locLower.Contains("hoàng mai") ||
                      (latitude >= 20.8 && latitude <= 21.4 && longitude >= 105.5 && longitude <= 106.1);

        bool isCanTho = locLower.Contains("cần thơ") || locLower.Contains("can tho") ||
                        (latitude >= 9.8 && latitude <= 10.3 && longitude >= 105.4 && longitude <= 106.0);

        bool isDaNang = locLower.Contains("đà nẵng") || locLower.Contains("da nang") ||
                        (latitude >= 15.8 && latitude <= 16.3 && longitude >= 108.0 && longitude <= 108.4);

        bool isHaiPhong = locLower.Contains("hải phòng") || locLower.Contains("hai phong") ||
                          (latitude >= 20.6 && latitude <= 21.1 && longitude >= 106.5 && longitude <= 107.0);

        // 2. Phân tích lượng mưa từ dữ liệu thời tiết thực
        double currentRain = weatherData?.Current?.Precipitation ?? 0.0;
        double rainProbability = weatherData?.Daily?.PrecipitationProbabilityMax?.FirstOrDefault() ?? 0;
        double nextHoursMaxRain = currentRain;

        if (weatherData?.Hourly?.Precipitation != null && weatherData.Hourly.Precipitation.Count > 0)
        {
            // Lấy lượng mưa tối đa trong 3 giờ tới
            int hourIndex = now.Hour;
            var upcomingRain = weatherData.Hourly.Precipitation.Skip(hourIndex).Take(4).ToList();
            if (upcomingRain.Count > 0)
            {
                nextHoursMaxRain = Math.Max(currentRain, upcomingRain.Max());
            }
        }

        warning.RainIntensity = Math.Max(currentRain, nextHoursMaxRain);

        // Đánh giá tình trạng thoát nước
        if (warning.RainIntensity >= 45.0)
        {
            warning.RainDrainageStatus = "Mưa xối xả cực đoan - Quá tải nghiêm trọng hệ thống thoát nước!";
            warning.RainDrainageColor = "#EF4444";
        }
        else if (warning.RainIntensity >= 25.0)
        {
            warning.RainDrainageStatus = "Mưa lớn dồn dập - Thoát nước chậm, nguy cơ ngập cục bộ.";
            warning.RainDrainageColor = "#EA580C";
        }
        else if (warning.RainIntensity >= 10.0)
        {
            warning.RainDrainageStatus = "Mưa vừa - Hệ thống cống thoát đang vận hành hết công suất.";
            warning.RainDrainageColor = "#F59E0B";
        }
        else
        {
            warning.RainDrainageStatus = "Hệ thống cống và kênh rạch thông thoáng, thoát nước tốt.";
            warning.RainDrainageColor = "#10B981";
        }

        // 3. Tính toán Triều Cường (Âm Lịch & Bán Nhật Triều) cho TP.HCM & ĐBSCL
        var lunar = VietnameseLunarHelper.ConvertSolarToLunar(now);
        int lDay = lunar.Day;
        int lMonth = lunar.Month;

        if (isHcm || isCanTho)
        {
            warning.HasTideData = true;
            warning.CityType = isHcm ? "Hcm" : "CanTho";
            warning.CityDisplayName = isHcm ? "TP. Hồ Chí Minh" : "Cần Thơ";

            // Kỳ triều cường xảy ra quanh ngày mùng 1-3 và ngày rằm 15-18 âm lịch
            bool isSpringTideNewMoon = (lDay >= 29 || lDay <= 4);
            bool isSpringTideFullMoon = (lDay >= 14 && lDay <= 19);
            bool isSpringTide = isSpringTideNewMoon || isSpringTideFullMoon;

            // Kỳ triều kém xảy ra quanh ngày mùng 7-10 và 21-24 âm lịch
            bool isNeapTide = (lDay >= 7 && lDay <= 11) || (lDay >= 21 && lDay <= 25);

            // Tháng 8, 9, 10, 11 Âm lịch là mùa gió chướng đẩy đỉnh triều cao nhất năm
            bool isPeakTideSeason = (lMonth >= 8 && lMonth <= 11);

            double estimatedMaxTide = 1.30;
            if (isSpringTide)
            {
                estimatedMaxTide = isPeakTideSeason ? 1.72 : 1.65;
                // Đỉnh triều các ngày chính mùng 1, 2 hoặc 15, 16
                if (lDay == 1 || lDay == 2 || lDay == 15 || lDay == 16)
                {
                    estimatedMaxTide += 0.05;
                }
                warning.TidePhaseText = isSpringTideFullMoon 
                    ? $"Kỳ Triều Cường Rằm (Tháng {lMonth} Âm lịch)" 
                    : $"Kỳ Triều Cường Đầu Tháng (Tháng {lMonth} Âm lịch)";
            }
            else if (isNeapTide)
            {
                estimatedMaxTide = 1.25;
                warning.TidePhaseText = "Kỳ Triều Kém (Mực nước sông thấp)";
            }
            else
            {
                estimatedMaxTide = isPeakTideSeason ? 1.52 : 1.44;
                warning.TidePhaseText = "Kỳ Triều Chuyển Tiếp (Bình thường)";
            }

            warning.CurrentTideLevel = estimatedMaxTide;
            warning.CurrentTideLevelDisplay = $"{estimatedMaxTide:F2} m ({warning.TideAlertBadge})";

            // Giờ đỉnh triều (2 đỉnh mỗi ngày cách nhau ~12 tiếng)
            int morningPeakH = 5 + ((lDay % 15) * 12) / 60;
            int morningPeakM = 30 + ((lDay % 5) * 8);
            if (morningPeakM >= 60) { morningPeakH++; morningPeakM -= 60; }

            int eveningPeakH = 17 + ((lDay % 15) * 12) / 60;
            int eveningPeakM = 15 + ((lDay % 5) * 8);
            if (eveningPeakM >= 60) { eveningPeakH++; eveningPeakM -= 60; }

            warning.TidePeakMorning = $"{morningPeakH:D2}:{morningPeakM:D2} - {morningPeakH + 1:D2}:{morningPeakM + 30:D2}";
            warning.TidePeakEvening = $"{eveningPeakH:D2}:{eveningPeakM:D2} - {eveningPeakH + 1:D2}:{eveningPeakM + 30:D2}";

            // Kiểm tra xem hiện tại có đang trong khung giờ đỉnh triều không
            double curHourVal = now.Hour + now.Minute / 60.0;
            double mPeakVal = morningPeakH + morningPeakM / 60.0;
            double ePeakVal = eveningPeakH + eveningPeakM / 60.0;

            bool isPeakMorningNow = (curHourVal >= mPeakVal - 0.75 && curHourVal <= mPeakVal + 1.5);
            bool isPeakEveningNow = (curHourVal >= ePeakVal - 0.75 && curHourVal <= ePeakVal + 1.5);
            warning.IsCurrentlyPeakTide = isPeakMorningNow || isPeakEveningNow;

            warning.PeakTideMeters = estimatedMaxTide;

            // v3.0 Beta: Tính toán 24 điểm mô phỏng sóng bán nhật triều trong ngày (Sine Wave Curve)
            double minTide = 0.85;
            double amplitude = (estimatedMaxTide - minTide) / 2.0;
            double meanLevel = minTide + amplitude;

            warning.TideCurve24h.Clear();
            for (int h = 0; h < 24; h++)
            {
                double phase12 = 2.0 * Math.PI * (h - mPeakVal) / 12.0;
                double phase24 = 2.0 * Math.PI * (h - ePeakVal) / 24.0;
                double level = meanLevel + (amplitude * Math.Cos(phase12)) + (0.04 * Math.Cos(phase24));
                level = Math.Max(0.80, Math.Min(estimatedMaxTide, level));
                warning.TideCurve24h.Add(new TideHourlyPoint
                {
                    Hour = h,
                    LevelMeters = Math.Round(level, 2),
                    IsCurrentHour = (h == now.Hour),
                    IsPeak = (h == morningPeakH || h == eveningPeakH)
                });
            }

            // Tính thời gian đếm ngược đến đỉnh triều tiếp theo
            double nextPeakTimeVal;
            string nextPeakTimeStr;
            if (curHourVal <= mPeakVal)
            {
                nextPeakTimeVal = mPeakVal;
                nextPeakTimeStr = $"{morningPeakH:D2}:{morningPeakM:D2}";
            }
            else if (curHourVal <= ePeakVal)
            {
                nextPeakTimeVal = ePeakVal;
                nextPeakTimeStr = $"{eveningPeakH:D2}:{eveningPeakM:D2}";
            }
            else
            {
                nextPeakTimeVal = mPeakVal + 24.0;
                nextPeakTimeStr = $"{morningPeakH:D2}:{morningPeakM:D2} (Sáng mai)";
            }

            double diffHoursTotal = Math.Max(0, nextPeakTimeVal - curHourVal);
            int diffH = (int)diffHoursTotal;
            int diffM = (int)((diffHoursTotal - diffH) * 60);
            if (diffH == 0 && diffM <= 15)
            {
                warning.NextPeakCountdown = $"⚠️ Đang diễn ra đỉnh triều ({nextPeakTimeStr})";
            }
            else
            {
                warning.NextPeakCountdown = $"Đỉnh kế tiếp: {nextPeakTimeStr} (còn ~{diffH}h {diffM:D2}p)";
            }

            if (warning.IsCurrentlyPeakTide)
            {
                warning.TideStatusText = estimatedMaxTide >= 1.60 
                    ? "Đang trong khung giờ Đỉnh Triều Cường! Mực nước tràn bờ." 
                    : "Đang trong khung giờ đỉnh triều trong ngày.";
            }
            else if (curHourVal < mPeakVal || (curHourVal > mPeakVal + 1.5 && curHourVal < ePeakVal))
            {
                warning.TideStatusText = "Mực nước sông đang trong chu kỳ dâng.";
            }
            else
            {
                warning.TideStatusText = "Triều đang rút thuận lợi cho việc thoát nước.";
            }

            // Đánh giá Rủi ro Tổng hợp (Mưa kết hợp Triều Cường)
            if (estimatedMaxTide >= 1.60 && warning.RainIntensity >= 20.0)
            {
                warning.RiskLevel = 3;
                warning.Headline = "Báo Động Kép: Mưa Lớn Trùng Đỉnh Triều Cường Vượt BĐ 3!";
                warning.Summary = $"Triều cường sông Sài Gòn dâng cao {estimatedMaxTide:F2}m kết hợp mưa dồn dập khiến nước không thể thoát ra sông. Ngập sâu diện rộng 30 - 60cm tại các quận ven sông.";
            }
            else if (estimatedMaxTide >= 1.60)
            {
                warning.RiskLevel = 2;
                warning.Headline = $"Cảnh Báo Triều Cường Đỉnh Điểm Đạt {estimatedMaxTide:F2}m (Vượt BĐ 3)";
                warning.Summary = $"Mực nước trạm Phú An / Nhà Bè vượt mức Báo động 3. Ngập cục bộ 20 - 45cm trên các tuyến đường ven sông, rạch trong khung giờ {warning.TidePeakEvening}.";
            }
            else if (warning.RainIntensity >= 30.0)
            {
                warning.RiskLevel = 2;
                warning.Headline = "Cảnh Báo Ngập Đô Thị Do Mưa Lớn Dồn Dập";
                warning.Summary = $"Cường độ mưa dự báo đạt {warning.RainIntensity:F1} mm/h vượt khả năng tiếp nhận của hệ thống cống nội đô. Các vùng trũng thấp có nguy cơ ngập 20 - 35cm.";
            }
            else if (warning.RainIntensity >= 15.0 || estimatedMaxTide >= 1.50)
            {
                warning.RiskLevel = 1;
                warning.Headline = "Nguy Cơ Ngập Nhẹ Tại Một Số Tuyến Đường Trũng";
                warning.Summary = "Mưa rào rải rác hoặc triều dâng mấp mé bờ kè. Một số đoạn đường trũng thấp bắt đầu đọng nước, di chuyển cần chú ý quan sát.";
            }
            else
            {
                warning.RiskLevel = 0;
                warning.Headline = "Tình Hình An Toàn - Đường Sá Khô Ráo & Thông Thoáng";
                warning.Summary = $"Mực nước triều ổn định ({estimatedMaxTide:F2}m), không có mưa lớn. Các tuyến giao thông TP.HCM lưu thông thuận lợi.";
            }

            // Danh sách tuyến đường ngập trọng điểm TP.HCM
            warning.HotspotRoads = GetHcmHotspots(warning.RiskLevel, estimatedMaxTide >= 1.55, warning.RainIntensity >= 15.0);
        }
        else if (isHanoi)
        {
            warning.HasTideData = false;
            warning.CityType = "Hanoi";
            warning.CityDisplayName = "Hà Nội";

            if (warning.RainIntensity >= 45.0)
            {
                warning.RiskLevel = 3;
                warning.Headline = "Báo Động Đỏ: Mưa Dồn Dập Cực Đoan Gây Ngập Sâu Diện Rộng!";
                warning.Summary = $"Lượng mưa {warning.RainIntensity:F1} mm/h gây quá tải toàn diện lưu vực sông Tô Lịch, Nhuệ, Kim Ngưu. Nguy cơ ngập 30 - 50cm, ngập sâu tê liệt các hầm chui Thăng Long và các trục phố chính.";
            }
            else if (warning.RainIntensity >= 25.0)
            {
                warning.RiskLevel = 2;
                warning.Headline = "Cảnh Báo Ngập Úng Đô Thị Do Mưa Lớn";
                warning.Summary = $"Mưa lớn liên tục đạt {warning.RainIntensity:F1} mm/h. Nguy cơ ngập 20 - 40cm tại các tuyến phố trũng thấp thuộc quận Đống Đa, Ba Đình, Cầu Giấy, Thanh Xuân.";
            }
            else if (warning.RainIntensity >= 10.0)
            {
                warning.RiskLevel = 1;
                warning.Headline = "Nguy Cơ Đọng Nước Cục Bộ & Trơn Trượt";
                warning.Summary = "Mưa rào rải rác làm xuất hiện các vũng nước đọng ở lòng đường và ngõ sâu. Mặt đường trơn trượt giờ tan tầm.";
            }
            else
            {
                warning.RiskLevel = 0;
                warning.Headline = "Hệ Thống Thoát Nước Hoạt Động Tốt - Giao Thông Thuận Lợi";
                warning.Summary = "Không có mưa lớn tại Hà Nội. Các tuyến phố và hầm chui thông thoáng, an toàn di chuyển.";
            }

            warning.HotspotRoads = GetHanoiHotspots(warning.RiskLevel, warning.RainIntensity >= 15.0);
        }
        else
        {
            // Các đô thị khác (Đà Nẵng, Hải Phòng hoặc vùng chung)
            warning.HasTideData = isDaNang || isHaiPhong;
            warning.CityType = "General";
            warning.CityDisplayName = string.IsNullOrWhiteSpace(locationName) ? "Khu vực của bạn" : locationName;

            if (warning.RainIntensity >= 45.0)
            {
                warning.RiskLevel = 3;
                warning.Headline = "Báo Động Ngập Úng Do Mưa Xối Xả";
                warning.Summary = $"Cường độ mưa {warning.RainIntensity:F1} mm/h rất lớn, có thể gây ngập cục bộ sâu tại các khu dân cư trũng thấp và đường ven sông suối.";
            }
            else if (warning.RainIntensity >= 25.0)
            {
                warning.RiskLevel = 2;
                warning.Headline = "Cảnh Báo Mưa To Nguy Cơ Ngập";
                warning.Summary = $"Mưa lớn kéo dài đạt {warning.RainIntensity:F1} mm/h. Đề phòng nước không kịp thoát gây ngập lòng đường.";
            }
            else if (warning.RainIntensity >= 10.0)
            {
                warning.RiskLevel = 1;
                warning.Headline = "Khả Năng Đọng Nước Do Mưa Rào";
                warning.Summary = "Mưa rào có thể gây ứ đọng nước cục bộ ở các điểm trũng.";
            }
            else
            {
                warning.RiskLevel = 0;
                warning.Headline = "Thời Tiết Thuận Lợi - Không Có Nguy Cơ Ngập Úng";
                warning.Summary = "Lượng mưa thấp, đường sá thông thoáng và di chuyển an toàn.";
            }

            if (isDaNang) warning.HotspotRoads = GetDaNangHotspots(warning.RiskLevel);
            else if (isHaiPhong) warning.HotspotRoads = GetHaiPhongHotspots(warning.RiskLevel);
        }

        // Lời khuyên an toàn phương tiện di chuyển
        GenerateCommuteAdvice(warning);

        return warning;
    }

    private static void GenerateCommuteAdvice(UrbanFloodWarning w)
    {
        if (w.RiskLevel >= 2)
        {
            w.MotorbikeAdvice = "🛵 Xe máy: Đi đều tay ga ở số thấp (số 1-2 với xe số), tuyệt đối không hạ ga giữa vũng ngập. Nếu nước cao quá tâm trục bánh xe (~25cm), tắt máy dắt bộ tránh nước sặc vào lọc gió.";
            w.CarAdvice = "🚗 Ô tô: Không cố vượt vùng ngập sâu quá 25cm (ngang tâm bánh xe sedan) kẻo thủy kích phá máy. Tắt điều hòa (A/C) để quạt tản nhiệt không hút nước. Giữ khoảng cách tránh sóng dềnh từ xe tải.";
        }
        else if (w.RiskLevel == 1)
        {
            w.MotorbikeAdvice = "🛵 Xe máy: Giảm tốc độ khi qua các vũng đọng nước, tránh đi sát mép vỉa hè để phòng sụt nắp cống hoặc hố ga che khuất.";
            w.CarAdvice = "🚗 Ô tô: Giảm tốc độ tránh tạt nước bẩn lên người đi xe máy hai bên đường. Bật đèn gầm chiếu sáng tăng tầm nhìn khi mưa.";
        }
        else
        {
            w.MotorbikeAdvice = "🛵 Xe máy: Đường thông thoáng, chạy đúng tốc độ và giữ khoảng cách an toàn.";
            w.CarAdvice = "🚗 Ô tô: Lộ trình thông suốt, thuận tiện lưu thông toàn bộ các tuyến đường.";
        }
    }

    private static List<FloodHotspotRoad> GetHcmHotspots(int riskLevel, bool hasHighTide, bool hasHeavyRain)
    {
        var list = new List<FloodHotspotRoad>
        {
            new() { District = "Quận 7", StreetName = "Đường Huỳnh Tấn Phát", EstimatedDepth = hasHighTide ? "30 - 50 cm" : "15 - 25 cm", Cause = hasHeavyRain ? "Mưa + Triều Cường" : "Do Triều Cường", SeverityLevel = hasHighTide ? 3 : 2, Note = "Đoạn từ cầu Phú Xuân đến đường Nguyễn Thị Thập" },
            new() { District = "Quận 7", StreetName = "Đường Trần Xuân Soạn", EstimatedDepth = hasHighTide ? "35 - 55 cm" : "20 - 30 cm", Cause = "Do Triều Cường", SeverityLevel = hasHighTide ? 3 : 2, Note = "Đoạn chạy dọc bờ kênh Kênh Tẻ gần dạ cầu Tân Thuận" },
            new() { District = "Quận 7 / Nhà Bè", StreetName = "Đường Lê Văn Lương", EstimatedDepth = hasHighTide ? "30 - 45 cm" : "15 - 25 cm", Cause = "Do Triều Cường", SeverityLevel = hasHighTide ? 3 : 2, Note = "Khu vực cầu Rạch Đỉa và cầu Long Kiểng" },
            new() { District = "Bình Thạnh", StreetName = "Đường Nguyễn Hữu Cảnh", EstimatedDepth = hasHeavyRain ? "25 - 45 cm" : "10 - 20 cm", Cause = hasHighTide && hasHeavyRain ? "Mưa + Triều Cường" : "Do Mưa Lớn", SeverityLevel = (hasHeavyRain || hasHighTide) ? 2 : 1, Note = "Đoạn trũng chân cầu Thủ Thiêm và chung cư The Manor" },
            new() { District = "Bình Thạnh", StreetName = "Quốc lộ 13 (Chân cầu Bình Triệu)", EstimatedDepth = "20 - 35 cm", Cause = "Do Triều Cường", SeverityLevel = hasHighTide ? 2 : 1, Note = "Đoạn từ ngã tư Bình Triệu đến cầu Ông Dầu" },
            new() { District = "TP. Thủ Đức", StreetName = "Đường Quốc Hương & Thảo Điền", EstimatedDepth = (hasHeavyRain || hasHighTide) ? "30 - 50 cm" : "15 - 20 cm", Cause = "Mưa + Triều Cường", SeverityLevel = (hasHeavyRain || hasHighTide) ? 3 : 1, Note = "Khu phố Thảo Điền, ngập sâu ô tô con không thể qua" },
            new() { District = "TP. Thủ Đức", StreetName = "Đường Võ Văn Ngân", EstimatedDepth = hasHeavyRain ? "25 - 40 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn dốc trước chợ Thủ Đức, dòng nước chảy xiết khi mưa to" },
            new() { District = "Quận 8", StreetName = "Bến Phú Định & Đường Mễ Cốc", EstimatedDepth = hasHighTide ? "35 - 55 cm" : "20 - 30 cm", Cause = "Do Triều Cường", SeverityLevel = hasHighTide ? 3 : 2, Note = "Ven kênh Đôi, nước tràn trực tiếp qua mặt đê bao" },
            new() { District = "Bình Tân", StreetName = "Đường Hồ Học Lãm", EstimatedDepth = "25 - 40 cm", Cause = hasHighTide ? "Mưa + Triều Cường" : "Do Mưa Lớn", SeverityLevel = 2, Note = "Đoạn giáp ranh Quốc lộ 1A đến đường An Dương Vương" },
            new() { District = "Gò Vấp", StreetName = "Đường Nguyễn Văn Khối (Cây Trâm)", EstimatedDepth = hasHeavyRain ? "20 - 35 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Khu vực trước cổng Công viên Làng Hoa" },
            new() { District = "Tân Phú", StreetName = "Đường Phan Anh & Tô Hiệu", EstimatedDepth = hasHeavyRain ? "25 - 45 cm" : "10 - 20 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 1, Note = "Đoạn ngã tư Bốn Xã trũng thấp thoát nước chậm" },
            new() { District = "Quận 12", StreetName = "Đường Song Hành QL22", EstimatedDepth = hasHeavyRain ? "20 - 35 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn qua nút giao ngã tư An Sương" }
        };

        if (riskLevel == 0)
        {
            // Trạng thái an toàn: giảm nhẹ độ sâu dự kiến nhưng vẫn giữ danh sách cho người dùng tham khảo
            foreach (var r in list)
            {
                r.EstimatedDepth = "Khô ráo / An toàn";
                r.SeverityLevel = 1;
            }
        }

        return list;
    }

    private static List<FloodHotspotRoad> GetHanoiHotspots(int riskLevel, bool hasHeavyRain)
    {
        var list = new List<FloodHotspotRoad>
        {
            new() { District = "Đống Đa", StreetName = "Đường Thái Hà - Huỳnh Thúc Kháng", EstimatedDepth = hasHeavyRain ? "30 - 45 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 1, Note = "Đoạn nút giao Chùa Bộc - Thái Hà và trước TT Chiếu phim Quốc Gia" },
            new() { District = "Đống Đa", StreetName = "Phố Nguyễn Khuyến - Văn Miếu", EstimatedDepth = hasHeavyRain ? "30 - 50 cm" : "10 - 20 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 1, Note = "Vùng lòng chảo trước cổng trường Lý Thường Kiệt" },
            new() { District = "Ba Đình", StreetName = "Phố Cao Bá Quát - Điện Biên Phủ", EstimatedDepth = hasHeavyRain ? "25 - 40 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn số nhà 15 - 29 Cao Bá Quát" },
            new() { District = "Thanh Xuân", StreetName = "Phố Vương Thừa Vũ & Bùi Xương Trạch", EstimatedDepth = hasHeavyRain ? "30 - 50 cm" : "15 - 20 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 2, Note = "Khu vực trũng lưu vực sông Tô Lịch" },
            new() { District = "Thanh Xuân", StreetName = "Đường Nguyễn Trãi", EstimatedDepth = hasHeavyRain ? "20 - 35 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn chân cầu vượt Ngã Tư Sở và cổng ĐH Khoa học Xã hội & Nhân văn" },
            new() { District = "Cầu Giấy", StreetName = "Phố Hoa Bằng & Dương Đình Nghệ", EstimatedDepth = hasHeavyRain ? "30 - 45 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 1, Note = "Khu vực ngõ 99 Hoa Bằng và nút giao Tôn Thất Thuyết" },
            new() { District = "Nam Từ Liêm", StreetName = "Hầm chui Đại lộ Thăng Long", EstimatedDepth = hasHeavyRain ? "35 - 55 cm" : "10 - 20 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 1, Note = "Các hầm chui số 3, 5, 6 đoạn Km9+656 nước dồn về vùng trũng" },
            new() { District = "Thanh Trì", StreetName = "Phố Triều Khúc & Tân Triều", EstimatedDepth = hasHeavyRain ? "30 - 50 cm" : "15 - 20 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 3 : 2, Note = "Khu vực ngõ 66 và chợ Triều Khúc trũng thấp" },
            new() { District = "Hoàn Kiếm", StreetName = "Phố Phùng Hưng - Bát Đàn", EstimatedDepth = hasHeavyRain ? "20 - 35 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Khu phố cổ đoạn gầm cầu đường sắt" },
            new() { District = "Hoàng Mai", StreetName = "Đường Định Công & Thịnh Liệt", EstimatedDepth = hasHeavyRain ? "25 - 40 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn ven sông Sét và ga Giáp Bát" },
            new() { District = "Hà Đông", StreetName = "Đường Quang Trung & Tô Hiệu", EstimatedDepth = hasHeavyRain ? "20 - 35 cm" : "10 - 15 cm", Cause = "Do Mưa Lớn", SeverityLevel = hasHeavyRain ? 2 : 1, Note = "Đoạn qua Bưu điện Hà Đông và cầu Trắng" }
        };

        if (riskLevel == 0)
        {
            foreach (var r in list)
            {
                r.EstimatedDepth = "Khô ráo / An toàn";
                r.SeverityLevel = 1;
            }
        }

        return list;
    }

    private static List<FloodHotspotRoad> GetDaNangHotspots(int riskLevel)
    {
        return new List<FloodHotspotRoad>
        {
            new() { District = "Thanh Khê", StreetName = "Đường Hàm Nghi & Hùng Vương", EstimatedDepth = riskLevel >= 2 ? "30 - 45 cm" : "Khô ráo", Cause = "Do Mưa Lớn", SeverityLevel = riskLevel >= 2 ? 3 : 1, Note = "Khu vực quanh bờ hồ Hàm Nghi" },
            new() { District = "Liên Chiểu", StreetName = "Đường Mẹ Suốt", EstimatedDepth = riskLevel >= 2 ? "35 - 55 cm" : "Khô ráo", Cause = "Do Mưa Lớn", SeverityLevel = riskLevel >= 2 ? 3 : 1, Note = "Vùng trũng Khe Cạn thoát nước chậm" },
            new() { District = "Hải Châu", StreetName = "Đường Trưng Nữ Vương", EstimatedDepth = riskLevel >= 2 ? "20 - 35 cm" : "Khô ráo", Cause = "Do Mưa Lớn", SeverityLevel = riskLevel >= 2 ? 2 : 1, Note = "Đoạn gần cầu Rồng và chợ Mới" }
        };
    }

    private static List<FloodHotspotRoad> GetHaiPhongHotspots(int riskLevel)
    {
        return new List<FloodHotspotRoad>
        {
            new() { District = "Ngô Quyền", StreetName = "Đường Cầu Đất & Lương Khánh Thiện", EstimatedDepth = riskLevel >= 2 ? "25 - 40 cm" : "Khô ráo", Cause = "Mưa + Triều Cường", SeverityLevel = riskLevel >= 2 ? 2 : 1, Note = "Khu vực trung tâm nội thành" },
            new() { District = "Lê Chân", StreetName = "Ngã tư An Dương & Tôn Đức Thắng", EstimatedDepth = riskLevel >= 2 ? "30 - 45 cm" : "Khô ráo", Cause = "Do Mưa Lớn", SeverityLevel = riskLevel >= 2 ? 3 : 1, Note = "Nút giao trũng thấp ven sông Tam Bạc" }
        };
    }
}
