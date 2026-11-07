using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("Warranties")]
public class Warranty
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ProductId { get; set; }

    public int? OrderDetailId { get; set; }

    [Required]
    [StringLength(20)]
    public string Imei { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [StringLength(20)]
    public string? OriginalImei { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(50)]
    public string WarrantyType { get; set; } = "Chính hãng Apple 12 tháng";

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Active"; // Active, Expired, Voided

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("OrderDetailId")]
    public virtual OrderDetail? OrderDetail { get; set; }

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
}
