using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Xml.Serialization;
// using Newtonsoft.Json; replaced with System.Text.Json

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

        public HttpUtil(IHttpClientFactory _httpClientFactory, ILogger<HttpUtil> logger)
        {
            _httpClient = _httpClientFactory.CreateClient();//.CreateClient("newgen");// httpClient;
            _logger = logger;
        }

        public async Task<T> GetAsync<T>(string url, ContentType responseType, IDictionary<string, string> headers = null, CookieContainer cookies = null)
        {
            _logger.LogInformation(url);
            ApplyHeaders(headers);
            SetAcceptHeader(responseType);
            var response = await _httpClient.GetAsync(url);
            return await HandleResponseAsync<T>(response, responseType);
        }

        public async Task<T> PostAsync<T>(string url, object content, IDictionary<string, string> headers = null, ContentType requestType = ContentType.Json, ContentType responseType = ContentType.Json, bool logRequest = true)
        {
            _logger.LogInformation(url);
            if (logRequest)
            {
                //check if content is string or an object
                if (content is string)
                {
                    _logger.LogInformation((string)content);
                }
                else
                {
                    _logger.LogInformation(JsonSerializer.Serialize(content));
                }
            }
            ApplyHeaders(headers);
            SetAcceptHeader(responseType);
            var httpContent = CreateHttpContent(content, requestType);
            var response = await _httpClient.PostAsync(url, httpContent);
            return await HandleResponseAsync<T>(response, responseType);
        }

        public async Task<T> PutAsync<T>(string url, object content, IDictionary<string, string> headers = null, ContentType requestType = ContentType.Json, ContentType responseType = ContentType.Json)
        {
            _logger.LogInformation(url);
            _logger.LogInformation(JsonSerializer.Serialize(content));
            ApplyHeaders(headers);
            SetAcceptHeader(responseType);
            var httpContent = CreateHttpContent(content, requestType);
            var response = await _httpClient.PutAsync(url, httpContent);
            return await HandleResponseAsync<T>(response, responseType);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            _logger.LogInformation(JsonSerializer.Serialize(request));
            return await _httpClient.SendAsync(request);
        }

        private void ApplyHeaders(IDictionary<string, string> headers)
        {
            _httpClient.DefaultRequestHeaders.Clear();

            // Apply headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        private void SetAcceptHeader(ContentType contentType)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            switch (contentType)
            {
                case ContentType.Json:
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    break;
                case ContentType.Xml:
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    break;
                case ContentType.PlainText:
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    break;
            }
        }

        private HttpContent CreateHttpContent(object content, ContentType contentType)
        {
            string stringContent;
            string mediaType;

            switch (contentType)
            {
                case ContentType.Json:
                    stringContent = content is string ? content.ToString() : JsonSerializer.Serialize(content);
                    mediaType = "application/json";
                    break;
                case ContentType.Xml:
                    stringContent = content.ToString(); // Assuming the content is already serialized as XML
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
            _logger.LogInformation(stringContent);
            return new StringContent(stringContent, Encoding.UTF8, mediaType);
        }

        private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, ContentType responseType)
        {
            try
            {
                response.EnsureSuccessStatusCode();
                string responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(responseString);
                return responseType switch
                {
                    ContentType.Json => JsonSerializer.Deserialize<T>(responseString),
                    ContentType.Xml => DeserializeXml<T>(responseString),
                    ContentType.SOAP => DeserializeXml<T>(responseString),
                    ContentType.PlainText => (T)(object)responseString,
                    _ => throw new InvalidOperationException("Unsupported response type."),
                };
            }
            catch (Exception ex)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(responseString);
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        private T DeserializeXml<T>(string xmlString)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new System.IO.StringReader(xmlString);
            return (T)serializer.Deserialize(reader);
        }
    }
}
