using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Blazor.Models.Entities;

[Table("Categories")]
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên dòng iPhone không được để trống")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
