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

        // com.atproto.repo.uploadBlob
        // The purpose of this method is to upload a binary blob (such as an image or file) to the Bluesky API.
        // It first retrieves the necessary access tokens by logging in, then constructs an HTTP request with the blob data and appropriate headers/
        // Finally it sends the request to the API endpoint responsible for handling blob uploads.

        // https://docs.bsky.app/docs/api/com-atproto-repo-upload-blob
        public async Task<JsonElement> uploadBlob(byte[] blobData, string mimeType)
        {
            var (did, accessJwt) = await getAccessTokens();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.uploadBlob")
            {
                Content = new ByteArrayContent(blobData)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            request.Headers.Add("Authorization", $"Bearer {accessJwt}");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Bluesky error: {response.StatusCode} - {responseBody}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("blob");
        }

        private ImageEmbed CreateImageEmbed(JsonElement blob, string altText)
        {
            return new ImageEmbed
            {
                images = new List<ImageItem>
                {
                    new ImageItem
                    {
                        alt = altText,
                        image = blob
                    }
                }
            };
        }

        // com.atproto.repo.createRecord
        // The purpose of this method is to post a text update to the user's Bluesky feed.
        // It first retrieves the necessary access tokens by logging in, then constructs a payload for the post, and finally sends a request to create the post on Bluesky.

        // To generate Base64 Images in PowerShell:
        // [Convert]::ToBase64String([IO.File]::ReadAllBytes("test.png"))
        public async Task postToBluesky(string? text, byte[]? imageBytes = null, string? mimeType = null)
        {
            var (did, accessJwt) = await getAccessTokens();

            object? embed = null;

            // If image provided → upload + attach
            if (imageBytes != null && mimeType != null)
            {
                var blob = await uploadBlob(imageBytes, mimeType);
                embed = CreateImageEmbed(blob, "uploaded image");
            }

            // Build record first
            var record = new Post
            {
                text = text,
                createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
            };

            // Only set embed if it exists
            if (embed != null)
            {
                record.embed = embed;
            }

            var payload = new PostPayload
            {
                repo = did,
                collection = "app.bsky.feed.post",
                record = record
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            Console.WriteLine(json); // debug

            var request = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.createRecord")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {accessJwt}");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Bluesky error: {response.StatusCode} - {responseBody}");
            }
        }

        /*
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
        */
    }
}