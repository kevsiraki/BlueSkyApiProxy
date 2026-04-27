using BlueSkyApiProxy.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace BlueSkyApiProxy.Services
{
    public class BlueSkyService
    {
        private readonly HttpClient _httpClient;
        //private readonly IConfiguration _config;
        private readonly BlueSkyOptions _options;

        private readonly string serviceUrl;
        private readonly string identifier;
        private readonly string password;

        // added these for caching the login
        private string? _cachedDid;
        private string? _cachedAccessJwt;
        private string? _cachedRefreshJwt;

        // Semaphore to ensure only one login/refresh happens at a time, preventing race conditions
        private readonly SemaphoreSlim _authLock = new(1, 1);

        // loggging
        private readonly ILogger<BlueSkyService> _logger;

        //public BlueSkyService(HttpClient httpClient, IConfiguration config, IOptions<BlueSkyOptions> options, ILogger<BlueSkyService> logger)
        public BlueSkyService(IHttpClientFactory httpClientFactory, IOptions<BlueSkyOptions> options, ILogger<BlueSkyService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            //_config = config;
            _options = options.Value;

            serviceUrl = _options.ServiceUrl;
            identifier = _options.Identifier;
            password = _options.Password;
            _logger = logger;
        }

        //com.atproto.server.createSession

        // The purpose of this method is to authenticate with the Bluesky API and retrieve the necessary access tokens (DID and JWT) for subsequent API calls.
        // It sends a login request with the user's credentials and processes the response to extract the tokens.
        // Updated getAccessTokens method with caching and semaphore for thread safety

        //https://docs.bsky.app/docs/api/com-atproto-server-create-session
        private async Task<(string? did, string? accessJwt)> getAccessTokens()
        {
            if (!string.IsNullOrEmpty(_cachedAccessJwt))
                return (_cachedDid, _cachedAccessJwt);

            await _authLock.WaitAsync();
            try
            {
                _logger.LogDebug("Creating new Bluesky session");

                if (!string.IsNullOrEmpty(_cachedAccessJwt))
                    return (_cachedDid, _cachedAccessJwt);

                var loginPayload = new Login
                {
                    Identifier = identifier,
                    Password = password
                };

                using var response = await _httpClient.PostAsync(
                    $"{serviceUrl}/xrpc/com.atproto.server.createSession",
                    new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json")
                );

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                _cachedAccessJwt = doc.RootElement.GetProperty("accessJwt").GetString();
                _cachedRefreshJwt = doc.RootElement.GetProperty("refreshJwt").GetString();
                _cachedDid = doc.RootElement.GetProperty("did").GetString();

                return (_cachedDid, _cachedAccessJwt);
            }
            finally
            {
                _authLock.Release();
            }
        }

        // POST /xrpc/com.atproto.repo.uploadBlob

        // The purpose of this method is to upload a binary blob (such as an image or file) to the Bluesky API.
        // It first retrieves the necessary access tokens by logging in, then constructs an HTTP request with the blob data and appropriate headers/
        // Finally it sends the request to the API endpoint responsible for handling blob uploads.

        // https://docs.bsky.app/docs/api/com-atproto-repo-upload-blob
        public async Task<JsonElement> uploadBlob(byte[] blobData, string mimeType)
        {
            var response = await SendWithAuthRetry((jwt) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.uploadBlob")
                {
                    Content = new ByteArrayContent(blobData)
                };

                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                req.Headers.Add("Authorization", $"Bearer {jwt}");

                return req;
            });

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bluesky error: {response.StatusCode} - {responseBody}");

            var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("blob");
        }

        // POST /xrpc/app.bsky.video.uploadVideo [WIP]

        // The purpose of this method is to upload a video file to the Bluesky API using the app.bsky.video.uploadVideo endpoint.
        // Similar to the uploadBlob method, it retrieves access tokens, constructs an HTTP request with the video data and appropriate headers,
        // and sends the request to the API endpoint responsible for handling video uploads.

        // https://docs.bsky.app/docs/api/app-bsky-video-upload-video
        public async Task<JsonElement> uploadVideo(byte[] videoData, string mimeType)
        {
            var response = await SendWithAuthRetry((jwt) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/app.bsky.video.uploadVideo")
                {
                    Content = new ByteArrayContent(videoData)
                };
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                req.Headers.Add("Authorization", $"Bearer {jwt}");
                return req;
            });
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Bluesky error: {response.StatusCode} - {responseBody}");
            var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("video");
        }

        // POST com.atproto.repo.deleteRecord

        // The purpose of this method is to delete a specific post from the user's Bluesky feed.
        // It first retrieves the necessary access tokens by logging in, then constructs a payload with the repository (DID), collection, and record key (rkey) of the post to be deleted.
        // Finally, it sends a request to the API endpoint responsible for handling record deletions and processes the response to ensure the deletion was successful.

        // https://docs.bsky.app/docs/api/com-atproto-repo-delete-record
        public async Task deletePost(string rkey)
        {
            var (did, _) = await getAccessTokens();

            var payload = new
            {
                repo = did,
                collection = "app.bsky.feed.post",
                rkey = rkey
            };

            var json = JsonSerializer.Serialize(payload);

            var response = await SendWithAuthRetry((jwt) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.deleteRecord")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                req.Headers.Add("Authorization", $"Bearer {jwt}");
                return req;
            });

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Delete failed: {response.StatusCode} - {body}");
            }
        }

        // POST /xrpc/com.atproto.repo.createRecord

        // The purpose of this method is to post a text update to the user's Bluesky feed.
        // It first retrieves the necessary access tokens by logging in, then constructs a payload for the post, and finally sends a request to create the post on Bluesky.

        // To generate Base64 Images in PowerShell:
        // [Convert]::ToBase64String([IO.File]::ReadAllBytes("test.png"))

        // Updated postToBluesky method to support multiple images and auto-generating facets from text URLs

        // https://docs.bsky.app/docs/api/com-atproto-repo-create-record
        public async Task postToBluesky(
            string? text,
            List<(byte[] data, string mimeType, string altText)>? images = null,
            List<Facet>? facets = null)
        {
            var (did, _) = await getAccessTokens();

            object? embed = null;

            if (images != null && images.Count > 0)
            {
                if (images.Count > 4)
                    throw new Exception("Bluesky supports a maximum of 4 images per post.");

                var imageItems = new List<ImageItem>();

                foreach (var img in images)
                {
                    var blob = await uploadBlob(img.data, img.mimeType);

                    imageItems.Add(new ImageItem
                    {
                        Alt = img.altText ?? "image",
                        Image = blob
                    });
                }

                embed = new ImageEmbed
                {
                    Images = imageItems
                };
            }

            // Build record first
            var record = new Post
            {
                Text = text,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
            };

            // Auto-generate facets from text
            var autoFacets = CreateFacetsFromText(text);

            // Merge manual + auto facets if both exist
            if (facets != null && facets.Count > 0)
            {
                if (autoFacets != null)
                    facets.AddRange(autoFacets);

                record.Facets = facets;
            }
            else if (autoFacets != null)
            {
                record.Facets = autoFacets;
            }

            // Only set embed if it exists
            if (embed != null)
            {
                record.Embed = embed;
            }

            var payload = new PostPayload
            {
                Repo = did,
                Collection = "app.bsky.feed.post",
                Record = record
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            });

            _logger.LogDebug("Posting payload: {Payload}", json);

            var response = await SendWithAuthRetry((jwt) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"{serviceUrl}/xrpc/com.atproto.repo.createRecord")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                req.Headers.Add("Authorization", $"Bearer {jwt}");
                return req;
            });

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Bluesky error: {response.StatusCode} - {responseBody}");
            }
        }

        // This is a simplified version of the postToBluesky method that only handles text posts without images.
        // Commented out for now, but can be useful for testing or as a fallback if image upload fails.
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

        // Helpers
        // Refresh session method to get new access token using refresh token
        private async Task RefreshSession()
        {
            await _authLock.WaitAsync();
            try
            {
                _logger.LogDebug("Refreshing Bluesky session");

                if (string.IsNullOrEmpty(_cachedRefreshJwt))
                {
                    _cachedAccessJwt = null;
                    await getAccessTokens();
                    return;
                }

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{serviceUrl}/xrpc/com.atproto.server.refreshSession"
                );

                request.Headers.Add("Authorization", $"Bearer {_cachedRefreshJwt}");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Refresh failed, falling back to login");
                    _cachedAccessJwt = null;
                    _cachedRefreshJwt = null;
                    await getAccessTokens();
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                _cachedAccessJwt = doc.RootElement.GetProperty("accessJwt").GetString();
                _cachedRefreshJwt = doc.RootElement.GetProperty("refreshJwt").GetString();
            }
            finally
            {
                _authLock.Release();
            }
        }

        // Auth Retry Logic: If any API call returns 401 Unauthorized, we can call RefreshSession to attempt to get a new access token and retry the request.
        private async Task<HttpResponseMessage> SendWithAuthRetry(Func<string, HttpRequestMessage> requestFactory)
        {
            var (_, accessJwt) = await getAccessTokens();

            var request = requestFactory(accessJwt);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                // simple retry once
                var retry = requestFactory(accessJwt);
                response = await _httpClient.SendAsync(retry);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();

                await RefreshSession();

                if (string.IsNullOrEmpty(_cachedAccessJwt))
                    throw new Exception("Authentication failed after refresh.");

                var retryRequest = requestFactory(_cachedAccessJwt);
                return await _httpClient.SendAsync(retryRequest);
            }

            return response;
        }
        private List<Facet>? CreateFacetsFromText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var facets = new List<Facet>();

            var matches = System.Text.RegularExpressions.Regex.Matches(
                text,
                @"https?:\/\/[^\s]+"
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var url = match.Value;

                int byteStart = Encoding.UTF8.GetByteCount(text.Substring(0, match.Index));
                int byteEnd = byteStart + Encoding.UTF8.GetByteCount(url);

                facets.Add(new Facet
                {
                    Index = new FacetIndex
                    {
                        ByteStart = byteStart,
                        ByteEnd = byteEnd
                    },
                    Features = new List<FacetFeature>
                    {
                        new FacetFeature
                        {
                            Type = "app.bsky.richtext.facet#link",
                            Uri = url
                        }
                    }
                });
            }

            return facets.Count > 0 ? facets : null;
        }
    }
}