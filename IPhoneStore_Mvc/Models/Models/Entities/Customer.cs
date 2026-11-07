using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("Customers")]
public class Customer
{
    [Key]
    public int Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
    [StringLength(255)]
    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSpent { get; set; } = 0;

    public int RewardPoints { get; set; } = 0;

    [StringLength(50)]
    public string MembershipTier { get; set; } = "Member"; // Member, Silver, Gold, VIP

    // Navigation
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
