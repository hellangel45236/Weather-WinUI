using System;
using System.Collections.Generic;
using System.Linq;
using WeatherApp.Helpers;
using WeatherApp.Models;

namespace WeatherApp.Services;

/// <summary>
/// Dịch vụ quản lý Theme động theo 4 Mùa & 4 Đại Lễ Hội (Seasonal & Festival Skin Engine)
/// Hỗ trợ tự động nhận diện theo Lịch Âm Dương Việt Nam & Tiết khí hoặc cho phép tùy chỉnh thủ công.
/// </summary>
public class SeasonalSkinService
{
    private static SeasonalSkinService? _instance;
    public static SeasonalSkinService Instance => _instance ??= new SeasonalSkinService();

    private readonly Dictionary<SeasonalSkinId, SeasonalSkinDefinition> _skins = new();

    public SeasonalSkinService()
    {
        InitializeSkins();
    }

    private void InitializeSkins()
    {
        // 1. TẾT CỔ TRUYỀN (LUNAR NEW YEAR)
        _skins[SeasonalSkinId.Tet] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Tet,
            DisplayName = "Tết Cổ Truyền",
            EnglishName = "Lunar New Year",
            Subtitle = "Hoàng Kim & Hồng Cát Tường • Xuân Giáp Thìn / Ất Tỵ",
            BadgeEmoji = "🧧",
            IconGlyph = "\uf06b",
            PrimaryAccentHex = "#E11D48",
            SecondaryAccentHex = "#F59E0B",
            GlowColorHex = "#38E11D48",
            BorderStrokeHex = "#60F59E0B",
            BackgroundTintHex = "#221F0910",
            BadgeBackgroundHex = "#2CE11D48",
            BadgeForegroundHex = "#FDA4AF",
            LightBadgeForegroundHex = "#9F1239",
            LightBadgeBackgroundHex = "#20E11D48",
            HeroGradientStartHex = "#881337",
            HeroGradientEndHex = "#78350F",
            ZenQuote = "Cung chúc tân xuân, vạn sự như ý — Trăm hoa đua nở, gia đạo an khang.",
            ZenClockHandHex = "#F59E0B",
            ZenAccentHex = "#E11D48",
            ParticleType = SeasonalParticleType.TetBlossom,
            WidgetTag = "Tết Cát Tường",
            WidgetBorderHex = "#F59E0B",
            WidgetTitleColorHex = "#FDA4AF",
            WidgetSeasonalGreeting = "Chúc Mừng Năm Mới • Vạn Sự Như Ý"
        };

