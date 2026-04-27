using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class ImageRequest
    {
        [JsonPropertyName("base64")]
        public string Base64 { get; set; } = "";
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "";
        [JsonPropertyName("altText")]
        public string? AltText { get; set; }
    }
}
