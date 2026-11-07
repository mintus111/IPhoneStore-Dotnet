using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký IPhoneStoreDbContext kết nối SQL Server (Database First dùng chung)
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
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase) || 
            path.StartsWith("/Admin/Login", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            context.Response.Redirect($"/Admin/Login?returnUrl={returnUrl}");
        }
        else
        {
            context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
        }
        return Task.CompletedTask;
    };
});

// Cấu hình Session cho Giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();

// Đăng ký dịch vụ Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Khởi tạo Seed Data cho Identity
await IdentityDataSeeder.SeedAsync(app.Services);

// Cấu hình Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// API Kiểm tra trùng lặp thời gian thực cho Đăng ký (Mục 2.4.1.3)
app.MapGet("/Account/CheckAvailability", async (
    string? type,
    string? value,
    UserManager<ApplicationUser> userManager,
    IPhoneStoreDbContext context) =>
{
    var result = await AuthHelper.CheckAvailabilityAsync(type, value, userManager, context);
    return Results.Json(new
    {
        available = result.Available,
        message = result.Message,
        exists = result.Exists,
        invalidFormat = result.InvalidFormat
    });
});

// API Kiểm tra trùng lặp mã SKU thời gian thực cho Thêm / Chỉnh sửa Sản phẩm
app.MapGet("/Admin/CheckSkuAvailability", async (
    string? sku,
    int? productId,
    IPhoneStoreDbContext context) =>
{
    if (string.IsNullOrWhiteSpace(sku))
    {
        return Results.Json(new
        {
            available = false,
            exists = false,
            message = "⚠️ Mã SKU không được để trống!"
        });
    }

    var trimmed = sku.Trim().ToLower();
    bool exists = await context.Products.AnyAsync(p => 
        (productId == null || p.Id != productId.Value) && 
        p.Sku != null && 
        p.Sku.ToLower() == trimmed);

    return Results.Json(new
    {
        available = !exists,
        exists = exists,
        message = exists
            ? $"⚠️ Mã SKU '{sku.Trim()}' đã tồn tại trên một sản phẩm khác trong hệ thống!"
            : $"✅ Mã SKU '{sku.Trim()}' hợp lệ (khả dụng)."
    });
});

app.Run();

