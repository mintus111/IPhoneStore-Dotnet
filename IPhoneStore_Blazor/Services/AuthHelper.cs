using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Blazor.Data;
using IPhoneStore_Blazor.Models.Entities;

namespace IPhoneStore_Blazor.Services;

public record AvailabilityResult(bool Available, string Message, bool Exists = false, bool InvalidFormat = false);

public static class AuthHelper
{
    public static readonly HashSet<string> UsernameBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "root", "support", "moderator", "helpdesk",
        "system", "null", "undefined", "guest", "istore", "manager", "staff", "cashier", "shipper", "apple"
    };

    public static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "password", "qwertyuiop", "admin123", "iloveyou", "11111111", "00000000"
    };

    public static string NormalizePhoneNumber(string phone)
    {
        var cleaned = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (cleaned.StartsWith("+84")) return cleaned;
        if (cleaned.StartsWith("84")) return "+" + cleaned;
        if (cleaned.StartsWith("0")) return "+84" + cleaned.Substring(1);
        return cleaned;
    }

    public static string GetCanonicalEmail(string email)
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

    public static async Task<ApplicationUser?> FindUserByIdentifierAsync(UserManager<ApplicationUser> userManager, string rawIdentifier)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier)) return null;

        var input = rawIdentifier.Trim();

        // 1. Định dạng Email
        if (input.Contains('@'))
        {
            var normalizedEmail = input.ToLowerInvariant();
            return await userManager.FindByEmailAsync(normalizedEmail)
                   ?? await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpper() || u.Email == normalizedEmail);
        }

        // 2. Định dạng Số điện thoại Việt Nam
        if (Regex.IsMatch(input, @"^(0|\+84)[0-9]{9,10}$"))
        {
            var e164 = NormalizePhoneNumber(input);
            return await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == e164 || u.PhoneNumber == input);
        }

        // 3. Định dạng Username
        return await userManager.FindByNameAsync(input.ToLowerInvariant())
               ?? await userManager.FindByNameAsync(input);
    }

    public static (bool IsValid, string? ErrorMessage) ValidatePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return (false, "Vui lòng nhập số điện thoại di động!");

        var cleaned = phone.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (!Regex.IsMatch(cleaned, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
            return (false, "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!");

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Tên đăng nhập không được để trống!");

        var uName = username.Trim();
        if (uName.Length < 6 || uName.Length > 30)
            return (false, "Tên đăng nhập phải có độ dài từ 6 đến 30 ký tự!");

        if (Regex.IsMatch(uName, @"^[0-9]+$"))
            return (false, "Tên đăng nhập không được chỉ chứa toàn chữ số (để phân biệt với số điện thoại)!");

        if (!Regex.IsMatch(uName, @"^[a-zA-Z0-9]([._](?![._])|[a-zA-Z0-9]){4,28}[a-zA-Z0-9]$"))
            return (false, "Tên đăng nhập chỉ gồm chữ cái, chữ số, dấu '.' hoặc '_', không bắt đầu/kết thúc bằng ký tự đặc biệt!");

        if (UsernameBlacklist.Contains(uName))
            return (false, $"Tên đăng nhập '{uName}' là từ khóa hệ thống dành riêng. Vui lòng chọn tên khác!");

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Địa chỉ email không được để trống!");

        var trimmed = email.Trim().ToLowerInvariant();
        if (!Regex.IsMatch(trimmed, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            return (false, "Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!");

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidatePasswordComplexity(string password, string? username = null, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 64)
            return (false, "Mật khẩu phải có độ dài từ 8 đến 64 ký tự!");

        int groups = 0;
        if (password.Any(char.IsUpper)) groups++;
        if (password.Any(char.IsLower)) groups++;
        if (password.Any(char.IsDigit)) groups++;
        if (password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) groups++;

        if (groups < 3)
            return (false, "Mật khẩu phải bao gồm ít nhất 3 trong 4 nhóm: chữ hoa (A-Z), chữ thường (a-z), chữ số (0-9) và ký tự đặc biệt (!@#$...)!");

        if (CommonPasswords.Contains(password))
            return (false, "Mật khẩu quá đơn giản và phổ biến. Vui lòng đặt mật khẩu bảo mật hơn!");

        if (!string.IsNullOrWhiteSpace(username) && password.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "Mật khẩu không được trùng với tên đăng nhập!");

        if (!string.IsNullOrWhiteSpace(phone) && password.Equals(phone.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "Mật khẩu không được trùng với số điện thoại!");

        return (true, null);
    }

    public static (string Question, string Answer) GenerateMathCaptcha()
    {
        var rand = new Random();
        int a = rand.Next(3, 16);
        int b = rand.Next(2, 12);
        return ($"{a} + {b} = ?", (a + b).ToString());
    }

    public static async Task<AvailabilityResult> CheckAvailabilityAsync(
        string? type,
        string? value,
        UserManager<ApplicationUser> userManager,
        IPhoneStoreDbContext context)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
            return new AvailabilityResult(false, "Dữ liệu kiểm tra không được để trống.");

        var cleanType = type.Trim().ToLowerInvariant();
        var val = value.Trim();

        // 1. KIỂM TRA SỐ ĐIỆN THOẠI
        if (cleanType == "phone")
        {
            var (isValid, errMsg) = ValidatePhoneNumber(val);
            if (!isValid)
                return new AvailabilityResult(false, errMsg ?? "Số điện thoại không đúng định dạng!", InvalidFormat: true);

            var cleanedPhone = val.Replace(" ", "").Replace(".", "").Replace("-", "");
            var e164 = NormalizePhoneNumber(cleanedPhone);

            var existsInUsers = await userManager.Users.AnyAsync(u => u.PhoneNumber == e164 || u.PhoneNumber == cleanedPhone);
            var existsInCustomers = await context.Customers.AnyAsync(c => c.PhoneNumber == e164 || c.PhoneNumber == cleanedPhone);

            if (existsInUsers || existsInCustomers)
                return new AvailabilityResult(false, "Số điện thoại này đã được đăng ký tài khoản. Bạn có muốn Đăng nhập ngay không?", Exists: true);

            return new AvailabilityResult(true, "Số điện thoại hợp lệ và khả dụng.");
        }

        // 2. KIỂM TRA TÊN ĐĂNG NHẬP
        if (cleanType == "username")
        {
            var (isValid, errMsg) = ValidateUsername(val);
            if (!isValid)
                return new AvailabilityResult(false, errMsg ?? "Tên đăng nhập không hợp lệ!", InvalidFormat: true);

            var exists = await userManager.FindByNameAsync(val.ToLowerInvariant()) != null ||
                         await userManager.FindByNameAsync(val) != null;

            if (exists)
                return new AvailabilityResult(false, "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!", Exists: true);

            return new AvailabilityResult(true, "Tên đăng nhập khả dụng.");
        }

        // 3. KIỂM TRA EMAIL
        if (cleanType == "email")
        {
            var lowerEmail = val.ToLowerInvariant();
            var (isValid, errMsg) = ValidateEmail(lowerEmail);
            if (!isValid)
                return new AvailabilityResult(false, errMsg ?? "Địa chỉ email không đúng định dạng!", InvalidFormat: true);

            var canonical = GetCanonicalEmail(lowerEmail);

            var existsInUsers = await userManager.Users.AnyAsync(u =>
                u.NormalizedEmail == lowerEmail.ToUpper() ||
                u.Email == lowerEmail ||
                u.Email == canonical ||
                u.UserName == lowerEmail);

            var existsInCustomers = await context.Customers.AnyAsync(c =>
                c.Email != null && (c.Email.ToLower() == lowerEmail || c.Email.ToLower() == canonical));

            if (!existsInUsers && !existsInCustomers && (canonical.EndsWith("@gmail.com") || canonical.EndsWith("@googlemail.com")))
            {
                var candidateUserEmails = await userManager.Users
                    .Where(u => u.Email != null && (u.Email.EndsWith("@gmail.com") || u.Email.EndsWith("@googlemail.com")))
                    .Select(u => u.Email!)
                    .Take(500)
                    .ToListAsync();
                existsInUsers = candidateUserEmails.Any(e => !string.IsNullOrEmpty(e) && GetCanonicalEmail(e) == canonical);

                if (!existsInUsers)
                {
                    var candidateCustEmails = await context.Customers
                        .Where(c => c.Email != null && (c.Email.EndsWith("@gmail.com") || c.Email.EndsWith("@googlemail.com")))
                        .Select(c => c.Email!)
                        .Take(500)
                        .ToListAsync();
                    existsInCustomers = candidateCustEmails.Any(e => !string.IsNullOrEmpty(e) && GetCanonicalEmail(e) == canonical);
                }
            }

            if (existsInUsers || existsInCustomers)
                return new AvailabilityResult(false, "Địa chỉ email này đã được liên kết với một tài khoản khác!", Exists: true);

            return new AvailabilityResult(true, "Email khả dụng.");
        }

        return new AvailabilityResult(false, "Loại kiểm tra không xác định.");
    }
}
