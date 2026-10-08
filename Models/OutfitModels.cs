using System;
using System.Collections.Generic;

namespace WeatherApp.Models;

public class OutfitAccessoryItem
{
    public string Name { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "\uf0e9"; // Default umbrella
    public string AccentColor { get; set; } = "#38BDF8";
    public bool IsEssential { get; set; } = false;
    public string BadgeText { get; set; } = string.Empty;
}

public class OutfitColorItem
{
    public string HexColor { get; set; } = "#38BDF8";
    public string Name { get; set; } = "Xanh Pastel";
}

public class OutfitAdvice
{
    public string Occasion { get; set; } = "Work"; // "Work", "School", "Casual", "Sport", "Travel"
    public string OccasionTitle { get; set; } = "Công sở / Đi làm";
    public string GenderStyle { get; set; } = "All"; // "All", "Men", "Women"
    public string GenderStyleTitle { get; set; } = "Tự do / Unisex";
    public string Headline { get; set; } = string.Empty;
    public string FashionStyleTag { get; set; } = "Smart Casual";
    public string ThermalComfortNotice { get; set; } = string.Empty;
    public string ThermalTagColor { get; set; } = "#10B981";
    public string FabricRecommendation { get; set; } = "Cotton 100% thoáng khí & co giãn tự nhiên";

    // Phân loại trang phục chi tiết
    public string TopClothing { get; set; } = string.Empty;
    public string BottomClothing { get; set; } = string.Empty;
    public string Outerwear { get; set; } = string.Empty;
    public string Footwear { get; set; } = string.Empty;

    // Cảnh báo di chuyển xe máy (đặc thù giao thông Việt Nam)
    public string MotorbikeWarning { get; set; } = string.Empty;
    public string MotorbikeWarningIcon { get; set; } = "\uf21c"; // motorcycle icon
    public string MotorbikeWarningColor { get; set; } = "#F59E0B";
    public string RaincoatAdvice { get; set; } = string.Empty;

    // Bảng màu trang phục đề xuất (Weather-matched Color Palette)
    public List<OutfitColorItem> SuggestedColors { get; set; } = new();

    // Danh sách vật dụng / phụ kiện nên mang theo
    public List<OutfitAccessoryItem> Accessories { get; set; } = new();

    // Câu tóm tắt nhanh OOTD
    public string QuickSummary { get; set; } = string.Empty;
    public string ShareableSummaryText { get; set; } = string.Empty;
}
