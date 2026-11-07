using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public class AccountViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public List<AccountViewModel> Accounts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    public async Task OnGetAsync()
    {
        var query = _userManager.Users.Include(u => u.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        Accounts = new List<AccountViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Accounts.Add(new AccountViewModel
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Customer",
                FullName = user.Customer?.FullName,
                PhoneNumber = user.Customer?.PhoneNumber ?? user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        }
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null && user.UserName != "admin")
        {
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            AlertMessage = user.IsActive
                ? $"Đã MỞ KHÓA tài khoản: {user.UserName}"
                : $"Đã KHÓA tài khoản: {user.UserName}";
        }
        return RedirectToPage(new { SearchKeyword });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null && user.UserName != "admin")
        {
            await _userManager.DeleteAsync(user);
            AlertMessage = $"Đã xóa tài khoản: {user.UserName}";
        }
        return RedirectToPage(new { SearchKeyword });
    }
}
