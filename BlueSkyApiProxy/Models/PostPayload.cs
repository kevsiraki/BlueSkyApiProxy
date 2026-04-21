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
            }
        };
    */
    public class PostPayload
    {
        public string? repo { get; set; }
        public string? collection { get; set; }
        public Post? record { get; set; }
        
    }
}
