using System;

namespace WeatherApp.Helpers;

/// <summary>
/// Bộ máy tra cứu và đối chiếu Ngày Lễ Việt Nam (Cả Dương lịch và Âm lịch truyền thống)
/// </summary>
public static class VietnameseHolidayHelper
{
    public class HolidayInfo
    {
        public bool IsHoliday { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty; // "NGHỈ LỄ", "KỶ NIỆM", "TRUYỀN THỐNG", "LỄ HỘI"
        public string Description { get; set; } = string.Empty;
        public bool IsOfficialDayOff { get; set; } // Ngày nghỉ lễ chính thức theo luật lao động
    }

    /// <summary>
    /// Tra cứu thông tin ngày lễ dựa trên ngày Dương lịch và ngày Âm lịch đã quy đổi
    /// </summary>
    public static HolidayInfo GetHoliday(DateTime solarDate, int lunarDay, int lunarMonth, bool isLeap)
    {
        int sDay = solarDate.Day;
        int sMonth = solarDate.Month;

        // 1. Ngày lễ theo DƯƠNG LỊCH
        if (sDay == 1 && sMonth == 1)
            return new HolidayInfo { IsHoliday = true, Name = "Tết Dương Lịch", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Ngày đầu năm mới theo Dương lịch trên toàn thế giới." };

        if (sDay == 14 && sMonth == 2)
            return new HolidayInfo { IsHoliday = true, Name = "Lễ Tình Nhân (Valentine)", Badge = "LỄ HỘI", Description = "Ngày tôn vinh tình yêu đôi lứa trên toàn thế giới." };

        if (sDay == 27 && sMonth == 2)
            return new HolidayInfo { IsHoliday = true, Name = "Ngày Thầy Thuốc Việt Nam", Badge = "KỶ NIỆM", Description = "Ngày tri ân các y bác sĩ và cán bộ y tế nước nhà." };

        if (sDay == 8 && sMonth == 3)
            return new HolidayInfo { IsHoliday = true, Name = "Quốc Tế Phụ Nữ (8/3)", Badge = "KỶ NIỆM", Description = "Ngày tôn vinh và gửi lời chúc tốt đẹp nhất tới phái đẹp." };

        if (sDay == 26 && sMonth == 3)
            return new HolidayInfo { IsHoliday = true, Name = "Thành Lập Đoàn TNCS Hồ Chí Minh", Badge = "KỶ NIỆM", Description = "Kỷ niệm ngày thành lập Đoàn Thanh niên Cộng sản Hồ Chí Minh (26/3/1931)." };

        if (sDay == 30 && sMonth == 4)
            return new HolidayInfo { IsHoliday = true, Name = "Giải Phóng Miền Nam (30/4)", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Kỷ niệm Ngày Giải phóng miền Nam, thống nhất non sông đất nước." };

        if (sDay == 1 && sMonth == 5)
            return new HolidayInfo { IsHoliday = true, Name = "Quốc Tế Lao Động (1/5)", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Ngày hội tôn vinh giai cấp công nhân và người lao động toàn thế giới." };

        if (sDay == 1 && sMonth == 6)
            return new HolidayInfo { IsHoliday = true, Name = "Quốc Tế Thiếu Nhi (1/6)", Badge = "LỄ HỘI", Description = "Ngày hội dành riêng cho các em thiếu nhi trên khắp năm châu." };

        if (sDay == 28 && sMonth == 6)
            return new HolidayInfo { IsHoliday = true, Name = "Ngày Gia Đình Việt Nam", Badge = "KỶ NIỆM", Description = "Tôn vinh mái ấm gia đình và các giá trị văn hóa truyền thống tốt đẹp." };

        if (sDay == 27 && sMonth == 7)
            return new HolidayInfo { IsHoliday = true, Name = "Ngày Thương Binh Liệt Sĩ (27/7)", Badge = "TRI ÂN", Description = "Tưởng nhớ và tri ân các anh hùng liệt sĩ, thương bệnh binh vì độc lập tự do." };

        if (sDay == 19 && sMonth == 8)
            return new HolidayInfo { IsHoliday = true, Name = "Cách Mạng Tháng Tám", Badge = "LỊCH SỬ", Description = "Kỷ niệm thắng lợi vĩ đại của cuộc Cách mạng Tháng Tám năm 1945." };

        if (sDay == 2 && sMonth == 9)
            return new HolidayInfo { IsHoliday = true, Name = "Quốc Khánh Việt Nam (2/9)", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Kỷ niệm ngày Chủ tịch Hồ Chí Minh đọc Tuyên ngôn Độc lập tại Ba Đình." };

        if (sDay == 10 && sMonth == 10)
            return new HolidayInfo { IsHoliday = true, Name = "Giải Phóng Thủ Đô (10/10)", Badge = "KỶ NIỆM", Description = "Kỷ niệm ngày tiếp quản Thủ đô Hà Nội ngàn năm văn hiến (10/10/1954)." };

        if (sDay == 20 && sMonth == 10)
            return new HolidayInfo { IsHoliday = true, Name = "Ngày Phụ Nữ Việt Nam (20/10)", Badge = "KỶ NIỆM", Description = "Tôn vinh người phụ nữ Việt Nam kiên cường, bất khuất, trung hậu, đảm đang." };

        if (sDay == 20 && sMonth == 11)
            return new HolidayInfo { IsHoliday = true, Name = "Ngày Nhà Giáo Việt Nam (20/11)", Badge = "TRI ÂN", Description = "Tôn sư trọng đạo, tri ân công ơn các thầy cô giáo vì sự nghiệp trồng người." };

        if (sDay == 22 && sMonth == 12)
            return new HolidayInfo { IsHoliday = true, Name = "Thành Lập Quân Đội Nhân Dân VN", Badge = "KỶ NIỆM", Description = "Kỷ niệm ngày thành lập Quân đội Nhân dân Việt Nam anh hùng (22/12/1944)." };

        if (sDay == 24 && sMonth == 12)
            return new HolidayInfo { IsHoliday = true, Name = "Đêm Giáng Sinh (Christmas Eve)", Badge = "LỄ HỘI", Description = "Đêm canh thức trước ngày lễ Giáng sinh ấm áp và an lành." };

        if (sDay == 25 && sMonth == 12)
            return new HolidayInfo { IsHoliday = true, Name = "Lễ Giáng Sinh (Noel)", Badge = "LỄ HỘI", Description = "Kỷ niệm ngày Chúa Giê-su ra đời, ngày hội sum họp gia đình và bạn bè." };

        // 2. Ngày lễ theo ÂM LỊCH TRUYỀN THỐNG (chỉ xét tháng không nhuận)
        if (!isLeap)
        {
            if (lunarDay == 23 && lunarMonth == 12)
                return new HolidayInfo { IsHoliday = true, Name = "Tiễn Ông Táo Chầu Trời", Badge = "TRUYỀN THỐNG", Description = "Lễ cúng tiễn Táo quân cưỡi cá chép về Trời bẩm báo công việc trần gian." };

            if ((lunarDay == 29 || lunarDay == 30) && lunarMonth == 12)
                return new HolidayInfo { IsHoliday = true, Name = "Đêm Tất Niên (Giao Thừa)", Badge = "TRỌNG ĐẠI", Description = "Khoảnh khắc thiêng liêng sum họp gia đình và chuyển giao giữa năm cũ và năm mới." };

            if (lunarDay == 1 && lunarMonth == 1)
                return new HolidayInfo { IsHoliday = true, Name = "Mùng 1 Tết Nguyên Đán", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Tết cổ truyền thiêng liêng - Ngày đầu tiên của năm mới âm lịch (Mùng một tết cha)." };

            if (lunarDay == 2 && lunarMonth == 1)
                return new HolidayInfo { IsHoliday = true, Name = "Mùng 2 Tết Nguyên Đán", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Ngày thứ hai của Tết Nguyên Đán (Mùng hai tết mẹ)." };

            if (lunarDay == 3 && lunarMonth == 1)
                return new HolidayInfo { IsHoliday = true, Name = "Mùng 3 Tết Nguyên Đán", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Ngày thứ ba của Tết Nguyên Đán (Mùng ba tết thầy)." };

            if (lunarDay == 4 && lunarMonth == 1)
                return new HolidayInfo { IsHoliday = true, Name = "Mùng 4 Tết", Badge = "TRUYỀN THỐNG", Description = "Lễ khai hạ, hóa vàng tiễn tổ tiên đầu năm mới." };

            if (lunarDay == 15 && lunarMonth == 1)
                return new HolidayInfo { IsHoliday = true, Name = "Rằm Tháng Giêng (Tết Nguyên Tiêu)", Badge = "TRUYỀN THỐNG", Description = "Đêm trăng tròn đầu tiên trong năm, ngày lễ cầu an phúc lành (Lễ Phật quanh năm không bằng Rằm tháng Giêng)." };

            if (lunarDay == 3 && lunarMonth == 3)
                return new HolidayInfo { IsHoliday = true, Name = "Tết Hàn Thực (Bánh trôi bánh chay)", Badge = "TRUYỀN THỐNG", Description = "Tục ăn bánh trôi bánh chay thanh đạm tưởng nhớ tổ tiên cội nguồn." };

            if (lunarDay == 10 && lunarMonth == 3)
                return new HolidayInfo { IsHoliday = true, Name = "Giỗ Tổ Hùng Vương (10/3 ÂL)", Badge = "NGHỈ LỄ", IsOfficialDayOff = true, Description = "Dù ai đi ngược về xuôi, nhớ ngày Giỗ Tổ mùng mười tháng ba." };

            if (lunarDay == 15 && lunarMonth == 4)
                return new HolidayInfo { IsHoliday = true, Name = "Đại Lễ Phật Đản", Badge = "TÔN GIÁO", Description = "Đại lễ kỷ niệm ngày Đức Phật Thích Ca Mâu Ni đản sinh (Rằm tháng 4)." };

            if (lunarDay == 5 && lunarMonth == 5)
                return new HolidayInfo { IsHoliday = true, Name = "Tết Đoan Ngọ (5/5 ÂL)", Badge = "TRUYỀN THỐNG", Description = "Tết Đoan Dương - phong tục giết sâu bọ, ăn bánh tro, cơm rượu nếp, quả tươi." };

            if (lunarDay == 15 && lunarMonth == 7)
                return new HolidayInfo { IsHoliday = true, Name = "Đại Lễ Vu Lan & Xá Tội Vong Nhân", Badge = "TRUYỀN THỐNG", Description = "Mùa Vu Lan báo hiếu công ơn cha mẹ và ngày xá tội vong nhân phúc đức." };

            if (lunarDay == 15 && lunarMonth == 8)
                return new HolidayInfo { IsHoliday = true, Name = "Tết Trung Thu (Rằm Tháng Tám)", Badge = "LỄ HỘI", Description = "Tết đoàn viên sum họp, ngắm trăng tròn, rước đèn ông sao và thưởng thức bánh trung thu." };

            if (lunarDay == 9 && lunarMonth == 9)
                return new HolidayInfo { IsHoliday = true, Name = "Tết Trùng Cửu (9/9 ÂL)", Badge = "TRUYỀN THỐNG", Description = "Tết hoa cúc, phong tục leo núi ngắm cảnh mùa thu thanh tịnh." };

            if (lunarDay == 15 && lunarMonth == 10)
                return new HolidayInfo { IsHoliday = true, Name = "Tết Hạ Nguyên (Tết Cơm Mới)", Badge = "TRUYỀN THỐNG", Description = "Lễ mừng lúa mới, tạ ơn trời đất mưa thuận gió hòa và mùa màng bội thu." };
        }

        return new HolidayInfo { IsHoliday = false };
    }
}
