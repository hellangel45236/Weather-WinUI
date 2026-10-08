using System;
using WeatherApp.Services;

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
    public static HolidayInfo GetHoliday(DateTime solarDate, int lunarDay, int lunarMonth, bool isLeap, bool? isVietnamese = null)
    {
        bool isVi = isVietnamese ?? LocalizationService.Instance.IsVietnamese;
        int sDay = solarDate.Day;
        int sMonth = solarDate.Month;

        // 1. Ngày lễ theo DƯƠNG LỊCH
        if (sDay == 1 && sMonth == 1)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Tết Dương Lịch" : "New Year's Day",
                Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                IsOfficialDayOff = true,
                Description = isVi ? "Ngày đầu năm mới theo Dương lịch trên toàn thế giới." : "The first day of the new Gregorian calendar year worldwide."
            };

        if (sDay == 14 && sMonth == 2)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Lễ Tình Nhân (Valentine)" : "Valentine's Day",
                Badge = isVi ? "LỄ HỘI" : "CELEBRATION",
                Description = isVi ? "Ngày tôn vinh tình yêu đôi lứa trên toàn thế giới." : "Celebrating romantic love and affection worldwide."
            };

        if (sDay == 27 && sMonth == 2)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Ngày Thầy Thuốc Việt Nam" : "Vietnamese Doctors' Day",
                Badge = isVi ? "KỶ NIỆM" : "TRIBUTE",
                Description = isVi ? "Ngày tri ân các y bác sĩ và cán bộ y tế nước nhà." : "Honoring physicians, healthcare workers, and medical staff in Vietnam."
            };

        if (sDay == 8 && sMonth == 3)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Quốc Tế Phụ Nữ (8/3)" : "International Women's Day",
                Badge = isVi ? "KỶ NIỆM" : "MEMORIAL",
                Description = isVi ? "Ngày tôn vinh và gửi lời chúc tốt đẹp nhất tới phái đẹp." : "Honoring women's social, economic, and cultural achievements worldwide."
            };

        if (sDay == 26 && sMonth == 3)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Thành Lập Đoàn TNCS Hồ Chí Minh" : "Ho Chi Minh Youth Union Day",
                Badge = isVi ? "KỶ NIỆM" : "ANNIVERSARY",
                Description = isVi ? "Kỷ niệm ngày thành lập Đoàn Thanh niên Cộng sản Hồ Chí Minh (26/3/1931)." : "Commemorating the founding of the Ho Chi Minh Communist Youth Union (1931)."
            };

        if (sDay == 30 && sMonth == 4)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Giải Phóng Miền Nam (30/4)" : "Reunification Day (30/4)",
                Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                IsOfficialDayOff = true,
                Description = isVi ? "Kỷ niệm Ngày Giải phóng miền Nam, thống nhất non sông đất nước." : "National holiday commemorating the reunification of Vietnam in 1975."
            };

        if (sDay == 1 && sMonth == 5)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Quốc Tế Lao Động (1/5)" : "International Workers' Day",
                Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                IsOfficialDayOff = true,
                Description = isVi ? "Ngày hội tôn vinh giai cấp công nhân và người lao động toàn thế giới." : "Honoring laborers and the international working class."
            };

        if (sDay == 1 && sMonth == 6)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Quốc Tế Thiếu Nhi (1/6)" : "International Children's Day",
                Badge = isVi ? "LỄ HỘI" : "FESTIVAL",
                Description = isVi ? "Ngày hội dành riêng cho các em thiếu nhi trên khắp năm châu." : "A joyous day celebrating children's rights and happiness worldwide."
            };

        if (sDay == 28 && sMonth == 6)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Ngày Gia Đình Việt Nam" : "Vietnamese Family Day",
                Badge = isVi ? "KỶ NIỆM" : "TRIBUTE",
                Description = isVi ? "Tôn vinh mái ấm gia đình và các giá trị văn hóa truyền thống tốt đẹp." : "Honoring family values and Vietnamese traditional cultural heritage."
            };

        if (sDay == 27 && sMonth == 7)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Ngày Thương Binh Liệt Sĩ (27/7)" : "War Invalids & Martyrs' Day",
                Badge = isVi ? "TRI ÂN" : "MEMORIAL",
                Description = isVi ? "Tưởng nhớ và tri ân các anh hùng liệt sĩ, thương bệnh binh vì độc lập tự do." : "Honoring fallen heroes, martyrs, and veterans who fought for freedom."
            };

        if (sDay == 19 && sMonth == 8)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Cách Mạng Tháng Tám" : "August Revolution Day",
                Badge = isVi ? "LỊCH SỬ" : "HISTORIC",
                Description = isVi ? "Kỷ niệm thắng lợi vĩ đại của cuộc Cách mạng Tháng Tám năm 1945." : "Commemorating the victory of the August Revolution in 1945."
            };

        if (sDay == 2 && sMonth == 9)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Quốc Khánh Việt Nam (2/9)" : "Vietnam National Day (2/9)",
                Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                IsOfficialDayOff = true,
                Description = isVi ? "Kỷ niệm ngày Chủ tịch Hồ Chí Minh đọc Tuyên ngôn Độc lập tại Ba Đình." : "Commemorating the Declaration of Independence by President Ho Chi Minh (1945)."
            };

        if (sDay == 10 && sMonth == 10)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Giải Phóng Thủ Đô (10/10)" : "Capital Liberation Day",
                Badge = isVi ? "KỶ NIỆM" : "ANNIVERSARY",
                Description = isVi ? "Kỷ niệm ngày tiếp quản Thủ đô Hà Nội ngàn năm văn hiến (10/10/1954)." : "Commemorating the historic liberation and handover of Hanoi in 1954."
            };

        if (sDay == 20 && sMonth == 10)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Ngày Phụ Nữ Việt Nam (20/10)" : "Vietnamese Women's Day",
                Badge = isVi ? "KỶ NIỆM" : "TRIBUTE",
                Description = isVi ? "Tôn vinh người phụ nữ Việt Nam kiên cường, bất khuất, trung hậu, đảm đang." : "Honoring Vietnamese women's grace, perseverance, and dedication."
            };

        if (sDay == 20 && sMonth == 11)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Ngày Nhà Giáo Việt Nam (20/11)" : "Vietnamese Teachers' Day",
                Badge = isVi ? "TRI ÂN" : "TRIBUTE",
                Description = isVi ? "Tôn sư trọng đạo, tri ân công ơn các thầy cô giáo vì sự nghiệp trồng người." : "Paying tribute to teachers and educators across Vietnam."
            };

        if (sDay == 22 && sMonth == 12)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Thành Lập Quân Đội Nhân Dân VN" : "People's Army of Vietnam Day",
                Badge = isVi ? "KỶ NIỆM" : "ANNIVERSARY",
                Description = isVi ? "Kỷ niệm ngày thành lập Quân đội Nhân dân Việt Nam anh hùng (22/12/1944)." : "Commemorating the founding of the People's Army of Vietnam in 1944."
            };

        if (sDay == 24 && sMonth == 12)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Đêm Giáng Sinh (Christmas Eve)" : "Christmas Eve",
                Badge = isVi ? "LỄ HỘI" : "FESTIVAL",
                Description = isVi ? "Đêm canh thức trước ngày lễ Giáng sinh ấm áp và an lành." : "Peaceful and joyous Christmas Eve vigil."
            };

        if (sDay == 25 && sMonth == 12)
            return new HolidayInfo
            {
                IsHoliday = true,
                Name = isVi ? "Lễ Giáng Sinh (Noel)" : "Christmas Day",
                Badge = isVi ? "LỄ HỘI" : "FESTIVAL",
                Description = isVi ? "Kỷ niệm ngày Chúa Giê-su ra đời, ngày hội sum họp gia đình và bạn bè." : "Celebrating the birth of Jesus Christ, bringing family and friends together."
            };

        // 2. Ngày lễ theo ÂM LỊCH TRUYỀN THỐNG (chỉ xét tháng không nhuận)
        if (!isLeap)
        {
            if (lunarDay == 23 && lunarMonth == 12)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tiễn Ông Táo Chầu Trời" : "Kitchen Gods Day",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Lễ cúng tiễn Táo quân cưỡi cá chép về Trời bẩm báo công việc trần gian." : "Traditional ritual bidding farewell to the Kitchen Gods ascending to heaven."
                };

            if ((lunarDay == 29 || lunarDay == 30) && lunarMonth == 12)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Đêm Tất Niên (Giao Thừa)" : "Lunar New Year's Eve",
                    Badge = isVi ? "TRỌNG ĐẠI" : "SACRED",
                    Description = isVi ? "Khoảnh khắc thiêng liêng sum họp gia đình và chuyển giao giữa năm cũ và năm mới." : "Sacred family gathering marking the transition between the old and new year."
                };

            if (lunarDay == 1 && lunarMonth == 1)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Mùng 1 Tết Nguyên Đán" : "1st Day of Lunar New Year",
                    Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                    IsOfficialDayOff = true,
                    Description = isVi ? "Tết cổ truyền thiêng liêng - Ngày đầu tiên của năm mới âm lịch (Mùng một tết cha)." : "The first day of Tet - Visiting paternal relatives and wishing prosperity."
                };

            if (lunarDay == 2 && lunarMonth == 1)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Mùng 2 Tết Nguyên Đán" : "2nd Day of Lunar New Year",
                    Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                    IsOfficialDayOff = true,
                    Description = isVi ? "Ngày thứ hai của Tết Nguyên Đán (Mùng hai tết mẹ)." : "The second day of Tet - Visiting maternal relatives and family elders."
                };

            if (lunarDay == 3 && lunarMonth == 1)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Mùng 3 Tết Nguyên Đán" : "3rd Day of Lunar New Year",
                    Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                    IsOfficialDayOff = true,
                    Description = isVi ? "Ngày thứ ba của Tết Nguyên Đán (Mùng ba tết thầy)." : "The third day of Tet - Visiting teachers and respected mentors."
                };

            if (lunarDay == 4 && lunarMonth == 1)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Mùng 4 Tết" : "4th Day of Lunar New Year",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Lễ khai hạ, hóa vàng tiễn tổ tiên đầu năm mới." : "Traditional ritual honoring ancestors and opening the spring season."
                };

            if (lunarDay == 15 && lunarMonth == 1)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Rằm Tháng Giêng (Tết Nguyên Tiêu)" : "Lantern Festival (15th Lunar)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Đêm trăng tròn đầu tiên trong năm, ngày lễ cầu an phúc lành (Lễ Phật quanh năm không bằng Rằm tháng Giêng)." : "First full moon of the lunar year, offering heartfelt prayers for health and luck."
                };

            if (lunarDay == 3 && lunarMonth == 3)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tết Hàn Thực (Bánh trôi bánh chay)" : "Cold Food Festival (Tet Han Thuc)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Tục ăn bánh trôi bánh chay thanh đạm tưởng nhớ tổ tiên cội nguồn." : "Traditional day enjoying sweet floating rice cakes in remembrance of ancestry."
                };

            if (lunarDay == 10 && lunarMonth == 3)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Giỗ Tổ Hùng Vương (10/3 ÂL)" : "Hung Kings Commemoration (10/3)",
                    Badge = isVi ? "NGHỈ LỄ" : "HOLIDAY",
                    IsOfficialDayOff = true,
                    Description = isVi ? "Dù ai đi ngược về xuôi, nhớ ngày Giỗ Tổ mùng mười tháng ba." : "National commemoration honoring the legendary founding kings of Vietnam."
                };

            if (lunarDay == 15 && lunarMonth == 4)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Đại Lễ Phật Đản" : "Vesak (Buddha's Birthday)",
                    Badge = isVi ? "TÔN GIÁO" : "RELIGIOUS",
                    Description = isVi ? "Đại lễ kỷ niệm ngày Đức Phật Thích Ca Mâu Ni đản sinh (Rằm tháng 4)." : "Grand Buddhist holiday commemorating the birth and enlightenment of Gautama Buddha."
                };

            if (lunarDay == 5 && lunarMonth == 5)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tết Đoan Ngọ (5/5 ÂL)" : "Dragon Boat Festival (5/5 Lunar)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Tết Đoan Dương - phong tục giết sâu bọ, ăn bánh tro, cơm rượu nếp, quả tươi." : "Traditional mid-year festival warding off pests, enjoying fermented rice wine."
                };

            if (lunarDay == 15 && lunarMonth == 7)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Đại Lễ Vu Lan & Xá Tội Vong Nhân" : "Vu Lan (Parents' Filial Piety)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Mùa Vu Lan báo hiếu công ơn cha mẹ và ngày xá tội vong nhân phúc đức." : "Buddhist festival dedicated to honoring parents and offering mercy to wandering souls."
                };

            if (lunarDay == 15 && lunarMonth == 8)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tết Trung Thu (Rằm Tháng Tám)" : "Mid-Autumn Festival",
                    Badge = isVi ? "LỄ HỘI" : "FESTIVAL",
                    Description = isVi ? "Tết đoàn viên sum họp, ngắm trăng tròn, rước đèn ông sao và thưởng thức bánh trung thu." : "Harvest moon reunion festival with lanterns, star lights, and delicious mooncakes."
                };

            if (lunarDay == 9 && lunarMonth == 9)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tết Trùng Cửu (9/9 ÂL)" : "Double Ninth Festival (9/9)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Tết hoa cúc, phong tục leo núi ngắm cảnh mùa thu thanh tịnh." : "Chrysanthemum festival celebrated by climbing hills to admire autumn scenery."
                };

            if (lunarDay == 15 && lunarMonth == 10)
                return new HolidayInfo
                {
                    IsHoliday = true,
                    Name = isVi ? "Tết Hạ Nguyên (Tết Cơm Mới)" : "New Rice Festival (Tet Ha Nguyen)",
                    Badge = isVi ? "TRUYỀN THỐNG" : "TRADITION",
                    Description = isVi ? "Lễ mừng lúa mới, tạ ơn trời đất mưa thuận gió hòa và mùa màng bội thu." : "Harvest ceremony offering newly harvested rice and thanksgiving prayers."
                };
        }

        return new HolidayInfo { IsHoliday = false };
    }
}
