using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;
using IPhoneStore_Mvc.Services;

var contentRoot = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Views"))
    ? Directory.GetCurrentDirectory()
    : Directory.Exists(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Views"))
        ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
        : Directory.GetCurrentDirectory();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});

// Cấu hình Logging sạch, loại bỏ EventLog của Windows để tránh lỗi ObjectDisposedException
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Đảm bảo BackgroundService gặp lỗi không làm sập toàn bộ Web Server
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// DbContext dùng chung IPhoneStoreDb
builder.Services.AddDbContext<IPhoneStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<IPhoneStoreDbContext>()
.AddDefaultTokenProviders();

// Cấu hình Application Cookie của Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            context.Response.Redirect($"/Admin/Account/Login?returnUrl={returnUrl}");
        }
        else
        {
            context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
        }
        return Task.CompletedTask;
    };
});

// Session cho Giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddHostedService<OrderHoldCleanupWorker>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Khởi tạo Seed Data cho Identity (Admin & Customer mẫu)
try
{
    await IdentityDataSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"[Seeder Error on Startup]: {ex.Message}");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Route cho Area Admin
app.MapControllerRoute(
    name: "admin_direct",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Admin" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