        // 2. TẾT TRUNG THU (MID-AUTUMN MOON)
        _skins[SeasonalSkinId.MidAutumn] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.MidAutumn,
            DisplayName = "Tết Trung Thu",
            EnglishName = "Mid-Autumn Moon Festival",
            Subtitle = "Dạ Nguyệt Đoàn Viên • Đèn Hoa Đăng & Thỏ Ngọc",
            BadgeEmoji = "🥮",
            IconGlyph = "\uf186",
            PrimaryAccentHex = "#6366F1",
            SecondaryAccentHex = "#FDE047",
            GlowColorHex = "#386366F1",
            BorderStrokeHex = "#60FDE047",
            BackgroundTintHex = "#22120E2E",
            BadgeBackgroundHex = "#2C6366F1",
            BadgeForegroundHex = "#FDE047",
            LightBadgeForegroundHex = "#4338CA",
            LightBadgeBackgroundHex = "#206366F1",
            HeroGradientStartHex = "#312E81",
            HeroGradientEndHex = "#78350F",
            ZenQuote = "Trăng rằm tỏa sáng vạn nhà — Đêm hội trăng rằm, an nhiên đoàn viên.",
            ZenClockHandHex = "#FDE047",
            ZenAccentHex = "#818CF8",
            ParticleType = SeasonalParticleType.MidAutumnMoonlit,
            WidgetTag = "Nguyệt Dạ",
            WidgetBorderHex = "#FDE047",
            WidgetTitleColorHex = "#C7D2FE",
            WidgetSeasonalGreeting = "Đêm Hội Trăng Rằm • Đoàn Viên Cát Lành"
        };

        // 3. GIÁNG SINH & NĂM MỚI (NOEL WONDER)
        _skins[SeasonalSkinId.Christmas] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Christmas,
            DisplayName = "Giáng Sinh & Năm Mới",
            EnglishName = "Christmas & New Year",
            Subtitle = "Noel Frozen Wonder • Tuyết Trắng & Thông Xanh",
            BadgeEmoji = "🎄",
            IconGlyph = "\uf7d9",
            PrimaryAccentHex = "#DC2626",
            SecondaryAccentHex = "#16A34A",
            GlowColorHex = "#38DC2626",
            BorderStrokeHex = "#6022C55E",
            BackgroundTintHex = "#1E0B1F14",
            BadgeBackgroundHex = "#2CDC2626",
            BadgeForegroundHex = "#86EFAC",
            LightBadgeForegroundHex = "#991B1B",
            LightBadgeBackgroundHex = "#20DC2626",
            HeroGradientStartHex = "#991B1B",
            HeroGradientEndHex = "#064E3B",
            ZenQuote = "Merry Christmas & Happy New Year — Nguyện chúc bình an và ấm áp lan tỏa.",
            ZenClockHandHex = "#38BDF8",
            ZenAccentHex = "#DC2626",
            ParticleType = SeasonalParticleType.ChristmasSnow,
            WidgetTag = "Giáng Sinh",
            WidgetBorderHex = "#22C55E",
            WidgetTitleColorHex = "#FCA5A5",
            WidgetSeasonalGreeting = "Merry Christmas • Giáng Sinh Ấm Áp"
        };

        // 4. HALLOWEEN (MIDNIGHT PHANTOM)
        _skins[SeasonalSkinId.Halloween] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Halloween,
            DisplayName = "Lễ Hội Halloween",
            EnglishName = "Halloween Night",
            Subtitle = "Midnight Phantom • Cam Bí Ngô & Tím Huyền Bí",
            BadgeEmoji = "🎃",
            IconGlyph = "\uf6e2",
            PrimaryAccentHex = "#F97316",
            SecondaryAccentHex = "#A855F7",
            GlowColorHex = "#38F97316",
            BorderStrokeHex = "#60A855F7",
            BackgroundTintHex = "#221A0B2E",
            BadgeBackgroundHex = "#2CF97316",
            BadgeForegroundHex = "#D8B4FE",
            LightBadgeForegroundHex = "#6B21A8",
            LightBadgeBackgroundHex = "#20A855F7",
            HeroGradientStartHex = "#4C1D95",
            HeroGradientEndHex = "#7C2D12",
            ZenQuote = "Trick or Treat! — Trong bóng đêm huyền bí, ngắm nhìn vũ điệu của bầu trời.",
            ZenClockHandHex = "#F97316",
            ZenAccentHex = "#C084FC",
            ParticleType = SeasonalParticleType.HalloweenSparks,
            WidgetTag = "Halloween",
            WidgetBorderHex = "#F97316",
            WidgetTitleColorHex = "#E9D5FF",
            WidgetSeasonalGreeting = "Spooky Season • Trick or Treat"
        };

        // 5. MÙA XUÂN (SPRING BLOOM)
        _skins[SeasonalSkinId.Spring] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Spring,
            DisplayName = "Mùa Xuân",
            EnglishName = "Spring Bloom",
            Subtitle = "Hoa Đào Nở Rộ & Chồi Non Mơn Mởn",
            BadgeEmoji = "🌸",
            IconGlyph = "\uf06c",
            PrimaryAccentHex = "#EC4899",
            SecondaryAccentHex = "#10B981",
            GlowColorHex = "#30EC4899",
            BorderStrokeHex = "#60EC4899",
            BackgroundTintHex = "#1B1A0B1A",
            BadgeBackgroundHex = "#28EC4899",
            BadgeForegroundHex = "#FBCFE8",
            LightBadgeForegroundHex = "#9D174D",
            LightBadgeBackgroundHex = "#20EC4899",
            HeroGradientStartHex = "#831843",
            HeroGradientEndHex = "#064E3B",
            ZenQuote = "Mùa xuân mơn mởn đâm chồi — Gió mát lành khẽ chạm vào sớm mai.",
            ZenClockHandHex = "#F472B6",
            ZenAccentHex = "#34D399",
            ParticleType = SeasonalParticleType.SpringPetals,
            WidgetTag = "Xuân Mới",
            WidgetBorderHex = "#EC4899",
            WidgetTitleColorHex = "#FBCFE8",
            WidgetSeasonalGreeting = "Xuân Tươi Mới • Vạn Vật Hồi Sinh"
        };

        // 6. MÙA HẠ (TROPICAL OASIS)
        _skins[SeasonalSkinId.Summer] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Summer,
            DisplayName = "Mùa Hạ",
            EnglishName = "Tropical Oasis",
            Subtitle = "Biển Ngọc & Nắng Vàng Nhiệt Đới Rực Rỡ",
            BadgeEmoji = "☀️",
            IconGlyph = "\uf185",
            PrimaryAccentHex = "#0EA5E9",
            SecondaryAccentHex = "#F59E0B",
            GlowColorHex = "#300EA5E9",
            BorderStrokeHex = "#60F59E0B",
            BackgroundTintHex = "#1B081A25",
            BadgeBackgroundHex = "#280EA5E9",
            BadgeForegroundHex = "#7DD3FC",
            LightBadgeForegroundHex = "#0369A1",
            LightBadgeBackgroundHex = "#200EA5E9",
            HeroGradientStartHex = "#0E7490",
            HeroGradientEndHex = "#B45309",
            ZenQuote = "Trời hạ rạng rỡ muôn nơi — Ngắm ánh dương rọi sáng từng góc phố.",
            ZenClockHandHex = "#F59E0B",
            ZenAccentHex = "#38BDF8",
            ParticleType = SeasonalParticleType.SummerSparkles,
            WidgetTag = "Hạ Nắng",
            WidgetBorderHex = "#0EA5E9",
            WidgetTitleColorHex = "#FDE68A",
            WidgetSeasonalGreeting = "Nắng Hạ Rực Rỡ • Năng Lượng Đầy Tràn"
        };

        // 7. MÙA THU (AMBER ROMANCE)
        _skins[SeasonalSkinId.Autumn] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Autumn,
            DisplayName = "Mùa Thu",
            EnglishName = "Amber Romance",
            Subtitle = "Hổ Phách & Lá Phong Vàng Hoài Niệm",
            BadgeEmoji = "🍁",
            IconGlyph = "\uf06c",
            PrimaryAccentHex = "#D97706",
            SecondaryAccentHex = "#EA580C",
            GlowColorHex = "#35D97706",
            BorderStrokeHex = "#60EA580C",
            BackgroundTintHex = "#20201206",
            BadgeBackgroundHex = "#2CD97706",
            BadgeForegroundHex = "#FDE68A",
            LightBadgeForegroundHex = "#92400E",
            LightBadgeBackgroundHex = "#20D97706",
            HeroGradientStartHex = "#78350F",
            HeroGradientEndHex = "#991B1B",
            ZenQuote = "Thu sang dịu mát heo may — Khẽ lắng nghe từng chiếc lá khẽ chao.",
            ZenClockHandHex = "#FBBF24",
            ZenAccentHex = "#FB923C",
            ParticleType = SeasonalParticleType.AutumnLeaves,
            WidgetTag = "Thu Vàng",
            WidgetBorderHex = "#D97706",
            WidgetTitleColorHex = "#FED7AA",
            WidgetSeasonalGreeting = "Mùa Thu Êm Ả • Gió Mát Trong Lành"
        };

        // 8. MÙA ĐÔNG (NORDIC AURORA)
        _skins[SeasonalSkinId.Winter] = new SeasonalSkinDefinition
        {
            Id = SeasonalSkinId.Winter,
            DisplayName = "Mùa Đông",
            EnglishName = "Nordic Aurora",
            Subtitle = "Băng Giá & Bắc Cực Quang Tĩnh Lặng",
            BadgeEmoji = "❄️",
            IconGlyph = "\uf2dc",
            PrimaryAccentHex = "#38BDF8",
            SecondaryAccentHex = "#818CF8",
            GlowColorHex = "#3038BDF8",
            BorderStrokeHex = "#60818CF8",
            BackgroundTintHex = "#1B09142A",
            BadgeBackgroundHex = "#2838BDF8",
            BadgeForegroundHex = "#BAE6FD",
            LightBadgeForegroundHex = "#0369A1",
            LightBadgeBackgroundHex = "#2038BDF8",
            HeroGradientStartHex = "#0C4A6E",
            HeroGradientEndHex = "#134E4A",
            ZenQuote = "Mùa đông trầm mặc tĩnh lặng — Hơi ấm bình yên trong từng nhịp thở.",
            ZenClockHandHex = "#38BDF8",
            ZenAccentHex = "#A5B4FC",
            ParticleType = SeasonalParticleType.WinterFrost,
            WidgetTag = "Đông Lạnh",
            WidgetBorderHex = "#38BDF8",
            WidgetTitleColorHex = "#C7D2FE",
            WidgetSeasonalGreeting = "Mùa Đông An Yên • Không Khí Trong Lành"
        };
    }

    /// <summary>
    /// Thuật toán tự động phát hiện Mùa hoặc Đại lễ hội dựa trên Lịch Âm Dương & Tiết khí
    /// </summary>
    public SeasonalSkinId DetectCurrentAutoSkin(DateTime now)
    {
        // 1. Kiểm tra Âm Lịch Việt Nam
        try
        {
            var lunar = VietnameseLunarHelper.ConvertSolarToLunar(now);
            
            // Tết Cổ Truyền: Từ 23 Tháng Chạp (Tháng 12 ÂL) đến hết mùng 10 Tháng Giêng (Tháng 1 ÂL)
            if ((lunar.Month == 12 && lunar.Day >= 23) || (lunar.Month == 1 && lunar.Day <= 10))
            {
                return SeasonalSkinId.Tet;
            }

            // Tết Trung Thu: Tuần lễ từ 10 đến 18 Tháng 8 Âm Lịch
            if (lunar.Month == 8 && lunar.Day >= 10 && lunar.Day <= 18)
            {
                return SeasonalSkinId.MidAutumn;
            }
        }
        catch { }

        // 2. Kiểm tra Đại lễ Dương Lịch
        // Halloween: Từ 25/10 đến 02/11
        if ((now.Month == 10 && now.Day >= 25) || (now.Month == 11 && now.Day <= 2))
        {
            return SeasonalSkinId.Halloween;
        }

        // Giáng Sinh & Năm Mới: Từ 15/12 đến 02/01
        if ((now.Month == 12 && now.Day >= 15) || (now.Month == 1 && now.Day <= 3))
        {
            return SeasonalSkinId.Christmas;
        }

        // 3. Phân định 4 Mùa theo Dương lịch & Khí hậu Việt Nam
        return now.Month switch
        {
            2 or 3 or 4 => SeasonalSkinId.Spring,     // Xuân: Tháng 2, 3, 4
            5 or 6 or 7 => SeasonalSkinId.Summer,     // Hạ: Tháng 5, 6, 7
            8 or 9 or 10 => SeasonalSkinId.Autumn,    // Thu: Tháng 8, 9, 10
            _ => SeasonalSkinId.Winter                // Đông: Tháng 11, 12, 1
        };
    }

    /// <summary>
    /// Lấy định nghĩa Skin đang hoạt động theo cài đặt
    /// </summary>
    public SeasonalSkinDefinition GetActiveSkin(string settingMode)
    {
        SeasonalSkinId targetId;

        if (string.Equals(settingMode, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            targetId = DetectCurrentAutoSkin(DateTime.Now);
        }
        else if (Enum.TryParse<SeasonalSkinId>(settingMode, true, out var parsedId))
        {
            targetId = parsedId;
        }
        else
        {
            targetId = DetectCurrentAutoSkin(DateTime.Now);
        }

        if (_skins.TryGetValue(targetId, out var skin))
        {
            return skin;
        }

        // Fallback Mùa Đông
        return _skins[SeasonalSkinId.Winter];
    }

    public SeasonalSkinDefinition GetSkinById(SeasonalSkinId id)
    {
        return _skins.TryGetValue(id, out var skin) ? skin : _skins[SeasonalSkinId.Winter];
    }

    public IReadOnlyList<SeasonalSkinDefinition> GetAllSkins()
    {
        return _skins.Values.ToList();
    }
}
