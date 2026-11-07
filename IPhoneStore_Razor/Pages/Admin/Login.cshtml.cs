using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")))
        {
            return RedirectToPage("/Admin/Products");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu Quản Trị!";
            return Page();
        }

        var user = await _userManager.FindByNameAsync(Username.Trim());
        if (user == null)
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu Quản Trị không chính xác!";
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản quản trị này đã bị vô hiệu hóa!";
            return Page();
        }

        var roles = await _userManager.GetRolesAsync(user);
        bool hasAdminAccess = roles.Any(r => r is "Admin" or "Manager" or "Staff" or "Cashier" or "Shipper");
        if (!hasAdminAccess)
        {
            ErrorMessage = "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập hệ thống Quản trị!";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu Quản Trị không chính xác!";
            return Page();
        }

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) && !ReturnUrl.Contains("/Account/Login"))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Admin/Products");
    }

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Admin/Login");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Admin/Login");
    }
}
