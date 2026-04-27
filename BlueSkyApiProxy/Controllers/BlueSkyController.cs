using BlueSkyApiProxy.Attributes;
using BlueSkyApiProxy.Models;
using BlueSkyApiProxy.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueSkyApiProxy.Controllers
{
    [ApiController]
    [ApiKey]
    [Route("api/[controller]")]
    public class BlueSkyController : ControllerBase
    {
        private readonly BlueSkyService _blueSkyService;

        public BlueSkyController(BlueSkyService blueSkyService)
        {
            _blueSkyService = blueSkyService;
        }

        [HttpPost("post")]
        public async Task<IActionResult> PostToBluesky([FromBody] CreatePostRequest request)
        {
            try
            {
                /*
                byte[]? imageBytes = null;

                if (!string.IsNullOrEmpty(request.imageBase64))
                {
                    imageBytes = Convert.FromBase64String(request.imageBase64);
                }

                await _blueSkyService.postToBluesky(
                    request.text,
                    imageBytes,
                    request.mimeType
                );
                */

                /* 
                 * This code snippet has been updated to support multiple images in a post.
                 * The CreatePostRequest model has been modified to include a list of ImageRequest objects, which contain the base64 string, MIME type, and alt text for each image. The controller method now processes this list and sends it to the BlueSkyService for posting. 
                 * This allows users to create posts with multiple images, enhancing the functionality of the API.
                 */


                var images = request.Images?.Select(img => (
                    data: Convert.FromBase64String(img.Base64),
                    mimeType: img.MimeType,
                    altText: img.AltText ?? "image"
                )).ToList();

                await _blueSkyService.postToBluesky(
                    request.Text,
                    images,
                    request.Facets
                );

                return Ok("Post created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error posting to BlueSky: {ex.Message}");
            }
        }

        [HttpDelete("delete/{rkey}")]
        public async Task<IActionResult> DeletePost(string rkey)
        {
            try
            {
                await _blueSkyService.deletePost(rkey);
                return Ok("Post deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting post: {ex.Message}");
            }
        }

        // This is the original method without image support, kept here for reference. The new method above supports both text and optional images in the post.
        // It has been commented out to avoid confusion, but it can be removed if no longer needed.

        /*
        public async Task<IActionResult> PostToBluesky([FromBody] Post post)
        {
            try
            {
                await _blueSkyService.postToBluesky(post.text);
                return Ok("Post created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error posting to BlueSky: {ex.Message}");
            }
        }
        */
    }
}