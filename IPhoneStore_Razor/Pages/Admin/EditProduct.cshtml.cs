using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditProductModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly IWebHostEnvironment _env;

    public EditProductModel(IPhoneStoreDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

        if (!id.HasValue || id.Value == 0)
        {
            Product = new Product
            {
                WarrantyMonths = 12,
                IsActive = true,
                Storage = "128GB"
            };
            return Page();
        }

        var prod = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImeis)
            .FirstOrDefaultAsync(p => p.Id == id.Value);

        if (prod == null) return NotFound();

        Product = prod;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? imageFile)
    {
        Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

        if (string.IsNullOrWhiteSpace(Product.Name))
        {
            ErrorMessage = "Tên iPhone không được để trống!";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Product.Sku))
        {
            ErrorMessage = "Mã SKU định danh của iPhone không được để trống!";
            return Page();
        }

        var skuTrimmed = Product.Sku.Trim();
        bool isSkuDuplicate = await _context.Products.AnyAsync(p =>
            p.Id != Product.Id &&
            p.Sku != null &&
            p.Sku.ToLower() == skuTrimmed.ToLower());

        if (isSkuDuplicate)
        {
            ErrorMessage = $"Mã SKU '{skuTrimmed}' đã tồn tại trên một sản phẩm khác trong hệ thống! Vui lòng nhập mã SKU khác.";
            return Page();
        }
        Product.Sku = skuTrimmed;

        if (Product.ListedPrice.HasValue && Product.Price > Product.ListedPrice.Value)
        {
            ErrorMessage = "Giá bán không thể cao hơn Giá niêm yết chính hãng Apple!";
            return Page();
        }

        if (imageFile != null && imageFile.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
            var uploadDir = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            Product.ImageUrl = $"/images/products/{fileName}";
        }

        if (Product.Id == 0)
        {
            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "CREATE_PRODUCT",
                EntityName = "Products",
                RecordId = Product.Id.ToString(),
                NewValues = $"Tạo sản phẩm mới SKU={Product.Sku}: {Product.Name}",
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            SuccessMessage = $"Đã thêm thành công sản phẩm {Product.Name}!";
        }
        else
        {
            var existing = await _context.Products.FindAsync(Product.Id);
            if (existing == null) return NotFound();

            var oldSummary = $"Name={existing.Name}, Price={existing.Price:N0}, Qty={existing.Quantity}, Sku={existing.Sku}";

            existing.Name = Product.Name.Trim();
            existing.Sku = Product.Sku?.Trim();
            existing.ModelSeries = Product.ModelSeries;
            existing.CategoryId = Product.CategoryId;
            existing.Price = Product.Price;
            existing.ListedPrice = Product.ListedPrice;
            existing.Storage = Product.Storage;
            existing.Color = Product.Color;
            existing.Specifications = Product.Specifications;
            existing.Description = Product.Description;
            if (!string.IsNullOrEmpty(Product.ImageUrl)) existing.ImageUrl = Product.ImageUrl;
            existing.Quantity = Product.Quantity;
            existing.WarrantyMonths = Product.WarrantyMonths;
            existing.IsActive = Product.IsActive;

            var newSummary = $"Name={existing.Name}, Price={existing.Price:N0}, Qty={existing.Quantity}, Sku={existing.Sku}";

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "EDIT_PRODUCT",
                EntityName = "Products",
                RecordId = existing.Id.ToString(),
                OldValues = oldSummary,
                NewValues = newSummary,
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            SuccessMessage = $"Đã cập nhật thông tin sản phẩm {Product.Name}!";
        }

        return RedirectToPage("/Admin/Products");
    }
}
