using SUI.AuthEmulator;
using SUI.AuthEmulator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.UseOpenTelemetry();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IAuthStoreService, MockAuthStoreService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IJwksKeyProvider, JwksKeyProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();

app.Run();
