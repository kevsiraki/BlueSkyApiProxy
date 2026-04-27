using BlueSkyApiProxy.Models;
using BlueSkyApiProxy.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Transient: A new instance is created every time it is requested. Use this for lightweight, stateless services.
//builder.Services.AddOpenApi();
//builder.Services.AddHttpClient<BlueSkyService>();

// Singleton: A single instance is created and shared throughout the application's lifetime. Use this for services that maintain state or are expensive to create.
builder.Services.AddHttpClient(); // factory only
builder.Services.AddSingleton<BlueSkyService>();

// Scoped: A new instance is created per scope. In web applications, a new scope is created for each request.
// Use this for services that should be unique to each request but can be shared within that request.
builder.Services.Configure<ConfigurationSettings>(
    builder.Configuration.GetSection("Configuration"));

// This is an example of how to use the Options pattern to bind configuration settings to a strongly-typed class.
// It is a scoped service, which means that a new instance of ConfigurationSettings will be created for each request. The settings are read from the "Configuration" section of the appsettings.json file (or other configuration sources).
// This allows you to easily access configuration values in your services by injecting IOptions<ConfigurationSettings>.
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

/*
Don't put secrets in appsettings.json.

Use this:

dotnet user-secrets init
dotnet user-secrets set "ApiKey" "my-super-secret-key"
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Password=..."

dotnet user-secrets list
dotnet user-secrets remove "ApiKey"

Reading the secrets is similar to appsettings.json.

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