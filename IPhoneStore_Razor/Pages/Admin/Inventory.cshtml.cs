using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff,Cashier,Shipper")]
public class InventoryModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public InventoryModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public InventoryViewModel Vm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? skuFilter, string? statusFilter, string? searchKeyword, int currentPage = 1)
    {
        const int pageSize = 10;
        var allImeis = await _context.ProductImeis.ToListAsync();

        var query = _context.ProductImeis
            .Include(i => i.Product)
            .ThenInclude(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(skuFilter))
            query = query.Where(i => i.Product.Sku == skuFilter);

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(i => i.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(i => i.Imei.ToLower().Contains(kw) ||
                                     (i.Product != null && i.Product.Name.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var imeis = await query.OrderByDescending(i => i.ImportDate)
            .Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

        var lowStock = await _context.Products
            .Where(p => p.Quantity <= 5 && p.IsActive)
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        var allProducts = await _context.Products.OrderBy(p => p.Name).ToListAsync();

        Vm = new InventoryViewModel
        {
            InStockCount = allImeis.Count(i => i.Status == "InStock"),
            ReservedCount = allImeis.Count(i => i.Status == "Reserved"),
            SoldCount = allImeis.Count(i => i.Status == "Sold"),
            DefectiveCount = allImeis.Count(i => i.Status == "Defective"),
            Imeis = imeis,
            LowStockProducts = lowStock,
            AllProducts = allProducts,
            SkuFilter = skuFilter,
            StatusFilter = statusFilter,
            SearchKeyword = searchKeyword,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateImeiStatusAsync(int id, string status)
    {
        var imei = await _context.ProductImeis.FindAsync(id);
        if (imei != null)
        {
            var old = imei.Status;
            imei.Status = status;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "UPDATE_IMEI_STATUS",
                EntityName = "ProductImeis",
                RecordId = id.ToString(),
                OldValues = $"Status={old}",
                NewValues = $"Status={status}",
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            SuccessMessage = $"Đã cập nhật trạng thái IMEI {imei.Imei} thành {status}!";
        }
        return RedirectToPage();
    }
}
