using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WeatherApp.Models;

public enum SeasonalSkinId
{
    Auto,           // Tự động nhận diện theo Lịch Âm Dương & Tiết Khí
    Spring,         // Mùa Xuân: Hoa Đào & Chồi Non (Spring Bloom)
    Summer,         // Mùa Hạ: Biển Ngọc & San Hô (Tropical Oasis)
    Autumn,         // Mùa Thu: Hổ Phách & Lá Phong (Amber Romance)
    Winter,         // Mùa Đông: Băng Giá & Cực Quang (Nordic Aurora)
    Tet,            // Tết Cổ Truyền: Hoàng Kim & Hồng Cát Tường (Lunar New Year)
    MidAutumn,      // Tết Trung Thu: Dạ Nguyệt & Đèn Hoa Đăng (Mid-Autumn Moon)
    Christmas,      // Giáng Sinh & Năm Mới: Tuyết Trắng & Thông Xanh (Noel Wonder)
    Halloween       // Lễ Hội Ma Quái: Cam Bí Ngô & Tím Huyền Bí (Midnight Phantom)
}

public enum SeasonalParticleType
{
    None,
    TetBlossom,         // Hoa mai vàng & hoa đào hồng rơi chao liệng
    MidAutumnMoonlit,   // Đốm đom đóm & hoa đăng lung linh bay bổng
    ChristmasSnow,      // Bông tuyết tinh thể lấp lánh
    HalloweenSparks,    // Đốm lửa ma trơi & hạt sương tím dập dờn
    SpringPetals,       // Cánh hoa anh đào phớt hồng bay dịu dàng
    SummerSparkles,     // Hạt bụi nắng vàng óng ánh lung linh
    AutumnLeaves,       // Lá phong vàng hổ phách & đỏ ấm chao liệng
    WinterFrost         // Tinh thể băng giá bay lơ lửng
}

public class SeasonalSkinDefinition
{
    public SeasonalSkinId Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf185"; // FontAwesome glyph
    public string IconFontFamily { get; set; } = "FontAwesomeSolid";
    public string BadgeEmoji { get; set; } = "🌸";
    public string Emoji => BadgeEmoji;
    public string Description => Subtitle;

    // Bảng màu Accent & Card Styling
    public string PrimaryAccentHex { get; set; } = "#38BDF8";
    public string SecondaryAccentHex { get; set; } = "#0284C7";
    public string GlowColorHex { get; set; } = "#3038BDF8";
    public string BorderStrokeHex { get; set; } = "#4038BDF8";
    public string BackgroundTintHex { get; set; } = "#15091828";
    public string BadgeBackgroundHex { get; set; } = "#2538BDF8";
    public string BadgeForegroundHex { get; set; } = "#38BDF8";
    public string LightBadgeForegroundHex { get; set; } = "#0284C7";
    public string LightBadgeBackgroundHex { get; set; } = "#150284C7";

    // Hero Card Seasonal Gradient Overrides
    public string HeroGradientStartHex { get; set; } = "#0F172A";
    public string HeroGradientEndHex { get; set; } = "#1E293B";

    // Định dạng Zen Mode
    public string ZenQuote { get; set; } = "Dành một khoảng lặng ngắm nhìn thời tiết và lắng nghe âm thanh cuộc sống.";
    public string ZenClockHandHex { get; set; } = "#38BDF8";
    public string ZenAccentHex { get; set; } = "#38BDF8";

    // Hiệu ứng hạt không gian
    public SeasonalParticleType ParticleType { get; set; } = SeasonalParticleType.None;

    // Định dạng Widget Studio & Desktop Widget
    public string WidgetTag { get; set; } = "Standard";
    public string WidgetBorderHex { get; set; } = "#38BDF8";
    public string WidgetTitleColorHex { get; set; } = "#38BDF8";
    public string WidgetSeasonalGreeting { get; set; } = string.Empty;
    public string WidgetGradientStartHex { get; set; } = "#F20F172A";
    public string WidgetGradientEndHex { get; set; } = "#F21E293B";

    // Đồng Bộ Matching Biểu Đồ 24h & Cung Mặt Trời
    public string HourlyCurveStrokeHex { get; set; } = "#38BDF8";
    public string HourlyGradientTopHex { get; set; } = "#5038BDF8";
    public string HourlyGradientBottomHex { get; set; } = "#0538BDF8";
    public string SunArcStrokeHex { get; set; } = "#F59E0B";

    // Viền Thẻ Bento Grid
    public string BentoCardBorderAccentHex { get; set; } = "#2538BDF8";

    // Gợi Ý Trang Phục & Phong Cách Sống Theo Mùa (Lifestyle OOTD)
    public string LifestyleOotdTip { get; set; } = "Mặc trang phục thoải mái, chú ý giữ ấm hoặc che chắn tùy theo diễn biến thời tiết.";
    public string LifestyleOotdBadge { get; set; } = "🌤️ Đời Sống";
}
