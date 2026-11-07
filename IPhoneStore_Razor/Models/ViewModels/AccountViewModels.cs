using System.ComponentModel.DataAnnotations;

namespace IPhoneStore_Razor.Models.ViewModels;

public class CustomerLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập, email hoặc số điện thoại!")]
    [Display(Name = "Tên đăng nhập / Email / Số điện thoại")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ trạng thái đăng nhập (30 ngày)")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }

    // Dành cho cơ chế phòng vệ Brute-force Cấp 1
    public bool RequireCaptcha { get; set; } = false;

    [Display(Name = "Mã xác thực bảo vệ")]
    public string? CaptchaCode { get; set; }
}

public class CustomerRegisterViewModel
{
    [Display(Name = "Tên đăng nhập (Tùy chọn)")]
    [StringLength(30, MinimumLength = 6, ErrorMessage = "Tên đăng nhập phải từ 6 đến 30 ký tự!")]
    [RegularExpression(@"^(?![0-9]+$)[a-zA-Z0-9]([._](?![._])|[a-zA-Z0-9]){4,28}[a-zA-Z0-9]$", 
        ErrorMessage = "Tên đăng nhập từ 6-30 ký tự, không chứa khoảng trắng/ký tự đặc biệt và không được toàn chữ số!")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống!")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 100 ký tự!")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống!")]
    [RegularExpression(@"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$", 
        ErrorMessage = "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!")]
    [Display(Name = "Số điện thoại di động")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng chuẩn!")]
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự!")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống!")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 64 ký tự!")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu!")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không trùng khớp!")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ giao hàng (Tùy chọn)")]
    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự!")]
    public string? Address { get; set; }

    public string? ReturnUrl { get; set; }
}
