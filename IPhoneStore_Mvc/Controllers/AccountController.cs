using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;
using IPhoneStore_Mvc.Models.ViewModels;

namespace IPhoneStore_Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IPhoneStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    // Danh sách từ cấm không được đặt làm Username theo chuẩn nghiệp vụ 2.4.1
    private static readonly HashSet<string> UsernameBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "support", "moderator", "helpdesk",
        "system", "null", "undefined", "guest", "istore", "manager", "staff", "cashier", "shipper", "apple"
    };

    // Danh sách mật khẩu phổ biến bị chặn
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "password", "qwertyuiop", "admin123", "iloveyou", "11111111", "00000000"
    };

    public AccountController(
        IPhoneStoreDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        var failedCount = HttpContext.Session.GetInt32("FailedLoginCount") ?? 0;
        bool requireCaptcha = failedCount >= 3;
        ViewBag.RequireCaptcha = requireCaptcha;

        if (requireCaptcha)
        {
            EnsureCaptchaGenerated();
        }

        var model = new CustomerLoginViewModel
        {
            ReturnUrl = returnUrl,
            RequireCaptcha = requireCaptcha,
            RememberMe = true
        };

        return View(model);
    }

    // POST: /Account/Login (Định danh 1 ô duy nhất & Phòng vệ Brute-force 3 Tầng)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(CustomerLoginViewModel model, string? username = null, string? password = null)
    {
        // Hỗ trợ backward-compatibility nếu form gửi name="username" và name="password"
        if (string.IsNullOrWhiteSpace(model.Identifier) && !string.IsNullOrWhiteSpace(username))
        {
            model.Identifier = username;
        }
        if (string.IsNullOrWhiteSpace(model.Password) && !string.IsNullOrWhiteSpace(password))
        {
            model.Password = password;
        }

        ViewBag.ReturnUrl = model.ReturnUrl;

        var sessionFails = HttpContext.Session.GetInt32("FailedLoginCount") ?? 0;
        bool requireCaptcha = model.RequireCaptcha || sessionFails >= 3;

        if (string.IsNullOrWhiteSpace(model.Identifier) || string.IsNullOrWhiteSpace(model.Password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin định danh và mật khẩu!";
            PrepareCaptchaView(requireCaptcha);
            model.RequireCaptcha = requireCaptcha;
            return View(model);
        }

        var rawIdentifier = model.Identifier.Trim();

        // 1. Phân loại đầu vào tất định (Deterministic Input Classifier)
        ApplicationUser? user = null;
        if (rawIdentifier.Contains('@'))
        {
            // Định dạng Email
            var normalizedEmail = rawIdentifier.ToLowerInvariant();
            user = await _userManager.FindByEmailAsync(normalizedEmail)
                   ?? await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpper() || u.Email == normalizedEmail);
        }
        else if (Regex.IsMatch(rawIdentifier, @"^(0|\+84)[0-9]{9,10}$"))
        {
            // Định dạng Số điện thoại Việt Nam
            var e164 = NormalizePhoneNumber(rawIdentifier);
            user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == e164 || u.PhoneNumber == rawIdentifier);
        }
        else
        {
            // Định dạng Username
            user = await _userManager.FindByNameAsync(rawIdentifier.ToLowerInvariant())
                   ?? await _userManager.FindByNameAsync(rawIdentifier);
        }

        // Kiểm tra xem User này có đang bị đánh dấu cần Captcha không (>= 3 lần thất bại)
        if (user != null && user.AccessFailedCount >= 3)
        {
            requireCaptcha = true;
        }

        // 2. Kiểm tra xác thực CAPTCHA nếu đang trong diện phòng vệ Cấp 1
        if (requireCaptcha)
        {
            var expectedAnswer = HttpContext.Session.GetString("CaptchaAnswer");
            if (string.IsNullOrWhiteSpace(model.CaptchaCode) || model.CaptchaCode.Trim() != expectedAnswer)
            {
                GenerateCaptcha();
                ViewBag.Error = "Mã xác thực bảo vệ (CAPTCHA) không chính xác! Vui lòng thử lại.";
                ViewBag.RequireCaptcha = true;
                model.RequireCaptcha = true;
                return View(model);
            }
        }

        // 3. Phòng thủ chống rà quét danh tính (Username Enumeration Defense)
        // Nếu không tìm thấy User: ghi nhận số lần thất bại, trả thông báo chung, tuyệt đối không tiết lộ tài khoản có tồn tại hay không!
        if (user == null)
        {
            sessionFails++;
            HttpContext.Session.SetInt32("FailedLoginCount", sessionFails);
            requireCaptcha = sessionFails >= 3;

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            PrepareCaptchaView(requireCaptcha);
            model.RequireCaptcha = requireCaptcha;
            return View(model);
        }

        // 4. Kiểm tra trạng thái khóa tài khoản (Brute-force Cấp 2 và Cấp 3)
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            if (user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1))
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa vĩnh viễn do nhập sai mật khẩu quá 10 lần. Vui lòng liên hệ bộ phận hỗ trợ hoặc sử dụng 'Quên mật khẩu' để khôi phục!";
            }
            else
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes);
                ViewBag.Error = $"Tài khoản tạm thời bị khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút hoặc dùng Quên mật khẩu.";
            }

            PrepareCaptchaView(true);
            model.RequireCaptcha = true;
            return View(model);
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Tài khoản của bạn đã bị ngừng kích hoạt. Vui lòng liên hệ quản trị viên cửa hàng!";
            PrepareCaptchaView(requireCaptcha);
            model.RequireCaptcha = requireCaptcha;
            return View(model);
        }

        // 5. Kiểm tra phân hệ: Chỉ tài khoản vai trò Customer mới được phép đăng nhập Cổng Khách Hàng.
        // Tài khoản nhân sự nội bộ (Admin, Manager, Staff, Cashier, Shipper) không được phép vào qua Storefront.
        var roles = await _userManager.GetRolesAsync(user);
        bool isCustomer = roles.Contains("Customer");
        bool isInternalOnly = roles.Any(r => r is "Admin" or "Manager" or "Staff" or "Cashier" or "Shipper") && !isCustomer;

        if (isInternalOnly)
        {
            // Trả thông báo chung, giấu kín đường dẫn Admin Area
            sessionFails++;
            HttpContext.Session.SetInt32("FailedLoginCount", sessionFails);
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            PrepareCaptchaView(sessionFails >= 3);
            model.RequireCaptcha = sessionFails >= 3;
            return View(model);
        }

        // 6. Xác thực mật khẩu
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password.Trim());
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
                ViewBag.Error = "Tài khoản của bạn đã bị khóa vĩnh viễn do nhập sai mật khẩu 10 lần liên tiếp. Vui lòng sử dụng 'Quên mật khẩu' để xác thực lại!";
                PrepareCaptchaView(true);
                model.RequireCaptcha = true;
                return View(model);
            }
            else if (failCount >= 5)
            {
                // Cấp 2: Khóa tạm thời 15 phút
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                ViewBag.Error = "Tài khoản tạm thời bị khóa 15 phút do nhập sai mật khẩu 5 lần. Vui lòng thử lại sau hoặc dùng Quên mật khẩu.";
                PrepareCaptchaView(true);
                model.RequireCaptcha = true;
                return View(model);
            }
            else if (failCount >= 3 || sessionFails >= 3)
            {
                // Cấp 1: Yêu cầu giải CAPTCHA
                GenerateCaptcha();
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác! Vui lòng nhập mã bảo vệ CAPTCHA bên dưới.";
                ViewBag.RequireCaptcha = true;
                model.RequireCaptcha = true;
                return View(model);
            }
            else
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                PrepareCaptchaView(false);
                model.RequireCaptcha = false;
                return View(model);
            }
        }

        // 7. Xác thực thành công: Reset bộ đếm lỗi & Đăng nhập
        await _userManager.ResetAccessFailedCountAsync(user);
        HttpContext.Session.Remove("FailedLoginCount");
        HttpContext.Session.Remove("CaptchaAnswer");
        HttpContext.Session.Remove("CaptchaQuestion");

        var result = await _signInManager.PasswordSignInAsync(user, model.Password.Trim(), isPersistent: model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ViewBag.Error = "Đăng nhập không thành công. Vui lòng thử lại!";
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Register
    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new CustomerRegisterViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Register (Chuẩn hóa Đăng ký, Drop-off Optimization & Validation Matrix)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CustomerRegisterViewModel model, string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            model.ReturnUrl = returnUrl;
        }
        ViewBag.ReturnUrl = model.ReturnUrl;

        // 1. Kiểm tra Họ và tên
        if (string.IsNullOrWhiteSpace(model.FullName) || model.FullName.Trim().Length < 2)
        {
            ViewBag.Error = "Vui lòng nhập họ và tên của bạn (tối thiểu 2 ký tự)!";
            return View(model);
        }

        // 2. Kiểm tra Số điện thoại theo chuẩn thị trường Việt Nam (10 số, đầu 03x-09x, loại trừ 02x)
        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            ViewBag.Error = "Vui lòng nhập số điện thoại di động!";
            return View(model);
        }

        var cleanedPhone = model.PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (!Regex.IsMatch(cleanedPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
        {
            ViewBag.Error = "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!";
            return View(model);
        }

        var e164Phone = NormalizePhoneNumber(cleanedPhone);

        // Kiểm tra tính duy nhất của Số điện thoại
        var phoneExists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == e164Phone || u.PhoneNumber == cleanedPhone);
        if (phoneExists)
        {
            ViewBag.Error = "Số điện thoại này đã được sử dụng trên hệ thống. Vui lòng đăng nhập hoặc dùng số khác!";
            return View(model);
        }

        // 3. Kiểm tra Email & Canonical Email Protection đối với Gmail
        string? canonicalEmail = null;
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var trimmedEmail = model.Email.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(trimmedEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                ViewBag.Error = "Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!";
                return View(model);
            }

            canonicalEmail = GetCanonicalEmail(trimmedEmail);

            var emailExists = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == trimmedEmail.ToUpper() || u.Email == trimmedEmail);
            if (emailExists)
            {
                ViewBag.Error = "Địa chỉ email này đã được liên kết với một tài khoản khác!";
                return View(model);
            }
        }

        // 4. Xử lý Tên đăng nhập (Username)
        string finalUsername;
        if (!string.IsNullOrWhiteSpace(model.Username))
        {
            var uName = model.Username.Trim();

            // Độ dài 6-30
            if (uName.Length < 6 || uName.Length > 30)
            {
                ViewBag.Error = "Tên đăng nhập phải có độ dài từ 6 đến 30 ký tự!";
                return View(model);
            }

            // Kiểm tra không được toàn số và đúng Regex
            if (Regex.IsMatch(uName, @"^[0-9]+$"))
            {
                ViewBag.Error = "Tên đăng nhập không được chỉ chứa toàn chữ số (để phân biệt với số điện thoại)!";
                return View(model);
            }

            if (!Regex.IsMatch(uName, @"^[a-zA-Z0-9]([._](?![._])|[a-zA-Z0-9]){4,28}[a-zA-Z0-9]$"))
            {
                ViewBag.Error = "Tên đăng nhập chỉ gồm chữ cái, chữ số, dấu '.' hoặc '_', không bắt đầu/kết thúc bằng ký tự đặc biệt!";
                return View(model);
            }

            // Kiểm tra Blacklist từ cấm hệ thống
            if (UsernameBlacklist.Contains(uName))
            {
                ViewBag.Error = $"Tên đăng nhập '{uName}' là từ khóa hệ thống dành riêng. Vui lòng chọn tên khác!";
                return View(model);
            }

            // Kiểm tra tính duy nhất
            if (await _userManager.FindByNameAsync(uName.ToLowerInvariant()) != null ||
                await _userManager.FindByNameAsync(uName) != null)
            {
                ViewBag.Error = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                return View(model);
            }

            finalUsername = uName.ToLowerInvariant();
        }
        else
        {
            // Tối ưu UX: Nếu khách không nhập Username, tự động lấy SĐT làm Username định danh
            finalUsername = e164Phone;
        }

        // 5. Kiểm tra Mật khẩu & Độ phức tạp (Complexity Rule: 3 trong 4 nhóm)
        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8 || model.Password.Length > 64)
        {
            ViewBag.Error = "Mật khẩu phải có độ dài từ 8 đến 64 ký tự!";
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập!";
            return View(model);
        }

        var pwd = model.Password;
        int complexityGroups = 0;
        if (pwd.Any(char.IsUpper)) complexityGroups++;
        if (pwd.Any(char.IsLower)) complexityGroups++;
        if (pwd.Any(char.IsDigit)) complexityGroups++;
        if (pwd.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) complexityGroups++;

        if (complexityGroups < 3)
        {
            ViewBag.Error = "Mật khẩu phải bao gồm ít nhất 3 trong 4 nhóm: chữ hoa (A-Z), chữ thường (a-z), chữ số (0-9) và ký tự đặc biệt (!@#$...)!";
            return View(model);
        }

        if (CommonPasswords.Contains(pwd) || pwd.Equals(finalUsername, StringComparison.OrdinalIgnoreCase) ||
            pwd.Equals(model.PhoneNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Error = "Mật khẩu quá đơn giản hoặc trùng với tên đăng nhập / số điện thoại. Vui lòng đặt mật khẩu bảo mật hơn!";
            return View(model);
        }

        // 6. Xử lý Địa chỉ (Tối ưu Drop-off Rate: Không ép nhập địa chỉ khi Đăng ký)
        var finalAddress = string.IsNullOrWhiteSpace(model.Address) ? "Chưa cập nhật địa chỉ" : model.Address.Trim();

        // 7. Khởi tạo ApplicationUser
        var user = new ApplicationUser
        {
            UserName = finalUsername,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant(),
            PhoneNumber = e164Phone,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password.Trim());
        if (!result.Succeeded)
        {
            ViewBag.Error = string.Join("; ", result.Errors.Select(e => e.Description));
            return View(model);
        }

        // Gán Role Customer
        await _userManager.AddToRoleAsync(user, "Customer");

        // 8. Khởi tạo Hồ sơ Khách hàng (Customer Profile)
        var customer = new Customer
        {
            UserId = user.Id,
            FullName = model.FullName.Trim(),
            PhoneNumber = e164Phone,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant(),
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

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // GET/POST: /Account/Logout
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // GET: /Account/CheckAvailability (API Kiểm tra trùng lặp thời gian thực cho màn hình Đăng ký)
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(string type, string value)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
        {
            return Json(new { available = false, message = "Dữ liệu không được để trống." });
        }

        var cleanType = type.Trim().ToLowerInvariant();
        var val = value.Trim();

        if (cleanType == "phone")
        {
            var cleanedPhone = val.Replace(" ", "").Replace(".", "").Replace("-", "");
            if (!Regex.IsMatch(cleanedPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
            {
                return Json(new { available = false, invalidFormat = true, message = "Số điện thoại không đúng định dạng (10 số đầu 03, 05, 07, 08, 09)!" });
            }

            var e164 = NormalizePhoneNumber(cleanedPhone);
            var exists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == e164 || u.PhoneNumber == cleanedPhone);
            if (exists)
            {
                return Json(new { available = false, exists = true, message = "Số điện thoại này đã được đăng ký tài khoản. Bạn có muốn Đăng nhập ngay không?" });
            }

            return Json(new { available = true, message = "Số điện thoại hợp lệ và khả dụng." });
        }

        if (cleanType == "username")
        {
            if (val.Length < 6 || val.Length > 30)
            {
                return Json(new { available = false, invalidFormat = true, message = "Tên đăng nhập phải từ 6 đến 30 ký tự!" });
            }

            if (Regex.IsMatch(val, @"^[0-9]+$"))
            {
                return Json(new { available = false, invalidFormat = true, message = "Tên đăng nhập không được chỉ chứa toàn chữ số!" });
            }

            if (!Regex.IsMatch(val, @"^[a-zA-Z0-9]([._](?![._])|[a-zA-Z0-9]){4,28}[a-zA-Z0-9]$"))
            {
                return Json(new { available = false, invalidFormat = true, message = "Tên đăng nhập không chứa ký tự đặc biệt hoặc dấu đầu/cuối!" });
            }

            if (UsernameBlacklist.Contains(val))
            {
                return Json(new { available = false, invalidFormat = true, message = $"Tên đăng nhập '{val}' là từ khóa hệ thống dành riêng!" });
            }

            var exists = await _userManager.FindByNameAsync(val.ToLowerInvariant()) != null ||
                         await _userManager.FindByNameAsync(val) != null;
            if (exists)
            {
                return Json(new { available = false, exists = true, message = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!" });
            }

            return Json(new { available = true, message = "Tên đăng nhập khả dụng." });
        }

        if (cleanType == "email")
        {
            var lowerEmail = val.ToLowerInvariant();
            if (!Regex.IsMatch(lowerEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                return Json(new { available = false, invalidFormat = true, message = "Địa chỉ email không đúng định dạng (VD: tenban@gmail.com)!" });
            }

            var canonical = GetCanonicalEmail(lowerEmail);

            // 1. Kiểm tra trực tiếp bằng SQL Index
            var existsInUsers = await _userManager.Users.AnyAsync(u =>
                u.NormalizedEmail == lowerEmail.ToUpper() ||
                u.Email == lowerEmail ||
                u.Email == canonical ||
                u.UserName == lowerEmail);

            var existsInCustomers = await _context.Customers.AnyAsync(c =>
                c.Email != null && (c.Email.ToLower() == lowerEmail || c.Email.ToLower() == canonical));

            // 2. Nếu là Gmail / Googlemail, kiểm tra Canonical nâng cao (chống trùng lặp dấu chấm và alias '+')
            if (!existsInUsers && !existsInCustomers && (canonical.EndsWith("@gmail.com") || canonical.EndsWith("@googlemail.com")))
            {
                var candidateUserEmails = await _userManager.Users
                    .Where(u => u.Email != null && (u.Email.EndsWith("@gmail.com") || u.Email.EndsWith("@googlemail.com")))
                    .Select(u => u.Email)
                    .ToListAsync();
                existsInUsers = candidateUserEmails.Any(e => !string.IsNullOrEmpty(e) && GetCanonicalEmail(e) == canonical);

                if (!existsInUsers)
                {
                    var candidateCustEmails = await _context.Customers
                        .Where(c => c.Email != null && (c.Email.EndsWith("@gmail.com") || c.Email.EndsWith("@googlemail.com")))
                        .Select(c => c.Email!)
                        .ToListAsync();
                    existsInCustomers = candidateCustEmails.Any(e => !string.IsNullOrEmpty(e) && GetCanonicalEmail(e) == canonical);
                }
            }

            if (existsInUsers || existsInCustomers)
            {
                return Json(new { available = false, exists = true, message = "Địa chỉ email này đã được liên kết với một tài khoản khác!" });
            }

            return Json(new { available = true, message = "Email khả dụng." });
        }

        return Json(new { available = false, message = "Loại kiểm tra không xác định." });
    }

    #region Helper Methods (Captcha, Phone Normalizer, Canonical Email)

    private void EnsureCaptchaGenerated()
    {
        var existingQuestion = HttpContext.Session.GetString("CaptchaQuestion");
        if (string.IsNullOrEmpty(existingQuestion))
        {
            GenerateCaptcha();
        }
        else
        {
            ViewBag.CaptchaQuestion = existingQuestion;
        }
    }

    private void PrepareCaptchaView(bool requireCaptcha)
    {
        ViewBag.RequireCaptcha = requireCaptcha;
        if (requireCaptcha)
        {
            EnsureCaptchaGenerated();
        }
    }

    private void GenerateCaptcha()
    {
        var rand = new Random();
        int a = rand.Next(3, 16);
        int b = rand.Next(2, 12);
        var question = $"{a} + {b} = ?";
        HttpContext.Session.SetString("CaptchaQuestion", question);
        HttpContext.Session.SetString("CaptchaAnswer", (a + b).ToString());
        ViewBag.CaptchaQuestion = question;
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var cleaned = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (cleaned.StartsWith("+84")) return cleaned;
        if (cleaned.StartsWith("84")) return "+" + cleaned;
        if (cleaned.StartsWith("0")) return "+84" + cleaned.Substring(1);
        return cleaned;
    }

    private static string GetCanonicalEmail(string email)
    {
        var trimmed = email.Trim().ToLowerInvariant();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex <= 0) return trimmed;

        var local = trimmed.Substring(0, atIndex);
        var domain = trimmed.Substring(atIndex + 1);

        if (domain == "gmail.com" || domain == "googlemail.com")
        {
            var plusIndex = local.IndexOf('+');
            if (plusIndex >= 0)
            {
                local = local.Substring(0, plusIndex);
            }
            local = local.Replace(".", "");
        }

        return $"{local}@{domain}";
    }

    #endregion
}
