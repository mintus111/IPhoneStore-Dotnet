using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;
using IPhoneStore_Mvc.Services;

namespace IPhoneStore_Mvc.Controllers;

public class CartController : Controller
{
    private readonly IPhoneStoreDbContext _context;
    private readonly CartService _cartService;

    public CartController(IPhoneStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    // GET: /Cart
    public IActionResult Index()
    {
        var items = _cartService.GetCart();
        ViewBag.TotalAmount = _cartService.GetTotalAmount();
        return View(items);
    }

    // POST: /Cart/AddToCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
            return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!" });

        var result = _cartService.AddToCart(product, quantity);
        return Json(new { success = result.Success, message = result.Message, cartCount = _cartService.GetCount() });
    }

    // POST: /Cart/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity)
    {
        _cartService.UpdateQuantity(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        _cartService.RemoveFromCart(productId);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cartService.ClearCart();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Cart/Checkout (BẮT BUỘC ĐĂNG NHẬP / ĐĂNG KÝ ĐỂ ĐẶT HÀNG & BẢO HÀNH)
    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        // Chặn tài khoản nội bộ (Admin, Staff...) không được thực hiện giao dịch mua hàng theo chuẩn AAR
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")) &&
            !User.IsInRole("Customer"))
        {
            TempData["CartError"] = $"Tài khoản nội bộ ({User.Identity.Name}) không được phép mua hàng. Vui lòng đăng xuất và dùng tài khoản Khách hàng để thanh toán!";
            return RedirectToAction(nameof(Index));
        }

        var items = _cartService.GetCart();
        if (items.Count == 0) return RedirectToAction(nameof(Index));

        ViewBag.CartItems = items;
        ViewBag.TotalAmount = _cartService.GetTotalAmount();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null)
            {
                ViewBag.FullName = customer.FullName;
                ViewBag.PhoneNumber = customer.PhoneNumber;
                ViewBag.Email = customer.Email;
                ViewBag.ShippingAddress = customer.Address;
            }
        }

        return View();
    }

    // POST: /Cart/PlaceOrder (BẮT BUỘC ĐĂNG NHẬP & TRANSACTION AN TOÀN)
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string fullName, string phoneNumber, string? email,
        string shippingAddress, string paymentMethod = "COD", string? notes = null)
    {
        // Chặn tài khoản nội bộ không được tạo đơn hàng
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")) &&
            !User.IsInRole("Customer"))
        {
            TempData["CartError"] = "Tài khoản quản trị viên / nhân sự nội bộ không được phép đặt mua hàng!";
            return RedirectToAction(nameof(Index));
        }

        var items = _cartService.GetCart();
        if (items.Count == 0) return RedirectToAction(nameof(Index));

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            TempData["CheckoutError"] = "Vui lòng nhập đầy đủ thông tin người nhận!";
            return RedirectToAction(nameof(Checkout));
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            customer = new Customer
            {
                UserId = userId,
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Email = email?.Trim(),
                Address = shippingAddress.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            customer.FullName = fullName.Trim();
            customer.PhoneNumber = phoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(email)) customer.Email = email.Trim();
            customer.Address = shippingAddress.Trim();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra tồn kho thực tế qua số mã IMEI InStock (Anti-Overselling Concurrency Check)
            var reservedImeisForOrder = new Dictionary<int, List<ProductImei>>();

            foreach (var item in items)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod == null || !prod.IsActive)
                {
                    await transaction.RollbackAsync();
                    TempData["CheckoutError"] = $"Sản phẩm '{item.ProductName}' không còn kinh doanh!";
                    return RedirectToAction(nameof(Checkout));
                }

                // Lấy danh sách IMEI có sẵn trong kho (Status = 'InStock')
                var availableImeis = await _context.ProductImeis
                    .Where(pi => pi.ProductId == item.ProductId && pi.Status == "InStock")
                    .Take(item.Quantity)
                    .ToListAsync();

                if (availableImeis.Count < item.Quantity)
                {
                    // Tự động đồng bộ lại Products.Quantity thực tế trong CSDL
                    prod.Quantity = await _context.ProductImeis.CountAsync(pi => pi.ProductId == item.ProductId && pi.Status == "InStock");
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();

                    TempData["CheckoutError"] = $"Sản phẩm '{prod.Name}' hiện không đủ mã IMEI trong kho để bán (Chỉ còn {availableImeis.Count} máy sẵn sàng)!";
                    return RedirectToAction(nameof(Checkout));
                }

                // Khấu trừ số lượng tồn kho
                prod.Quantity -= item.Quantity;
                reservedImeisForOrder[item.ProductId] = availableImeis;
            }

            // 2. Tạo Đơn hàng mới
            var order = new Order
            {
                CustomerId = customer.Id,
                OrderDate = DateTime.Now,
                TotalAmount = _cartService.GetTotalAmount(),
                OrderStatus = "Pending",
                PaymentMethod = paymentMethod,
                ShippingAddress = shippingAddress.Trim(),
                Notes = notes?.Trim()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 3. Tạo chi tiết đơn hàng và giữ chỗ IMEI cho đơn hàng (Reserved)
            foreach (var item in items)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    TotalPrice = item.TotalPrice
                };

                // Giữ chỗ các mã IMEI thực tế cho đơn hàng này để Admin có sẵn IMEI gán xuất kho
                if (reservedImeisForOrder.TryGetValue(item.ProductId, out var imeis) && imeis.Count > 0)
                {
                    var firstImei = imeis.First();
                    detail.ProductImeiId = firstImei.Id;
                    detail.ImeiNumber = firstImei.Imei;

                    foreach (var imei in imeis)
                    {
                        imei.Status = "Reserved";
                        imei.OrderId = order.Id;
                    }
                }

                _context.OrderDetails.Add(detail);
            }

            // 4. Ghi AuditLog
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = User.Identity?.Name,
                Action = "PLACE_ORDER",
                EntityName = "Orders",
                RecordId = order.Id.ToString(),
                NewValues = $"{{\"OrderId\":{order.Id},\"TotalAmount\":{order.TotalAmount},\"PaymentMethod\":\"{order.PaymentMethod}\"}}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _cartService.ClearCart();
            return RedirectToAction("Success", new { orderId = order.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            TempData["CheckoutError"] = "Số lượng tồn kho vừa có biến động do khách hàng khác đặt mua cùng lúc. Vui lòng thử lại!";
            return RedirectToAction(nameof(Checkout));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["CheckoutError"] = "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại!";
            return RedirectToAction(nameof(Checkout));
        }
    }

    // GET: /Cart/Success
    public async Task<IActionResult> Success(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return RedirectToAction("Index", "Home");
        return View(order);
    }
}
