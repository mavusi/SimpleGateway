using SimpleGateway.Api.Utils;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SimpleGateway.Api.Data;
using SimpleGateway.Api.Models;
using Microsoft.AspNetCore.Http;

var gatewayBuilder = WebApplication.CreateBuilder(args);

var connectionString = gatewayBuilder.Configuration.GetValue<string>("POSTGRES_CONNECTION")
                       ?? gatewayBuilder.Configuration.GetConnectionString("Default")
                       ?? "Host=localhost;Port=5432;Database=simplegateway;Username=postgres;Password=postgres";

gatewayBuilder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8000));
gatewayBuilder.Services.AddOpenApi();
gatewayBuilder.Services.AddSwaggerGen();
gatewayBuilder.Services.AddHttpClient();
gatewayBuilder.Services.AddSingleton<HttpUtil>();
gatewayBuilder.Services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
gatewayBuilder.Logging.AddConsole();
gatewayBuilder.Services.AddDbContext<GatewayDbContext>(options => options.UseNpgsql(connectionString));
var gatewayApp = gatewayBuilder.Build();

// Ensure database exists
using (var scope = gatewayApp.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();
    db.Database.EnsureCreated();
}

gatewayApp.MapOpenApi();

gatewayApp.UseSwagger();
gatewayApp.UseSwaggerUI();

if (!gatewayApp.Environment.IsEnvironment("Docker"))
{
    gatewayApp.UseHttpsRedirection();
}

async Task<object> ProcessRequest(HttpRequest req, HttpUtil httpUtil, ILogger<Program> logger, JsonSerializerOptions jsonOptions, GatewayDbContext db)
{
    logger.LogInformation("Processing request {Method} {Path}", req.Method, req.Path);
    var endpoint = await db.Endpoints.FirstOrDefaultAsync(e => e.Path == req.Path.ToString() && e.Method == req.Method);

    if (endpoint != null)
    {
        var service = await db.Services.FindAsync(endpoint.ServiceId);
        if (service != null)
        {
            var url = service.Url.TrimEnd('/') + req.Path + req.QueryString;
            logger.LogInformation("Forwarding to {Url}", url);
            var requestMessage = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(req.Method), url);

            foreach (var h in req.Headers.Where(h => !new[] { "Host", "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailers", "Transfer-Encoding", "Upgrade" }.Contains(h.Key)))
            {
                requestMessage.Headers.TryAddWithoutValidation(h.Key, h.Value.ToString());
            }

            if (req.ContentLength > 0)
            {
                req.Body.Position = 0;
                requestMessage.Content = new System.Net.Http.StreamContent(req.Body);
            }

            var response = await httpUtil.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            return new { statusCode = (int)response.StatusCode, headers = responseHeaders, body = responseBody };
        }
    }

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
adminBuilder.Services.AddDbContext<GatewayDbContext>(options => options.UseNpgsql(connectionString));
var adminApp = adminBuilder.Build();

// Ensure database exists for admin as well
using (var scope2 = adminApp.Services.CreateScope())
{
    var db = scope2.ServiceProvider.GetRequiredService<GatewayDbContext>();
    db.Database.EnsureCreated();
}

adminApp.MapOpenApi();

adminApp.UseSwagger();
adminApp.UseSwaggerUI();

if (!adminApp.Environment.IsEnvironment("Docker"))
{
    adminApp.UseHttpsRedirection();
}

adminApp.MapGet("/admin/services", async (GatewayDbContext db) => Results.Ok(await db.Services.ToListAsync()));
adminApp.MapGet("/admin/services/{id}", async (string id, GatewayDbContext db) =>
    await db.Services.FindAsync(id) is ServiceConfig s ? Results.Ok(s) : Results.NotFound());
