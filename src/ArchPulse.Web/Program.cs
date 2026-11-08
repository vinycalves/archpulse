var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"] ?? "https+http://archpulse-api");
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapDefaultEndpoints();

app.MapMethods(
    "/api/{**path}",
    ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    ProxyApiAsync);

app.MapFallbackToFile("index.html");

app.Run();

static async Task ProxyApiAsync(HttpContext context, IHttpClientFactory httpClientFactory)
{
    var client = httpClientFactory.CreateClient("api");
    var target = context.Request.Path + context.Request.QueryString;

    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

    if (context.Request.ContentLength > 0
        || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        request.Content = new StreamContent(context.Request.Body);

        foreach (var header in context.Request.Headers)
        {
            if (!request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }
    else
    {
        foreach (var header in context.Request.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    using var response = await client.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    context.Response.StatusCode = (int)response.StatusCode;

    foreach (var header in response.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in response.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}
