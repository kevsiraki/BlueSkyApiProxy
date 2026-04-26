namespace BlueSkyApiProxy.Models
{
    // This class represents the structure of a post that will be sent to BlueSky.
    // It is part of the PostPayload, which includes the repository (DID), collection, and the post record itself.

    public class Post
    {
        public string? text { get; set; }
        public string? createdAt { get; set; }
        public object? embed { get; set; }
    }
}
