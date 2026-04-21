using BlueSkyApiProxy.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BlueSkyApiProxy.Services
{
    public class BlueSkyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly BlueSkyOptions _options;

        private readonly string serviceUrl;
        private readonly string identifier;
        private readonly string password;

        public BlueSkyService(HttpClient httpClient, IConfiguration config, IOptions<BlueSkyOptions> options)
        {
            _httpClient = httpClient;
            _config = config;
            _options = options.Value;

            serviceUrl = _options.ServiceUrl;
            identifier = _options.Identifier;
            password = _options.Password;
        }

        // The purpose of this method is to authenticate with the Bluesky API and retrieve the necessary access tokens (DID and JWT) for subsequent API calls.
        // It sends a login request with the user's credentials and processes the response to extract the tokens.
        private async Task<(string? did, string? accessJwt)> getAccessTokens()
        {
            // 1. Login (createSession)
            var loginPayload = new Login
            {
                identifier = identifier,
                password = password
            };

            var loginResponse = await _httpClient.PostAsync(
                $"{serviceUrl}/xrpc/com.atproto.server.createSession",
                new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json")
            );

            loginResponse.EnsureSuccessStatusCode();

            string? loginJson = await loginResponse.Content.ReadAsStringAsync();
            var loginDoc = JsonDocument.Parse(loginJson);

            string? accessJwt = loginDoc.RootElement.GetProperty("accessJwt").GetString();
            string? did = loginDoc.RootElement.GetProperty("did").GetString();

            return (did, accessJwt);

        }

        // The purpose of this method is to post a text update to the user's Bluesky feed.
        // It first retrieves the necessary access tokens by logging in, then constructs a payload for the post, and finally sends a request to create the post on Bluesky.
        public async Task postToBluesky(string? text)
        {
            // 2. Create Post (repo.createRecord)
            var (did, accessJwt) = await getAccessTokens();

            var payload = new PostPayload
            {
                repo = did,
                collection = "app.bsky.feed.post",
                record = new Post
                {
                    text = text,
                    createdAt = DateTime.UtcNow.ToString("o")
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.createRecord")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {accessJwt}");

            var postResponse = await _httpClient.SendAsync(request);

            postResponse.EnsureSuccessStatusCode();
        }
    }
}