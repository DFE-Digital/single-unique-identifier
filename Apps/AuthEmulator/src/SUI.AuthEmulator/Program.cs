using System.IO.Abstractions;
using SUI.AuthEmulator;
using SUI.AuthEmulator.Configurations;
using SUI.AuthEmulator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.UseOpenTelemetry();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            // It is safe to allow all origins and methods because this is a mock service.
#pragma warning disable S5122
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
#pragma warning restore S5122
        }
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// Added to allow for reliable test mock times
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IAuthStoreService, MockAuthStoreService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IJwksKeyProvider, JwksKeyProvider>();

builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection(AuthSettings.SectionName)
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

await app.RunAsync();
