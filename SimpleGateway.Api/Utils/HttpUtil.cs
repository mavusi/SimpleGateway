using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace SimpleGateway.Api.Utils
{
    public enum ContentType
    {
        Json,
        Xml,
        PlainText,
        SOAP
    }

    public class HttpUtil
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpUtil> _logger;

        public HttpUtil(HttpClient httpClient, ILogger<HttpUtil> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendRequestAsync(string method, string url, object content = null, IDictionary<string, string> headers = null, ContentType requestType = ContentType.Json, CookieContainer? cookies = null, bool logRequest = true)
        {
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentException("HTTP method must be provided", nameof(method));
            var httpMethod = new HttpMethod(method.Trim());
            var request = new HttpRequestMessage(httpMethod, url);

            if (content != null)
            {
                var httpContent = CreateHttpContent(content, requestType);
                request.Content = httpContent;
                if (logRequest)
                {
                    try
                    {
                        var contentString = await request.Content.ReadAsStringAsync();
                        _logger.LogInformation(contentString);
                    }
                    catch { /* ignore logging failures */ }
                }
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    // Try to add to request headers; if invalid for headers, add to content headers when possible
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value) && request.Content != null)
                    {
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

            if (cookies != null)
            {
                try
                {
                    var cookieHeader = cookies.GetCookieHeader(new Uri(url));
                    if (!string.IsNullOrEmpty(cookieHeader)) request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                }
                catch { /* ignore cookie header if URI invalid */ }
            }

            var response = await _httpClient.SendAsync(request);

            // Log status code
            _logger.LogInformation("Response status: {StatusCode}", (int)response.StatusCode);

            // Log body when present
            if (response.Content != null)
            {
                try
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseBody)) _logger.LogInformation(responseBody);
                }
                catch { /* ignore logging failures */ }
            }

            return response;
        }

        private HttpContent CreateHttpContent(object content, ContentType contentType)
        {
            if (content == null) return null;

            string stringContent;
            string mediaType;
            switch (contentType)
            {
                case ContentType.Json:
                    stringContent = content is string ? content.ToString() : JsonConvert.SerializeObject(content);
                    mediaType = "application/json";
                    break;
                case ContentType.Xml:
                    stringContent = content.ToString();
                    mediaType = "application/xml";
                    break;
                case ContentType.PlainText:
                    stringContent = content.ToString();
                    mediaType = "text/plain";
                    break;
                case ContentType.SOAP:
                    stringContent = content.ToString();
                    mediaType = "text/xml";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported content type.");
            }

            return new StringContent(stringContent, Encoding.UTF8, mediaType);
        }
    }
}
