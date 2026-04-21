using BlueSkyApiProxy.Models;
using BlueSkyApiProxy.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddHttpClient<BlueSkyService>();

// Don't put secrets in appsettings.json.

// Use this:

// dotnet user-secrets init
// dotnet user-secrets set "ApiKey" "my-super-secret-key"
// dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Password=..."

// dotnet user-secrets list
// dotnet user-secrets remove "ApiKey"

// Reading the secrets is similar to appsettings.json.

/*

var apiKey = builder.Configuration["Configuration:ApiKey"];

Or, if you are fancy...

public class ConfigurationSettings
{
    public string ApiKey { get; set; }
}

builder.Services.Configure<ConfigurationSettings>(
    builder.Configuration.GetSection("Configuration"));

public class MyService
{
    private readonly string _apiKey;

    public MyService(IOptions<ConfigurationSettings> config)
    {
        _apiKey = config.Value.ApiKey;
    }
}

*/

builder.Services.Configure<ConfigurationSettings>(
    builder.Configuration.GetSection("Configuration"));

builder.Services.Configure<BlueSkyOptions>(
    builder.Configuration.GetSection("BlueSky"));

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