adminApp.MapPost("/admin/services", async (HttpRequest req, GatewayDbContext db) =>
{
    var isHtmx = req.Headers.ContainsKey("HX-Request");
    ServiceConfig service;
    var body = string.Empty;
    if (!string.IsNullOrWhiteSpace(req.ContentType) && req.ContentType.StartsWith("application/json", System.StringComparison.OrdinalIgnoreCase))
    {
        using var reader = new System.IO.StreamReader(req.Body);
        body = await reader.ReadToEndAsync();
        service = JsonSerializer.Deserialize<ServiceConfig>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ServiceConfig();
    }
    else
    {
        var form = await req.ReadFormAsync();
        service = new ServiceConfig { Id = form["id"], Name = form["name"], Url = form["url"] };
    }

    if (string.IsNullOrWhiteSpace(service.Id))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 400;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Id is required";
            return Results.Content("Id is required", "text/plain");
        }
        return Results.BadRequest("Id is required");
    }
    if (await db.Services.AnyAsync(x => x.Id == service.Id))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 409;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = $"Service {service.Id} already exists";
            return Results.Content($"Service {service.Id} already exists", "text/plain");
        }
        return Results.Conflict($"Service {service.Id} already exists");
    }

    await db.Services.AddAsync(service);
    await db.SaveChangesAsync();

    if (isHtmx)
    {
        var html = await AdminRenderer.BuildServicesListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Service created";
        return Results.Content(html, "text/html");
    }

    return Results.Created($"/admin/services/{service.Id}", service);
});
adminApp.MapPut("/admin/services/{id}", async (string id, HttpRequest req, GatewayDbContext db) =>
{
    var isHtmx = req.Headers.ContainsKey("HX-Request");
    var existing = await db.Services.FindAsync(id);
    if (existing == null)
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 404;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Service not found";
            return Results.Content("Service not found", "text/plain");
        }
        return Results.NotFound();
    }

    ServiceConfig update;
    if (!string.IsNullOrWhiteSpace(req.ContentType) && req.ContentType.StartsWith("application/json", System.StringComparison.OrdinalIgnoreCase))
    {
        using var reader = new System.IO.StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        update = JsonSerializer.Deserialize<ServiceConfig>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ServiceConfig();
    }
    else
    {
        var form = await req.ReadFormAsync();
        update = new ServiceConfig { Id = id, Name = form["name"], Url = form["url"] };
    }

    update.Id = id;
    db.Entry(existing).CurrentValues.SetValues(update);
    await db.SaveChangesAsync();

    if (isHtmx)
    {
        var html = await AdminRenderer.BuildServicesListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Service updated";
        return Results.Content(html, "text/html");
    }

    return Results.Ok(update);
});
adminApp.MapDelete("/admin/services/{id}", async (string id, HttpRequest req, GatewayDbContext db) =>
{
    var isHtmx = req.Headers.ContainsKey("HX-Request");
    var toDelete = await db.Services.FindAsync(id);
    if (toDelete == null)
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 404;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Service not found";
            return Results.Content("Service not found", "text/plain");
        }
        return Results.NotFound();
    }
    db.Services.Remove(toDelete);
    await db.SaveChangesAsync();

    if (isHtmx)
    {
        var html = await AdminRenderer.BuildServicesListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Service deleted";
        return Results.Content(html, "text/html");
    }

    return Results.NoContent();
});

adminApp.MapGet("/admin/endpoints", async (GatewayDbContext db) => Results.Ok(await db.Endpoints.ToListAsync()));
adminApp.MapGet("/admin/endpoints/{id}", async (string id, GatewayDbContext db) =>
    await db.Endpoints.FindAsync(id) is EndpointConfig e ? Results.Ok(e) : Results.NotFound());
