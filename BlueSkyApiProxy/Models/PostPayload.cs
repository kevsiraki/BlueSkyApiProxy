using System.Text.Json.Serialization;

namespace BlueSkyApiProxy.Models
{
    // This class represents the payload for a post request to BlueSky.
    // It includes the repository (DID), collection, and the post record itself.
    /*
        var payload = new
        {
            repo = did,
            collection = "app.bsky.feed.post",
            record = new
            {
                text = text,
                createdAt = DateTime.UtcNow.ToString("o")
                ...
            }
        };
    */
    public class PostPayload
    {
        [JsonPropertyName("repo")]
        public string? Repo { get; set; }
        [JsonPropertyName("collection")]
        public string? Collection { get; set; }
        [JsonPropertyName("record")]
        public Post? Record { get; set; }
        
    }
}
