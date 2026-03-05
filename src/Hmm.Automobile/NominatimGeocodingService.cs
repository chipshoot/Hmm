using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly GeocodingSettings _settings;
        private readonly ILogger<NominatimGeocodingService> _logger;

        public NominatimGeocodingService(
            HttpClient httpClient,
            IOptions<GeocodingSettings> settings,
            ILogger<NominatimGeocodingService> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ProcessingResult<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude)
        {
            if (latitude is < -90 or > 90)
            {
                return ProcessingResult<GeoAddress>.Invalid("Latitude must be between -90 and 90");
            }

            if (longitude is < -180 or > 180)
            {
                return ProcessingResult<GeoAddress>.Invalid("Longitude must be between -180 and 180");
            }

            try
            {
                var lat = latitude.ToString(CultureInfo.InvariantCulture);
                var lon = longitude.ToString(CultureInfo.InvariantCulture);
                var requestUrl = $"{_settings.BaseUrl}/reverse?lat={lat}&lon={lon}&format=json&addressdetails=1";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("User-Agent", _settings.UserAgent);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("error", out var errorElement))
                {
                    return ProcessingResult<GeoAddress>.NotFound(errorElement.GetString() ?? "Location not found");
                }

                var address = root.GetProperty("address");
                var geoAddress = new GeoAddress
                {
                    Street = GetAddressComponent(address, "road"),
                    City = GetAddressComponent(address, "city", "town", "village", "hamlet"),
                    State = GetAddressComponent(address, "state", "province"),
                    Country = GetAddressComponent(address, "country"),
                    ZipCode = GetAddressComponent(address, "postcode"),
                    FormattedAddress = root.TryGetProperty("display_name", out var displayName)
                        ? displayName.GetString()
                        : string.Empty,
                    Latitude = latitude,
                    Longitude = longitude
                };

                return ProcessingResult<GeoAddress>.Ok(geoAddress);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Geocoding request failed for ({Latitude}, {Longitude})", latitude, longitude);
                return ProcessingResult<GeoAddress>.Fail($"Geocoding service unavailable: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse geocoding response for ({Latitude}, {Longitude})", latitude, longitude);
                return ProcessingResult<GeoAddress>.Fail("Failed to parse geocoding response");
            }
        }

        private static string GetAddressComponent(JsonElement address, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (address.TryGetProperty(key, out var value))
                {
                    return value.GetString();
                }
            }
            return string.Empty;
        }
    }
}
