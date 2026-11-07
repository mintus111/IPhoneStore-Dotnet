using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace IPhoneStore_Mvc.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ngày tạo tài khoản")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation: 1 User có 1 hồ sơ Customer (hoặc null nếu là Admin thuần)
    public virtual Customer? Customer { get; set; }
}
