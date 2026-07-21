namespace Froststrap.Models.APIs.RoValra
{
    public class RoValrasServer
    {
        [JsonPropertyName("first_seen")]
        public DateTime? FirstSeen { get; set; }

        [JsonPropertyName("server_id")]
        public string? ServerId { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("datacenter_id")]
        public int? DatacenterId { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("region_code")]
        public string? RegionCode { get; set; }

        [JsonPropertyName("place_version")]
        public int? PlaceVersion { get; set; }

        [JsonPropertyName("playing")]
        public int? Playing { get; set; }

        [JsonPropertyName("max_players")]
        public int? MaxPlayers { get; set; }
    }

    public class RoValraRegionServersResponse
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }

        [JsonPropertyName("servers")]
        public List<RoValrasServer>? Servers { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class RoValraTimeResponse
    {
        [JsonPropertyName("servers")]
        public List<RoValrasServer>? Servers { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
