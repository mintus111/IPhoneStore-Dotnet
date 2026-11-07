using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ProductsModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public ProductsModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StorageFilter { get; set; }

    // Phân trang
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // Model binding cho Form Thêm / Sửa
    [BindProperty]
    public Product InputProduct { get; set; } = new();

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                     p.ModelSeries.ToLower().Contains(kw) ||
                                     p.Color.ToLower().Contains(kw));
        }

        if (CategoryId.HasValue && CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(StorageFilter))
        {
            query = query.Where(p => p.Storage == StorageFilter);
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Products = await query
            .OrderByDescending(p => p.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Thêm mới hoặc Cập nhật iPhone
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InputProduct.Name) || InputProduct.Price <= 0 || InputProduct.CategoryId <= 0)
        {
            AlertMessage = "Vui lòng nhập đầy đủ Tên, Dòng máy và Giá bán hợp lệ!";
            AlertType = "danger";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(InputProduct.Sku))
        {
            AlertMessage = "Mã SKU định danh của iPhone không được để trống!";
            AlertType = "danger";
            return RedirectToPage();
        }

        var skuTrimmed = InputProduct.Sku.Trim();
        bool isSkuDuplicate = await _context.Products.AnyAsync(p =>
            p.Id != InputProduct.Id &&
            p.Sku != null &&
            p.Sku.ToLower() == skuTrimmed.ToLower());

        if (isSkuDuplicate)
        {
            AlertMessage = $"Mã SKU '{skuTrimmed}' đã tồn tại trong hệ thống! Vui lòng nhập mã SKU khác không trùng lặp.";
            AlertType = "danger";
            return RedirectToPage();
        }
        InputProduct.Sku = skuTrimmed;

        if (InputProduct.Id == 0)
        {
            InputProduct.IsActive = true;
            _context.Products.Add(InputProduct);
            AlertMessage = $"Đã thêm thành công iPhone: {InputProduct.Name}";
            AlertType = "success";
        }
        else
        {
            var existing = await _context.Products.FindAsync(InputProduct.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.CategoryId = InputProduct.CategoryId;
            existing.Sku = skuTrimmed;
            existing.Name = InputProduct.Name;
            existing.ModelSeries = InputProduct.ModelSeries;
            existing.Color = InputProduct.Color;
            existing.Storage = InputProduct.Storage;
            existing.Specifications = InputProduct.Specifications;
            existing.Price = InputProduct.Price;
            existing.Quantity = InputProduct.Quantity;
            existing.WarrantyMonths = InputProduct.WarrantyMonths;
            existing.ImageUrl = InputProduct.ImageUrl;
            existing.Description = InputProduct.Description;
            existing.IsActive = InputProduct.IsActive;

            AlertMessage = $"Cập nhật thành công thông tin iPhone: {existing.Name}";
            AlertType = "success";
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    /// <summary>
    /// Xóa hoặc Đổi trạng thái kinh doanh của iPhone
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            // Kiểm tra xem iPhone đã có trong đơn hàng nào chưa
            var hasOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == id);
            if (hasOrders)
            {
                // Nếu đã có đơn hàng thì chuyển trạng thái ngừng kinh doanh
                product.IsActive = false;
                AlertMessage = $"iPhone {product.Name} đã có đơn hàng lịch sử nên được chuyển sang trạng thái: Ngừng kinh doanh.";
                AlertType = "warning";
            }
            else
            {
                _context.Products.Remove(product);
                AlertMessage = $"Đã xóa thành công iPhone: {product.Name}";
                AlertType = "success";
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
