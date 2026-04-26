namespace BlueSkyApiProxy.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ImageEmbed
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; } = "app.bsky.embed.images";

        public List<ImageItem> images { get; set; } = new();
    }

    public class ImageItem
    {
        public string alt { get; set; } = "";
        public JsonElement image { get; set; }
    }
}
