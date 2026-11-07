using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace IPhoneStore_Blazor.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [Display(Name = "Trạng thái hoạt động")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ngày tạo tài khoản")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Customer? Customer { get; set; }
}
