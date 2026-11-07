using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("Orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string? OrderCode { get; set; }

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0;

    [Required]
    [StringLength(50)]
    public string OrderStatus { get; set; } = "Pending"; // Pending, Confirmed, Shipping, Completed, Cancelled

    [Required]
    [StringLength(50)]
    public string PaymentStatus { get; set; } = "Chưa thanh toán"; // Chưa thanh toán, Đã thanh toán, Chờ hoàn tiền, Đã hoàn tiền

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer, ApplePay

    [StringLength(100)]
    public string? ShippingUnit { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
    [StringLength(255)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImei> ProductImeis { get; set; } = new List<ProductImei>();
}
