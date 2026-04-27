using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class FacetFeature
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; } = default!;

        // for links
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        // for mentions
        [JsonPropertyName("did")]
        public string? Did { get; set; }

        // for hashtags
        [JsonPropertyName("tag")]
        public string? Tag { get; set; }
    }
}
