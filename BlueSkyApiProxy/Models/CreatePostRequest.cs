namespace BlueSkyApiProxy.Models
{
    public class CreatePostRequest
    {
        public string? text { get; set; }

        // optional image
        public string? imageBase64 { get; set; }
        public string? mimeType { get; set; }
    }
}
