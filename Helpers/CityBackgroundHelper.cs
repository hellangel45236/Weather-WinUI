using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using WeatherApp.Models;

namespace WeatherApp.Helpers;

public static class CityBackgroundHelper
{
    private static readonly Dictionary<string, string> CityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Hồ Chí Minh / Sài Gòn & các quận huyện, phường
        { "hồ chí minh", "Sài Gòn.jpg" },
        { "ho chi minh", "Sài Gòn.jpg" },
        { "sài gòn", "Sài Gòn.jpg" },
        { "sai gon", "Sài Gòn.jpg" },
        { "tphcm", "Sài Gòn.jpg" },
        { "hcm", "Sài Gòn.jpg" },
        { "đa kao", "Sài Gòn.jpg" },
        { "da kao", "Sài Gòn.jpg" },
        { "bến nghé", "Sài Gòn.jpg" },
        { "ben nghe", "Sài Gòn.jpg" },
        { "bến thành", "Sài Gòn.jpg" },
        { "ben thanh", "Sài Gòn.jpg" },
        { "quận 1", "Sài Gòn.jpg" },
        { "quan 1", "Sài Gòn.jpg" },
        { "quận 3", "Sài Gòn.jpg" },
        { "quan 3", "Sài Gòn.jpg" },
        { "quận 4", "Sài Gòn.jpg" },
        { "quận 5", "Sài Gòn.jpg" },
        { "quận 7", "Sài Gòn.jpg" },
        { "quận 10", "Sài Gòn.jpg" },
        { "bình thạnh", "Sài Gòn.jpg" },
        { "binh thanh", "Sài Gòn.jpg" },
        { "phú nhuận", "Sài Gòn.jpg" },
        { "phu nhuan", "Sài Gòn.jpg" },
        { "thủ đức", "Sài Gòn.jpg" },
        { "thu duc", "Sài Gòn.jpg" },
        { "tân bình", "Sài Gòn.jpg" },
        { "tan binh", "Sài Gòn.jpg" },
        { "gò vấp", "Sài Gòn.jpg" },
        { "go vap", "Sài Gòn.jpg" },

        // Hà Nội & các quận huyện
        { "hà nội", "Hà Nội.jpg" },
        { "ha noi", "Hà Nội.jpg" },
        { "hanoi", "Hà Nội.jpg" },
        { "ba đình", "Hà Nội.jpg" },
        { "ba dinh", "Hà Nội.jpg" },
        { "hoàn kiếm", "Hà Nội.jpg" },
        { "hoan kiem", "Hà Nội.jpg" },
        { "tây hồ", "Hà Nội.jpg" },
        { "tay ho", "Hà Nội.jpg" },
        { "đống đa", "Hà Nội.jpg" },
        { "dong da", "Hà Nội.jpg" },
        { "cầu giấy", "Hà Nội.jpg" },
        { "cau giay", "Hà Nội.jpg" },
        { "hai bà trưng", "Hà Nội.jpg" },
        { "hai ba trung", "Hà Nội.jpg" },
        { "hoàng mai", "Hà Nội.jpg" },
        { "hoang mai", "Hà Nội.jpg" },
        { "thanh xuân", "Hà Nội.jpg" },
        { "thanh xuan", "Hà Nội.jpg" },
        { "hà đông", "Hà Nội.jpg" },
        { "ha dong", "Hà Nội.jpg" },
        { "nam từ liêm", "Hà Nội.jpg" },
        { "bắc từ liêm", "Hà Nội.jpg" },
        { "long biên", "Hà Nội.jpg" },

        // Đà Nẵng
        { "đà nẵng", "Đà Nẵng.jpg" },
        { "da nang", "Đà Nẵng.jpg" },
        { "danang", "Đà Nẵng.jpg" },
        { "hải châu", "Đà Nẵng.jpg" },
        { "sơn trà", "Đà Nẵng.jpg" },
        { "ngũ hành sơn", "Đà Nẵng.jpg" },
        { "thanh khê", "Đà Nẵng.jpg" },

        // Đà Lạt / Lâm Đồng
        { "đà lạt", "Đà Lạt.jpg" },
        { "da lat", "Đà Lạt.jpg" },
        { "dalat", "Đà Lạt.jpg" },
        { "lâm đồng", "Đà Lạt.jpg" },
        { "lam dong", "Đà Lạt.jpg" },

        // Nha Trang / Khánh Hòa
        { "nha trang", "Nha Trang.jpg" },
        { "khánh hòa", "Nha Trang.jpg" },
        { "khanh hoa", "Nha Trang.jpg" },

        // Hải Phòng
        { "hải phòng", "Hải Phòng.png" },
        { "hai phong", "Hải Phòng.png" },

        // Hạ Long / Quảng Ninh
        { "hạ long", "Hạ Long.jpg" },
        { "ha long", "Hạ Long.jpg" },
        { "quảng ninh", "Hạ Long.jpg" },
        { "quang ninh", "Hạ Long.jpg" },

        // Tây Ninh
        { "tây ninh", "Tây Ninh.jpg" },
        { "tay ninh", "Tây Ninh.jpg" },

        // Quốc tế
        { "tokyo", "Tokyo.jpg" },
        { "japan", "Tokyo.jpg" },
        { "nhật bản", "Tokyo.jpg" },
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

        // 2. Chế độ chọn ảnh cố định từ danh sách mẫu
        if (settings.CityBackgroundMode == "Preset" && !string.IsNullOrEmpty(settings.SelectedCityImage) && settings.SelectedCityImage != "Auto")
        {
            string presetPath = Path.Combine(dir, settings.SelectedCityImage);
            if (File.Exists(presetPath)) return presetPath;
        }

        // 3. Chế độ Tự động nhận diện theo tên thành phố (Auto)
        if (!string.IsNullOrWhiteSpace(locationTitle))
        {
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
        }

        // 4. Fallback mặc định: đảm bảo luôn có hình nền nghệ thuật khi tính năng này được BẬT
        string defaultPath = Path.Combine(dir, "Sài Gòn.jpg");
        if (File.Exists(defaultPath)) return defaultPath;

        var available = GetAvailablePresets();
        if (available.Count > 0)
        {
            string firstPath = Path.Combine(dir, available[0]);
            if (File.Exists(firstPath)) return firstPath;
        }

        return null;
    }

    public static BitmapImage? LoadOptimizedBitmap(string imagePath, int decodeWidth = 850)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return null;

        try
        {
            var bitmap = new BitmapImage
            {
                DecodePixelWidth = decodeWidth,
                DecodePixelType = DecodePixelType.Logical
            };

            using var fileStream = File.OpenRead(imagePath);
            var memStream = new MemoryStream();
            fileStream.CopyTo(memStream);
            memStream.Position = 0;
            bitmap.SetSource(memStream.AsRandomAccessStream());
            return bitmap;
        }
        catch
        {
            try
            {
                var fallback = new BitmapImage
                {
                    DecodePixelWidth = decodeWidth,
                    DecodePixelType = DecodePixelType.Logical,
                    UriSource = new Uri(imagePath)
                };
                return fallback;
            }
            catch
            {
                return null;
            }
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
