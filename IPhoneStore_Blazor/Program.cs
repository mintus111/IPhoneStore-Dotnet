using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Blazor.Components;
using IPhoneStore_Blazor.Data;
using IPhoneStore_Blazor.Models.Entities;
using IPhoneStore_Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + Interactive Server (Blazor Web App)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DbContextFactory + Scoped DbContext: giải pháp chuẩn cho Blazor Server kết hợp ASP.NET Core Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<IPhoneStoreDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<IPhoneStoreDbContext>>().CreateDbContext());

// Cấu hình ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<IPhoneStoreDbContext>()
.AddDefaultTokenProviders();

// Cookie Authentication + chuyển hướng thông minh cho Admin / User
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/admin/login?returnUrl={returnUrl}");
            }
            else
            {
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Cấu hình Session cho Quản lý Thất bại đăng nhập & Math CAPTCHA
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Giỏ hàng (lưu trình duyệt) + Toast thông báo
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

// Tự động Seed Roles & Tài khoản mặc định Identity
await IdentityDataSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Đăng xuất Admin (xoá cookie -> về /admin/login)
app.MapGet("/Admin/SignOut", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/admin/login");
});

// Đăng xuất Khách (xoá cookie -> về trang chủ)
app.MapGet("/Account/SignOut", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

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

// Endpoint tự động chuẩn hóa font tiếng Việt trong CSDL
app.MapGet("/api/fix-encoding", async (IPhoneStoreDbContext context) =>
{
    static string CleanFont(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var s = System.Text.RegularExpressions.Regex.Replace(input, @"Sa\s*M[^\s]*c", "Sa Mạc", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"M[áâå\?º¡]+c", "Mạc", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return s.Replace("Sa Máº¡c", "Sa Mạc")
                .Replace("Máº¡c", "Mạc")
                .Replace("Sa Måº¡c", "Sa Mạc")
                .Replace("Måº¡c", "Mạc")
                .Replace("Sa Mº¡c", "Sa Mạc")
                .Replace("Mº¡c", "Mạc")
                .Replace("Sa M?c", "Sa Mạc")
                .Replace("áº¡", "ạ")
                .Replace("åº¡", "ạ")
                .Replace("º¡", "ạ");
    }

    int productCount = 0;
    var products = await context.Products.ToListAsync();
    foreach (var p in products)
    {
        bool changed = false;
        if (p.Name != null)
        {
            var c = CleanFont(p.Name);
            if (c != p.Name) { p.Name = c; changed = true; }
        }
        if (p.Color != null)
        {
            var c = CleanFont(p.Color);
            if (c != p.Color) { p.Color = c; changed = true; }
        }
        if (p.Description != null)
        {
            var c = CleanFont(p.Description);
            if (c != p.Description) { p.Description = c; changed = true; }
        }
        if (changed) productCount++;
    }
    await context.SaveChangesAsync();
    return Results.Json(new { success = true, updatedProducts = productCount });
});

// Đăng nhập nhanh từ Modal Popup (Đồng bộ Định danh 1 ô & Phân hệ)
app.MapPost("/Account/QuickLogin", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var identifier = (form["username"].ToString().Trim()).Length > 0 ? form["username"].ToString().Trim() : form["identifier"].ToString().Trim();
    var password = form["password"].ToString().Trim();
    var returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/Account/Login?error=Vui lòng nhập đầy đủ thông tin đăng nhập");
    }

    var user = await AuthHelper.FindUserByIdentifierAsync(userManager, identifier);
    if (user == null || !user.IsActive)
    {
        return Results.Redirect("/Account/Login?error=Tên đăng nhập hoặc mật khẩu không chính xác!");
    }

    // Chặn nhân sự nội bộ đăng nhập nhầm cổng khách hàng (Mục 2.4.2.4)
    var roles = await userManager.GetRolesAsync(user);
    bool isCustomer = roles.Contains("Customer");
    bool isInternalOnly = roles.Any(r => r is "Admin" or "Manager" or "Staff" or "Cashier" or "Shipper") && !isCustomer;
    if (isInternalOnly)
    {
        return Results.Redirect("/Account/Login?error=Tên đăng nhập hoặc mật khẩu không chính xác!");
    }

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        return Results.Redirect("/Account/Login?error=Tên đăng nhập hoặc mật khẩu không chính xác!");
    }

    if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
    {
        return Results.Redirect(returnUrl);
    }
    return Results.Redirect(roles.Contains("Admin") ? "/admin" : "/");
}).DisableAntiforgery();

