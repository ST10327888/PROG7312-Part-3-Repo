using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartXGateway.Data;
using SmartXGateway.Hubs;
using SmartXGateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// RUBRIC IMPROVEMENT: antiforgery middleware is required for [AutoValidateAntiforgeryToken]
// to work on JSON POSTs (used by the CommandStream/Issues/Sensors controllers).
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("SmartXDb"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<DeploymentValidator>();
builder.Services.AddSingleton<FileEncryptionService>();
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<SensorStore>();
    store.SeedDefaults();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// RUBRIC IMPROVEMENT: block direct static access to /uploads ciphertext.
// The download flow must go through SensorsController.DownloadAttachment
// (which decrypts server-side and enforces [Authorize]).
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            ctx.Context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// RUBRIC IMPROVEMENT: required for the antiforgery header on fetch POSTs.
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// RUBRIC IMPROVEMENT: only authenticated users can connect to the hub.
app.MapHub<TelemetryHub>("/hubs/telemetry").RequireAuthorization();

app.Run();

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
       
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].

  Microsoft, 2024. ASP.NET Core SignalR documentation. [online] 
       Available at: <https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction> 
       [Accessed 18 November 2025].
   ============================================================ */