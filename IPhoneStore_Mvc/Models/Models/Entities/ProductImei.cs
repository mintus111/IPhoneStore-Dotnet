using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("ProductImeis")]
public class ProductImei
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [StringLength(20)]
    public string Imei { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SerialNumber { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "InStock"; // InStock, Reserved, Sold, Defective, Warranty

    public int? OrderId { get; set; }

    public DateTime ImportDate { get; set; } = DateTime.Now;

    public DateTime? ExportDate { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
}
