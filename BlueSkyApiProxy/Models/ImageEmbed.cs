namespace BlueSkyApiProxy.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ImageEmbed
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; } = "app.bsky.embed.images";
        [JsonPropertyName("images")]
        public List<ImageItem> Images { get; set; } = new();
    }
}
