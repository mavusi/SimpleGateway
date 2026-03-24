using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("{*path}", async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "GET", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Get");

app.MapPost("{*path}", async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "POST", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Post");

app.MapPut("{*path}", async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "PUT", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Put");

app.MapDelete("{*path}", async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "DELETE", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Delete");

app.MapMethods("{*path}", new[] { "PATCH" }, async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "PATCH", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Patch");

app.MapMethods("{*path}", new[] { "OPTIONS" }, async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "OPTIONS", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Options");

app.MapMethods("{*path}", new[] { "HEAD" }, async (Microsoft.AspNetCore.Http.HttpRequest req) =>
{
    using var reader = new System.IO.StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();

    var headers = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var h in req.Headers)
    {
        headers[h.Key] = h.Value.ToString();
    }

    var cookies = new System.Collections.Generic.Dictionary<string, string>();
    foreach (var c in req.Cookies)
    {
        cookies[c.Key] = c.Value;
    }

    return new { method = "HEAD", path = req.Path.ToString(), headers, cookies, body };
})
.WithName("Head");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
