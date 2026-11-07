using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages.Account;

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
    public string Identifier { get; set; } = string.Empty;

    // Giữ thuộc tính Username để backward-compatibility
    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; } = true;

    [BindProperty]
    public bool RequireCaptcha { get; set; } = false;

    [BindProperty]
    public string? CaptchaCode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string CaptchaQuestion { get; set; } = "7 + 5 = ?";

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Bỏ qua nếu ReturnUrl trỏ về chính trang Login hoặc Register để tránh lặp vô hạn
        if (!string.IsNullOrEmpty(ReturnUrl) &&
            (ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
             ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase)))
        {
            ReturnUrl = null;
        }

        // Nếu đã đăng nhập thành công thì tự động chuyển hướng về trang chủ (hoặc ReturnUrl hợp lệ)
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) &&
                Url.IsLocalUrl(ReturnUrl) &&
                !ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
                !ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase) &&
                !ReturnUrl.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(ReturnUrl);
            }
            return RedirectToPage("/Index");
        }

        var sessionFails = HttpContext.Session.GetInt32("FailedLoginCount") ?? 0;
        RequireCaptcha = sessionFails >= 3;
        if (RequireCaptcha)
        {
            EnsureCaptchaGenerated();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? username = null, string? password = null)
    {
        // Bỏ qua nếu ReturnUrl trỏ về chính trang Login hoặc Register để tránh lặp vô hạn
        if (!string.IsNullOrEmpty(ReturnUrl) &&
            (ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
             ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase)))
        {
            ReturnUrl = null;
        }

        // Hỗ trợ tương thích ngược nếu form gửi trường name="Username" hoặc name="Identifier"
        if (string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrWhiteSpace(Username))
        {
            Identifier = Username;
        }
        else if (string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrWhiteSpace(username))
        {
            Identifier = username;
        }

        if (string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(password))
        {
            Password = password;
        }

        var sessionFails = HttpContext.Session.GetInt32("FailedLoginCount") ?? 0;
        bool isCaptchaRequired = RequireCaptcha || sessionFails >= 3;

        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin định danh và mật khẩu!";
            PrepareCaptchaView(isCaptchaRequired);
            return Page();
        }

        var rawIdentifier = Identifier.Trim();

        // 1. Phân loại đầu vào tất định (Deterministic Input Classifier)
        var user = await AuthHelper.FindUserByIdentifierAsync(_userManager, rawIdentifier);

        // Kiểm tra xem User này có đang bị đánh dấu cần Captcha không (>= 3 lần thất bại)
        if (user != null && user.AccessFailedCount >= 3)
        {
            isCaptchaRequired = true;
        }

        // 2. Kiểm tra xác thực CAPTCHA nếu đang trong diện phòng vệ Cấp 1
        if (isCaptchaRequired)
        {
            var expectedAnswer = HttpContext.Session.GetString("CaptchaAnswer");
            if (string.IsNullOrWhiteSpace(CaptchaCode) || CaptchaCode.Trim() != expectedAnswer)
            {
                GenerateCaptcha();
                ErrorMessage = "Mã xác thực bảo vệ (CAPTCHA) không chính xác! Vui lòng thử lại.";
                RequireCaptcha = true;
                return Page();
            }
        }

        // 3. Phòng thủ chống rà quét danh tính (Username Enumeration Defense - Mục 2.4.2.4)
        if (user == null)
        {
            sessionFails++;
            HttpContext.Session.SetInt32("FailedLoginCount", sessionFails);
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            PrepareCaptchaView(sessionFails >= 3);
            return Page();
        }

        // 4. Kiểm tra trạng thái khóa tài khoản (Brute-force Cấp 2 và Cấp 3)
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            if (user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1))
            {
                ErrorMessage = "Tài khoản của bạn đã bị khóa vĩnh viễn do nhập sai mật khẩu quá 10 lần. Vui lòng liên hệ bộ phận hỗ trợ hoặc sử dụng 'Quên mật khẩu' để khôi phục!";
            }
            else
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes);
                ErrorMessage = $"Tài khoản tạm thời bị khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút hoặc dùng Quên mật khẩu.";
            }

            PrepareCaptchaView(true);
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản của bạn đã bị ngừng kích hoạt. Vui lòng liên hệ quản trị viên cửa hàng!";
            PrepareCaptchaView(isCaptchaRequired);
            return Page();
        }

        // 5. Kiểm tra phân hệ: Chỉ tài khoản vai trò Customer mới được phép đăng nhập Cổng Khách Hàng.
        // Tuyệt đối KHÔNG hiển thị liên kết hay gợi ý về đường dẫn Admin Area để chống rà quét
        var roles = await _userManager.GetRolesAsync(user);
        bool isCustomer = roles.Contains("Customer");
        bool isInternalOnly = roles.Any(r => r is "Admin" or "Manager" or "Staff" or "Cashier" or "Shipper") && !isCustomer;

        if (isInternalOnly)
        {
            sessionFails++;
            HttpContext.Session.SetInt32("FailedLoginCount", sessionFails);
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            PrepareCaptchaView(sessionFails >= 3);
            return Page();
        }

        // 6. Xác thực mật khẩu
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, Password.Trim());
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            var failCount = await _userManager.GetAccessFailedCountAsync(user);

            sessionFails++;
            HttpContext.Session.SetInt32("FailedLoginCount", sessionFails);

            if (failCount >= 10)
            {
                // Cấp 3: Khóa cứng vĩnh viễn
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                ErrorMessage = "Tài khoản của bạn đã bị khóa vĩnh viễn do nhập sai mật khẩu 10 lần liên tiếp. Vui lòng sử dụng 'Quên mật khẩu' để xác thực lại!";
                PrepareCaptchaView(true);
                return Page();
            }
            else if (failCount >= 5)
            {
                // Cấp 2: Khóa tạm thời 15 phút
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                ErrorMessage = "Tài khoản tạm thời bị khóa 15 phút do nhập sai mật khẩu 5 lần. Vui lòng thử lại sau hoặc dùng Quên mật khẩu.";
                PrepareCaptchaView(true);
                return Page();
            }
            else if (failCount >= 3 || sessionFails >= 3)
            {
                // Cấp 1: Yêu cầu giải CAPTCHA
                GenerateCaptcha();
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác! Vui lòng nhập mã bảo vệ CAPTCHA bên dưới.";
                RequireCaptcha = true;
                return Page();
            }
            else
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                PrepareCaptchaView(false);
                return Page();
            }
        }

        // 7. Xác thực thành công: Reset bộ đếm lỗi & Đăng nhập
        await _userManager.ResetAccessFailedCountAsync(user);
        HttpContext.Session.Remove("FailedLoginCount");
        HttpContext.Session.Remove("CaptchaAnswer");
        HttpContext.Session.Remove("CaptchaQuestion");

        var result = await _signInManager.PasswordSignInAsync(user, Password.Trim(), isPersistent: RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ErrorMessage = "Đăng nhập không thành công. Vui lòng thử lại!";
            return Page();
        }

        // Chuyển hướng sau khi đăng nhập thành công:
        // Nếu có ReturnUrl hợp lệ và không phải trang Account thì về ReturnUrl, ngược lại về Trang chủ (/Index)
        if (!string.IsNullOrEmpty(ReturnUrl) && 
            Url.IsLocalUrl(ReturnUrl) && 
            !ReturnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) && 
            !ReturnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase) &&
            !ReturnUrl.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnGetLogoutAsync(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        if (!string.IsNullOrEmpty(returnUrl) && 
            Url.IsLocalUrl(returnUrl) && 
            !returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
            !returnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostLogoutAsync(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        if (!string.IsNullOrEmpty(returnUrl) && 
            Url.IsLocalUrl(returnUrl) && 
            !returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
            !returnUrl.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private void EnsureCaptchaGenerated()
    {
        var existingQuestion = HttpContext.Session.GetString("CaptchaQuestion");
        if (string.IsNullOrEmpty(existingQuestion))
        {
            GenerateCaptcha();
        }
        else
        {
            CaptchaQuestion = existingQuestion;
        }
    }

    private void PrepareCaptchaView(bool isRequired)
    {
        RequireCaptcha = isRequired;
        if (isRequired)
        {
            EnsureCaptchaGenerated();
        }
    }

    private void GenerateCaptcha()
    {
        var (question, answer) = AuthHelper.GenerateMathCaptcha();
        HttpContext.Session.SetString("CaptchaQuestion", question);
        HttpContext.Session.SetString("CaptchaAnswer", answer);
        CaptchaQuestion = question;
    }
}
