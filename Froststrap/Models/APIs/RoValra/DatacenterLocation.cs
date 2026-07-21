namespace Froststrap.Models.APIs.RoValra
{
    public class DatacenterLocation
    {
        [JsonPropertyName("city")]
        public string City { get; set; } = "";

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("country_name")]
        public string? CountryName { get; set; }

        [JsonPropertyName("latLong")]
        public string[] LatLong { get; set; } = null!;
    }
}
