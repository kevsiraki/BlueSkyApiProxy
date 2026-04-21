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
    }
}