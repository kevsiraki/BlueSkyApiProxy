using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class ImageItem
    {
        [JsonPropertyName("alt")]
        public string Alt { get; set; } = "";
        [JsonPropertyName("image")]
        public JsonElement Image { get; set; }
    }
}
