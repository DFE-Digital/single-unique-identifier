using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using SUI.Transfer.API.Endpoint;
using SUI.Transfer.Application.Services;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IFetchingService, FetchingService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.UseExceptionHandler();

// configure http profile for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapFetchEndpoint();

app.UseHttpsRedirection();

await app.RunAsync();

// Allow for setting up factory in tests
public partial class Program
{
}