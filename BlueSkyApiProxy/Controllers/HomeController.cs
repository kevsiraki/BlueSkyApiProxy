using BlueSkyApiProxy.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BlueSkyApiProxy.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/")]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        public HomeController() { }

        [HttpGet]
        public async Task<IActionResult> Home()
        {
            return Ok("BlueSky Services Are Running.");
        }
    }

    [ApiKey]
    [Route("api/[controller]")]
    public class HomeProtectedController : ControllerBase
    {
        public HomeProtectedController() { }

        [HttpGet]
        public async Task<IActionResult> HomeProtected()
        {
            return Ok("BlueSky Services and Auth Are Running.");
        }
    }
}
