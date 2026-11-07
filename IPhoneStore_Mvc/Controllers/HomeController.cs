using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Services;

namespace IPhoneStore_Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IPhoneStoreDbContext _context;
    private readonly CartService _cartService;

    public HomeController(IPhoneStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(
        int? categoryId,
        string? searchKeyword,
        string? storageFilter,
        string? sortBy,
        int currentPage = 1)
    {
        const int pageSize = 8;

        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(storageFilter))
            query = query.Where(p => p.Storage == storageFilter);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                     p.ModelSeries.ToLower().Contains(kw) ||
                                     p.Color.ToLower().Contains(kw));
        }

        query = sortBy switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name"       => query.OrderBy(p => p.Name),
            _            => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var products = await query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories    = categories;
        ViewBag.CategoryId    = categoryId;
        ViewBag.SearchKeyword = searchKeyword;
        ViewBag.StorageFilter = storageFilter;
        ViewBag.SortBy        = sortBy;
        ViewBag.CurrentPage   = currentPage;
        ViewBag.TotalPages    = totalPages;
        ViewBag.TotalCount    = totalCount;

        return View(products);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null) return NotFound();

        // Đồng bộ số lượng tồn kho theo số lượng IMEI thực tế còn sẵn sàng trong kho (InStock)
        var inStockImeis = await _context.ProductImeis
            .CountAsync(pi => pi.ProductId == product.Id && pi.Status == "InStock");
        product.Quantity = inStockImeis;

        // Lấy tất cả biến thể cùng ModelSeries (hoặc cùng Category) để hiển thị chọn Dung lượng & Màu sắc
        var variants = await _context.Products
            .Where(p => p.CategoryId == product.CategoryId && p.ModelSeries == product.ModelSeries && p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();

        foreach (var v in variants)
        {
            v.Quantity = await _context.ProductImeis.CountAsync(pi => pi.ProductId == v.Id && pi.Status == "InStock");
        }

        var related = await _context.Products
            .Where(p => p.CategoryId == product.CategoryId && p.ModelSeries != product.ModelSeries && p.IsActive)
            .Take(4)
            .ToListAsync();

        ViewBag.Variants = variants;
        ViewBag.RelatedProducts = related;
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
            return Json(new { success = false, message = "Sản phẩm không tồn tại!" });

        // Đồng bộ và kiểm tra số lượng IMEI InStock thực tế
        var inStockImeis = await _context.ProductImeis
            .CountAsync(pi => pi.ProductId == product.Id && pi.Status == "InStock");
        product.Quantity = inStockImeis;

        if (inStockImeis <= 0)
        {
            return Json(new { success = false, message = "iPhone này hiện đã hết hàng trong kho (hết mã IMEI sẵn bán)!" });
        }

        var result = _cartService.AddToCart(product, quantity);
        return Json(new { success = result.Success, message = result.Message, cartCount = _cartService.GetCount() });
    }

    public IActionResult Privacy() => View();
    public IActionResult Error() => View();
}
