using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    // This class represents the payload for a login request to BlueSky.
    public class Login
    {
        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }
}
