using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    public class FacetIndex
    {
        [JsonPropertyName("byteStart")]
        public int ByteStart { get; set; }
        [JsonPropertyName("byteEnd")]
        public int ByteEnd { get; set; }
    }
}
