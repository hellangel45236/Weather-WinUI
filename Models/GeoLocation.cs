using System.Text.Json.Serialization;

namespace WeatherApp.Models;

public class GeoLocationInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Country)) return City;
            if (City.EndsWith(Country, StringComparison.OrdinalIgnoreCase) ||
                City.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase) ||
                City.Contains("Viet Nam", StringComparison.OrdinalIgnoreCase) ||
                City.Contains("Vietnam", StringComparison.OrdinalIgnoreCase))
            {
                return City;
            }
            return $"{City}, {Country}";
        }
    }

    public bool IsAutoDetected { get; set; } = true;
    public string DetectionSource { get; set; } = "GPS";

    public GeoLocationInfo() { }

    public GeoLocationInfo(double latitude, double longitude, string city, string country, bool isAutoDetected = true, string detectionSource = "GPS")
    {
        Latitude = latitude;
        Longitude = longitude;
        City = city;
        Country = country;
        IsAutoDetected = isAutoDetected;
        DetectionSource = detectionSource;
    }
}

public class OpenMeteoGeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingItem>? Results { get; set; }
}

public class GeocodingItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("admin1")]
    public string? Admin1 { get; set; }

    public string DisplayText => !string.IsNullOrEmpty(Admin1) && Admin1 != Name
        ? $"{Name}, {Admin1}, {Country}"
        : $"{Name}, {Country}";

    public override string ToString() => DisplayText;
}

public class IpWhoIsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}

public class IpApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }
}

public class ReverseGeocodeResponse
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("locality")]
    public string? Locality { get; set; }

    [JsonPropertyName("principalSubdivision")]
    public string? PrincipalSubdivision { get; set; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; set; }
}

public class GeoJsResponse
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }
}

public class FreeIpApiResponse
{
    [JsonPropertyName("cityName")]
    public string? CityName { get; set; }

    [JsonPropertyName("regionName")]
    public string? RegionName { get; set; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }
}
