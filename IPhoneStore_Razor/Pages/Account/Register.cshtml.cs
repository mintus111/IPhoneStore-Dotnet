using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(
        IPhoneStoreDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Tùy chọn - Chống Drop-off rate (Mục 2.4.1.2)
    [BindProperty]
    public string? Address { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!string.IsNullOrEmpty(ReturnUrl) &&
            (ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
             ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase)))
        {
            ReturnUrl = null;
        }

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) &&
                Url.IsLocalUrl(ReturnUrl) &&
                !ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
                !ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(ReturnUrl);
            }
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. Kiểm tra Họ và tên
        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length < 2)
        {
            ErrorMessage = "Vui lòng nhập họ và tên của bạn (tối thiểu 2 ký tự)!";
            return Page();
        }

        // 2. Kiểm tra Số điện thoại theo chuẩn thị trường Việt Nam (10 số, đầu 03x-09x)
        var (isPhoneValid, phoneErr) = AuthHelper.ValidatePhoneNumber(PhoneNumber);
        if (!isPhoneValid)
        {
            ErrorMessage = phoneErr ?? "Số điện thoại không hợp lệ!";
            return Page();
        }

        var cleanedPhone = PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        var e164Phone = AuthHelper.NormalizePhoneNumber(cleanedPhone);

        var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == e164Phone || u.PhoneNumber == cleanedPhone) ||
                          await _context.Customers.AnyAsync(c => c.PhoneNumber == e164Phone || c.PhoneNumber == cleanedPhone);
        if (phoneExists)
        {
            ErrorMessage = "Số điện thoại này đã được sử dụng trên hệ thống. Vui lòng đăng nhập hoặc dùng số khác!";
            return Page();
        }

        // 3. Kiểm tra Email & Canonical Email Protection đối với Gmail
        string? canonicalEmail = null;
        if (!string.IsNullOrWhiteSpace(Email))
        {
            var (isEmailValid, emailErr) = AuthHelper.ValidateEmail(Email);
            if (!isEmailValid)
            {
                ErrorMessage = emailErr ?? "Địa chỉ email không đúng định dạng!";
                return Page();
            }

            var trimmedEmail = Email.Trim().ToLowerInvariant();
            canonicalEmail = AuthHelper.GetCanonicalEmail(trimmedEmail);

            var emailExists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == trimmedEmail.ToUpper() || u.Email == trimmedEmail || u.Email == canonicalEmail);
            if (!emailExists)
            {
                emailExists = await _context.Customers.AnyAsync(c => c.Email != null && (c.Email.ToLower() == trimmedEmail || c.Email.ToLower() == canonicalEmail));
            }

            if (emailExists)
            {
                ErrorMessage = "Địa chỉ email này đã được liên kết với một tài khoản khác!";
                return Page();
            }
        }

        // 4. Xử lý Tên đăng nhập (Username)
        string finalUsername;
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var (isUserValid, userErr) = AuthHelper.ValidateUsername(Username);
            if (!isUserValid)
            {
                ErrorMessage = userErr ?? "Tên đăng nhập không hợp lệ!";
                return Page();
            }

            var uName = Username.Trim();
            var userExists = await _userManager.FindByNameAsync(uName.ToLowerInvariant()) != null ||
                             await _userManager.FindByNameAsync(uName) != null;
            if (userExists)
            {
                ErrorMessage = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                return Page();
            }

            finalUsername = uName.ToLowerInvariant();
        }
        else
        {
            // Tối ưu UX: Nếu khách không nhập Username, tự động lấy SĐT E.164 làm Username định danh
            finalUsername = e164Phone;
        }

        // 5. Kiểm tra Mật khẩu & Độ phức tạp (Complexity Rule: 3 trong 4 nhóm)
        var (isPwdValid, pwdErr) = AuthHelper.ValidatePasswordComplexity(Password, finalUsername, e164Phone);
        if (!isPwdValid)
        {
            ErrorMessage = pwdErr ?? "Mật khẩu không đạt yêu cầu bảo mật!";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập!";
            return Page();
        }

        // 6. Xử lý Địa chỉ (Tối ưu Drop-off Rate: Không ép nhập địa chỉ khi Đăng ký)
        var finalAddress = string.IsNullOrWhiteSpace(Address) ? "Chưa cập nhật địa chỉ" : Address.Trim();

        // 7. Khởi tạo ApplicationUser
        var user = new ApplicationUser
        {
            UserName = finalUsername,
            Email = canonicalEmail,
            PhoneNumber = e164Phone,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, Password.Trim());
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
            return Page();
        }

        // Gán Role Customer
        await _userManager.AddToRoleAsync(user, "Customer");

        // 8. Khởi tạo Hồ sơ Khách hàng (Customer Profile)
        var customer = new Customer
        {
            UserId = user.Id,
            FullName = FullName.Trim(),
            PhoneNumber = e164Phone,
            Email = canonicalEmail,
            Address = finalAddress,
            CreatedAt = DateTime.Now,
            MembershipTier = "Member",
            RewardPoints = 0,
            TotalSpent = 0
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // 9. Tự động đăng nhập & Điều hướng
        await _signInManager.SignInAsync(user, isPersistent: true);

        if (!string.IsNullOrEmpty(ReturnUrl) &&
            (ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
             ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase)))
        {
            ReturnUrl = null;
        }

        if (!string.IsNullOrEmpty(ReturnUrl) && 
            Url.IsLocalUrl(ReturnUrl) && 
            !ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) && 
            !ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }
}
