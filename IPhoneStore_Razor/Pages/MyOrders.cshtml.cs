using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages;

public class MyOrdersModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public MyOrdersModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = new();
    public bool IsGuest { get; set; }
    public Customer? Customer { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || User.Identity?.IsAuthenticated != true)
        {
            IsGuest = true;
            return Page();
        }

        Customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (Customer != null)
        {
            Orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductImei)
                .Where(o => o.CustomerId == Customer.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            return RedirectToPage("/Account/Login");
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

        if (order != null && (order.OrderStatus == "Pending" || order.OrderStatus == "Chờ duyệt"))
        {
            var oldStatus = order.OrderStatus;
            var oldPayment = order.PaymentStatus;

            order.OrderStatus = "Cancelled";
            order.Notes = string.IsNullOrEmpty(order.Notes)
                ? "[Khách hàng hủy đơn]"
                : $"{order.Notes} | [Khách hàng hủy đơn]";

            // Yêu cầu 2: Nếu đã thanh toán trước (VietQR/Thẻ) -> Chuyển sang Chờ hoàn tiền
            // Nếu chưa thanh toán (COD) -> Giữ nguyên Chưa thanh toán
            if (oldPayment is "Đã thanh toán" or "Paid")
            {
                order.PaymentStatus = "Chờ hoàn tiền";
            }
            else
            {
                order.PaymentStatus = "Chưa thanh toán";
            }

            // Hoàn lại tồn kho Product.Quantity
            foreach (var item in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    prod.Quantity += item.Quantity;
                }
            }

            // Giải phóng mã IMEI giữ chỗ
            var reservedImeis = await _context.ProductImeis
                .Where(pi => pi.OrderId == order.Id && (pi.Status == "Reserved" || pi.Status == "Sold" || pi.Status == "InStock"))
                .ToListAsync();

            foreach (var imei in reservedImeis)
            {
                imei.Status = "InStock";
                imei.OrderId = null;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = User.Identity?.Name ?? customer.FullName,
                Action = "CANCEL_ORDER",
                EntityName = "Orders",
                RecordId = order.Id.ToString(),
                OldValues = $"{{\"OrderStatus\":\"{oldStatus}\",\"PaymentStatus\":\"{oldPayment}\"}}",
                NewValues = $"{{\"OrderStatus\":\"Cancelled\",\"PaymentStatus\":\"{order.PaymentStatus}\",\"Reason\":\"Customer Cancelled\"}}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            if (order.PaymentStatus == "Chờ hoàn tiền")
            {
                TempData["SuccessMessage"] = $"Đã hủy thành công đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"}. Đơn đã chuyển sang 'Chờ hoàn tiền', nhân viên CSKH & kế toán sẽ liên hệ hoàn tiền cho bạn sớm nhất!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã hủy thành công đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"}. Số lượng máy và mã giữ kho đã được hoàn trả!";
            }
        }

        return RedirectToPage();
    }
}
