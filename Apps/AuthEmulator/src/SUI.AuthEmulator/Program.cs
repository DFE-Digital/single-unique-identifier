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
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

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
