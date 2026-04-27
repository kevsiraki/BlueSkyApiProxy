using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class CreatePostRequest
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("images")]
        public List<ImageRequest>? Images { get; set; }
        [JsonPropertyName("facets")]
        public List<Facet>? Facets { get; set; }
    }
}
