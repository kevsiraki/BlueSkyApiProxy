using BlueSkyApiProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BlueSkyApiProxy.Attributes
{
    /// <summary>
    /// This attribute is used to secure API endpoints by requiring a valid API key in the request headers.
    /// 
    /// Compared to the original source implementation:
    /// 
    /// Improvements include:
    /// - Separation between header name and configuration key path.
    /// - Proper null/empty checks for configuration values.
    /// - Constant-time comparison using CryptographicOperations.FixedTimeEquals
    ///   to prevent timing attacks instead of string.Equals().
    /// 
    /// This version keeps endpoint-level control (attribute-based security)
    /// instead of global middleware so specific controllers can be protected.
    /// 
    /// Source inspiration:
    /// https://codingsonata.com/secure-asp-net-core-web-api-using-api-key-authentication/
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        // Header expected from client requests
        private const string HEADER_NAME = "ApiKey";

        // 4/20/2026: Moved this to DI.

        // Path inside IConfiguration where API key is stored
        //private const string CONFIG_PATH = "Configuration:ApiKey";

        // 2/17/2026 - Allow CORS preflight requests
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            if (context.HttpContext.Request.Method == HttpMethods.Options)
            {
                await next();
                return;
            }

            // Try retrieving API key from request headers
            if (!context.HttpContext.Request.Headers.TryGetValue(HEADER_NAME, out var extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Api Key was not provided"
                };
                return;
            }

            //var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            //var apiKey = config[CONFIG_PATH];

            //DI: 
            var options = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<ConfigurationSettings>>();

            var apiKey = options.Value.ApiKey;

            // Check if API key exists in configuration.
            // Returning 500 instead of 401 because this indicates
            // server misconfiguration rather than client error.
            if (string.IsNullOrEmpty(apiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 500,
                    Content = "API Key configuration missing"
                };
                return;
            }

            // Extract header value as string.
            // Request headers return StringValues, so normalize to string.
            var incomingKey = extractedApiKey.ToString();

            // Convert both keys to byte arrays for constant-time comparison.
            // This avoids timing attacks that can occur with normal string comparison.
            var storedBytes = Encoding.UTF8.GetBytes(apiKey);
            var incomingBytes = Encoding.UTF8.GetBytes(incomingKey);

            bool isApiKeyValid = CryptographicOperations.FixedTimeEquals(
                storedBytes,
                incomingBytes
            );

            if (!isApiKeyValid)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Api Key is not valid"
                };
                return;
            }

            // Continue request pipeline if API key validation succeeds
            await next();
        }
    }
}
