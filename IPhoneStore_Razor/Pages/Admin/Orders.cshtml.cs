using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff,Cashier,Shipper")]
public class OrdersModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public OrdersModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = new();

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int ShippingCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        TotalCount = await _context.Orders.CountAsync();
        PendingCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ duyệt");
        ConfirmedCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
        ShippingCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
        CompletedCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất" || o.OrderStatus == "Hoàn thành");
        CancelledCount = await _context.Orders.CountAsync(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");

        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductImei)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            if (StatusFilter == "Pending")
                query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ duyệt");
            else if (StatusFilter == "Confirmed")
                query = query.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
            else if (StatusFilter == "Shipping")
                query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao");
            else if (StatusFilter == "Completed")
                query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn tất" || o.OrderStatus == "Hoàn thành");
            else if (StatusFilter == "Cancelled")
                query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
            else
                query = query.Where(o => o.OrderStatus == StatusFilter);
        }

        Orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus, string? paymentStatus)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductImei)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        // 1. Kiểm tra trạng thái kết thúc (Terminal State): Completed / Cancelled
        if (order.OrderStatus is "Completed" or "Cancelled" or "Hoàn tất" or "Đã hủy")
        {
            ErrorMessage = "Đơn hàng đã ở trạng thái kết thúc, không thể chỉnh sửa!";
            return RedirectToPage(new { StatusFilter });
        }

        var currentNorm = NormalizeStatus(order.OrderStatus);
        var targetNorm = NormalizeStatus(newStatus);

        // 2. Kiểm tra State Machine luồng hợp lệ:
        if (currentNorm != targetNorm)
        {
            bool isValidTransition = (currentNorm, targetNorm) switch
            {
                ("Pending", "Confirmed" or "Cancelled") => true,
                ("Confirmed", "Shipping" or "Cancelled") => true,
                ("Shipping", "Completed" or "Cancelled") => true,
                _ => false
            };

            if (!isValidTransition)
            {
                ErrorMessage = $"Chuyển trạng thái từ '{order.OrderStatus}' sang '{newStatus}' không hợp lệ theo quy trình State Machine!";
                return RedirectToPage(new { StatusFilter });
            }
        }

        // 3. Yêu cầu 4: Kiểm tra ràng buộc IMEI trước khi sang Shipping hoặc Completed
        if (targetNorm is "Shipping" or "Completed")
        {
            bool allImeisAssigned = order.OrderDetails.All(od => od.ProductImeiId.HasValue && od.ProductImeiId > 0);
            if (!allImeisAssigned)
            {
                ErrorMessage = "Không thể chuyển trạng thái sang Đang giao hoặc Hoàn tất khi chưa gán đủ 100% mã IMEI cho toàn bộ máy trong đơn hàng!";
                return RedirectToPage(new { StatusFilter });
            }
        }

        // 4. Yêu cầu 3: Khóa không cho chọn "Chờ hoàn tiền" & "Đã hoàn tiền" khi đơn đang hoạt động
        if (targetNorm != "Cancelled" && (paymentStatus is "Chờ hoàn tiền" or "Đã hoàn tiền"))
        {
            ErrorMessage = "Chỉ đơn hàng đã hủy mới được phép chọn trạng thái Chờ hoàn tiền hoặc Đã hoàn tiền!";
            return RedirectToPage(new { StatusFilter });
        }

        var now = DateTime.Now;
        var oldStatus = order.OrderStatus;
        var oldPayment = order.PaymentStatus;
        order.OrderStatus = targetNorm;

        // 5. Yêu cầu 1: Ràng buộc Đơn hoàn tất bắt buộc phải "Đã thanh toán"
        if (targetNorm == "Completed")
        {
            order.PaymentStatus = "Đã thanh toán";

            // Tự động kích hoạt bảo hành điện tử Apple Care nếu có máy gán IMEI
            foreach (var od in order.OrderDetails)
            {
                if (od.ProductImei != null || od.ProductImeiId.HasValue)
                {
                    var existingWarranty = await _context.Warranties.FirstOrDefaultAsync(w => w.OrderDetailId == od.Id);
                    if (existingWarranty == null)
                    {
                        var imeiObj = od.ProductImei ?? await _context.ProductImeis.FindAsync(od.ProductImeiId);
                        if (imeiObj != null)
                        {
                            var months = od.Product?.WarrantyMonths ?? 12;
                            _context.Warranties.Add(new Warranty
                            {
                                Imei = imeiObj.Imei,
                                SerialNumber = imeiObj.SerialNumber,
                                ProductId = od.ProductId,
                                CustomerId = order.CustomerId,
                                OrderDetailId = od.Id,
                                StartDate = now,
                                EndDate = now.AddMonths(months),
                                WarrantyType = $"Chính hãng Apple {months} tháng",
                                Status = "Active",
                                Notes = $"Kích hoạt tự động khi đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"} hoàn thành",
                                CreatedAt = now
                            });
                        }
                    }
                }
            }
        }
        // 6. Yêu cầu 2: Xử lý Hủy đơn hàng giữa "Khách đã thanh toán" vs "Khách chưa thanh toán"
        else if (targetNorm == "Cancelled")
        {
            if (oldPayment is "Đã thanh toán" or "Paid" || paymentStatus is "Chờ hoàn tiền" or "Đã hoàn tiền")
            {
                order.PaymentStatus = (paymentStatus == "Đã hoàn tiền") ? "Đã hoàn tiền" : "Chờ hoàn tiền";
            }
            else
            {
                order.PaymentStatus = "Chưa thanh toán";
            }

            // Hoàn trả số lượng tồn kho Product.Quantity
            foreach (var od in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(od.ProductId);
                if (prod != null)
                {
                    prod.Quantity += od.Quantity;
                }
            }

            // Giải phóng mã IMEI đã gán hoặc giữ kho về InStock
            var assignedImeis = await _context.ProductImeis.Where(pi => pi.OrderId == order.Id).ToListAsync();
            foreach (var imei in assignedImeis)
            {
                if (imei.Status is "Reserved" or "Sold")
                {
                    imei.Status = "InStock";
                    imei.OrderId = null;
                }
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                order.PaymentStatus = paymentStatus;
            }
        }

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "UPDATE_ORDER_STATUS",
            EntityName = "Orders",
            RecordId = order.Id.ToString(),
            OldValues = $"Status={oldStatus}, PaymentStatus={oldPayment}",
            NewValues = $"Status={targetNorm}, PaymentStatus={order.PaymentStatus}",
            UserName = User.Identity?.Name ?? "Admin",
            Timestamp = now
        });

        await _context.SaveChangesAsync();
        SuccessMessage = $"Cập nhật trạng thái đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"} thành: {targetNorm} (Thanh toán: {order.PaymentStatus})";
        return RedirectToPage(new { StatusFilter });
    }

    public static string NormalizeStatus(string? status)
    {
        return status switch
        {
            "Chờ duyệt" or "Pending" => "Pending",
            "Đã duyệt" or "Đã xác nhận" or "Confirmed" => "Confirmed",
            "Đang giao" or "Đang giao hàng" or "Shipping" => "Shipping",
            "Hoàn tất" or "Hoàn thành" or "Completed" => "Completed",
            "Đã hủy" or "Cancelled" => "Cancelled",
            _ => status ?? "Pending"
        };
    }
}
