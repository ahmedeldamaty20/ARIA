using Services;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

builder.Services.AddOpenApi();

builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "codebase-mcp/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

    var token = builder.Configuration["GitHub:Token"];
    // Fall back to common env var name if the configuration key wasn't set
    if (string.IsNullOrEmpty(token))
        token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

    if (!string.IsNullOrEmpty(token))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});

builder.Services.AddHttpClient<EmbeddingService>(client =>
{
    var apiKey = builder.Configuration["OpenAI:ApiKey"]
        ?? throw new InvalidOperationException("OpenAI:ApiKey is required");

    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
});

builder.Services.AddSingleton<ChunkingService>();
builder.Services.AddSingleton<PineconeService>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
