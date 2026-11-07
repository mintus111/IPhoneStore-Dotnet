using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("WarrantyClaims")]
public class WarrantyClaim
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string? ClaimCode { get; set; }

    [Required]
    public int WarrantyId { get; set; }

    public DateTime ClaimDate { get; set; } = DateTime.Now;

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    public string? TechnicianNotes { get; set; }

    [StringLength(100)]
    public string? ActionTaken { get; set; } // Đổi máy mới (Exchange), Thay màn hình, Thay pin, Sửa chữa

    [StringLength(20)]
    public string? OldImei { get; set; }

    [StringLength(20)]
    public string? NewImei { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Tiếp nhận"; // Tiếp nhận, Chờ kiểm tra, Gửi Apple, Hoàn tất, Từ chối

    public DateTime? ResolvedDate { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation
    [ForeignKey("WarrantyId")]
    public virtual Warranty? Warranty { get; set; }
}
