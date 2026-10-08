using System;
using System.Collections.Generic;

namespace WeatherApp.Helpers;

/// <summary>
/// Bộ công cụ tính toán và chuyển đổi Dương lịch sang Âm lịch Việt Nam chuẩn xác (Múi giờ UTC+7),
/// xác định 24 Tiết khí truyền thống theo thuật toán thiên văn Hồ Ngọc Đức,
/// tính toán Can Chi Ba Trụ (Năm, Tháng, Ngày), Hoàng Đạo / Hắc Đạo, Giờ Hoàng Đạo, Tuần Trăng và Đếm ngược lễ hội.
/// </summary>
public static class VietnameseLunarHelper
{
    private static readonly string[] Can = { "Giáp", "Ất", "Bính", "Đinh", "Mậu", "Kỷ", "Canh", "Tân", "Nhâm", "Quý" };
    private static readonly string[] Chi = { "Tý", "Sửu", "Dần", "Mão", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };

    private static readonly string[] SolarTerms = 
    {
        "Xuân Phân", "Thanh Minh", "Cốc Vũ", "Lập Hạ", "Tiểu Mãn", "Mang Chủng",
        "Hạ Chí", "Tiểu Thử", "Đại Thử", "Lập Thu", "Xử Thử", "Bạch Lộ",
        "Thu Phân", "Hàn Lộ", "Sương Giáng", "Lập Đông", "Tiểu Tuyết", "Đại Tuyết",
        "Đông Chí", "Tiểu Hàn", "Đại Hàn", "Lập Xuân", "Vũ Thủy", "Kinh Trập"
    };

    private static readonly string[] SolarTermsEn =
    {
        "Spring Equinox", "Pure Brightness", "Grain Rain", "Start of Summer", "Grain Buds", "Grain in Ear",
        "Summer Solstice", "Minor Heat", "Major Heat", "Start of Autumn", "End of Heat", "White Dew",
        "Autumn Equinox", "Cold Dew", "Frost's Descent", "Start of Winter", "Minor Snow", "Major Snow",
        "Winter Solstice", "Minor Cold", "Major Cold", "Start of Spring", "Rain Water", "Awakening of Insects"
    };

    private static readonly string[] ZodiacDeities =
    {
        "Thanh Long", "Minh Đường", "Thiên Hình", "Chu Tước",
        "Kim Quỹ", "Thiên Đức", "Bạch Hổ", "Ngọc Đường",
        "Thiên Lao", "Huyền Vũ", "Tư Mệnh", "Câu Trận"
    };

    private static readonly bool[] IsDeityAuspicious =
    {
        true,  // Thanh Long (Hoàng Đạo)
        true,  // Minh Đường (Hoàng Đạo)
        false, // Thiên Hình (Hắc Đạo)
        false, // Chu Tước (Hắc Đạo)
        true,  // Kim Quỹ (Hoàng Đạo)
        true,  // Thiên Đức (Hoàng Đạo)
        false, // Bạch Hổ (Hắc Đạo)
        true,  // Ngọc Đường (Hoàng Đạo)
        false, // Thiên Lao (Hắc Đạo)
        false, // Huyền Vũ (Hắc Đạo)
        true,  // Tư Mệnh (Hoàng Đạo)
        false  // Câu Trận (Hắc Đạo)
    };

    public class LunarDateResult
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsLeap { get; set; }
        public string CanChiYear { get; set; } = string.Empty;
        public string CanChiMonth { get; set; } = string.Empty;
        public string CanChiDay { get; set; } = string.Empty;
        public string SolarTerm { get; set; } = string.Empty;
        public string HolidayName { get; set; } = string.Empty;
        public string FullDisplay { get; set; } = string.Empty;

        // Hoàng Đạo & Hắc Đạo
        public bool IsAuspiciousDay { get; set; } = true;
        public string AuspiciousDayName { get; set; } = "Hoàng Đạo (Tốt)";
        public string AuspiciousDayColor { get; set; } = "#10B981";
        public string AuspiciousHoursFormatted { get; set; } = string.Empty;
        public List<string> AuspiciousHoursList { get; set; } = new();

