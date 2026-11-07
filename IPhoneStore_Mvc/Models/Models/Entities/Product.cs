using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Mvc.Models.Entities;

[Table("Products")]
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn dòng iPhone")]
    [Display(Name = "Dòng iPhone")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Tên iPhone không được để trống")]
    [StringLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dòng máy không được để trống (VD: iPhone 16 Pro Max)")]
    [StringLength(100)]
    [Display(Name = "Dòng máy")]
    public string ModelSeries { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Mã SKU")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Màu sắc không được để trống")]
    [StringLength(50)]
    [Display(Name = "Màu sắc")]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dung lượng không được để trống (128GB, 256GB, 512GB, 1TB)")]
    [StringLength(20)]
    [Display(Name = "Dung lượng bộ nhớ")]
    public string Storage { get; set; } = "128GB";

    [Display(Name = "Thông số kỹ thuật")]
    public string? Specifications { get; set; }

    [Range(0, 200000000, ErrorMessage = "Giá niêm yết không được là số âm (tối đa 200.000.000 VNĐ)")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá niêm yết (VNĐ)")]
    public decimal? ListedPrice { get; set; }

    [Required(ErrorMessage = "Giá bán không được để trống")]
    [Range(1000, 200000000, ErrorMessage = "Giá bán phải lớn hơn 0 và tối đa 200.000.000 VNĐ")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Giá bán (VNĐ)")]
    public decimal Price { get; set; }

    [Range(0, 10000, ErrorMessage = "Số lượng không hợp lệ")]
    [Display(Name = "Số lượng tồn kho")]
    public int Quantity { get; set; } = 0;

    [Display(Name = "Bảo hành (tháng)")]
    [Range(0, 60)]
    public int WarrantyMonths { get; set; } = 12;

    [StringLength(500)]
    [Display(Name = "Đường dẫn hình ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Helpers
    [NotMapped]
    public bool IsInStock => Quantity > 0;

    // Navigation
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImei> ProductImeis { get; set; } = new List<ProductImei>();

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
