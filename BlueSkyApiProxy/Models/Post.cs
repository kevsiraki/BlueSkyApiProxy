using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    // This class represents the structure of a post that will be sent to BlueSky.
    // It is part of the PostPayload, which includes the repository (DID), collection, and the post record itself.

    public class Post
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }
        [JsonPropertyName("embed")]
        public object? Embed { get; set; }
        [JsonPropertyName("facets")]
        public List<Facet>? Facets { get; set; } // Added Facets property to support text facets
    }
}
