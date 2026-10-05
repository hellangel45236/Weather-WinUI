namespace WeatherApp.Models;

public enum AdviceSeverity
{
    Info,       // Thông tin / gợi ý tốt (Xanh lam / Xanh lá)
    Warning,    // Cần lưu ý (Cam / Vàng)
    Alert       // Cảnh báo thời tiết xấu / nguy hiểm (Đỏ / Tím)
}

public class WeatherAdvice
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
    public AdviceSeverity Severity { get; set; } = AdviceSeverity.Info;
    public string BadgeText => Severity switch
    {
        AdviceSeverity.Alert => "CẢNH BÁO",
        AdviceSeverity.Warning => "LƯU Ý",
        _ => "GỢI Ý"
    };
    public string BadgeBackground => Severity switch
    {
        AdviceSeverity.Alert => "#E81123",
        AdviceSeverity.Warning => "#FF8C00",
        _ => "#0078D4"
    };
}
