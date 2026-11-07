using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(IPhoneStoreDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Doanh thu
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }

    // Đơn hàng
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int ShippingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }

    // Sản phẩm & Tồn kho
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public List<Product> LowStockProducts { get; set; } = new();
    public List<Product> AllActiveProducts { get; set; } = new();

    // Thống kê IMEI
    public int TotalImeis { get; set; }
    public int InStockImeis { get; set; }
    public int ReservedImeis { get; set; }
    public int SoldImeis { get; set; }
    public int DefectiveImeis { get; set; }

    public int ActiveWarranties { get; set; }

    // Top sản phẩm bán chạy
    public List<BestSellerItemRazor> TopSellingProducts { get; set; } = new();

    // Đơn hàng gần đây
    public List<Order> RecentOrders { get; set; } = new();

    // Nhật ký kiểm toán
    public List<AuditLog> RecentAuditLogs { get; set; } = new();

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDashboardMetricsAsync();
    }

    private async Task LoadDashboardMetricsAsync()
    {
        var now = DateTime.Now;
        var startOfToday = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Doanh thu từ các đơn Completed
        var completedOrders = await _context.Orders
            .Where(o => o.OrderStatus == "Completed")
            .ToListAsync();

        TotalRevenue = completedOrders.Sum(o => o.TotalAmount);
        TodayRevenue = completedOrders.Where(o => o.OrderDate >= startOfToday).Sum(o => o.TotalAmount);
        MonthRevenue = completedOrders.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.TotalAmount);

        // Đơn hàng
        var allOrders = await _context.Orders.ToListAsync();
        TotalOrders = allOrders.Count;
        PendingOrders = allOrders.Count(o => o.OrderStatus == "Pending");
        ConfirmedOrders = allOrders.Count(o => o.OrderStatus == "Confirmed");
        ShippingOrders = allOrders.Count(o => o.OrderStatus == "Shipping");
        CompletedOrders = completedOrders.Count;
        CancelledOrders = allOrders.Count(o => o.OrderStatus == "Cancelled");

        // Sản phẩm & Tồn kho
        var allProds = await _context.Products.Include(p => p.Category).ToListAsync();
        TotalProducts = allProds.Count(p => p.IsActive);
        LowStockProducts = allProds.Where(p => p.Quantity <= 5 && p.IsActive).OrderBy(p => p.Quantity).ToList();
        LowStockCount = LowStockProducts.Count;
        AllActiveProducts = allProds.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();

        // IMEI stats
        var imeis = await _context.ProductImeis.ToListAsync();
        TotalImeis = imeis.Count;
        InStockImeis = imeis.Count(i => i.Status == "InStock");
        ReservedImeis = imeis.Count(i => i.Status == "Reserved");
        SoldImeis = imeis.Count(i => i.Status == "Sold");
        DefectiveImeis = imeis.Count(i => i.Status == "Defective");

        ActiveWarranties = await _context.Warranties.CountAsync(w => w.Status == "Active" && w.EndDate >= now);

        // Top sản phẩm bán chạy nhất
        TopSellingProducts = await _context.OrderDetails
            .Include(od => od.Product)
            .Include(od => od.Order)
            .Where(od => od.Order!.OrderStatus == "Completed" || od.Order.OrderStatus == "Shipping")
            .GroupBy(od => new { od.ProductId, od.Product!.Name, od.Product.ModelSeries, od.Product.Storage, od.Product.Color, od.Product.Price, od.Product.ImageUrl })
            .Select(g => new BestSellerItemRazor
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                ModelSeries = g.Key.ModelSeries,
                Storage = g.Key.Storage,
                Color = g.Key.Color,
                Price = g.Key.Price,
                ImageUrl = g.Key.ImageUrl,
                UnitsSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(5)
            .ToListAsync();

        // Đơn hàng gần đây
        RecentOrders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(6)
            .ToListAsync();

        // Nhật ký kiểm toán
        RecentAuditLogs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(6)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBatchImportImeisAsync(int productId, string imeisText)
    {
        if (productId <= 0 || string.IsNullOrWhiteSpace(imeisText))
        {
            AlertMessage = "Vui lòng chọn iPhone và nhập danh sách mã IMEI!";
            AlertType = "danger";
            return RedirectToPage();
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            AlertMessage = "Sản phẩm không tồn tại!";
            AlertType = "danger";
            return RedirectToPage();
        }

        var lines = imeisText.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (lines.Count == 0)
        {
            AlertMessage = "Không tìm thấy mã IMEI hợp lệ trong dữ liệu nhập!";
            AlertType = "danger";
            return RedirectToPage();
        }

        var existingImeis = await _context.ProductImeis
            .Where(i => lines.Contains(i.Imei))
            .Select(i => i.Imei)
            .ToListAsync();

        var validImeis = lines.Except(existingImeis).ToList();

        if (validImeis.Count == 0)
        {
            AlertMessage = $"Tất cả {lines.Count} mã IMEI đã tồn tại trong hệ thống!";
            AlertType = "danger";
            return RedirectToPage();
        }

        var newEntries = validImeis.Select(imei => new ProductImei
        {
            ProductId = productId,
            Imei = imei,
            SerialNumber = "S" + imei.Substring(Math.Max(0, imei.Length - 8)),
            Status = "InStock",
            ImportDate = DateTime.Now
        }).ToList();

        await _context.ProductImeis.AddRangeAsync(newEntries);
        product.Quantity += validImeis.Count;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "Admin",
            Action = "IMPORT_IMEI",
            EntityName = "ProductImeis",
            RecordId = productId.ToString(),
            NewValues = $"Đã nhập lô {validImeis.Count} IMEI cho máy {product.Name}",
            Timestamp = DateTime.Now,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync();

        var dupMsg = existingImeis.Count > 0 ? $" (Đã bỏ qua {existingImeis.Count} mã trùng)" : "";
        AlertMessage = $"Đã nhập thành công {validImeis.Count} mã IMEI vào kho cho dòng {product.Name}!{dupMsg}";
        AlertType = "success";

        return RedirectToPage();
    }

    public class BestSellerItemRazor
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ModelSeries { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
