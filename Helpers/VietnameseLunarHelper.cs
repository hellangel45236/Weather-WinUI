using System;

namespace WeatherApp.Helpers;

/// <summary>
/// Bộ công cụ tính toán và chuyển đổi Dương lịch sang Âm lịch Việt Nam chuẩn xác (Múi giờ UTC+7)
/// và xác định 24 Tiết khí truyền thống theo thuật toán thiên văn Hồ Ngọc Đức.
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

    public class LunarDateResult
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsLeap { get; set; }
        public string CanChiYear { get; set; } = string.Empty;
        public string SolarTerm { get; set; } = string.Empty;
        public string HolidayName { get; set; } = string.Empty;
        public string FullDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chuyển đổi một ngày Dương lịch (mặc định UTC+7) sang thông tin Âm lịch & Tiết khí
    /// </summary>
    public static LunarDateResult ConvertSolarToLunar(DateTime solarDate)
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
        int b11 = a11;
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

        // Can Chi năm
        string canChiYear = $"{Can[(lunarYear + 6) % 10]} {Chi[(lunarYear + 8) % 12]}";

        // Tiết khí
        string solarTerm = GetSolarTerm(jd);

        // Ngày lễ truyền thống
        string holiday = GetTraditionalHoliday(lunarDay, lunarMonth, isLeap);

        var result = new LunarDateResult
        {
            Day = lunarDay,
            Month = lunarMonth,
            Year = lunarYear,
            IsLeap = isLeap,
            CanChiYear = canChiYear,
            SolarTerm = solarTerm,
            HolidayName = holiday
        };

        // Chuỗi hiển thị trang nhã
        string dayFormatted = lunarDay < 10 ? $"0{lunarDay}" : $"{lunarDay}";
        string monthFormatted = lunarMonth < 10 ? $"0{lunarMonth}" : $"{lunarMonth}";
        string leapStr = isLeap ? " (Nhuận)" : "";

        if (!string.IsNullOrEmpty(holiday))
        {
            result.FullDisplay = $"🌙 {dayFormatted}/{monthFormatted} ÂL ({holiday}) • Tiết {solarTerm}";
        }
        else
        {
            result.FullDisplay = $"🌙 {dayFormatted}/{monthFormatted} ÂL (Năm {canChiYear}) • Tiết {solarTerm}";
        }

        return result;
    }

    private static string GetTraditionalHoliday(int day, int month, bool isLeap)
    {
        if (isLeap) return string.Empty;

        return (day, month) switch
        {
            (1, 1) => "Mùng 1 Tết Nguyên Đán",
            (2, 1) => "Mùng 2 Tết",
            (3, 1) => "Mùng 3 Tết",
            (15, 1) => "Rằm Tháng Giêng",
            (3, 3) => "Tết Hàn Thực",
            (10, 3) => "Giỗ Tổ Hùng Vương",
            (15, 4) => "Lễ Phật Đản",
            (5, 5) => "Tết Đoan Ngọ",
            (15, 7) => "Lễ Vu Lan",
            (15, 8) => "Tết Trung Thu",
            (9, 9) => "Tết Trùng Cửu",
            (15, 10) => "Tết Hạ Nguyên",
            (23, 12) => "Tiễn Ông Táo",
            (30, 12) => "Tất Niên",
            (29, 12) => "Tất Niên",
            (1, _) => "Mùng 1",
            (15, _) => "Ngày Rằm",
            _ => string.Empty
        };
    }

    private static string GetSolarTerm(int jd)
    {
        int termIndex = GetSunLongitude(jd, 7);
        if (termIndex >= 0 && termIndex < SolarTerms.Length)
        {
            return SolarTerms[termIndex];
        }
        return "Bình thường";
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
