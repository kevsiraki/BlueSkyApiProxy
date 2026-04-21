namespace BlueSkyApiProxy.Models
{
    // This class represents the payload for a login request to BlueSky.
    public class Login
    {
        public string? identifier { get; set; }
        public string? password { get; set; }
    }
}