        // Tuần Trăng (Moon Phase)
        public string MoonPhaseIcon { get; set; } = "🌕";
        public string MoonPhaseName { get; set; } = "Trăng Tròn";
        public int MoonIllumination { get; set; } = 100;
    }

    public class FestivalCountdownItem
    {
        public string Title { get; set; } = string.Empty;
        public string LunarDateText { get; set; } = string.Empty;
        public string SolarDateText { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public string DaysRemainingText { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = "\uf06b";
        public string AccentColor { get; set; } = "#DC2626";
    }

    /// <summary>
    /// Chuyển đổi một ngày Dương lịch (mặc định UTC+7) sang thông tin Âm lịch, Can Chi, Tiết khí, Hoàng Đạo & Tuần trăng
    /// </summary>
    public static LunarDateResult ConvertSolarToLunar(DateTime solarDate, bool isVietnamese = true)
    {
        int day = solarDate.Day;
        int month = solarDate.Month;
        int year = solarDate.Year;

        int jd = JulianDayFromDate(day, month, year);
        int k = (int)Math.Floor((jd - 2415021.076998695) / 29.530588853);
        int nm = GetNewMoonDay(k, 7);
        if (nm > jd)
        {
            k--;
            nm = GetNewMoonDay(k, 7);
        }

        int a11 = GetSunLongitude(GetNewMoonDay(GetLunarMonth11(year, 7), 7), 7);
        int lunarYear;

        if (a11 >= 9)
        {
            lunarYear = year;
        }
        else
        {
            lunarYear = year - 1;
        }

        // Tính tháng âm lịch
        int lunarMonth = k - GetLunarMonth11(lunarYear, 7) + 11;
        bool isLeap = false;

        // Xử lý năm nhuận âm lịch
        int off = GetLunarMonth11(lunarYear, 7);
        int leapMonth = GetLeapMonthOffset(off, 7);
        if (leapMonth > 0)
        {
            if (k >= off + leapMonth)
            {
                lunarMonth--;
                if (k == off + leapMonth)
                {
                    isLeap = true;
                }
            }
        }

        if (lunarMonth > 12) lunarMonth -= 12;
        if (lunarMonth <= 0) lunarMonth += 12;

        int lunarDay = jd - nm + 1;

        // 1. Can Chi Năm
        int yearCanIndex = (lunarYear + 6) % 10;
        int yearChiIndex = (lunarYear + 8) % 12;
        string canChiYear = $"{Can[yearCanIndex]} {Chi[yearChiIndex]}";

        // 2. Can Chi Tháng (Ngũ Hổ Độn)
        int monthChiIndex = (lunarMonth + 1) % 12;
        int monthCanIndex = (yearCanIndex * 2 + lunarMonth + 1) % 10;
        string canChiMonth = $"{Can[monthCanIndex]} {Chi[monthChiIndex]}";

        // 3. Can Chi Ngày (từ số ngày Julian jd)
        int dayCanIndex = (jd + 9) % 10;
        int dayChiIndex = (jd + 1) % 12;
        string canChiDay = $"{Can[dayCanIndex]} {Chi[dayChiIndex]}";

        // 4. Tiết khí
        string solarTerm = GetSolarTerm(jd, isVietnamese);

        // 5. Ngày lễ truyền thống
        string holiday = GetTraditionalHoliday(lunarDay, lunarMonth, isLeap, isVietnamese);

        // 6. Hoàng Đạo / Hắc Đạo của ngày
        var (isAuspicious, auspiciousName, auspiciousColor) = GetAuspiciousZodiac(lunarMonth, dayChiIndex, isVietnamese);

        // 7. Giờ Hoàng Đạo trong ngày
        var (hoursList, hoursFormatted) = GetAuspiciousHours(dayChiIndex);

        // 8. Tuần trăng (Moon Phase)
        var (moonIcon, moonName, moonIllumination) = GetMoonPhase(lunarDay, isVietnamese);

        var result = new LunarDateResult
        {
            Day = lunarDay,
            Month = lunarMonth,
            Year = lunarYear,
            IsLeap = isLeap,
            CanChiYear = canChiYear,
            CanChiMonth = canChiMonth,
            CanChiDay = canChiDay,
            SolarTerm = solarTerm,
            HolidayName = holiday,
            IsAuspiciousDay = isAuspicious,
            AuspiciousDayName = auspiciousName,
            AuspiciousDayColor = auspiciousColor,
            AuspiciousHoursList = hoursList,
            AuspiciousHoursFormatted = hoursFormatted,
            MoonPhaseIcon = moonIcon,
            MoonPhaseName = moonName,
            MoonIllumination = moonIllumination
        };

        // Chuỗi hiển thị trang nhã
        string dayFormatted = lunarDay < 10 ? $"0{lunarDay}" : $"{lunarDay}";
        string monthFormatted = lunarMonth < 10 ? $"0{lunarMonth}" : $"{lunarMonth}";

        if (!string.IsNullOrEmpty(holiday))
        {
            result.FullDisplay = isVietnamese
                ? $"🌙 {dayFormatted}/{monthFormatted} ÂL ({holiday}) • Tiết {solarTerm} • {canChiDay}"
                : $"🌙 {dayFormatted}/{monthFormatted} Lunar ({holiday}) • Term: {solarTerm} • {canChiDay}";
        }
        else
        {
            result.FullDisplay = isVietnamese
                ? $"🌙 {dayFormatted}/{monthFormatted} ÂL (Năm {canChiYear}) • Tiết {solarTerm} • {canChiDay}"
                : $"🌙 {dayFormatted}/{monthFormatted} Lunar (Year {canChiYear}) • Term: {solarTerm} • {canChiDay}";
        }

        return result;
    }

    /// <summary>
    /// Xác định ngày Hoàng Đạo hay Hắc Đạo theo 12 Thần cổ truyền
    /// </summary>
    public static (bool isAuspicious, string name, string color) GetAuspiciousZodiac(int lunarMonth, int dayChiIndex, bool isVietnamese = true)
    {
        // Vị trí khởi Thanh Long theo tháng âm lịch
        int startChi = (lunarMonth % 6) switch
        {
            1 => 0,  // Tháng 1, 7: Khởi tại Tý
            2 => 2,  // Tháng 2, 8: Khởi tại Dần
            3 => 4,  // Tháng 3, 9: Khởi tại Thìn
            4 => 6,  // Tháng 4, 10: Khởi tại Ngọ
            5 => 8,  // Tháng 5, 11: Khởi tại Thân
            0 => 10, // Tháng 6, 12: Khởi tại Tuất
            _ => 0
        };

        int deityIndex = (dayChiIndex - startChi + 12) % 12;
        bool isAuspicious = IsDeityAuspicious[deityIndex];
        string deityName = ZodiacDeities[deityIndex];

        string fullTitle = isVietnamese
            ? (isAuspicious ? $"Hoàng Đạo ({deityName})" : $"Hắc Đạo ({deityName})")
            : (isAuspicious ? $"Auspicious ({deityName})" : $"Inauspicious ({deityName})");
        string color = isAuspicious ? "#10B981" : "#EF4444";

        return (isAuspicious, fullTitle, color);
    }

    /// <summary>
    /// Lấy danh sách 6 giờ Hoàng Đạo (tốt nhất) trong ngày theo Chi của ngày
    /// </summary>
    public static (List<string> list, string formatted) GetAuspiciousHours(int dayChiIndex)
    {
        var hours = new List<string>();

        switch (dayChiIndex)
        {
            case 2: // Dần
            case 8: // Thân
                hours.Add("Tý (23h-01h)");
                hours.Add("Sửu (01h-03h)");
                hours.Add("Thìn (07h-09h)");
                hours.Add("Tỵ (09h-11h)");
                hours.Add("Mùi (13h-15h)");
                hours.Add("Tuất (19h-21h)");
                break;

            case 3: // Mão
            case 9: // Dậu
                hours.Add("Tý (23h-01h)");
                hours.Add("Dần (03h-05h)");
                hours.Add("Mão (05h-07h)");
                hours.Add("Ngọ (11h-13h)");
                hours.Add("Mùi (13h-15h)");
                hours.Add("Dậu (17h-19h)");
                break;

            case 4: // Thìn
            case 10: // Tuất
                hours.Add("Dần (03h-05h)");
                hours.Add("Thìn (07h-09h)");
                hours.Add("Tỵ (09h-11h)");
                hours.Add("Thân (15h-17h)");
                hours.Add("Dậu (17h-19h)");
                hours.Add("Hợi (21h-23h)");
                break;

            case 5: // Tỵ
            case 11: // Hợi
                hours.Add("Sửu (01h-03h)");
                hours.Add("Thìn (07h-09h)");
                hours.Add("Ngọ (11h-13h)");
                hours.Add("Mùi (13h-15h)");
                hours.Add("Tuất (19h-21h)");
                hours.Add("Hợi (21h-23h)");
                break;

            case 0: // Tý
            case 6: // Ngọ
                hours.Add("Tý (23h-01h)");
                hours.Add("Sửu (01h-03h)");
                hours.Add("Mão (05h-07h)");
                hours.Add("Ngọ (11h-13h)");
                hours.Add("Thân (15h-17h)");
                hours.Add("Dậu (17h-19h)");
                break;

            case 1: // Sửu
            case 7: // Mùi
            default:
                hours.Add("Dần (03h-05h)");
                hours.Add("Mão (05h-07h)");
                hours.Add("Tỵ (09h-11h)");
                hours.Add("Thân (15h-17h)");
                hours.Add("Tuất (19h-21h)");
                hours.Add("Hợi (21h-23h)");
                break;
        }

        string formatted = string.Join(", ", hours);
        return (hours, formatted);
    }

    /// <summary>
    /// Xác định pha mặt trăng dựa vào ngày âm lịch
    /// </summary>
    public static (string icon, string name, int illumination) GetMoonPhase(int lunarDay, bool isVietnamese = true)
    {
        return lunarDay switch
        {
            1 => ("🌑", isVietnamese ? "Trăng Non (Sóc / Mới)" : "New Moon", 2),
            >= 2 and <= 6 => ("🌒", isVietnamese ? "Trăng Lưỡi Liềm Đầu Tháng" : "Waxing Crescent", Math.Min(35, lunarDay * 7)),
            >= 7 and <= 9 => ("🌓", isVietnamese ? "Bán Nguyệt Đầu Tháng (Thượng Huyền)" : "First Quarter", 50),
            >= 10 and <= 13 => ("🌔", isVietnamese ? "Trăng Khuyết Đầu Tháng" : "Waxing Gibbous", 75),
            14 or 15 or 16 => ("🌕", isVietnamese ? "Trăng Tròn (Vọng / Rằm)" : "Full Moon", 100),
            >= 17 and <= 21 => ("🌖", isVietnamese ? "Trăng Khuyết Cuối Tháng" : "Waning Gibbous", 75),
            >= 22 and <= 24 => ("🌗", isVietnamese ? "Bán Nguyệt Cuối Tháng (Hạ Huyền)" : "Third Quarter", 50),
            _ => ("🌘", isVietnamese ? "Trăng Tàn Cuối Tháng" : "Waning Crescent", Math.Max(5, (30 - lunarDay) * 7))
        };
    }

    /// <summary>
    /// Chuyển đổi ngược từ ngày Âm lịch sang ngày Dương lịch chính xác
    /// </summary>
    public static DateTime? ConvertLunarToSolar(int lunarDay, int lunarMonth, int lunarYear, bool isLeap = false)
    {
        try
        {
            // Quét trong khoảng an toàn từ 01/01 của năm lunarYear đến 30/04 của năm lunarYear + 1
            DateTime start = new DateTime(Math.Max(1900, lunarYear), 1, 1);
            DateTime end = new DateTime(Math.Min(2100, lunarYear + 1), 5, 1);
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                var l = ConvertSolarToLunar(d);
                if (l.Day == lunarDay && l.Month == lunarMonth && l.Year == lunarYear && l.IsLeap == isLeap)
                {
                    return d;
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Lấy danh sách đếm ngược các sự kiện và lễ hội truyền thống sắp tới
    /// </summary>
    public static List<FestivalCountdownItem> GetUpcomingFestivals(DateTime today, bool isVietnamese = true)
    {
        var list = new List<FestivalCountdownItem>();
        int currentYear = today.Year;

        // 1. Tết Nguyên Đán (Mùng 1 tháng Giêng)
        DateTime? tetThisYear = ConvertLunarToSolar(1, 1, currentYear);
        DateTime? nextTet = (tetThisYear.HasValue && tetThisYear.Value.Date >= today.Date)
            ? tetThisYear
            : ConvertLunarToSolar(1, 1, currentYear + 1);

        if (nextTet.HasValue)
        {
            int days = (nextTet.Value.Date - today.Date).Days;
            list.Add(new FestivalCountdownItem
            {
                Title = isVietnamese ? $"Tết Nguyên Đán {nextTet.Value.Year}" : $"Lunar New Year {nextTet.Value.Year}",
                LunarDateText = isVietnamese ? "Mùng 1 Tháng Giêng" : "1st Lunar Month",
                SolarDateText = nextTet.Value.ToString("dd/MM/yyyy"),
                DaysRemaining = days,
                DaysRemainingText = days == 0 ? (isVietnamese ? "Hôm nay!" : "Today!") : (isVietnamese ? $"Còn {days} ngày" : $"In {days} days"),
                IconGlyph = "\uf06b",
                AccentColor = "#DC2626"
            });
        }

        // 2. Giỗ Tổ Hùng Vương (10 tháng Ba)
        DateTime? gioTo = ConvertLunarToSolar(10, 3, currentYear);
        if (!gioTo.HasValue || gioTo.Value.Date < today.Date)
        {
            gioTo = ConvertLunarToSolar(10, 3, currentYear + 1);
        }
        if (gioTo.HasValue)
        {
            int days = (gioTo.Value.Date - today.Date).Days;
            list.Add(new FestivalCountdownItem
            {
                Title = isVietnamese ? "Giỗ Tổ Hùng Vương" : "Hung Kings' Festival",
                LunarDateText = isVietnamese ? "10 Tháng Ba ÂL" : "10th of 3rd Lunar Month",
                SolarDateText = gioTo.Value.ToString("dd/MM/yyyy"),
                DaysRemaining = days,
                DaysRemainingText = days == 0 ? (isVietnamese ? "Hôm nay!" : "Today!") : (isVietnamese ? $"Còn {days} ngày" : $"In {days} days"),
                IconGlyph = "\uf024",
                AccentColor = "#F59E0B"
            });
        }

        // 3. Tết Trung Thu (15 tháng Tám)
        DateTime? trungThu = ConvertLunarToSolar(15, 8, currentYear);
        if (!trungThu.HasValue || trungThu.Value.Date < today.Date)
        {
            trungThu = ConvertLunarToSolar(15, 8, currentYear + 1);
        }
        if (trungThu.HasValue)
        {
            int days = (trungThu.Value.Date - today.Date).Days;
            list.Add(new FestivalCountdownItem
            {
                Title = isVietnamese ? "Tết Trung Thu (Rằm Tháng 8)" : "Mid-Autumn Festival",
                LunarDateText = isVietnamese ? "15 Tháng Tám ÂL" : "15th of 8th Lunar Month",
                SolarDateText = trungThu.Value.ToString("dd/MM/yyyy"),
                DaysRemaining = days,
                DaysRemainingText = days == 0 ? (isVietnamese ? "Hôm nay!" : "Today!") : (isVietnamese ? $"Còn {days} ngày" : $"In {days} days"),
                IconGlyph = "\uf186",
                AccentColor = "#8B5CF6"
            });
        }

        // 4. Ngày Rằm kế tiếp (15 ÂL gần nhất)
        for (int i = 0; i <= 35; i++)
        {
            DateTime probe = today.AddDays(i);
            var l = ConvertSolarToLunar(probe, isVietnamese);
            if (l.Day == 15)
            {
                int days = i;
                list.Add(new FestivalCountdownItem
                {
                    Title = isVietnamese ? $"Rằm Tháng {l.Month} ÂL" : $"Full Moon (Month {l.Month})",
                    LunarDateText = isVietnamese ? $"15/{l.Month} ÂL ({l.CanChiDay})" : $"15th Lunar ({l.CanChiDay})",
                    SolarDateText = probe.ToString("dd/MM/yyyy"),
                    DaysRemaining = days,
                    DaysRemainingText = days == 0 ? (isVietnamese ? "Hôm nay (Chính Rằm)!" : "Today (Full Moon)!") : (isVietnamese ? $"Còn {days} ngày" : $"In {days} days"),
                    IconGlyph = "\uf111",
                    AccentColor = "#06B6D4"
                });
                break;
            }
        }

        // 5. Ngày Mùng 1 kế tiếp (Sóc)
        for (int i = 0; i <= 35; i++)
        {
            DateTime probe = today.AddDays(i);
            var l = ConvertSolarToLunar(probe, isVietnamese);
            if (l.Day == 1 && i > 0)
            {
                int days = i;
                list.Add(new FestivalCountdownItem
                {
                    Title = isVietnamese ? $"Mùng 1 Tháng {l.Month} ÂL" : $"New Moon (Month {l.Month})",
                    LunarDateText = isVietnamese ? $"01/{l.Month} ÂL ({l.CanChiDay})" : $"1st Lunar ({l.CanChiDay})",
                    SolarDateText = probe.ToString("dd/MM/yyyy"),
                    DaysRemaining = days,
                    DaysRemainingText = isVietnamese ? $"Còn {days} ngày" : $"In {days} days",
                    IconGlyph = "\uf192",
                    AccentColor = "#10B981"
                });
                break;
            }
        }

        return list;
    }

    private static string GetTraditionalHoliday(int day, int month, bool isLeap, bool isVietnamese = true)
    {
        if (isLeap) return string.Empty;

        return (day, month) switch
        {
            (1, 1) => isVietnamese ? "Mùng 1 Tết Nguyên Đán" : "Lunar New Year's Day",
            (2, 1) => isVietnamese ? "Mùng 2 Tết" : "2nd Day of Tet",
            (3, 1) => isVietnamese ? "Mùng 3 Tết" : "3rd Day of Tet",
            (15, 1) => isVietnamese ? "Rằm Tháng Giêng" : "Lantern Festival",
            (3, 3) => isVietnamese ? "Tết Hàn Thực" : "Cold Food Festival",
            (10, 3) => isVietnamese ? "Giỗ Tổ Hùng Vương" : "Hung Kings Commemoration",
            (15, 4) => isVietnamese ? "Lễ Phật Đản" : "Buddha's Birthday",
            (5, 5) => isVietnamese ? "Tết Đoan Ngọ" : "Dragon Boat Festival",
            (15, 7) => isVietnamese ? "Lễ Vu Lan" : "Vu Lan Festival",
            (15, 8) => isVietnamese ? "Tết Trung Thu" : "Mid-Autumn Festival",
            (9, 9) => isVietnamese ? "Tết Trùng Cửu" : "Double Ninth Festival",
            (15, 10) => isVietnamese ? "Tết Hạ Nguyên" : "Ha Nguyen Festival",
            (23, 12) => isVietnamese ? "Tiễn Ông Táo" : "Kitchen Gods Day",
            (30, 12) => isVietnamese ? "Tất Niên" : "Lunar New Year's Eve",
            (29, 12) => isVietnamese ? "Tất Niên" : "Lunar New Year's Eve",
            (1, _) => isVietnamese ? "Mùng 1" : "1st Day",
            (15, _) => isVietnamese ? "Ngày Rằm" : "Full Moon",
            _ => string.Empty
        };
    }

    private static string GetSolarTerm(int jd, bool isVietnamese = true)
    {
        int termIndex = GetSunLongitude(jd, 7);
        if (termIndex >= 0 && termIndex < SolarTerms.Length)
        {
            return isVietnamese ? SolarTerms[termIndex] : SolarTermsEn[termIndex];
        }
        return isVietnamese ? "Bình thường" : "Normal";
    }

    private static int JulianDayFromDate(int d, int m, int y)
    {
        if (m <= 2)
        {
            y -= 1;
            m += 12;
        }
        int a = y / 100;
        int b = 2 - a + (a / 4);
        return (int)(Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5);
    }

    private static int GetNewMoonDay(int k, double timeZone)
    {
        double t = k / 1236.85;
        double t2 = t * t;
        double t3 = t2 * t;
        double dr = Math.PI / 180.0;

        double jde = 2415020.75933 + 29.53058868 * k + 0.0001178 * t2 - 0.000000155 * t3
                     + 0.00033 * Math.Sin((166.56 + 132.87 * t - 0.009173 * t2) * dr);

        double m = 359.2242 + 29.10535608 * k - 0.0000333 * t2 - 0.00000347 * t3;
        double mpr = 306.0253 + 385.81691806 * k + 0.0107306 * t2 + 0.00001236 * t3;
        double f = 21.2964 + 390.67050646 * k - 0.0016528 * t2 - 0.00000239 * t3;

        double c1 = (0.1734 - 0.000393 * t) * Math.Sin(m * dr)
                    + 0.0021 * Math.Sin(2 * m * dr)
                    - 0.4068 * Math.Sin(mpr * dr)
                    + 0.0161 * Math.Sin(2 * mpr * dr)
                    - 0.0004 * Math.Sin(3 * mpr * dr)
                    + 0.0104 * Math.Sin(2 * f * dr)
                    - 0.0051 * Math.Sin((m + mpr) * dr)
                    - 0.0074 * Math.Sin((m - mpr) * dr)
                    + 0.0004 * Math.Sin((2 * f + m) * dr)
                    - 0.0004 * Math.Sin((2 * f - m) * dr)
                    - 0.0006 * Math.Sin((2 * f + mpr) * dr)
                    + 0.0010 * Math.Sin((2 * f - mpr) * dr)
                    + 0.0005 * Math.Sin((m + 2 * mpr) * dr);

        double jd = jde + c1 + (timeZone / 24.0);
        return (int)Math.Floor(jd + 0.5);
    }

    private static int GetSunLongitude(int jd, double timeZone)
    {
        double t = (jd - 2451545.0 + 0.5 - (timeZone / 24.0)) / 36525.0;
        double t2 = t * t;
        double dr = Math.PI / 180.0;

        double l0 = 280.46646 + 36000.76983 * t + 0.0003032 * t2;
        double m = 357.52911 + 35999.05029 * t - 0.0001537 * t2;
        double c = (1.914602 - 0.004817 * t - 0.000014 * t2) * Math.Sin(m * dr)
                   + (0.019993 - 0.000101 * t) * Math.Sin(2 * m * dr)
                   + 0.000289 * Math.Sin(3 * m * dr);

        double theta = l0 + c;
        theta = (theta % 360.0 + 360.0) % 360.0;

        return (int)Math.Floor(theta / 15.0);
    }

    private static int GetLunarMonth11(int year, double timeZone)
    {
        int off = JulianDayFromDate(31, 12, year) - 2415021;
        int k = (int)Math.Floor(off / 29.530588853);
        int nm = GetNewMoonDay(k, timeZone);
        int sunLong = GetSunLongitude(nm, timeZone);

        if (sunLong >= 9)
        {
            return k;
        }
        return k - 1;
    }

    private static int GetLeapMonthOffset(int a11, double timeZone)
    {
        int k = a11;
        int last = 0;
        int i = 1;
        int arc = GetSunLongitude(GetNewMoonDay(k, timeZone), timeZone);

        do
        {
            last = arc;
            k++;
            arc = GetSunLongitude(GetNewMoonDay(k, timeZone), timeZone);
            if (arc == last)
            {
                return i;
            }
            i++;
        } while (k < a11 + 13);

        return 0;
    }
}
