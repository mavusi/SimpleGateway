using System.Collections.Concurrent;

var gatewayServices = new ConcurrentDictionary<string, ServiceConfig>();
var gatewayEndpoints = new ConcurrentDictionary<string, EndpointConfig>();

var gatewayBuilder = WebApplication.CreateBuilder(args);
gatewayBuilder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8000));
gatewayBuilder.Services.AddOpenApi();
gatewayBuilder.Services.AddSwaggerGen();
var gatewayApp = gatewayBuilder.Build();

gatewayApp.MapOpenApi();

gatewayApp.UseSwagger();
gatewayApp.UseSwaggerUI();

if (!gatewayApp.Environment.IsEnvironment("Docker"))
{
    gatewayApp.UseHttpsRedirection();
}

static async Task<object> EchoRequest(Microsoft.AspNetCore.Http.HttpRequest req)
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    var headers = req.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
    var cookies = req.Cookies.ToDictionary(c => c.Key, c => c.Value);
    return new { method = req.Method, path = req.Path.ToString(), query = req.Query.ToDictionary(q => q.Key, q => q.Value.ToString()), headers, cookies, body };
}

string allPath = "{**catchAll}";

gatewayApp.MapGet(allPath, EchoRequest).WithName("GatewayGet");
gatewayApp.MapPost(allPath, EchoRequest).WithName("GatewayPost");
gatewayApp.MapPut(allPath, EchoRequest).WithName("GatewayPut");
gatewayApp.MapDelete(allPath, EchoRequest).WithName("GatewayDelete");
gatewayApp.MapMethods(allPath, new[] { "PATCH" }, EchoRequest).WithName("GatewayPatch");
gatewayApp.MapMethods(allPath, new[] { "OPTIONS" }, EchoRequest).WithName("GatewayOptions");
gatewayApp.MapMethods(allPath, new[] { "HEAD" }, EchoRequest).WithName("GatewayHead");

var adminBuilder = WebApplication.CreateBuilder(args);
adminBuilder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8001));
adminBuilder.Services.AddOpenApi();
adminBuilder.Services.AddSwaggerGen();
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
    if (!gatewayEndpoints.TryAdd(endpoint.Id, endpoint)) return Results.Conflict($"Endpoint {endpoint.Id} already exists");
    return Results.Created($"/admin/endpoints/{endpoint.Id}", endpoint);
});
adminApp.MapPut("/admin/endpoints/{id}", (string id, EndpointConfig update) =>
{
    if (!gatewayEndpoints.ContainsKey(id)) return Results.NotFound();
    update.Id = id;
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
