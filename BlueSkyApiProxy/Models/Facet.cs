using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class Facet
    {
        [JsonPropertyName("index")]
        public FacetIndex Index { get; set; } = new();
        [JsonPropertyName("features")]
        public List<FacetFeature> Features { get; set; } = new();
    }
}