adminApp.MapPost("/admin/endpoints", async (HttpRequest req, GatewayDbContext db) =>
{
    EndpointConfig endpoint;
    if (!string.IsNullOrWhiteSpace(req.ContentType) && req.ContentType.StartsWith("application/json", System.StringComparison.OrdinalIgnoreCase))
    {
        using var reader = new System.IO.StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        endpoint = JsonSerializer.Deserialize<EndpointConfig>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new EndpointConfig();
    }
    else
    {
        var form = await req.ReadFormAsync();
        endpoint = new EndpointConfig { Id = form["id"], ServiceId = form["serviceId"], Path = form["path"], Method = form["method"] };
    }

    var isHtmx = req.Headers.ContainsKey("HX-Request");
    if (string.IsNullOrWhiteSpace(endpoint.Id))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 400;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Id is required";
            return Results.Content("Id is required", "text/plain");
        }
        return Results.BadRequest("Id is required");
    }
    if (string.IsNullOrWhiteSpace(endpoint.ServiceId))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 400;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "ServiceId is required";
            return Results.Content("ServiceId is required", "text/plain");
        }
        return Results.BadRequest("ServiceId is required");
    }
    if (!await db.Services.AnyAsync(s => s.Id == endpoint.ServiceId))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 400;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Service does not exist";
            return Results.Content("Service does not exist", "text/plain");
        }
        return Results.BadRequest("Service does not exist");
    }
    if (await db.Endpoints.AnyAsync(x => x.Id == endpoint.Id))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 409;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = $"Endpoint {endpoint.Id} already exists";
            return Results.Content($"Endpoint {endpoint.Id} already exists", "text/plain");
        }
        return Results.Conflict($"Endpoint {endpoint.Id} already exists");
    }
    await db.Endpoints.AddAsync(endpoint);
    await db.SaveChangesAsync();
    if (isHtmx)
    {
        var html = await AdminRenderer.BuildEndpointsListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Endpoint created";
        return Results.Content(html, "text/html");
    }

    return Results.Created($"/admin/endpoints/{endpoint.Id}", endpoint);
});
adminApp.MapPut("/admin/endpoints/{id}", async (string id, HttpRequest req, GatewayDbContext db) =>
{
    var isHtmx = req.Headers.ContainsKey("HX-Request");
    var existing = await db.Endpoints.FindAsync(id);
    if (existing == null)
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 404;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Endpoint not found";
            return Results.Content("Endpoint not found", "text/plain");
        }
        return Results.NotFound();
    }

    EndpointConfig update;
    if (!string.IsNullOrWhiteSpace(req.ContentType) && req.ContentType.StartsWith("application/json", System.StringComparison.OrdinalIgnoreCase))
    {
        using var reader = new System.IO.StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        update = JsonSerializer.Deserialize<EndpointConfig>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new EndpointConfig();
    }
    else
    {
        var form = await req.ReadFormAsync();
        update = new EndpointConfig { Id = id, ServiceId = form["serviceId"], Path = form["path"], Method = form["method"] };
    }

    if (string.IsNullOrWhiteSpace(update.ServiceId) || !await db.Services.AnyAsync(s => s.Id == update.ServiceId))
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 400;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Service does not exist";
            return Results.Content("Service does not exist", "text/plain");
        }
        return Results.BadRequest("Service does not exist");
    }

    update.Id = id;
    db.Entry(existing).CurrentValues.SetValues(update);
    await db.SaveChangesAsync();

    if (isHtmx)
    {
        var html = await AdminRenderer.BuildEndpointsListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Endpoint updated";
        return Results.Content(html, "text/html");
    }

    return Results.Ok(update);
});
adminApp.MapDelete("/admin/endpoints/{id}", async (string id, HttpRequest req, GatewayDbContext db) =>
{
    var isHtmx = req.Headers.ContainsKey("HX-Request");
    var toDel = await db.Endpoints.FindAsync(id);
    if (toDel == null)
    {
        if (isHtmx)
        {
            req.HttpContext.Response.StatusCode = 404;
            req.HttpContext.Response.Headers["X-HTMX-Error"] = "Endpoint not found";
            return Results.Content("Endpoint not found", "text/plain");
        }
        return Results.NotFound();
    }
    db.Endpoints.Remove(toDel);
    await db.SaveChangesAsync();

    if (isHtmx)
    {
        var html = await AdminRenderer.BuildEndpointsListHtml(db, adminApp.Environment.ContentRootPath);
        req.HttpContext.Response.Headers["X-HTMX-Message"] = "Endpoint deleted";
        return Results.Content(html, "text/html");
    }

    return Results.NoContent();
});

// Serve SPA static files for admin UI (wwwroot/admin)
adminApp.UseDefaultFiles();
adminApp.UseStaticFiles();

// HTMX partial endpoints (server-side rendered fragments)
adminApp.MapGet("/admin/partials/endpoints/list", async (GatewayDbContext db) =>
{
    var endpoints = await db.Endpoints.OrderBy(e => e.Path).ToListAsync();
    var services = await db.Services.ToDictionaryAsync(s => s.Id, s => s.Name);
    var rows = new System.Text.StringBuilder();
    foreach (var e in endpoints)
    {
        var svcName = services.ContainsKey(e.ServiceId) ? System.Net.WebUtility.HtmlEncode(services[e.ServiceId]) : System.Net.WebUtility.HtmlEncode(e.ServiceId);
        rows.AppendLine("<tr>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Id)}</td>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{svcName}</td>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Path)}</td>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(e.Method)}</td>");
        rows.AppendLine("  <td class=\"px-4 py-2 text-sm text-right\">\n" +
                        $"    <button class=\"mr-2 px-2 py-1 bg-yellow-500 text-white rounded\" hx-get=\"/admin/partials/endpoints/form/{System.Net.WebUtility.UrlEncode(e.Id)}\" hx-target=\"#main\" hx-swap=\"innerHTML\">Edit</button>\n" +
                        $"    <button class=\"px-2 py-1 bg-red-600 text-white rounded\" hx-delete=\"/admin/endpoints/{System.Net.WebUtility.UrlEncode(e.Id)}\" hx-confirm=\"Are you sure?\" hx-target=\"#main\" hx-swap=\"innerHTML\">Delete</button>\n"
                        );
        rows.AppendLine("</tr>");
    }
    var html = await SimpleGateway.Api.Utils.AdminRenderer.BuildEndpointsListHtml(db, adminApp.Environment.ContentRootPath);
    return Results.Content(html, "text/html");
});

