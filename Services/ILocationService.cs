using WeatherApp.Models;

namespace WeatherApp.Services;

public interface ILocationService
{
    Task<GeoLocationInfo> GetCurrentLocationAsync();
    Task<List<GeocodingItem>> SearchLocationsAsync(string query);
    Task<string> GetReverseGeocodedCityNameAsync(double latitude, double longitude);
}
