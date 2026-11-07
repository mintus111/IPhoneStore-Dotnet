using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;
    private readonly CartService _cartService;

    public CheckoutModel(IPhoneStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string ShippingAddress { get; set; } = string.Empty;

    [BindProperty]
    public string PaymentMethod { get; set; } = "COD";

    [BindProperty]
    public string? Notes { get; set; }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")) &&
            !User.IsInRole("Customer"))
        {
            TempData["CartError"] = $"Tài khoản nội bộ ({User.Identity.Name}) không được phép thực hiện giao dịch mua hàng. Vui lòng đăng xuất để dùng tài khoản Khách hàng!";
            return RedirectToPage("/Cart");
        }

        CartItems = _cartService.GetCart();
        TotalAmount = _cartService.GetTotalAmount();

        if (CartItems.Count == 0)
        {
            return RedirectToPage("/Cart");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null)
            {
                FullName = customer.FullName;
                PhoneNumber = customer.PhoneNumber;
                Email = customer.Email;
                ShippingAddress = customer.Address;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("Staff") || User.IsInRole("Cashier") || User.IsInRole("Shipper")) &&
            !User.IsInRole("Customer"))
        {
            TempData["CartError"] = "Tài khoản quản trị viên / nhân sự nội bộ không được phép đặt mua hàng!";
            return RedirectToPage("/Cart");
        }

        CartItems = _cartService.GetCart();
        TotalAmount = _cartService.GetTotalAmount();

        if (CartItems.Count == 0)
        {
            return RedirectToPage("/Cart");
        }

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber) || string.IsNullOrWhiteSpace(ShippingAddress))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!";
            return Page();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

        if (customer == null)
        {
            customer = new Customer
            {
                UserId = userId,
                FullName = FullName.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = Email?.Trim(),
                Address = ShippingAddress.Trim(),
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            customer.FullName = FullName.Trim();
            customer.PhoneNumber = PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(Email)) customer.Email = Email.Trim();
            customer.Address = ShippingAddress.Trim();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Kiểm tra tồn kho thực tế qua số mã IMEI InStock sẵn sàng trong kho
            var reservedImeisForOrder = new Dictionary<int, List<ProductImei>>();

            foreach (var item in CartItems)
            {
                var p = await _context.Products.FindAsync(item.ProductId);
                if (p == null || !p.IsActive)
                {
                    await transaction.RollbackAsync();
                    ErrorMessage = $"Sản phẩm '{item.ProductName}' không còn kinh doanh!";
                    return Page();
                }

                // Lấy danh sách IMEI thực tế có sẵn (Status = 'InStock')
                var availableImeis = await _context.ProductImeis
                    .Where(pi => pi.ProductId == item.ProductId && pi.Status == "InStock")
                    .Take(item.Quantity)
                    .ToListAsync();

                if (availableImeis.Count < item.Quantity)
                {
                    // Tự động đồng bộ lại Products.Quantity thực tế trong CSDL
                    p.Quantity = await _context.ProductImeis.CountAsync(pi => pi.ProductId == item.ProductId && pi.Status == "InStock");
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();

                    ErrorMessage = $"Sản phẩm '{p.Name}' hiện không đủ mã IMEI trong kho để bán (Chỉ còn {availableImeis.Count} máy sẵn sàng)!";
                    return Page();
                }

                p.Quantity -= item.Quantity;
                reservedImeisForOrder[item.ProductId] = availableImeis;
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                OrderDate = DateTime.Now,
                TotalAmount = TotalAmount,
                OrderStatus = "Pending",
                PaymentMethod = PaymentMethod,
                ShippingAddress = ShippingAddress.Trim(),
                Notes = Notes?.Trim()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            order.OrderCode = $"#IPS{order.Id:D6}";
            await _context.SaveChangesAsync();

            foreach (var item in CartItems)
            {
                var orderDetail = new OrderDetail
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
                    orderDetail.ProductImeiId = firstImei.Id;
                    orderDetail.ImeiNumber = firstImei.Imei;

                    foreach (var imei in imeis)
                    {
                        imei.Status = "Reserved";
                        imei.OrderId = order.Id;
                    }
                }

                _context.OrderDetails.Add(orderDetail);
            }

            // Ghi AuditLog
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = User.Identity?.Name,
                Action = "PLACE_ORDER",
                EntityName = "Orders",
                RecordId = order.Id.ToString(),
                NewValues = $"{{\"OrderId\":{order.Id},\"PaymentMethod\":\"{order.PaymentMethod}\",\"TotalAmount\":{order.TotalAmount}}}",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _cartService.ClearCart();
            return RedirectToPage("/OrderSuccess", new { orderId = order.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            ErrorMessage = "Số lượng tồn kho vừa có biến động do khách hàng khác đặt mua. Vui lòng thử lại!";
            return Page();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ErrorMessage = "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại!";
            return Page();
        }
    }
}
