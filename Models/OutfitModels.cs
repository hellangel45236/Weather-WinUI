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
    public string ReflectionRate { get; set; } = "85%";
}

public class OutfitTimeSlotItem
{
    public string SlotKey { get; set; } = "morning";
    public string TimeDisplay { get; set; } = "07:00";
    public string PeriodName { get; set; } = "Sáng Dịu Mát";
    public string TempDisplay { get; set; } = "25°C";
    public string WeatherIconGlyph { get; set; } = "\uf185";
    public string SummaryText { get; set; } = "Sơ mi nhẹ + Khoác mỏng";
    public string AccentColor { get; set; } = "#38BDF8";
    public bool IsActive { get; set; } = false;
}

public class OutfitAdvice
{
    public string Occasion { get; set; } = "Work"; // "Work", "School", "Casual", "Sport", "Travel"
    public string OccasionTitle { get; set; } = "Công sở / Đi làm";
    public string GenderStyle { get; set; } = "All"; // "All", "Men", "Women"
    public string GenderStyleTitle { get; set; } = "Tự do / Unisex";
    public string CommuteMethod { get; set; } = "Motorbike"; // "Motorbike", "Car", "Bus", "Walk"
    public string CommuteMethodTitle { get; set; } = "Xe máy";
    public string ThermalPreference { get; set; } = "Normal"; // "ColdSensitive", "Normal", "HeatSensitive"
    public string Headline { get; set; } = string.Empty;
    public string FashionStyleTag { get; set; } = "Smart Casual";
    public string ThermalComfortNotice { get; set; } = string.Empty;
    public string ThermalTagColor { get; set; } = "#10B981";
    public string FabricRecommendation { get; set; } = "Cotton 100% thoáng khí & co giãn tự nhiên";

    // 6 Lớp Phối Đồ Chi Tiết (6-Layer Modular Wardrobe Anatomy)
    public string Headwear { get; set; } = string.Empty;
    public string HeadwearIcon { get; set; } = "\uf06e";
    public string TopClothing { get; set; } = string.Empty;
    public string TopClothingIcon { get; set; } = "\uf553";
    public string Outerwear { get; set; } = string.Empty;
    public string OuterwearIcon { get; set; } = "\uf72e";
    public string BottomClothing { get; set; } = string.Empty;
    public string BottomClothingIcon { get; set; } = "\uf005";
    public string Footwear { get; set; } = string.Empty;
    public string FootwearIcon { get; set; } = "\uf549";
    public string CarryGear { get; set; } = string.Empty;
    public string CarryGearIcon { get; set; } = "\uf290";

    // Chỉ số khoa học vật liệu & thân nhiệt (Climate Science Telemetry)
    public int BreathabilityScore { get; set; } = 92;
    public string BreathabilityLabel { get; set; } = "Rất thoáng khí";
    public string UvDefenseRating { get; set; } = "UPF 50+ (Tối đa)";
    public string WaterRepellency { get; set; } = "Kháng nước nhẹ DWR";
    public string ThermalReflectionRating { get; set; } = "88% phản xạ";

    // Cảnh báo di chuyển xe máy & giao thông Việt Nam
    public string MotorbikeWarning { get; set; } = string.Empty;
    public string MotorbikeWarningIcon { get; set; } = "\uf21c"; // motorcycle icon
    public string MotorbikeWarningColor { get; set; } = "#F59E0B";
    public string RaincoatAdvice { get; set; } = string.Empty;

    // Khung giờ mô phỏng trong ngày (Time Simulation Scrubber)
    public List<OutfitTimeSlotItem> TimeSlots { get; set; } = new();
    public string SelectedTimeSlotKey { get; set; } = "live";

    // Bảng màu trang phục đề xuất (Weather-matched Color Palette)
    public List<OutfitColorItem> SuggestedColors { get; set; } = new();

    // Danh sách vật dụng / phụ kiện nên mang theo
    public List<OutfitAccessoryItem> Accessories { get; set; } = new();

    // Câu tóm tắt nhanh OOTD
    public string QuickSummary { get; set; } = string.Empty;
    public string ShareableSummaryText { get; set; } = string.Empty;
}
