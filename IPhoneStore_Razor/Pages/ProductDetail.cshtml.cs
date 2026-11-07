using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages;

public class ProductDetailModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly CartService _cartService;

    public ProductDetailModel(IPhoneStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public Product? Product { get; set; }
    public List<Product> Variants { get; set; } = new();
    public List<Product> RelatedProducts { get; set; } = new();

    [BindProperty]
    public int Quantity { get; set; } = 1;

    [TempData]
    public string? AlertMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (Product == null)
        {
            return NotFound();
        }

        // Đồng bộ số lượng tồn kho theo số lượng IMEI thực tế InStock trong kho
        var inStockImeis = await _context.ProductImeis
            .CountAsync(pi => pi.ProductId == Product.Id && pi.Status == "InStock");
        Product.Quantity = inStockImeis;

        // Lấy tất cả biến thể cùng ModelSeries
        Variants = await _context.Products
            .Where(p => p.CategoryId == Product.CategoryId && p.ModelSeries == Product.ModelSeries && p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();

        foreach (var v in Variants)
        {
            v.Quantity = await _context.ProductImeis.CountAsync(pi => pi.ProductId == v.Id && pi.Status == "InStock");
        }

        // Lấy sản phẩm cùng dòng máy khác
        RelatedProducts = await _context.Products
            .Where(p => p.CategoryId == Product.CategoryId && p.ModelSeries != Product.ModelSeries && p.IsActive)
            .Take(4)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null || !product.IsActive)
        {
            return NotFound();
        }

        var inStockImeis = await _context.ProductImeis
            .CountAsync(pi => pi.ProductId == product.Id && pi.Status == "InStock");
        product.Quantity = inStockImeis;

        if (inStockImeis <= 0)
        {
            AlertMessage = "iPhone này hiện đã hết hàng trong kho (hết mã IMEI sẵn bán)!";
            return RedirectToPage(new { id });
        }

        var result = _cartService.AddToCart(product, Quantity);
        AlertMessage = result.Message;
        return RedirectToPage(new { id });
    }
}
