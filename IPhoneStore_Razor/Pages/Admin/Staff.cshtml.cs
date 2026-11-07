using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class StaffModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffModel(IPhoneStoreDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public class CustomerItemViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int OrderCount { get; set; }
        public int WarrantyCount { get; set; }
    }

    public List<CustomerItemViewModel> Customers { get; set; } = new();
    public int TotalCustomers { get; set; }
    public int ActiveAccounts { get; set; }
    public int LockedAccounts { get; set; }
    public int TotalOrders { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        // 1. Đảm bảo tài khoản admin không bị gắn nhầm role Customer
        var adminUser = await _userManager.FindByNameAsync("admin");
        if (adminUser != null && await _userManager.IsInRoleAsync(adminUser, "Customer"))
        {
            await _userManager.RemoveFromRoleAsync(adminUser, "Customer");
        }

        // 2. Tự động seed đủ 5 khách hàng thành viên nếu chưa có
        var hasAllFive = await _context.Customers.CountAsync(c => c.User != null &&
            (c.User.UserName == "nghecute" || c.User.UserName == "mainguyen" || c.User.UserName == "hungtran" || c.User.UserName == "longle" || c.User.UserName == "thaopham")) == 5;

        if (!hasAllFive)
        {
            await IdentityDataSeeder.SeedAsync(HttpContext.RequestServices);
        }

        // 3. Truy vấn dữ liệu khách hàng
        var query = _context.Customers
            .Include(c => c.User)
            .Include(c => c.Orders)
            .Include(c => c.Warranties)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                     c.PhoneNumber.Contains(kw) ||
                                     (c.Email != null && c.Email.ToLower().Contains(kw)) ||
                                     (c.User != null && c.User.UserName!.ToLower().Contains(kw)) ||
                                     c.Id.ToString() == kw);
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            if (StatusFilter == "active")
                query = query.Where(c => c.User != null && c.User.IsActive);
            else if (StatusFilter == "locked")
                query = query.Where(c => c.User != null && !c.User.IsActive);
            else if (StatusFilter == "guest")
                query = query.Where(c => c.UserId == null);
        }

        var allCustomers = await _context.Customers.Include(c => c.User).Include(c => c.Orders).ToListAsync();
        var rawList = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        Customers = rawList.Select(c => new CustomerItemViewModel
        {
            Id = c.Id,
            UserId = c.UserId,
            UserName = c.User?.UserName,
            FullName = c.FullName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Address = c.Address,
            CreatedAt = c.CreatedAt,
            IsActive = c.User?.IsActive ?? true,
            OrderCount = c.Orders.Count,
            WarrantyCount = c.Warranties.Count
        }).ToList();

        TotalCustomers = allCustomers.Count;
        ActiveAccounts = allCustomers.Count(c => c.User != null && c.User.IsActive);
        LockedAccounts = allCustomers.Count(c => c.User != null && !c.User.IsActive);
        TotalOrders = allCustomers.Sum(c => c.Orders.Count);
    }

    // HANDLER 1: Chỉnh sửa thông tin khách hàng trực tiếp qua Modal (Hỗ trợ Ajax)
    public async Task<IActionResult> OnPostEditCustomerAsync(int id, string fullName, string phoneNumber, string? email, string? address)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString().Contains("application/json"));

        var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            var msg = "Không tìm thấy hồ sơ khách hàng trên hệ thống!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        // Validate Họ tên
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
        {
            var msg = "Vui lòng nhập họ và tên của khách hàng (tối thiểu 2 ký tự)!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        // Validate SĐT
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            var msg = "Vui lòng nhập số điện thoại di động!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (!Regex.IsMatch(cleanPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
        {
            var msg = "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        string stdPhone = cleanPhone.StartsWith("+84") ? ("0" + cleanPhone.Substring(3)) : cleanPhone;
        string altPhone = cleanPhone.StartsWith("0") ? ("+84" + cleanPhone.Substring(1)) : cleanPhone;

        var dupPhone = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id != id && (c.PhoneNumber == stdPhone || c.PhoneNumber == altPhone || c.PhoneNumber == cleanPhone));
        if (dupPhone != null)
        {
            var msg = $"Số điện thoại {stdPhone} này đã tồn tại trên hệ thống (đang được liên kết với khách hàng #{dupPhone.Id} - {dupPhone.FullName})!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        // Validate Email
        string? cleanEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            cleanEmail = email.Trim().ToLower();
            if (!Regex.IsMatch(cleanEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                var msg = "Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!";
                if (isAjax) return new JsonResult(new { success = false, message = msg });
                ErrorMessage = msg;
                return RedirectToPage();
            }

            var dupEmail = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id != id && c.Email != null && c.Email.ToLower() == cleanEmail);
            if (dupEmail != null)
            {
                var msg = $"Địa chỉ email {cleanEmail} này đã được liên kết với khách hàng {dupEmail.FullName}!";
                if (isAjax) return new JsonResult(new { success = false, message = msg });
                ErrorMessage = msg;
                return RedirectToPage();
            }
        }

        var cleanAddress = address?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(cleanAddress) && cleanAddress.Length < 5)
        {
            var msg = "Vui lòng nhập số nhà, tên đường cụ thể (tối thiểu 5 ký tự)!";
            if (isAjax) return new JsonResult(new { success = false, message = msg });
            ErrorMessage = msg;
            return RedirectToPage();
        }

        // Cập nhật CSDL
        customer.FullName = fullName.Trim();
        customer.PhoneNumber = stdPhone;
        customer.Email = cleanEmail;
        customer.Address = cleanAddress;

        if (customer.User != null)
        {
            customer.User.PhoneNumber = stdPhone;
            if (!string.IsNullOrWhiteSpace(cleanEmail))
            {
                customer.User.Email = cleanEmail;
                customer.User.NormalizedEmail = cleanEmail.ToUpperInvariant();
            }
            await _userManager.UpdateAsync(customer.User);
        }

        await _context.SaveChangesAsync();

        var successMsg = $"Đã cập nhật thông tin khách hàng {customer.FullName} thành công!";
        if (isAjax) return new JsonResult(new { success = true, message = successMsg });

        SuccessMessage = successMsg;
        return RedirectToPage();
    }

    // HANDLER 2: Cấp lại mật khẩu khách hàng
    public async Task<IActionResult> OnPostResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && !string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword.Trim());
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                SuccessMessage = $"Đã cấp lại mật khẩu thành công cho tài khoản {user.UserName}!";
            }
            else
            {
                ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
            }
        }
        return RedirectToPage();
    }

    // HANDLER 3: Khóa / Mở khóa tài khoản phòng vệ 2 lớp
    public async Task<IActionResult> OnPostToggleStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            if (!user.IsActive)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.LockoutEnabled = true;
            }
            else
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }

            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            SuccessMessage = user.IsActive
                ? $"Đã kích hoạt và mở khóa thành công cho tài khoản {user.UserName}!"
                : $"Đã khóa tài khoản {user.UserName}!";
        }
        return RedirectToPage();
    }
}