adminApp.MapGet("/admin/partials/endpoints/form/{id?}", async (string? id, GatewayDbContext db) =>
{
    EndpointConfig? existing = null;
    if (!string.IsNullOrWhiteSpace(id)) existing = await db.Endpoints.FindAsync(id);
    var services = await db.Services.OrderBy(s => s.Name).ToListAsync();
    var options = new System.Text.StringBuilder();
    foreach (var s in services)
    {
        var sel = existing != null && existing.ServiceId == s.Id ? " selected" : "";
        options.AppendLine($"<option value=\"{System.Net.WebUtility.HtmlEncode(s.Id)}\"{sel}>{System.Net.WebUtility.HtmlEncode(s.Name)} ({System.Net.WebUtility.HtmlEncode(s.Id)})</option>");
    }
    var tpl = SimpleGateway.Api.Utils.AdminRenderer.ReadTemplate(adminApp.Environment.ContentRootPath, "partials/endpoints-form.html");
    var hx = existing == null ? "hx-post=\"/admin/endpoints\"" : $"hx-put=\"/admin/endpoints/{System.Net.WebUtility.UrlEncode(existing.Id)}\"";
    var html = tpl.Replace("{{id}}", System.Net.WebUtility.HtmlEncode(existing?.Id ?? string.Empty))
                  .Replace("{{readonly}}", existing != null ? "readonly" : string.Empty)
                  .Replace("{{hx}}", hx)
                  .Replace("{{serviceOptions}}", options.ToString())
                  .Replace("{{path}}", System.Net.WebUtility.HtmlEncode(existing?.Path ?? string.Empty))
                  .Replace("{{method}}", System.Net.WebUtility.HtmlEncode(existing?.Method ?? "GET"));
    return Results.Content(html, "text/html");
});

adminApp.MapGet("/admin/partials/services/list", async (GatewayDbContext db) =>
{
    var services = await db.Services.OrderBy(s => s.Name).ToListAsync();
    var rows = new System.Text.StringBuilder();
    foreach (var s in services)
    {
        rows.AppendLine("<tr>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Id)}</td>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Name)}</td>");
        rows.AppendLine($"  <td class=\"px-4 py-2 text-sm text-gray-700\">{System.Net.WebUtility.HtmlEncode(s.Url)}</td>");
        rows.AppendLine("  <td class=\"px-4 py-2 text-sm text-right\">\n" +
                        $"    <button class=\"mr-2 px-2 py-1 bg-yellow-500 text-white rounded\" hx-get=\"/admin/partials/services/form/{System.Net.WebUtility.UrlEncode(s.Id)}\" hx-target=\"#main\" hx-swap=\"innerHTML\">Edit</button>\n" +
                        $"    <button class=\"px-2 py-1 bg-red-600 text-white rounded\" hx-delete=\"/admin/services/{System.Net.WebUtility.UrlEncode(s.Id)}\" hx-confirm=\"Are you sure?\" hx-target=\"#main\" hx-swap=\"innerHTML\">Delete</button>\n"
                        );
        rows.AppendLine("</tr>");
    }
    var html = await SimpleGateway.Api.Utils.AdminRenderer.BuildServicesListHtml(db, adminApp.Environment.ContentRootPath);
    return Results.Content(html, "text/html");
});

adminApp.MapGet("/admin/partials/services/form/{id?}", async (string? id, GatewayDbContext db) =>
{
    ServiceConfig? existing = null;
    if (!string.IsNullOrWhiteSpace(id)) existing = await db.Services.FindAsync(id);
    var tpl = SimpleGateway.Api.Utils.AdminRenderer.ReadTemplate(adminApp.Environment.ContentRootPath, "partials/services-form.html");
    var hx = existing == null ? "hx-post=\"/admin/services\"" : $"hx-put=\"/admin/services/{System.Net.WebUtility.UrlEncode(existing.Id)}\"";
    var html = tpl.Replace("{{id}}", System.Net.WebUtility.HtmlEncode(existing?.Id ?? string.Empty))
                  .Replace("{{readonly}}", existing != null ? "readonly" : string.Empty)
                  .Replace("{{hx}}", hx)
                  .Replace("{{name}}", System.Net.WebUtility.HtmlEncode(existing?.Name ?? string.Empty))
                  .Replace("{{url}}", System.Net.WebUtility.HtmlEncode(existing?.Url ?? string.Empty));
    return Results.Content(html, "text/html");
});


// Redirect convenience routes to the SPA index
adminApp.MapGet("/admin", () => Results.Redirect("/admin/index.html"));
adminApp.MapGet("/admin/ui", () => Results.Redirect("/admin/index.html"));

await Task.WhenAll(gatewayApp.RunAsync(), adminApp.RunAsync());

