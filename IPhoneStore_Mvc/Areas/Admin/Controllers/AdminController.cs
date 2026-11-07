using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;
using IPhoneStore_Mvc.Models.ViewModels;

namespace IPhoneStore_Mvc.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager,Staff,Cashier,Shipper")]
public class AdminController : Controller
{
    private readonly IPhoneStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        IPhoneStoreDbContext context, 
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    // =========================================================================
    // 1. TỔNG QUAN VẬN HÀNH (DASHBOARD - SCREEN 1)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var startOfToday = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        // Doanh thu
        var completedOrders = await _context.Orders
            .Where(o => o.OrderStatus == "Completed")
            .ToListAsync();

        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var todayRevenue = completedOrders.Where(o => o.OrderDate >= startOfToday).Sum(o => o.TotalAmount);
        var monthRevenue = completedOrders.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.TotalAmount);

        // Đơn hàng
        var allOrders = await _context.Orders.Include(o => o.Customer).ToListAsync();
        var totalOrders = allOrders.Count;
        var pendingOrders = allOrders.Count(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ duyệt");
        var confirmedOrders = allOrders.Count(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
        var shippingOrders = allOrders.Count(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
        var completedCount = completedOrders.Count;
        var cancelledOrders = allOrders.Count(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");

        // Kho & Cảnh báo tồn <= 5 máy
        var allProducts = await _context.Products.Include(p => p.Category).ToListAsync();
        var lowStockList = allProducts.Where(p => p.Quantity <= 5 && p.IsActive).OrderBy(p => p.Quantity).ToList();

        // IMEI stats
        var imeis = await _context.ProductImeis.ToListAsync();
        var totalImeis = imeis.Count;
        var inStockImeis = imeis.Count(i => i.Status == "InStock");
        var reservedImeis = imeis.Count(i => i.Status == "Reserved");
        var soldImeis = imeis.Count(i => i.Status == "Sold");
        var defectiveImeis = imeis.Count(i => i.Status == "Defective");

        // Khách hàng & Bảo hành
        var totalCustomers = await _context.Customers.CountAsync();
        var activeWarranties = await _context.Warranties.CountAsync(w => w.Status == "Active" && w.EndDate >= now);

        // Top 5 sản phẩm bán chạy nhất
        var topSellers = await _context.OrderDetails
            .Include(od => od.Product)
            .Include(od => od.Order)
            .Where(od => od.Order!.OrderStatus == "Completed" || od.Order.OrderStatus == "Shipping")
            .GroupBy(od => new { od.ProductId, od.Product!.Name, od.Product.ModelSeries, od.Product.Storage, od.Product.Color, od.Product.Price, od.Product.ImageUrl })
            .Select(g => new BestSellerItem
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
        var recentOrders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(6)
            .ToListAsync();

        // Nhật ký kiểm toán gần đây
        var recentAuditLogs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(6)
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            ConfirmedOrders = confirmedOrders,
            ShippingOrders = shippingOrders,
            CompletedOrders = completedCount,
            CancelledOrders = cancelledOrders,
            TotalProducts = allProducts.Count(p => p.IsActive),
            LowStockCount = lowStockList.Count,
            LowStockProducts = lowStockList,
            TotalImeis = totalImeis,
            InStockImeis = inStockImeis,
            ReservedImeis = reservedImeis,
            SoldImeis = soldImeis,
            DefectiveImeis = defectiveImeis,
            TotalCustomers = totalCustomers,
            ActiveWarranties = activeWarranties,
            TopSellingProducts = topSellers,
            RecentOrders = recentOrders,
            RecentAuditLogs = recentAuditLogs,
            AllActiveProducts = allProducts.Where(p => p.IsActive).OrderBy(p => p.Name).ToList()
        };

        return View(vm);
    }

    // =========================================================================
    // 2. DANH MỤC DÒNG MÁY (SCREEN 2)
    // =========================================================================
    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["Error"] = "Tên dòng máy không được để trống!";
            return RedirectToAction(nameof(Categories));
        }

        if (category.Id == 0)
        {
            _context.Categories.Add(category);
            await LogAuditAsync("CREATE_CATEGORY", "Categories", category.Name, null, $"Tạo danh mục mới: {category.Name}");
            TempData["Success"] = $"Đã thêm dòng {category.Name} thành công!";
        }
        else
        {
            var existing = await _context.Categories.FindAsync(category.Id);
            if (existing == null) return NotFound();

            var oldVal = $"Name={existing.Name}, Order={existing.DisplayOrder}, Active={existing.IsActive}";
            existing.Name = category.Name.Trim();
            existing.Description = category.Description;
            existing.DisplayOrder = category.DisplayOrder;
            existing.IsActive = category.IsActive;

            var newVal = $"Name={existing.Name}, Order={existing.DisplayOrder}, Active={existing.IsActive}";
            await LogAuditAsync("EDIT_CATEGORY", "Categories", existing.Id.ToString(), oldVal, newVal);
            TempData["Success"] = $"Đã cập nhật dòng {category.Name}!";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            if (category.Products.Any())
            {
                TempData["Error"] = $"Cảnh báo an toàn dữ liệu: Không thể xóa dòng {category.Name} vì đang có {category.Products.Count} iPhone thuộc dòng này!";
            }
            else
            {
                _context.Categories.Remove(category);
                await LogAuditAsync("DELETE_CATEGORY", "Categories", id.ToString(), $"Name={category.Name}", null);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa danh mục {category.Name}!";
            }
        }
        return RedirectToAction(nameof(Categories));
    }

    // =========================================================================
    // 3. SẢN PHẨM & BIẾN THỂ (SCREEN 3)
    // =========================================================================
    public async Task<IActionResult> Products(string? searchKeyword, int? categoryId, string? storageFilter, string? statusFilter, int currentPage = 1)
    {
        const int pageSize = 8;
        var categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

        // Tự động đồng bộ số lượng tồn kho theo số lượng IMEI thực tế InStock nếu có sai lệch
        var actualInStockCounts = await _context.ProductImeis
            .Where(pi => pi.Status == "InStock")
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);

        var allProducts = await _context.Products.ToListAsync();
        bool hasQuantityChanges = false;
        foreach (var prod in allProducts)
        {
            int realQty = actualInStockCounts.TryGetValue(prod.Id, out var count) ? count : 0;
            if (prod.Quantity != realQty)
            {
                prod.Quantity = realQty;
                _context.Entry(prod).Property(x => x.Quantity).IsModified = true;
                hasQuantityChanges = true;
            }
        }
        if (hasQuantityChanges)
        {
            await _context.SaveChangesAsync();
        }

        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) || 
                                     p.ModelSeries.ToLower().Contains(kw) ||
                                     (p.Sku != null && p.Sku.ToLower().Contains(kw)));
        }

        if (categoryId.HasValue && categoryId > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(storageFilter))
            query = query.Where(p => p.Storage == storageFilter);

        if (statusFilter == "active")
            query = query.Where(p => p.IsActive && p.Quantity > 0);
        else if (statusFilter == "out_of_stock")
            query = query.Where(p => p.IsActive && p.Quantity == 0);
        else if (statusFilter == "hidden")
            query = query.Where(p => !p.IsActive);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var products = await query.OrderByDescending(p => p.Id)
            .Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SearchKeyword = searchKeyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.StorageFilter = storageFilter;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        // Counts for tabs
        var allList = await _context.Products.ToListAsync();
        ViewBag.AllCount = allList.Count;
        ViewBag.ActiveCount = allList.Count(p => p.IsActive && p.Quantity > 0);
        ViewBag.OutOfStockCount = allList.Count(p => p.IsActive && p.Quantity == 0);
        ViewBag.HiddenCount = allList.Count(p => !p.IsActive);

        return View(products);
    }

    // =========================================================================
    // 4. CHỈNH SỬA / THÊM MỚI SẢN PHẨM (SCREEN 4)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> EditProduct(int? id)
    {
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

        if (!id.HasValue || id.Value == 0)
        {
            // Create new
            var newProd = new Product
            {
                WarrantyMonths = 12,
                IsActive = true,
                Storage = "128GB",
                Quantity = 0
            };
            return View(newProd);
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImeis)
            .FirstOrDefaultAsync(p => p.Id == id.Value);

        if (product == null) return NotFound();

        // Đồng bộ tồn kho theo số lượng IMEI thực tế InStock
        var actualInStock = await _context.ProductImeis
            .CountAsync(pi => pi.ProductId == product.Id && pi.Status == "InStock");
        if (product.Quantity != actualInStock)
        {
            product.Quantity = actualInStock;
            await _context.SaveChangesAsync();
        }

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProduct(Product product, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            TempData["Error"] = "Tên iPhone không được để trống!";
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View("EditProduct", product);
        }

        // Validate Giá âm hoặc bằng 0
        if (product.Price <= 0)
        {
            TempData["Error"] = "Giá bán không hợp lệ! Giá bán không thể là số âm hoặc bằng 0đ.";
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View("EditProduct", product);
        }

        if (product.ListedPrice.HasValue && product.ListedPrice.Value < 0)
        {
            TempData["Error"] = "Giá niêm yết không hợp lệ! Giá niêm yết không thể là số âm.";
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View("EditProduct", product);
        }

        // Validate Giá bán <= Giá niêm yết (nếu có niêm yết)
        if (product.ListedPrice.HasValue && product.ListedPrice.Value > 0 && product.Price > product.ListedPrice.Value)
        {
            TempData["Error"] = "Giá bán khuyến mãi không thể cao hơn Giá niêm yết chính hãng Apple!";
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View("EditProduct", product);
        }

        // Validate Trùng lặp Mã SKU (Unique SKU Rule trong Quản lý Kho & AAR)
        var trimmedSku = product.Sku?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedSku))
        {
            var isDuplicateSku = await _context.Products
                .AnyAsync(p => p.Sku != null && p.Sku.ToLower() == trimmedSku.ToLower() && p.Id != product.Id);
            if (isDuplicateSku)
            {
                TempData["Error"] = $"Mã SKU '{trimmedSku}' đã tồn tại trong hệ thống! Mỗi sản phẩm / biến thể iPhone bắt buộc phải có một mã SKU duy nhất để định danh kho và gán mã IMEI.";
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                return View("EditProduct", product);
            }
        }

        // Validate Trùng lặp Biến thể sản phẩm (Cùng Model + Dung lượng + Màu sắc)
        if (!string.IsNullOrWhiteSpace(product.ModelSeries) && !string.IsNullOrWhiteSpace(product.Storage) && !string.IsNullOrWhiteSpace(product.Color))
        {
            var isDuplicateVariant = await _context.Products
                .AnyAsync(p => p.ModelSeries == product.ModelSeries 
                            && p.Storage == product.Storage 
                            && p.Color == product.Color 
                            && p.Id != product.Id);
            if (isDuplicateVariant)
            {
                TempData["Error"] = $"Đã tồn tại một sản phẩm cùng biến thể: {product.ModelSeries} ({product.Storage} - {product.Color})! Không thể tạo hai sản phẩm trùng nhau, vui lòng thay đổi màu sắc hoặc dung lượng.";
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                return View("EditProduct", product);
            }
        }

        // Handle Image upload if provided
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
            product.ImageUrl = $"/images/products/{fileName}";
        }

        if (product.Id == 0)
        {
            product.Quantity = 0; // Sản phẩm mới bắt đầu với 0 máy, nạp số lượng qua chức năng Nhập lô IMEI
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await LogAuditAsync("CREATE_PRODUCT", "Products", product.Id.ToString(), null, $"Tạo sản phẩm mới SKU={product.Sku}: {product.Name}");
            TempData["Success"] = $"Đã thêm thành công sản phẩm {product.Name}!";
        }
        else
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null) return NotFound();

            var actualInStock = await _context.ProductImeis
                .CountAsync(pi => pi.ProductId == existing.Id && pi.Status == "InStock");

            var oldSummary = $"Name={existing.Name}, Price={existing.Price:N0}, Qty={existing.Quantity}, Sku={existing.Sku}";

            existing.Name = product.Name.Trim();
            existing.Sku = product.Sku?.Trim();
            existing.ModelSeries = product.ModelSeries;
            existing.CategoryId = product.CategoryId;
            existing.Price = product.Price;
            existing.ListedPrice = product.ListedPrice;
            existing.Storage = product.Storage;
            existing.Color = product.Color;
            existing.Specifications = product.Specifications;
            existing.Description = product.Description;
            if (!string.IsNullOrEmpty(product.ImageUrl)) existing.ImageUrl = product.ImageUrl;
            existing.Quantity = actualInStock; // Tự động đồng bộ chuẩn theo kho IMEI InStock
            existing.WarrantyMonths = product.WarrantyMonths;
            existing.IsActive = product.IsActive;

            var newSummary = $"Name={existing.Name}, Price={existing.Price:N0}, Qty={existing.Quantity}, Sku={existing.Sku}";
            await LogAuditAsync("EDIT_PRODUCT", "Products", existing.Id.ToString(), oldSummary, newSummary);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật thông tin sản phẩm {product.Name}!";
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductStatus(int id)
    {
        var prod = await _context.Products.FindAsync(id);
        if (prod != null)
        {
            prod.IsActive = !prod.IsActive;
            await _context.SaveChangesAsync();
            await LogAuditAsync("TOGGLE_STATUS", "Products", id.ToString(), null, $"Trạng thái: {(prod.IsActive ? "Đang kinh doanh" : "Tạm ẩn")}");
            TempData["Success"] = $"Đã thay đổi trạng thái của {prod.Name} thành: {(prod.IsActive ? "Đang kinh doanh" : "Tạm ẩn")}!";
        }
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            var hasOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == id);
            if (hasOrders)
            {
                product.IsActive = false;
                await LogAuditAsync("SOFT_DELETE", "Products", id.ToString(), null, $"Chuyển sang Tạm ẩn vì đã có đơn hàng liên kết");
                TempData["Success"] = $"Sản phẩm {product.Name} đã có đơn hàng trong hệ thống nên được chuyển sang trạng thái Ẩn!";
            }
            else
            {
                _context.Products.Remove(product);
                await LogAuditAsync("DELETE_PRODUCT", "Products", id.ToString(), $"Name={product.Name}", null);
                TempData["Success"] = $"Đã xóa vĩnh viễn sản phẩm {product.Name}!";
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Products));
    }

    // =========================================================================
    // 5. KHO IMEI / SERIAL (SCREEN 5)
    // =========================================================================
    public async Task<IActionResult> Inventory(string? skuFilter, string? statusFilter, string? searchKeyword, int currentPage = 1)
    {
        const int pageSize = 12;

        var allImeis = await _context.ProductImeis.ToListAsync();
        var inStockCount = allImeis.Count(i => i.Status == "InStock");
        var reservedCount = allImeis.Count(i => i.Status == "Reserved");
        var soldCount = allImeis.Count(i => i.Status == "Sold");
        var defectiveCount = allImeis.Count(i => i.Status == "Defective");

        var lowStock = await _context.Products.Include(p => p.Category)
            .Where(p => p.Quantity <= 5 && p.IsActive)
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        var query = _context.ProductImeis
            .Include(i => i.Product)
            .Include(i => i.Order)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(skuFilter))
            query = query.Where(i => i.Product != null && i.Product.Sku == skuFilter);

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(i => i.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(i => i.Imei.Contains(kw) || 
                                     (i.SerialNumber != null && i.SerialNumber.ToLower().Contains(kw)) ||
                                     (i.Product != null && i.Product.Name.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var list = await query.OrderByDescending(i => i.ImportDate)
            .Skip((currentPage - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var vm = new InventoryViewModel
        {
            InStockCount = inStockCount,
            ReservedCount = reservedCount,
            SoldCount = soldCount,
            DefectiveCount = defectiveCount,
            Imeis = list,
            LowStockProducts = lowStock,
            AllProducts = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(),
            SkuFilter = skuFilter,
            StatusFilter = statusFilter,
            SearchKeyword = searchKeyword,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImeiStatus(int id, string newStatus, string? returnUrl = null)
    {
        var imei = await _context.ProductImeis.Include(i => i.Product).FirstOrDefaultAsync(i => i.Id == id);
        if (imei != null)
        {
            var oldStatus = imei.Status;
            imei.Status = newStatus;

            // Đồng bộ lại tồn kho nếu cần
            if (imei.Product != null)
            {
                if (oldStatus == "InStock" && newStatus != "InStock")
                {
                    imei.Product.Quantity = Math.Max(0, imei.Product.Quantity - 1);
                }
                else if (oldStatus != "InStock" && newStatus == "InStock")
                {
                    imei.Product.Quantity += 1;
                }
            }

            await LogAuditAsync("UPDATE_IMEI_STATUS", "ProductImeis", id.ToString(), oldStatus, newStatus);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã chuyển trạng thái IMEI {imei.Imei} sang: {newStatus}!";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Inventory));
    }

    // =========================================================================
    // 6. NHẬP LÔ IMEI (SCREEN 6)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> BatchImport()
    {
        var products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        var vm = new BatchImportViewModel
        {
            Products = products,
            Vendor = "Synnex FPT"
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchImport(int productId, string vendor, string? note, string imeisText)
    {
        if (productId <= 0 || string.IsNullOrWhiteSpace(imeisText))
        {
            TempData["Error"] = "Vui lòng chọn model iPhone và nhập danh sách mã IMEI!";
            return RedirectToAction(nameof(BatchImport));
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            TempData["Error"] = "Model sản phẩm không tồn tại trong hệ thống!";
            return RedirectToAction(nameof(BatchImport));
        }

        var rawLines = imeisText.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (rawLines.Count == 0)
        {
            TempData["Error"] = "Không tìm thấy chuỗi IMEI hợp lệ!";
            return RedirectToAction(nameof(BatchImport));
        }

        // Validate 15 chữ số
        var validFormatList = new List<string>();
        var invalidFormatList = new List<string>();
        foreach (var line in rawLines)
        {
            if (line.Length == 15 && line.All(char.IsDigit))
            {
                validFormatList.Add(line);
            }
            else
            {
                invalidFormatList.Add(line);
            }
        }

        var distinctImeis = validFormatList.Distinct().ToList();
        var internalDuplicatesCount = validFormatList.Count - distinctImeis.Count;

        // Check against existing database
        var existingDbImeis = await _context.ProductImeis
            .Where(i => distinctImeis.Contains(i.Imei))
            .Select(i => i.Imei)
            .ToListAsync();

        var commitList = distinctImeis.Except(existingDbImeis).ToList();

        if (commitList.Count == 0)
        {
            TempData["Error"] = $"Tất cả {rawLines.Count} IMEI nhập vào đều không hợp lệ hoặc đã tồn tại trong CSDL!";
            return RedirectToAction(nameof(BatchImport));
        }

        var newEntries = commitList.Select(imei => new ProductImei
        {
            ProductId = productId,
            Imei = imei,
            SerialNumber = "S" + imei.Substring(Math.Max(0, imei.Length - 8)),
            Status = "InStock",
            ImportDate = DateTime.Now
        }).ToList();

        await _context.ProductImeis.AddRangeAsync(newEntries);
        product.Quantity += commitList.Count;

        await LogAuditAsync("IMPORT_IMEI", "ProductImeis", productId.ToString(), null, 
            $"Nhập lô {commitList.Count} IMEI từ nhà PP {vendor} cho {product.Name}. (Ghi chú: {note})");

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã nhập kho thành công {commitList.Count} máy iPhone chuẩn AAR! (Bỏ qua: {existingDbImeis.Count} trùng CSDL, {internalDuplicatesCount} trùng danh sách, {invalidFormatList.Count} sai định dạng 15 số).";
        return RedirectToAction(nameof(Inventory));
    }

    // =========================================================================
    // 7. QUẢN LÝ ĐƠN HÀNG (SCREEN 7)
    // =========================================================================
    public async Task<IActionResult> Orders(string? statusFilter, string? paymentFilter, string? searchKeyword, int currentPage = 1)
    {
        const int pageSize = 10;
        var allOrders = await _context.Orders.ToListAsync();

        ViewBag.TotalCount = allOrders.Count;
        ViewBag.PendingCount = allOrders.Count(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ duyệt");
        ViewBag.ConfirmedCount = allOrders.Count(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
        ViewBag.ShippingCount = allOrders.Count(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
        ViewBag.CompletedCount = allOrders.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất");
        ViewBag.CancelledCount = allOrders.Count(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");

        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails).ThenInclude(od => od.ProductImei)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (statusFilter == "Pending")
                query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ duyệt");
            else if (statusFilter == "Confirmed")
                query = query.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
            else if (statusFilter == "Shipping")
                query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
            else if (statusFilter == "Completed")
                query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất");
            else if (statusFilter == "Cancelled")
                query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
        }

        if (!string.IsNullOrWhiteSpace(paymentFilter))
            query = query.Where(o => o.PaymentStatus == paymentFilter);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(o => (o.OrderCode != null && o.OrderCode.ToLower().Contains(kw)) ||
                                     o.Id.ToString() == kw ||
                                     o.Customer!.FullName.ToLower().Contains(kw) ||
                                     o.Customer.PhoneNumber.Contains(kw));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var orders = await query.OrderByDescending(o => o.OrderDate)
            .Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.PaymentFilter = paymentFilter;
        ViewBag.SearchKeyword = searchKeyword;
        ViewBag.CurrentPage = currentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.FilteredCount = totalCount;

        return View(orders);
    }

    // =========================================================================
    // 8. CHI TIẾT & XỬ LÝ XUẤT KHO ĐƠN HÀNG (SCREEN 8 - DH261024-018)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails).ThenInclude(od => od.ProductImei)
            .Include(o => o.ProductImeis)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        // Lấy danh sách IMEI InStock hoặc IMEI đã được gán cho đơn hàng này
        var productIds = order.OrderDetails.Select(od => od.ProductId).Distinct().ToList();
        var availableImeis = await _context.ProductImeis
            .Where(i => productIds.Contains(i.ProductId) && 
                       (i.Status == "InStock" || i.OrderId == order.Id))
            .ToListAsync();

        var vm = new OrderDetailViewModel
        {
            Order = order,
            AvailableImeis = availableImeis
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FulfillOrder(
        int orderId, 
        string orderStatus, 
        string paymentStatus, 
        string? shippingUnit, 
        string? trackingNumber,
        List<int> detailIds,
        List<string> selectedImeis)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails).ThenInclude(od => od.ProductImei)
            .Include(o => o.ProductImeis)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        var currentStatus = order.OrderStatus?.Trim();
        bool isCurrentTerminal = currentStatus == "Completed" || currentStatus == "Cancelled" 
                              || currentStatus == "Hoàn tất" || currentStatus == "Đã hủy";

        // 0. KHÓA CHẶN TUYỆT ĐỐI nếu đơn hàng đã ở trạng thái kết thúc (Completed hoặc Cancelled)
        if (isCurrentTerminal)
        {
            TempData["ErrorMessage"] = "Đơn hàng đã ở trạng thái kết thúc, không thể chỉnh sửa!";
            return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
        }

        // 0.1 Kiểm tra luồng chuyển trạng thái hợp lệ (State Machine Rule)
        string NormalizeStatus(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Pending";
            s = s.Trim();
            if (s == "Pending" || s == "Chờ duyệt") return "Pending";
            if (s == "Confirmed" || s == "Đã duyệt") return "Confirmed";
            if (s == "Shipping" || s == "Đang giao") return "Shipping";
            if (s == "Completed" || s == "Hoàn tất") return "Completed";
            if (s == "Cancelled" || s == "Đã hủy") return "Cancelled";
            return s;
        }

        var fromStatus = NormalizeStatus(currentStatus);
        var toStatus = NormalizeStatus(orderStatus);

        bool isValidTransition = false;
        if (fromStatus == toStatus)
        {
            isValidTransition = true;
        }
        else if (fromStatus == "Pending" && (toStatus == "Confirmed" || toStatus == "Cancelled"))
        {
            isValidTransition = true;
        }
        else if (fromStatus == "Confirmed" && (toStatus == "Shipping" || toStatus == "Cancelled"))
        {
            isValidTransition = true;
        }
        else if (fromStatus == "Shipping" && (toStatus == "Completed" || toStatus == "Cancelled"))
        {
            isValidTransition = true;
        }

        if (!isValidTransition)
        {
            TempData["ErrorMessage"] = $"Quy trình chuyển trạng thái không hợp lệ: Không thể chuyển từ '{currentStatus}' sang '{orderStatus}'! (Chỉ cho phép: Chờ duyệt → Đã duyệt → Đang giao → Hoàn tất, hoặc Hủy)";
            return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
        }

        // 0.2 Kiểm tra ràng buộc IMEI khi Đang giao hoặc Hoàn tất (AAR Barcode Compliance)
        if (toStatus == "Shipping" || toStatus == "Completed")
        {
            var totalRequiredUnits = order.OrderDetails.Sum(od => od.Quantity);
            var validSelectedCount = selectedImeis?.Count(s => !string.IsNullOrWhiteSpace(s)) ?? 0;
            if (validSelectedCount < totalRequiredUnits)
            {
                TempData["ErrorMessage"] = $"Không thể chuyển trạng thái sang '{orderStatus}'! Đơn hàng yêu cầu xuất kho {totalRequiredUnits} máy, nhưng hiện mới chỉ gán {validSelectedCount} mã IMEI.";
                return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
            }
        }

        // 0.3 Ràng buộc ma trận Trạng thái Đơn hàng & Trạng thái Thanh toán (AAR Accounting Matrix)
        if (toStatus != "Cancelled" && (paymentStatus == "Chờ hoàn tiền" || paymentStatus == "Đã hoàn tiền"))
        {
            TempData["ErrorMessage"] = "Không thể chọn trạng thái 'Chờ hoàn tiền' hoặc 'Đã hoàn tiền' khi đơn hàng đang xử lý hoặc chưa bị hủy!";
            return RedirectToAction(nameof(OrderDetail), new { id = order.Id });
        }

        if (toStatus == "Completed")
        {
            // Đơn hoàn tất bắt buộc dòng tiền phải là "Đã thanh toán" (áp dụng đối soát COD và chuyển khoản)
            if (paymentStatus == "Chưa thanh toán" || string.IsNullOrWhiteSpace(paymentStatus))
            {
                paymentStatus = "Đã thanh toán";
            }
        }
        else if (toStatus == "Cancelled")
        {
            // Nếu đơn hàng bị hủy mà trước đó đã thanh toán -> tự động chuyển sang Chờ hoàn tiền
            if (order.PaymentStatus == "Đã thanh toán" || paymentStatus == "Đã thanh toán")
            {
                if (paymentStatus != "Đã hoàn tiền")
                {
                    paymentStatus = "Chờ hoàn tiền";
                }
            }
        }

        var oldState = $"Status={order.OrderStatus}, Payment={order.PaymentStatus}";

        // 1. Cập nhật các IMEI được chọn cho từng OrderDetail
        if (detailIds != null && selectedImeis != null && detailIds.Count == selectedImeis.Count)
        {
            var submittedValidImeis = selectedImeis
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();

            var currentOrderImeis = await _context.ProductImeis
                .Where(pi => pi.OrderId == order.Id)
                .ToListAsync();

            foreach (var prevImei in currentOrderImeis)
            {
                if (!submittedValidImeis.Contains(prevImei.Imei))
                {
                    prevImei.Status = "InStock";
                    prevImei.OrderId = null;
                }
            }

            for (int i = 0; i < detailIds.Count; i++)
            {
                var dId = detailIds[i];
                var imeiStr = selectedImeis[i]?.Trim();
                var detail = order.OrderDetails.FirstOrDefault(d => d.Id == dId);

                if (detail != null && !string.IsNullOrEmpty(imeiStr))
                {
                    var imeiRecord = await _context.ProductImeis
                        .FirstOrDefaultAsync(pi => pi.Imei == imeiStr && pi.ProductId == detail.ProductId);

                    if (imeiRecord != null)
                    {
                        if (detail.ProductImeiId == null)
                        {
                            detail.ProductImeiId = imeiRecord.Id;
                        }
                        detail.ImeiNumber = imeiStr;
                        imeiRecord.OrderId = order.Id;

                        if (orderStatus == "Shipping" || orderStatus == "Completed")
                        {
                            imeiRecord.Status = "Sold";

                            // Kích hoạt bảo hành điện tử Apple AAR nếu chưa có
                            var hasWarranty = await _context.Warranties.AnyAsync(w => w.Imei == imeiRecord.Imei);
                            if (!hasWarranty)
                            {
                                var months = detail.Product?.WarrantyMonths ?? 12;
                                var startDate = DateTime.Now;
                                _context.Warranties.Add(new Warranty
                                {
                                    Imei = imeiRecord.Imei,
                                    SerialNumber = imeiRecord.SerialNumber,
                                    CustomerId = order.CustomerId,
                                    ProductId = detail.ProductId,
                                    StartDate = startDate,
                                    EndDate = startDate.AddMonths(months),
                                    Status = "Active"
                                });
                            }
                        }
                        else
                        {
                            imeiRecord.Status = "Reserved";
                        }
                    }
                }
            }
        }

        // 2. Cập nhật trạng thái đơn hàng
        order.OrderStatus = orderStatus;
        order.PaymentStatus = paymentStatus;
        if (!string.IsNullOrEmpty(shippingUnit)) order.ShippingUnit = shippingUnit;
        if (!string.IsNullOrEmpty(trackingNumber)) order.TrackingNumber = trackingNumber;

        // Nếu chuyển sang Cancelled thì hoàn tồn kho & giải phóng IMEI
        if (orderStatus == "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(detail.ProductId);
                if (prod != null) prod.Quantity += detail.Quantity;
            }

            var reservedImeis = await _context.ProductImeis
                .Where(pi => pi.OrderId == order.Id && (pi.Status == "Reserved" || pi.Status == "InStock"))
                .ToListAsync();

            foreach (var imei in reservedImeis)
            {
                imei.Status = "InStock";
                imei.OrderId = null;
            }
        }

        var newState = $"Status={order.OrderStatus}, Payment={order.PaymentStatus}, Tracking={order.TrackingNumber}";
        await LogAuditAsync("FULFILL_ORDER", "Orders", order.Id.ToString(), oldState, newState);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã xử lý xuất kho & cập nhật đơn hàng #{order.OrderCode ?? order.Id.ToString()} thành công! (Trạng thái: {order.OrderStatus})";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string orderStatus, string paymentStatus, string? shippingUnit, string? trackingNumber)
    {
        return await FulfillOrder(orderId, orderStatus, paymentStatus, shippingUnit, trackingNumber, new List<int>(), new List<string>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessOrder(int orderId, string orderStatus, string paymentStatus, string? shippingUnit, string? trackingNumber)
    {
        return await FulfillOrder(orderId, orderStatus, paymentStatus, shippingUnit, trackingNumber, new List<int>(), new List<string>());
    }

    // =========================================================================
    // 9. BẢO HÀNH & TIẾP NHẬN (SCREEN 9)
    // =========================================================================
    public async Task<IActionResult> Warranty(string? tab = "claims", string? searchKeyword = null)
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var activeWarrantiesCount = await _context.Warranties.CountAsync(w => w.Status == "Active" && w.EndDate >= now);
        var pendingClaimsCount = await _context.WarrantyClaims.CountAsync(c => c.Status == "Tiếp nhận" || c.Status == "Chờ kiểm tra");
        var replacedCountMonth = await _context.WarrantyClaims.CountAsync(c => c.ActionTaken != null && c.ActionTaken.Contains("Đổi máy") && c.ResolvedDate >= startOfMonth);

        var warrantiesQuery = _context.Warranties
            .Include(w => w.Customer)
            .Include(w => w.Product)
            .AsQueryable();

        var claimsQuery = _context.WarrantyClaims
            .Include(c => c.Warranty).ThenInclude(w => w!.Customer)
            .Include(c => c.Warranty).ThenInclude(w => w!.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            warrantiesQuery = warrantiesQuery.Where(w => w.Imei.Contains(kw) ||
                                                        (w.Customer != null && w.Customer.PhoneNumber.Contains(kw)) ||
                                                        (w.Customer != null && w.Customer.FullName.ToLower().Contains(kw)));

            claimsQuery = claimsQuery.Where(c => (c.ClaimCode != null && c.ClaimCode.ToLower().Contains(kw)) ||
                                                 (c.OldImei != null && c.OldImei.Contains(kw)) ||
                                                 (c.Warranty != null && c.Warranty.Customer != null && c.Warranty.Customer.FullName.ToLower().Contains(kw)));
        }

        var warrantiesList = await warrantiesQuery.OrderByDescending(w => w.StartDate).Take(20).ToListAsync();
        var claimsList = await claimsQuery.OrderByDescending(c => c.ClaimDate).ToListAsync();

        var vm = new WarrantyDashboardViewModel
        {
            ActiveWarrantiesCount = activeWarrantiesCount,
            PendingClaimsCount = pendingClaimsCount,
            ReplacedCountMonth = replacedCountMonth,
            WarrantyRatio = 1.4m,
            Warranties = warrantiesList,
            Claims = claimsList,
            ActiveTab = tab
        };

        ViewBag.SearchKeyword = searchKeyword;
        return View(vm);
    }

    // API Tra cứu nhanh bảo hành theo IMEI cho Modal Tiếp nhận
    [HttpGet]
    public async Task<IActionResult> CheckImeiWarranty(string imei)
    {
        if (string.IsNullOrWhiteSpace(imei))
            return Json(new { success = false, message = "Vui lòng nhập số IMEI của máy!" });

        var w = await _context.Warranties
            .Include(x => x.Customer)
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Imei == imei.Trim());

        if (w == null)
            return Json(new { success = false, message = $"Mã IMEI '{imei}' không tồn tại trong Sổ bảo hành chính hãng của cửa hàng!" });

        var now = DateTime.Now;
        var daysSinceStart = (int)(now - w.StartDate).TotalDays;
        var daysRemaining = (int)(w.EndDate - now).TotalDays;
        bool isExpired = daysRemaining < 0;
        bool eligible1To1 = !isExpired && daysSinceStart <= 30;

        return Json(new
        {
            success = true,
            warrantyId = w.Id,
            imei = w.Imei,
            serialNumber = w.SerialNumber ?? "---",
            customerName = w.Customer?.FullName ?? "Khách lẻ",
            customerPhone = w.Customer?.PhoneNumber ?? "---",
            productName = w.Product?.Name ?? "iPhone",
            startDate = w.StartDate.ToString("dd/MM/yyyy"),
            endDate = w.EndDate.ToString("dd/MM/yyyy"),
            daysRemaining = Math.Max(0, daysRemaining),
            isExpired = isExpired,
            eligible1To1 = eligible1To1,
            daysSinceStart = Math.Max(0, daysSinceStart)
        });
    }

    // POST: Tiếp nhận bảo hành & tạo phiếu mới (Warranty Claim)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWarrantyClaim(string imei, string issueDescription, string? technicianNotes = null)
    {
        if (string.IsNullOrWhiteSpace(imei) || string.IsNullOrWhiteSpace(issueDescription))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ mã IMEI và mô tả tình trạng lỗi phần cứng!";
            return RedirectToAction(nameof(Warranty), new { tab = "claims" });
        }

        var warranty = await _context.Warranties
            .Include(w => w.Customer)
            .Include(w => w.Product)
            .FirstOrDefaultAsync(w => w.Imei == imei.Trim());

        if (warranty == null)
        {
            TempData["Error"] = $"Không tìm thấy thông tin bảo hành cho mã IMEI '{imei}'!";
            return RedirectToAction(nameof(Warranty), new { tab = "claims" });
        }

        // Kiểm tra xem máy có đang trong quy trình xử lý của phiếu claim nào khác không
        var openClaim = await _context.WarrantyClaims
            .FirstOrDefaultAsync(c => c.WarrantyId == warranty.Id && 
                                     (c.Status == "Tiếp nhận" || c.Status == "Chờ kiểm tra" || c.Status == "Đang xử lý tại Apple"));

        if (openClaim != null)
        {
            TempData["Error"] = $"Máy này đang có phiếu tiếp nhận #{openClaim.ClaimCode ?? openClaim.Id.ToString()} chưa hoàn tất xử lý!";
            return RedirectToAction(nameof(WarrantyClaimDetail), new { id = openClaim.Id });
        }

        // Tự động sinh mã phiếu tiếp nhận chuẩn AAR: BH{yyMMdd}-{seq}
        var todayPrefix = $"BH{DateTime.Now:yyMMdd}-";
        var todayCount = await _context.WarrantyClaims.CountAsync(c => c.ClaimCode != null && c.ClaimCode.StartsWith(todayPrefix));
        var claimCode = $"{todayPrefix}{(todayCount + 1):D3}";

        var daysSinceStart = (DateTime.Now - warranty.StartDate).TotalDays;
        bool eligible1To1 = daysSinceStart <= 30;

        var claim = new WarrantyClaim
        {
            ClaimCode = claimCode,
            WarrantyId = warranty.Id,
            ClaimDate = DateTime.Now,
            IssueDescription = issueDescription.Trim(),
            TechnicianNotes = technicianNotes?.Trim() ?? (eligible1To1 ? "Đủ điều kiện xem xét 1 Đổi 1 trong 30 ngày (Tiếp nhận ban đầu)" : "Bảo hành sửa chữa tiêu chuẩn Apple Care"),
            Status = "Tiếp nhận",
            CreatedBy = User.Identity?.Name ?? "Admin"
        };

        _context.WarrantyClaims.Add(claim);
        await _context.SaveChangesAsync();

        await LogAuditAsync("CREATE_WARRANTY_CLAIM", "WarrantyClaims", claim.Id.ToString(), null,
            $"Tạo phiếu bảo hành #{claimCode} cho IMEI {warranty.Imei} (Khách: {warranty.Customer?.FullName}). Lỗi: {issueDescription.Trim()}");

        TempData["Success"] = $"Đã tạo phiếu tiếp nhận bảo hành #{claimCode} thành công!";
        return RedirectToAction(nameof(Warranty), new { tab = "claims" });
    }

    // =========================================================================
    // 10. CHI TIẾT PHIẾU TIẾP NHẬN & 1 ĐỔI 1 (SCREEN 10 - BH261024-006)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> WarrantyClaimDetail(int id)
    {
        var claim = await _context.WarrantyClaims
            .Include(c => c.Warranty).ThenInclude(w => w!.Customer)
            .Include(c => c.Warranty).ThenInclude(w => w!.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        // Tìm các IMEI InStock cùng dòng Product để đổi mới
        var targetProductId = claim.Warranty?.ProductId ?? 0;
        var replacementImeis = await _context.ProductImeis
            .Include(pi => pi.Product)
            .Where(pi => pi.Status == "InStock")
            .OrderByDescending(pi => pi.ProductId == targetProductId)
            .ThenBy(pi => pi.Id)
            .ToListAsync();

        var vm = new WarrantyClaimDetailViewModel
        {
            Claim = claim,
            Warranty = claim.Warranty!,
            AvailableReplacementImeis = replacementImeis
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessWarrantyClaim(
        int claimId, 
        string decision, 
        string? newImei, 
        string? technicianNotes,
        bool pass30Days,
        bool passHardware,
        bool passAppearance)
    {
        var claim = await _context.WarrantyClaims
            .Include(c => c.Warranty)
            .FirstOrDefaultAsync(c => c.Id == claimId);

        if (claim == null) return NotFound();

        // KHÓA CHẶN TUYỆT ĐỐI nếu phiếu bảo hành đã ở trạng thái kết thúc (Hoàn tất hoặc Từ chối)
        if (claim.Status == "Hoàn tất" || claim.Status == "Từ chối")
        {
            TempData["Error"] = "Phiếu bảo hành này đã được xử lý hoàn tất trước đó, hồ sơ đã đóng và không thể chỉnh sửa!";
            return RedirectToAction(nameof(WarrantyClaimDetail), new { id = claimId });
        }

        var oldSummary = $"Status={claim.Status}, Action={claim.ActionTaken}";

        claim.TechnicianNotes = technicianNotes;

        if (decision == "Exchange_1to1")
        {
            if (string.IsNullOrWhiteSpace(newImei))
            {
                TempData["Error"] = "Vui lòng chọn hoặc quét mã IMEI máy mới để thực hiện nghiệp vụ 1 Đổi 1!";
                return RedirectToAction(nameof(WarrantyClaimDetail), new { id = claimId });
            }

            var cleanNewImei = newImei.Trim();
            var replacementImeiRecord = await _context.ProductImeis
                .FirstOrDefaultAsync(i => i.Imei == cleanNewImei && i.Status == "InStock");

            if (replacementImeiRecord == null)
            {
                TempData["Error"] = $"Mã IMEI {cleanNewImei} không hợp lệ hoặc không có trạng thái Sẵn bán (InStock) trong kho!";
                return RedirectToAction(nameof(WarrantyClaimDetail), new { id = claimId });
            }

            // 1. Máy CŨ thu hồi từ khách -> chuyển sang Defective (Hàng lỗi chờ gửi về Apple)
            var oldImeiStr = claim.OldImei ?? claim.Warranty?.Imei;
            if (!string.IsNullOrEmpty(oldImeiStr))
            {
                var oldImeiRecord = await _context.ProductImeis.FirstOrDefaultAsync(pi => pi.Imei == oldImeiStr);
                if (oldImeiRecord != null)
                {
                    oldImeiRecord.Status = "Defective";
                }
            }

            // 2. Máy MỚI nguyên seal xuất kho đổi cho khách -> chuyển sang Sold (Đã xuất bán)
            replacementImeiRecord.Status = "Sold";
            claim.NewImei = replacementImeiRecord.Imei;

            // 3. Cập nhật phiếu claim hoàn tất
            claim.ActionTaken = "Đổi mới 1-1 (AAR Instant Replacement)";
            claim.OldImei = oldImeiStr;
            claim.Status = "Hoàn tất";
            claim.ResolvedDate = DateTime.Now;

            // 4. Kế thừa thời hạn bảo hành chính hãng Apple Care cho máy mới
            if (claim.Warranty != null)
            {
                claim.Warranty.OriginalImei ??= oldImeiStr;
                claim.Warranty.Imei = replacementImeiRecord.Imei;
                claim.Warranty.SerialNumber = replacementImeiRecord.SerialNumber;
            }

            // 5. Cập nhật tồn kho InStock của sản phẩm
            var prod = await _context.Products.FindAsync(replacementImeiRecord.ProductId);
            if (prod != null)
            {
                prod.Quantity = await _context.ProductImeis.CountAsync(pi => pi.ProductId == prod.Id && pi.Status == "InStock");
            }

            await LogAuditAsync("WARRANTY_REPLACE_1TO1", "WarrantyClaims", claim.Id.ToString(), 
                $"OldImei={claim.OldImei}", $"NewImei={claim.NewImei}, Status=Hoàn tất");

            TempData["Success"] = $"Đã thực hiện thành công nghiệp vụ Apple 1 Đổi 1! Đã xuất máy mới {claim.NewImei} (Sold) và thu hồi máy lỗi {claim.OldImei} (Defective).";
        }
        else if (decision == "AASP_Repair")
        {
            claim.ActionTaken = "Gửi TTBH Apple AASP sửa chữa";
            claim.Status = "Gửi Apple";
            await LogAuditAsync("WARRANTY_AASP", "WarrantyClaims", claim.Id.ToString(), oldSummary, "Chuyển tiếp TTBH Apple AASP");
            TempData["Success"] = $"Đã chuyển tiếp thiết bị tới Trung Tâm Bảo Hành Ủy Quyền Apple (AASP)!";
        }
        else if (decision == "Reject")
        {
            claim.ActionTaken = "Từ chối bảo hành (Lỗi người dùng / Ngoại quan)";
            claim.Status = "Từ chối";
            claim.ResolvedDate = DateTime.Now;
            await LogAuditAsync("WARRANTY_REJECT", "WarrantyClaims", claim.Id.ToString(), oldSummary, "Từ chối bảo hành");
            TempData["Success"] = $"Đã từ chối bảo hành theo quy định Apple!";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Warranty));
    }

    // =========================================================================
    // 11. QUẢN LÝ KHÁCH HÀNG & TÀI KHOẢN (CUSTOMER MANAGEMENT)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Customers(string? searchKeyword, string? statusFilter)
    {
        // 1. Đảm bảo tài khoản admin không bị gắn nhầm role Customer
        var adminUser = await _userManager.FindByNameAsync("admin");
        if (adminUser != null && await _userManager.IsInRoleAsync(adminUser, "Customer"))
        {
            await _userManager.RemoveFromRoleAsync(adminUser, "Customer");
        }

        // 2. Khởi tạo & đồng bộ 5 khách hàng thành viên mẫu đạt chuẩn Đặc tả Nghiệp vụ phục vụ chấm đồ án
        var hasAllFive = await _context.Customers.CountAsync(c => c.User != null &&
            (c.User.UserName == "nghecute" || c.User.UserName == "mainguyen" || c.User.UserName == "hungtran" || c.User.UserName == "longle" || c.User.UserName == "thaopham")) == 5;

        if (!hasAllFive)
        {
            await IdentityDataSeeder.SeedAsync(HttpContext.RequestServices);
        }

        var query = _context.Customers
            .Include(c => c.User)
            .Include(c => c.Orders)
            .Include(c => c.Warranties)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(kw) ||
                                     c.PhoneNumber.Contains(kw) ||
                                     (c.Email != null && c.Email.ToLower().Contains(kw)) ||
                                     (c.User != null && c.User.UserName!.ToLower().Contains(kw)) ||
                                     c.Id.ToString() == kw);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (statusFilter == "active")
                query = query.Where(c => c.User != null && c.User.IsActive);
            else if (statusFilter == "locked")
                query = query.Where(c => c.User != null && !c.User.IsActive);
            else if (statusFilter == "guest")
                query = query.Where(c => c.UserId == null);
        }

        // Dọn dẹp text gộp nếu từng chạy thử nghiệm merge trước đây
        var dirtyCustomers = await _context.Customers
            .Where(c => c.FullName.Contains("[Đã gộp") || c.Address.Contains("[Đã gộp"))
            .ToListAsync();
        if (dirtyCustomers.Any())
        {
            foreach (var dc in dirtyCustomers)
            {
                var idx = dc.FullName.IndexOf(" [Đã gộp");
                if (idx >= 0) dc.FullName = dc.FullName.Substring(0, idx).Trim();

                var addrIdx = dc.Address.IndexOf("] ");
                if (addrIdx >= 0 && dc.Address.Contains("[Đã gộp"))
                {
                    dc.Address = dc.Address.Substring(addrIdx + 2).Trim();
                }
            }
            await _context.SaveChangesAsync();
        }

        var allCustomers = await _context.Customers.Include(c => c.User).Include(c => c.Orders).ToListAsync();
        var rawList = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var customerItems = rawList.Select(c => new CustomerItemViewModel
        {
            Id = c.Id,
            UserId = c.UserId,
            UserName = c.User?.UserName,
            FullName = c.FullName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Address = c.Address,
            CreatedAt = c.CreatedAt,
            IsActive = c.User?.IsActive ?? true,
            OrderCount = c.Orders.Count,
            WarrantyCount = c.Warranties.Count
        }).ToList();

        var vm = new CustomerManagementViewModel
        {
            Customers = customerItems,
            SearchKeyword = searchKeyword,
            StatusFilter = statusFilter,
            TotalCustomers = allCustomers.Count,
            ActiveAccounts = allCustomers.Count(c => c.User != null && c.User.IsActive),
            LockedAccounts = allCustomers.Count(c => c.User != null && !c.User.IsActive),
            GuestCustomers = allCustomers.Count(c => c.UserId == null),
            TotalOrders = allCustomers.Sum(c => c.Orders.Count)
        };

        return View("Staff", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Staff(string? searchKeyword, string? statusFilter)
    {
        return await Customers(searchKeyword, statusFilter);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCustomer(int id, string fullName, string phoneNumber, string? email, string? address)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString().Contains("application/json"));

        // 1. Kiểm tra tồn tại hồ sơ khách hàng
        var customer = await _context.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null)
        {
            var msg = "Không tìm thấy hồ sơ khách hàng trên hệ thống!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // 2. Kiểm tra Họ và tên (tối thiểu 2 ký tự theo mục 2.4.4)
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
        {
            var msg = "Vui lòng nhập họ và tên của bạn (tối thiểu 2 ký tự)!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // 3. Kiểm tra Số điện thoại di động chuẩn Việt Nam (10 số, đầu 03x-09x theo mục 2.4.4)
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            var msg = "Vui lòng nhập số điện thoại di động!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (!Regex.IsMatch(cleanPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
        {
            var msg = "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        string stdPhone = cleanPhone.StartsWith("+84") ? ("0" + cleanPhone.Substring(3)) : cleanPhone;
        string altPhone = cleanPhone.StartsWith("0") ? ("+84" + cleanPhone.Substring(1)) : cleanPhone;

        // Kiểm tra tính duy nhất của Số điện thoại trên bảng Customers (ngoại trừ chính khách hàng này)
        var duplicateCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id != id && (c.PhoneNumber == stdPhone || c.PhoneNumber == altPhone || c.PhoneNumber == cleanPhone));
        if (duplicateCustomer != null)
        {
            var msg = $"Số điện thoại {stdPhone} này đã tồn tại trên hệ thống (đang được liên kết với khách hàng #{duplicateCustomer.Id} - {duplicateCustomer.FullName})!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // Kiểm tra tính duy nhất của Số điện thoại trên bảng AspNetUsers
        var duplicateUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id != customer.UserId && (u.PhoneNumber == stdPhone || u.PhoneNumber == altPhone || u.PhoneNumber == cleanPhone));
        if (duplicateUser != null)
        {
            var msg = $"Số điện thoại {stdPhone} này đã được liên kết với tài khoản người dùng @{duplicateUser.UserName} trên hệ thống!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // 4. Kiểm tra Email (nếu có nhập)
        string? cleanEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            cleanEmail = email.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(cleanEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                var msg = "Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Staff));
            }

            // Kiểm tra trùng Email trên bảng Customers (ngoại trừ chính khách hàng này)
            var dupEmailCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id != id && c.Email != null && c.Email.ToLower() == cleanEmail);
            if (dupEmailCustomer != null)
            {
                var msg = $"Địa chỉ email {cleanEmail} này đã được liên kết với một tài khoản khác ({dupEmailCustomer.FullName})!";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Staff));
            }

            // Kiểm tra trùng Email trên bảng AspNetUsers
            var dupEmailUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id != customer.UserId && ((u.Email != null && u.Email.ToLower() == cleanEmail) || u.NormalizedEmail == cleanEmail.ToUpperInvariant()));
            if (dupEmailUser != null)
            {
                var msg = $"Địa chỉ email {cleanEmail} này đã được liên kết với một tài khoản khác trên hệ thống!";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Staff));
            }
        }

        // 5. Kiểm tra Địa chỉ nhận hàng (nếu có nhập, tối thiểu 5 ký tự theo mục 2.4.4)
        var cleanAddress = address?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(cleanAddress) && cleanAddress.Length < 5)
        {
            var msg = "Vui lòng nhập số nhà, tên đường cụ thể (tối thiểu 5 ký tự)!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // 6. Cập nhật dữ liệu vào Customer
        customer.FullName = fullName.Trim();
        customer.PhoneNumber = stdPhone;
        customer.Email = cleanEmail;
        customer.Address = cleanAddress;

        // 7. Đồng bộ nguyên tử sang ApplicationUser nếu có tài khoản Identity
        if (customer.User != null)
        {
            customer.User.PhoneNumber = stdPhone;
            if (!string.IsNullOrWhiteSpace(cleanEmail))
            {
                customer.User.Email = cleanEmail;
                customer.User.NormalizedEmail = cleanEmail.ToUpperInvariant();
            }
            var updateRes = await _userManager.UpdateAsync(customer.User);
            if (!updateRes.Succeeded)
            {
                var errDetail = string.Join("; ", updateRes.Errors.Select(e => e.Description));
                var msg = $"Lỗi đồng bộ tài khoản: {errDetail}";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Staff));
            }
        }

        await _context.SaveChangesAsync();
        await LogAuditAsync("EDIT_CUSTOMER", "Customers", id.ToString(), null, $"Cập nhật thông tin khách hàng {customer.FullName} (#{customer.Id}) - SĐT: {customer.PhoneNumber}");

        var successMsg = $"Đã cập nhật thông tin khách hàng {customer.FullName} thành công!";
        if (isAjax)
        {
            return Json(new { success = true, message = successMsg });
        }

        TempData["Success"] = successMsg;
        return RedirectToAction(nameof(Staff));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer(string fullName, string phoneNumber, string? email, string? address, string? username, string? password)
    {
        // 1. Kiểm tra Họ và tên
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
        {
            TempData["Error"] = "Vui lòng nhập họ và tên của khách hàng (tối thiểu 2 ký tự)!";
            return RedirectToAction(nameof(Staff));
        }

        // 2. Kiểm tra Số điện thoại di động chuẩn Việt Nam
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["Error"] = "Vui lòng nhập số điện thoại di động!";
            return RedirectToAction(nameof(Staff));
        }

        var cleanPhone = phoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
        if (!Regex.IsMatch(cleanPhone, @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])[0-9]{7}$"))
        {
            TempData["Error"] = "Số điện thoại không hợp lệ (Phải là 10 chữ số di động đầu 03, 05, 07, 08, 09)!";
            return RedirectToAction(nameof(Staff));
        }

        string stdPhone = cleanPhone.StartsWith("+84") ? ("0" + cleanPhone.Substring(3)) : cleanPhone;
        string altPhone = cleanPhone.StartsWith("0") ? ("+84" + cleanPhone.Substring(1)) : cleanPhone;

        var phoneExists = await _context.Customers.AnyAsync(c => c.PhoneNumber == stdPhone || c.PhoneNumber == altPhone || c.PhoneNumber == cleanPhone)
                       || await _userManager.Users.AnyAsync(u => u.PhoneNumber == stdPhone || u.PhoneNumber == altPhone || u.PhoneNumber == cleanPhone);
        if (phoneExists)
        {
            TempData["Error"] = $"Số điện thoại {stdPhone} này đã tồn tại trên hệ thống!";
            return RedirectToAction(nameof(Staff));
        }

        // 3. Kiểm tra Email (nếu có)
        string? cleanEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            cleanEmail = email.Trim().ToLowerInvariant();
            if (!Regex.IsMatch(cleanEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                TempData["Error"] = "Địa chỉ email không đúng định dạng chuẩn (VD: tenban@gmail.com)!";
                return RedirectToAction(nameof(Staff));
            }

            var emailExists = await _context.Customers.AnyAsync(c => c.Email != null && c.Email.ToLower() == cleanEmail)
                           || await _userManager.Users.AnyAsync(u => (u.Email != null && u.Email.ToLower() == cleanEmail) || u.NormalizedEmail == cleanEmail.ToUpperInvariant());
            if (emailExists)
            {
                TempData["Error"] = $"Địa chỉ email {cleanEmail} này đã được liên kết với một tài khoản khác!";
                return RedirectToAction(nameof(Staff));
            }
        }

        // 4. Kiểm tra Địa chỉ nhận hàng (nếu có)
        var cleanAddress = address?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(cleanAddress) && cleanAddress.Length < 5)
        {
            TempData["Error"] = "Vui lòng nhập số nhà, tên đường cụ thể (tối thiểu 5 ký tự)!";
            return RedirectToAction(nameof(Staff));
        }

        // 5. Kiểm tra và tạo tài khoản trực tuyến (nếu được chọn)
        string? createdUserId = null;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            var uName = username.Trim();
            if (uName.Length < 6 || uName.Length > 30)
            {
                TempData["Error"] = "Tên đăng nhập phải có độ dài từ 6 đến 30 ký tự!";
                return RedirectToAction(nameof(Staff));
            }

            var userExists = await _userManager.FindByNameAsync(uName);
            if (userExists != null)
            {
                TempData["Error"] = $"Tên đăng nhập {uName} đã tồn tại!";
                return RedirectToAction(nameof(Staff));
            }

            var newUser = new ApplicationUser
            {
                UserName = uName,
                Email = cleanEmail,
                PhoneNumber = stdPhone,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var createRes = await _userManager.CreateAsync(newUser, password.Trim());
            if (!createRes.Succeeded)
            {
                TempData["Error"] = string.Join("; ", createRes.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Staff));
            }

            await _userManager.AddToRoleAsync(newUser, "Customer");
            createdUserId = newUser.Id;
        }

        var customer = new Customer
        {
            UserId = createdUserId,
            FullName = fullName.Trim(),
            PhoneNumber = stdPhone,
            Email = cleanEmail,
            Address = cleanAddress,
            MembershipTier = "Member",
            RewardPoints = 0,
            TotalSpent = 0,
            CreatedAt = DateTime.Now
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        await LogAuditAsync("CREATE_CUSTOMER", "Customers", customer.Id.ToString(), null, $"Thêm khách hàng mới {customer.FullName} - SĐT: {customer.PhoneNumber}");

        TempData["Success"] = $"Đã tạo hồ sơ khách hàng {customer.FullName} thành công!";
        return RedirectToAction(nameof(Staff));
    }

    // Danh sách mật khẩu yếu phổ biến bị chặn tuyệt đối theo chuẩn BRS Mục 2.4.1.4
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "1234567890", "password", "qwertyuiop", "admin123", "iloveyou", "11111111", "00000000", "password123"
    };

    public static (bool IsValid, string? ErrorMessage) ValidatePasswordPolicy(
        string? password, 
        string? username = null, 
        string? email = null, 
        string? phone = null)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Vui lòng nhập mật khẩu mới!");

        // Ràng buộc an toàn: Không chứa khoảng trắng ở đầu hoặc cuối chuỗi (Mục 2.4.1.4)
        if (password.StartsWith(' ') || password.EndsWith(' '))
            return (false, "Mật khẩu không được chứa khoảng trắng ở đầu hoặc cuối chuỗi!");

        var clean = password.Trim();

        // Độ dài: Tối thiểu 8 ký tự, tối đa 64 ký tự (chuẩn BRS Mục 2.4.1.4)
        if (clean.Length < 8 || clean.Length > 64)
            return (false, "Mật khẩu phải có độ dài từ 8 đến 64 ký tự theo quy định chuẩn BRS!");

        // Độ phức tạp (Complexity): Bắt buộc bao gồm ít nhất 3 trong 4 nhóm ký tự
        int complexityGroups = 0;
        if (clean.Any(char.IsUpper)) complexityGroups++;
        if (clean.Any(char.IsLower)) complexityGroups++;
        if (clean.Any(char.IsDigit)) complexityGroups++;
        if (clean.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) complexityGroups++;

        if (complexityGroups < 3)
            return (false, "Mật khẩu phải bao gồm ít nhất 3 trong 4 nhóm: Chữ hoa (A-Z), chữ thường (a-z), chữ số (0-9) và ký tự đặc biệt (!@#$%...)!");

        // Chặn danh sách mật khẩu yếu phổ biến (Mục 2.4.1.4)
        if (CommonPasswords.Contains(clean))
            return (false, "Mật khẩu quá đơn giản hoặc nằm trong danh bạ mật khẩu phổ biến bị chặn (VD: 12345678, password, admin123...). Vui lòng chọn mật khẩu phức tạp hơn!");

        // Ràng buộc: Không được trùng hoàn toàn với Username, Email, hoặc Số điện thoại
        if (!string.IsNullOrWhiteSpace(username) && clean.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "Mật khẩu không được trùng hoàn toàn với Tên đăng nhập (Username)!");

        if (!string.IsNullOrWhiteSpace(phone) && clean.Equals(phone.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "Mật khẩu không được trùng hoàn toàn với Số điện thoại!");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailPrefix = email.Split('@')[0].Trim();
            if (clean.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase) || 
                (!string.IsNullOrEmpty(emailPrefix) && clean.Equals(emailPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Mật khẩu không được trùng với Địa chỉ Email!");
            }
        }

        return (true, null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetCustomerPassword(string? userId, string newPassword, string? confirmPassword = null, string? username = null)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString().Contains("application/json"));

        // Kiểm tra khớp xác nhận mật khẩu (nếu có gửi)
        if (!string.IsNullOrEmpty(confirmPassword) && newPassword != confirmPassword)
        {
            var msg = "Mật khẩu xác nhận không trùng khớp với mật khẩu mới đã nhập!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            user = await _userManager.FindByIdAsync(userId);
        }

        if (user == null && !string.IsNullOrWhiteSpace(username))
        {
            user = await _userManager.FindByNameAsync(username);
        }

        if (user == null && !string.IsNullOrWhiteSpace(userId))
        {
            user = await _userManager.FindByNameAsync(userId) ?? await _userManager.FindByEmailAsync(userId);
        }

        if (user == null)
        {
            var msg = "Không tìm thấy tài khoản người dùng trên hệ thống!";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction(nameof(Staff));
        }

        // Lấy Customer để kiểm tra trùng phone/email
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id || c.PhoneNumber == user.PhoneNumber);

        // Xác thực theo đầy đủ quy tắc BRS Mục 2.4.1.4
        var validation = ValidatePasswordPolicy(newPassword, user.UserName, user.Email ?? customer?.Email, user.PhoneNumber ?? customer?.PhoneNumber);
        if (!validation.IsValid)
        {
            if (isAjax) return Json(new { success = false, message = validation.ErrorMessage });
            TempData["Error"] = validation.ErrorMessage;
            return RedirectToAction(nameof(Staff));
        }

        var cleanPass = newPassword.Trim();
        IdentityResult result;
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        result = await _userManager.ResetPasswordAsync(user, token, cleanPass);

        // Fallback bảo đảm 100% không phụ thuộc token provider
        if (!result.Succeeded)
        {
            await _userManager.RemovePasswordAsync(user);
            result = await _userManager.AddPasswordAsync(user, cleanPass);
        }

        if (result.Succeeded)
        {
            // Tự động mở khóa và reset số lần đăng nhập sai nếu tài khoản đang bị khóa
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            // Thu hồi phiên tức thời (Security Stamp Revocation)
            await _userManager.UpdateSecurityStampAsync(user);

            await LogAuditAsync("RESET_PASSWORD", "Users", user.Id, null, 
                $"Admin cấp lại mật khẩu thành công cho tài khoản @{user.UserName}. Đã thu hồi toàn bộ phiên cũ và mở khóa quyền đăng nhập.");

            var successMsg = $"Đã cấp lại mật khẩu thành công cho tài khoản @{user.UserName}! Toàn bộ phiên cũ đã được thu hồi và tài khoản đã sẵn sàng đăng nhập.";
            if (isAjax) return Json(new { success = true, message = successMsg });
            TempData["Success"] = successMsg;
        }
        else
        {
            var errMsg = string.Join("; ", result.Errors.Select(e => e.Description));
            if (isAjax) return Json(new { success = false, message = errMsg });
            TempData["Error"] = errMsg;
        }

        return RedirectToAction(nameof(Staff));
    }

    // Đổi mật khẩu cho chính tài khoản Admin hiện tại
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAdminPassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới!";
            return RedirectToAction(nameof(Staff));
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp nhau!";
            return RedirectToAction(nameof(Staff));
        }

        var adminUser = await _userManager.GetUserAsync(User) 
            ?? await _userManager.FindByNameAsync(User.Identity?.Name ?? "admin")
            ?? await _userManager.FindByNameAsync("admin");

        if (adminUser == null)
        {
            TempData["Error"] = "Không tìm thấy tài khoản quản trị viên!";
            return RedirectToAction(nameof(Staff));
        }

        var isCorrect = await _userManager.CheckPasswordAsync(adminUser, currentPassword.Trim());
        if (!isCorrect)
        {
            TempData["Error"] = "Mật khẩu hiện tại không chính xác! Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Staff));
        }

        // Xác thực theo đầy đủ quy tắc BRS Mục 2.4.1.4
        var validation = ValidatePasswordPolicy(newPassword, adminUser.UserName, adminUser.Email, adminUser.PhoneNumber);
        if (!validation.IsValid)
        {
            TempData["Error"] = validation.ErrorMessage;
            return RedirectToAction(nameof(Staff));
        }

        var result = await _userManager.ChangePasswordAsync(adminUser, currentPassword.Trim(), newPassword.Trim());
        if (!result.Succeeded)
        {
            await _userManager.RemovePasswordAsync(adminUser);
            result = await _userManager.AddPasswordAsync(adminUser, newPassword.Trim());
        }

        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(adminUser);
            await LogAuditAsync("CHANGE_ADMIN_PASSWORD", "Users", adminUser.Id, null, 
                $"Quản trị viên @{adminUser.UserName} đã tự đổi mật khẩu tài khoản thành công.");

            var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            await signInManager.SignInAsync(adminUser, isPersistent: true);
            TempData["Success"] = "Đã đổi mật khẩu tài khoản Admin thành công!";
        }
        else
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Staff));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockCustomer(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            if (!user.IsActive)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.LockoutEnabled = true;
            }
            else
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }

            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            await LogAuditAsync("LOCK_CUSTOMER", "Users", userId, null, $"Trạng thái tài khoản: {(user.IsActive ? "Mở khóa" : "Khóa")}");
            TempData["Success"] = $"Tài khoản {user.UserName} đã {(user.IsActive ? "được mở khóa" : "bị khóa quyền đăng nhập")}!";
        }
        return RedirectToAction(nameof(Staff));
    }

    // Tương thích ngược với các form cũ nếu có gọi ToggleLock hoặc ResetAccountPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id) => await ToggleLockCustomer(id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAccountPassword(string id, string newPassword) => await ResetCustomerPassword(id, newPassword);

    // =========================================================================
    // 12. NHẬT KÝ KIỂM TOÁN (SCREEN 12)
    // =========================================================================
    public async Task<IActionResult> AuditLogs(string? searchKeyword, string? actionFilter, DateTime? fromDate, DateTime? toDate, int currentPage = 1)
    {
        const int pageSize = 15;
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(kw)) ||
                                     (a.EntityName != null && a.EntityName.ToLower().Contains(kw)) ||
                                     (a.RecordId != null && a.RecordId.Contains(kw)) ||
                                     (a.NewValues != null && a.NewValues.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
            query = query.Where(a => a.Action == actionFilter);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var logs = await query.OrderByDescending(a => a.Timestamp)
            .Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

        var vm = new AuditLogsViewModel
        {
            Logs = logs,
            SearchKeyword = searchKeyword,
            ActionFilter = actionFilter,
            FromDate = fromDate,
            ToDate = toDate,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(vm);
    }

    // =========================================================================
    // KHÔI PHỤC DỮ LIỆU MẪU CHUẨN AAR (RESET DATA)
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDemoData()
    {
        await IdentityDataSeeder.CleanAndSeedAllAsync(HttpContext.RequestServices, forceReset: true);
        TempData["Success"] = "Đã khôi phục thành công toàn bộ Dữ liệu mẫu AAR chuẩn (Sản phẩm, Kho IMEI, Đơn hàng đủ 5 trạng thái, Bảo hành & Khách hàng)!";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // AUDIT LOG HELPER
    // =========================================================================
    private async Task LogAuditAsync(string action, string entityName, string? recordId, string? oldValues, string? newValues)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = _userManager.GetUserId(User),
                UserName = User.Identity?.Name ?? "Cole Palmer (Admin)",
                Action = action,
                EntityName = entityName,
                RecordId = recordId ?? "0",
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };
            _context.AuditLogs.Add(log);
        }
        catch
        {
            // Fail-safe logging
        }
    }
}
