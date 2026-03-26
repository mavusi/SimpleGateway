using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimpleGateway.Api.Utils;
using System.Dynamic;
using SimpleGateway.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace SimpleGateway.Api.Controllers
{
    [ApiController]
    public class GatewayController : ControllerBase
    {
        private readonly HttpUtil _httpUtil;
        private readonly GatewayDbContext _db;
        private readonly IConfiguration _configuration;

        public GatewayController(HttpUtil httpUtil, GatewayDbContext db, IConfiguration configuration)
        {
            _httpUtil = httpUtil;
            _db = db;
            _configuration = configuration;
        }

        [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS")]
        [Route("")]
        [Route("{**path}")]
        public async Task<IActionResult> Get(string? path = null)
        {
            // Only handle gateway requests if running in Gateway mode
            if (_configuration["AppMode"] != "Gateway")
            {
                return NotFound();
            }
            // Enable buffering so the request body can be read here
            Request.EnableBuffering();

            string body;
            using (var reader = new StreamReader(Request.Body, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            var cookies = Request.Cookies.ToDictionary(k => k.Key, v => v.Value);

            // Build headers dictionary (join multiple values with comma)
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in Request.Headers)
            {
                if (string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase)) continue; // avoid overriding Host
                headers[h.Key] = string.Join(", ", h.Value.ToArray());
            }

            // Validate target path/URL
            if (string.IsNullOrWhiteSpace(path)) return BadRequest("Path is required");

            var url = path;

            // If path is not an absolute URL, try to resolve a GatewayEndpoint and its GatewayService
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var lookup = path.TrimStart('/');

                var endpoint = await _db.Endpoints
                    .Include(e => e.Service)
                    .FirstOrDefaultAsync(e => e.Path == path || e.Path == lookup || e.Path == "/" + lookup);

                if (endpoint == null) return NotFound("GatewayEndpoint not found for path");

                // Ensure method matches if configured on the endpoint
                if (!string.IsNullOrWhiteSpace(endpoint.Method) && !string.Equals(endpoint.Method, Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status405MethodNotAllowed);
                }

                if (endpoint.Service == null || string.IsNullOrWhiteSpace(endpoint.Service.Url))
                {
                    return BadRequest("GatewayService or its URL is not configured for the endpoint");
                }

                var serviceUrl = endpoint.Service.Url.TrimEnd('/');
                var endpointPath = endpoint.Path ?? string.Empty;
                endpointPath = endpointPath.TrimStart('/');

                url = serviceUrl + "/" + endpointPath;
            }

            // Prepare cookies container for HttpUtil
            System.Net.CookieContainer? cookieContainer = null;
            if (cookies.Any())
            {
                try
                {
                    cookieContainer = new System.Net.CookieContainer();
                    var baseUri = new Uri(url);
                    foreach (var kv in cookies)
                    {
                        cookieContainer.Add(baseUri, new System.Net.Cookie(kv.Key, kv.Value));
                    }
                }
                catch
                {
                    // if cookie adding fails, ignore and continue without cookie container
                    cookieContainer = null;
                }
            }

            // Determine content type for HttpUtil
            ContentType requestContentType = ContentType.Json;
            var contentTypeHeader = Request.ContentType;
            if (!string.IsNullOrEmpty(contentTypeHeader))
            {
                var ct = contentTypeHeader.ToLowerInvariant();
                if (ct.Contains("xml")) requestContentType = ContentType.Xml;
                else if (ct.Contains("text")) requestContentType = ContentType.PlainText;
                else if (ct.Contains("soap")) requestContentType = ContentType.SOAP;
                else if (ct.Contains("json")) requestContentType = ContentType.Json;
            }

            // Forward the request using HttpUtil
            var method = Request.Method;
            object? content = string.IsNullOrEmpty(body) ? null : (object)body;

            var upstreamResponse = await _httpUtil.SendRequestAsync(method, url, content, headers, requestContentType, cookieContainer, logRequest: false);

            // Read response body
            var upstreamBody = upstreamResponse.Content != null ? await upstreamResponse.Content.ReadAsStringAsync() : string.Empty;

            // Copy response headers to current response (except some restricted headers)
            foreach (var header in upstreamResponse.Headers)
            {
                try { Response.Headers[header.Key] = header.Value.ToArray(); } catch { }
            }
            if (upstreamResponse.Content != null)
            {
                foreach (var header in upstreamResponse.Content.Headers)
                {
                    try { Response.Headers[header.Key] = header.Value.ToArray(); } catch { }
                }
            }

            // Return content with the same status code and content type
            var contentType = upstreamResponse.Content?.Headers?.ContentType?.ToString();
            return new ObjectResult(upstreamBody)
            {
                StatusCode = (int)upstreamResponse.StatusCode,
                DeclaredType = typeof(string)
            };
        }
    }
}
