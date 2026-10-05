using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using WeatherApp.Models;

namespace WeatherApp.Helpers;

public static class CityBackgroundHelper
{
    private static readonly Dictionary<string, string> CityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "hồ chí minh", "Sài Gòn.jpg" },
        { "ho chi minh", "Sài Gòn.jpg" },
        { "sài gòn", "Sài Gòn.jpg" },
        { "sai gon", "Sài Gòn.jpg" },
        { "tphcm", "Sài Gòn.jpg" },
        { "hcm", "Sài Gòn.jpg" },
        { "hà nội", "Hà Nội.jpg" },
        { "ha noi", "Hà Nội.jpg" },
        { "hanoi", "Hà Nội.jpg" },
        { "đà nẵng", "Đà Nẵng.jpg" },
        { "da nang", "Đà Nẵng.jpg" },
        { "danang", "Đà Nẵng.jpg" },
        { "đà lạt", "Đà Lạt.jpg" },
        { "da lat", "Đà Lạt.jpg" },
        { "dalat", "Đà Lạt.jpg" },
        { "nha trang", "Nha Trang.jpg" },
        { "khánh hòa", "Nha Trang.jpg" },
        { "hải phòng", "Hải Phòng.png" },
        { "hai phong", "Hải Phòng.png" },
        { "hạ long", "Hạ Long.jpg" },
        { "ha long", "Hạ Long.jpg" },
        { "quảng ninh", "Hạ Long.jpg" },
        { "tây ninh", "Tây Ninh.jpg" },
        { "tay ninh", "Tây Ninh.jpg" },
        { "tokyo", "Tokyo.jpg" },
        { "japan", "Tokyo.jpg" },
        { "new york", "New York.jpg" },
        { "nyc", "New York.jpg" }
    };

    public static string GetCityPictureDirectory()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "City Picture"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "City Picture"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "City Picture")
        };

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir)) return dir;
        }

        return Path.Combine(AppContext.BaseDirectory, "Assets", "City Picture");
    }

    public static List<string> GetAvailablePresets()
    {
        var list = new List<string>();
        string dir = GetCityPictureDirectory();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
                {
                    list.Add(Path.GetFileName(file));
                }
            }
        }
        return list;
    }

    public static string? ResolveImagePath(string locationTitle, AppSettings settings)
    {
        if (!settings.EnableCityBackground) return null;

        string dir = GetCityPictureDirectory();

        // 1. Chế độ ảnh tự chọn từ máy tính
        if (settings.CityBackgroundMode == "Custom" && !string.IsNullOrEmpty(settings.CustomCityImagePath) && File.Exists(settings.CustomCityImagePath))
        {
            return settings.CustomCityImagePath;
        }

        // 2. Chế độ chọn ảnh cố định từ danh sách
        if (settings.CityBackgroundMode == "Preset" && !string.IsNullOrEmpty(settings.SelectedCityImage) && settings.SelectedCityImage != "Auto")
        {
            string presetPath = Path.Combine(dir, settings.SelectedCityImage);
            if (File.Exists(presetPath)) return presetPath;
        }

        // 3. Chế độ Tự động nhận diện theo tên thành phố (Auto)
        if (string.IsNullOrWhiteSpace(locationTitle)) return null;

        string normalized = RemoveDiacritics(locationTitle.ToLowerInvariant());

        foreach (var kvp in CityKeywords)
        {
            string keywordNorm = RemoveDiacritics(kvp.Key);
            if (locationTitle.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || normalized.Contains(keywordNorm, StringComparison.OrdinalIgnoreCase))
            {
                string matchedPath = Path.Combine(dir, kvp.Value);
                if (File.Exists(matchedPath)) return matchedPath;
            }
        }

        return null;
    }

    public static BitmapImage? LoadOptimizedBitmap(string imagePath, int decodeWidth = 850)
    {
        if (!File.Exists(imagePath)) return null;

        try
        {
            var bitmap = new BitmapImage
            {
                DecodePixelWidth = decodeWidth,
                DecodePixelType = DecodePixelType.Logical
            };
            bitmap.UriSource = new Uri(imagePath);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }
}
