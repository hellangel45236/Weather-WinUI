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
}

public class OutfitAdvice
{
    public string Occasion { get; set; } = "Work"; // "Work", "School", "Casual"
    public string OccasionTitle { get; set; } = "Công sở / Đi làm";
    public string Headline { get; set; } = string.Empty;
    public string ThermalComfortNotice { get; set; } = string.Empty;
    public string ThermalTagColor { get; set; } = "#10B981";

    // Phân loại trang phục chi tiết
    public string TopClothing { get; set; } = string.Empty;
    public string BottomClothing { get; set; } = string.Empty;
    public string Outerwear { get; set; } = string.Empty;
    public string Footwear { get; set; } = string.Empty;

    // Cảnh báo di chuyển xe máy (đặc thù giao thông Việt Nam)
    public string MotorbikeWarning { get; set; } = string.Empty;
    public string MotorbikeWarningIcon { get; set; } = "\uf21c"; // motorcycle icon
    public string MotorbikeWarningColor { get; set; } = "#F59E0B";

    // Danh sách vật dụng / phụ kiện nên mang theo
    public List<OutfitAccessoryItem> Accessories { get; set; } = new();

    // Câu tóm tắt nhanh OOTD
    public string QuickSummary { get; set; } = string.Empty;
}