// Đăng ký nhanh từ Modal Popup (Đồng bộ Anti-Dropoff & Chuẩn hóa E.164)
app.MapPost("/Account/QuickRegister", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IPhoneStoreDbContext dbContext) =>
{
    bool isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                  context.Request.Headers.Accept.ToString().Contains("application/json");

    var form = await context.Request.ReadFormAsync();
    var rawUsername = form["username"].ToString().Trim();
    var fullName = form["fullName"].ToString().Trim();
    var email = form["email"].ToString().Trim();
    var phone = form["phone"].ToString().Trim();
    var password = form["password"].ToString().Trim();

    if (string.IsNullOrEmpty(fullName) || fullName.Length < 2)
    {
        var msg = "Vui lòng nhập họ và tên (tối thiểu 2 ký tự)!";
        if (isAjax) return Results.Json(new { success = false, message = msg });
        return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
    }

    var (isPhoneValid, phoneErr) = AuthHelper.ValidatePhoneNumber(phone);
    if (!isPhoneValid)
    {
        var msg = phoneErr ?? "Số điện thoại không hợp lệ!";
        if (isAjax) return Results.Json(new { success = false, message = msg });
        return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
    }
    var e164Phone = AuthHelper.NormalizePhoneNumber(phone);

    // Xử lý username (nếu để trống tự lấy SĐT)
    string finalUsername = e164Phone;
    if (!string.IsNullOrEmpty(rawUsername))
    {
        var (isUserValid, userErr) = AuthHelper.ValidateUsername(rawUsername);
        if (!isUserValid)
        {
            var msg = userErr ?? "Tên đăng nhập không hợp lệ!";
            if (isAjax) return Results.Json(new { success = false, message = msg });
            return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
        }

        var uName = rawUsername.Trim();
        var userExists = await userManager.FindByNameAsync(uName.ToLowerInvariant()) != null ||
                         await userManager.FindByNameAsync(uName) != null;
        if (userExists)
        {
            var msg = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
            if (isAjax) return Results.Json(new { success = false, message = msg });
            return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
        }

        finalUsername = uName.ToLowerInvariant();
    }

    // Kiểm tra mật khẩu
    var (isPwdValid, pwdErr) = AuthHelper.ValidatePasswordComplexity(password, finalUsername, e164Phone);
    if (!isPwdValid)
    {
        var msg = pwdErr ?? "Mật khẩu không đạt chuẩn bảo mật!";
        if (isAjax) return Results.Json(new { success = false, message = msg });
        return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
    }

    // Kiểm tra email nếu có
    string? canonicalEmail = null;
    if (!string.IsNullOrEmpty(email))
    {
        var (isEmailValid, emailErr) = AuthHelper.ValidateEmail(email);
        if (!isEmailValid)
        {
            var msg = emailErr ?? "Email không đúng định dạng!";
            if (isAjax) return Results.Json(new { success = false, message = msg });
            return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
        }
        canonicalEmail = AuthHelper.GetCanonicalEmail(email);

        var emailExists = await userManager.Users.AnyAsync(u => u.NormalizedEmail == canonicalEmail.ToUpper() || u.Email == canonicalEmail) ||
                          await dbContext.Customers.AnyAsync(c => c.Email != null && c.Email.ToLower() == canonicalEmail.ToLower());
        if (emailExists)
        {
            var msg = "Địa chỉ email này đã được liên kết với một tài khoản khác!";
            if (isAjax) return Results.Json(new { success = false, message = msg });
            return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(msg));
        }
    }

    var user = new ApplicationUser
    {
        UserName = finalUsername,
        Email = canonicalEmail,
        PhoneNumber = e164Phone,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(user, "Customer");
        var customer = new Customer
        {
            UserId = user.Id,
            FullName = fullName,
            Email = user.Email,
            PhoneNumber = e164Phone,
            Address = "Chưa cập nhật địa chỉ",
            CreatedAt = DateTime.UtcNow,
            MembershipTier = "Member",
            RewardPoints = 0,
            TotalSpent = 0
        };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        await signInManager.SignInAsync(user, isPersistent: true);

        if (isAjax) return Results.Json(new { success = true, redirectUrl = "/" });
        return Results.Redirect("/");
    }

    var errSummary = string.Join("; ", result.Errors.Select(e => e.Description));
    if (isAjax) return Results.Json(new { success = false, message = errSummary });
    return Results.Redirect("/Account/Register?error=" + Uri.EscapeDataString(errSummary));
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

