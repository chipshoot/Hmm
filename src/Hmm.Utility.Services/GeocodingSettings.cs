namespace Hmm.Utility.Services
{
    public class GeocodingSettings
    {
        public const string SectionName = "GeocodingSettings";

        public string Provider { get; set; } = "Nominatim";

        public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

        public string ApiKey { get; set; }

        public string UserAgent { get; set; } = "HmmApp/1.0";
    }
}
