using SimpleGateway.Api.Utils;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;

var gatewayServices = new ConcurrentDictionary<string, ServiceConfig>();
var gatewayEndpoints = new ConcurrentDictionary<string, EndpointConfig>();

var gatewayBuilder = WebApplication.CreateBuilder(args);
gatewayBuilder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8000));
gatewayBuilder.Services.AddOpenApi();
gatewayBuilder.Services.AddSwaggerGen();
gatewayBuilder.Services.AddHttpClient();
gatewayBuilder.Services.AddSingleton<HttpUtil>();
gatewayBuilder.Services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
gatewayBuilder.Logging.AddConsole();
var gatewayApp = gatewayBuilder.Build();

gatewayApp.MapOpenApi();

gatewayApp.UseSwagger();
gatewayApp.UseSwaggerUI();

if (!gatewayApp.Environment.IsEnvironment("Docker"))
{
    gatewayApp.UseHttpsRedirection();
}

async Task<object> ProcessRequest(Microsoft.AspNetCore.Http.HttpRequest req, HttpUtil httpUtil, ILogger<Program> logger, JsonSerializerOptions jsonOptions)
{
    logger.LogInformation("Processing request {Method} {Path}", req.Method, req.Path);
    var endpoint = gatewayEndpoints.Values.FirstOrDefault(e => e.Path == req.Path.ToString() && e.Method == req.Method);
    
    //logger.LogInformation(endpoint.Path);

    if (endpoint != null && gatewayServices.TryGetValue(endpoint.ServiceId, out var service))
    {
        var url = service.Url.TrimEnd('/') + req.Path + req.QueryString;
        logger.LogInformation("Forwarding to {Url}", url);
        var requestMessage = new HttpRequestMessage(new HttpMethod(req.Method), url);
        foreach (var h in req.Headers.Where(h => !new[] { "Host", "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailers", "Transfer-Encoding", "Upgrade" }.Contains(h.Key)))
        {
            requestMessage.Headers.TryAddWithoutValidation(h.Key, h.Value.ToString());
        }
        if (req.ContentLength > 0)
        {
            req.Body.Position = 0;
            requestMessage.Content = new StreamContent(req.Body);
        }
        var response = await httpUtil.SendAsync(requestMessage);
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
        return new { statusCode = (int)response.StatusCode, headers = responseHeaders, body = responseBody };
    }
    else
    {
        using var reader = new System.IO.StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var headers = req.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var cookies = req.Cookies.ToDictionary(c => c.Key, c => c.Value);

        object parsedBody = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                parsedBody = JsonSerializer.Deserialize<object>(body, jsonOptions);
            }
            catch
            {
                parsedBody = body;
            }
        }

        return new { method = req.Method, path = req.Path.ToString(), query = req.Query.ToDictionary(q => q.Key, q => q.Value.ToString()), headers, cookies, body, parsedBody, message = "No configured endpoint found" };
    }
}

string allPath = "{**catchAll}";

gatewayApp.MapGet(allPath, ProcessRequest).WithName("GatewayGet");
gatewayApp.MapPost(allPath, ProcessRequest).WithName("GatewayPost");
gatewayApp.MapPut(allPath, ProcessRequest).WithName("GatewayPut");
gatewayApp.MapDelete(allPath, ProcessRequest).WithName("GatewayDelete");
gatewayApp.MapMethods(allPath, new[] { "PATCH" }, ProcessRequest).WithName("GatewayPatch");
gatewayApp.MapMethods(allPath, new[] { "OPTIONS" }, ProcessRequest).WithName("GatewayOptions");
gatewayApp.MapMethods(allPath, new[] { "HEAD" }, ProcessRequest).WithName("GatewayHead");

var adminBuilder = WebApplication.CreateBuilder(args);
adminBuilder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8001));
adminBuilder.Services.AddOpenApi();
adminBuilder.Services.AddSwaggerGen();
adminBuilder.Logging.AddConsole();
var adminApp = adminBuilder.Build();

adminApp.MapOpenApi();

adminApp.UseSwagger();
adminApp.UseSwaggerUI();

if (!adminApp.Environment.IsEnvironment("Docker"))
{
    adminApp.UseHttpsRedirection();
}

adminApp.MapGet("/admin/services", () => Results.Ok(gatewayServices.Values));
adminApp.MapGet("/admin/services/{id}", (string id) => gatewayServices.TryGetValue(id, out var s) ? Results.Ok(s) : Results.NotFound());
adminApp.MapPost("/admin/services", (ServiceConfig service) =>
{
    if (string.IsNullOrWhiteSpace(service.Id)) return Results.BadRequest("Id is required");
    if (!gatewayServices.TryAdd(service.Id, service)) return Results.Conflict($"Service {service.Id} already exists");
    return Results.Created($"/admin/services/{service.Id}", service);
});
adminApp.MapPut("/admin/services/{id}", (string id, ServiceConfig update) =>
{
    if (!gatewayServices.ContainsKey(id)) return Results.NotFound();
    update.Id = id;
    gatewayServices[id] = update;

    var oldval = gatewayServices[id].Url;
    var newval = update.Url;

    foreach (var endpoint in gatewayEndpoints.Where(x => x.Value.ServiceId == update.Id))
    { 
        endpoint.Value.Path = endpoint.Value.Path.Replace(oldval, newval);
    }

    return Results.Ok(update);
});
adminApp.MapDelete("/admin/services/{id}", (string id) => gatewayServices.TryRemove(id, out var _ ) ? Results.NoContent() : Results.NotFound());

adminApp.MapGet("/admin/endpoints", () => Results.Ok(gatewayEndpoints.Values));
adminApp.MapGet("/admin/endpoints/{id}", (string id) => gatewayEndpoints.TryGetValue(id, out var e) ? Results.Ok(e) : Results.NotFound());
adminApp.MapPost("/admin/endpoints", (EndpointConfig endpoint) =>
{
    if (string.IsNullOrWhiteSpace(endpoint.Id)) return Results.BadRequest("Id is required");
    if (string.IsNullOrWhiteSpace(endpoint.ServiceId)) return Results.BadRequest("ServiceId is required");
    if (!gatewayServices.ContainsKey(endpoint.ServiceId)) return Results.BadRequest("Service does not exist");

    var service = gatewayServices[endpoint.ServiceId];

    endpoint.Path = service.Url.TrimEnd('/') + endpoint.Path;

    if (!gatewayEndpoints.TryAdd(endpoint.Id, endpoint)) return Results.Conflict($"Endpoint {endpoint.Id} already exists");
    return Results.Created($"/admin/endpoints/{endpoint.Id}", endpoint);
});
adminApp.MapPut("/admin/endpoints/{id}", (string id, EndpointConfig update) =>
{
    if (!gatewayEndpoints.ContainsKey(id)) return Results.NotFound();

    var service = gatewayServices[id];

    update.Id = id;
    update.Path =  service.Url.TrimEnd('/') + update.Path;
    gatewayEndpoints[id] = update;
    return Results.Ok(update);
});
adminApp.MapDelete("/admin/endpoints/{id}", (string id) => gatewayEndpoints.TryRemove(id, out var _) ? Results.NoContent() : Results.NotFound());

await Task.WhenAll(gatewayApp.RunAsync(), adminApp.RunAsync());

public class ServiceConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class EndpointConfig
{
    public string Id { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}
