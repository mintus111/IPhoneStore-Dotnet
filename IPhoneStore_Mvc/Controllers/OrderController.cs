using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;

using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IPhoneStoreDbContext _context;

    public OrderController(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> MyOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || User.Identity?.IsAuthenticated != true)
        {
            ViewBag.IsGuest = true;
            return View(new List<IPhoneStore_Mvc.Models.Entities.Order>());
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        ViewBag.Customer = customer;
        if (customer == null)
        {
            return View(new List<IPhoneStore_Mvc.Models.Entities.Order>());
        }

        var orders = await _context.Orders
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails).ThenInclude(od => od.ProductImei)
            .Include(o => o.ProductImeis)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) return RedirectToAction(nameof(MyOrders));

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

        if (order != null && (order.OrderStatus == "Pending" || order.OrderStatus == "Chờ duyệt"))
        {
            order.OrderStatus = "Cancelled";
            if (order.PaymentStatus == "Đã thanh toán")
            {
                order.PaymentStatus = "Chờ hoàn tiền";
            }
            order.Notes = string.IsNullOrEmpty(order.Notes)
                ? "[Khách hàng hủy đơn]"
                : $"{order.Notes} | [Khách hàng hủy đơn]";

            // 1. Hoàn lại số lượng tồn kho sản phẩm & giải phóng IMEI nếu có
            foreach (var detail in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(detail.ProductId);
                if (prod != null)
                {
                    prod.Quantity += detail.Quantity;
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

            // 2. Ghi nhật ký AuditLogs
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = User.Identity?.Name ?? customer.FullName,
                Action = "CANCEL_ORDER",
                EntityName = "Orders",
                RecordId = order.Id.ToString(),
                OldValues = "{\"OrderStatus\":\"Pending\"}",
                NewValues = "{\"OrderStatus\":\"Cancelled\",\"Reason\":\"Customer Cancelled\"}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã hủy thành công đơn hàng #IPS{order.Id:D6}. Toàn bộ số lượng máy đã được hoàn trả về kho!";
        }
        else
        {
            TempData["Error"] = "Đơn hàng này không thể hủy vì đã được xác nhận, đang vận chuyển hoặc đã bị hủy trước đó.";
        }

        return RedirectToAction(nameof(MyOrders));
    }
}
