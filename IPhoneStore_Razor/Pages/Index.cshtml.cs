using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages;

public class IndexModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly CartService _cartService;

    public IndexModel(IPhoneStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StorageFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    // Phân trang
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Lọc theo Dòng máy
        if (CategoryId.HasValue && CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        // Lọc theo dung lượng bộ nhớ
        if (!string.IsNullOrWhiteSpace(StorageFilter))
        {
            query = query.Where(p => p.Storage == StorageFilter);
        }

        // Tìm kiếm theo từ khóa
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                     p.ModelSeries.ToLower().Contains(kw) ||
                                     p.Color.ToLower().Contains(kw) ||
                                     (p.Specifications != null && p.Specifications.ToLower().Contains(kw)));
        }

        // Sắp xếp
        query = SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.Id)
        };

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Products = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Handler thêm iPhone vào giỏ hàng qua AJAX
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
        {
            return new JsonResult(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!" });
        }

        var result = _cartService.AddToCart(product, quantity);
        return new JsonResult(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetCount()
        });
    }
}
