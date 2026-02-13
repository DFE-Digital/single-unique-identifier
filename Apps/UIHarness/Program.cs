using Microsoft.AspNetCore.Authentication.Cookies;
using UIHarness.Hubs;
using UIHarness.Interfaces;
using UIHarness.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.Cookie.Name = "UIHarness.Auth";
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IPersonRepository, JsonPersonRepository>();
builder.Services.AddSingleton<IFindAnId, SimulatedFindAnId>();
builder.Services.AddSingleton<ICustodianRepository, JsonCustodianRepository>();
builder.Services.AddSingleton<IFindARecord, SimulatedFindARecord>();
builder.Services.AddSingleton<IFetchARecord, SimulatedFetchARecord>();
builder.Services.AddSingleton<IRecordTemplateRepository, JsonRecordTemplateRepository>();
builder.Services.AddSingleton<IFetchARecord, SimulatedFetchARecord>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.Configure<BackgroundWorkerOptions>(o => o.WorkerCount = 4);
builder.Services.AddHostedService<QueuedHostedService>();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<RealtimeHub>("/hubs/realtime");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
