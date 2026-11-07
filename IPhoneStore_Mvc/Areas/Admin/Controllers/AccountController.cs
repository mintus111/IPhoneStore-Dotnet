using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Admin/Login hoặc /Admin/Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && 
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")))
        {
            return RedirectToAction("Index", "Admin", new { area = "Admin" });
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: /Admin/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu Quản Trị!";
            return View();
        }

        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user == null)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu Quản Trị không chính xác!";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Tài khoản quản trị này đã bị vô hiệu hóa!";
            return View();
        }

        // Kiểm tra quyền: Tài khoản phải thuộc danh sách vai trò nội bộ quản trị vận hành
        var roles = await _userManager.GetRolesAsync(user);
        bool hasAdminAccess = roles.Any(r => r is "Admin" or "Manager" or "Staff" or "Cashier" or "Shipper");
        if (!hasAdminAccess)
        {
            ViewBag.Error = "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập hệ thống Quản trị!";
            return View();
        }

        // Đăng nhập qua SignInManager của Identity
        var result = await _signInManager.PasswordSignInAsync(user, password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu Quản Trị không chính xác!";
            return View();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/Account/Login"))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Admin", new { area = "Admin" });
    }

    // GET & POST: /Admin/Logout
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account", new { area = "Admin" });
    }
}
