using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff,Cashier,Shipper")]
public class OrderDetailModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public OrderDetailModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public OrderDetailViewModel Vm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductImei)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var productIds = order.OrderDetails.Select(od => od.ProductId).Distinct().ToList();
        var availableImeis = await _context.ProductImeis
            .Where(pi => productIds.Contains(pi.ProductId) && 
                        (pi.Status == "InStock" || (pi.Status == "Reserved" && pi.OrderId == order.Id)))
            .ToListAsync();

        Vm = new OrderDetailViewModel
        {
            Order = order,
            AvailableImeis = availableImeis
        };

        return Page();
    }

    public async Task<IActionResult> OnPostFulfillOrderAsync(
        int orderId, 
        Dictionary<int, int> itemImeis, 
        string shippingUnit, 
        string trackingNumber)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductImei)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        // 1. Kiểm tra trạng thái kết thúc (Terminal State)
        if (order.OrderStatus is "Completed" or "Cancelled" or "Hoàn tất" or "Đã hủy")
        {
            ErrorMessage = "Đơn hàng đã ở trạng thái kết thúc, không thể chỉnh sửa!";
            return RedirectToPage(new { id = orderId });
        }

        // 2. Kiểm tra quy trình xuất kho State Machine (chỉ được xuất kho khi Pending hoặc Confirmed)
        var currentNorm = NormalizeStatus(order.OrderStatus);
        if (currentNorm is not ("Pending" or "Confirmed"))
        {
            ErrorMessage = $"Đơn hàng đang ở trạng thái '{order.OrderStatus}', không thể xuất kho!";
            return RedirectToPage(new { id = orderId });
        }

        // 3. Yêu cầu 4: Kiểm tra trùng lặp IMEI và đảm bảo gán đủ 100% IMEI cho toàn bộ sản phẩm
        if (itemImeis != null)
        {
            var selectedIds = itemImeis.Values.Where(id => id > 0).ToList();
            if (selectedIds.Count != selectedIds.Distinct().Count())
            {
                ErrorMessage = "Phát hiện mã IMEI bị trùng lặp giữa các sản phẩm trong đơn!";
                return RedirectToPage(new { id = orderId });
            }
        }

        foreach (var od in order.OrderDetails)
        {
            bool hasAssigned = od.ProductImeiId.HasValue && od.ProductImeiId > 0;
            bool willAssign = itemImeis != null && itemImeis.TryGetValue(od.Id, out var imeiId) && imeiId > 0;
            if (!hasAssigned && !willAssign)
            {
                ErrorMessage = "Chưa gán đủ 100% mã IMEI cho tất cả sản phẩm trong đơn hàng. Bắt buộc phải gán đủ IMEI trước khi xuất kho sang Đang giao!";
                return RedirectToPage(new { id = orderId });
            }
        }

        var now = DateTime.Now;

        foreach (var od in order.OrderDetails)
        {
            if (itemImeis != null && itemImeis.TryGetValue(od.Id, out var imeiId) && imeiId > 0)
            {
                var imei = await _context.ProductImeis.FindAsync(imeiId);
                if (imei != null)
                {
                    od.ProductImeiId = imeiId;
                    od.ImeiNumber = imei.Imei;
                    imei.Status = "Sold";
                    imei.OrderId = order.Id;
                    imei.ExportDate = now;

                    var prod = od.Product;
                    var months = prod?.WarrantyMonths ?? 12;

                    _context.Warranties.Add(new Warranty
                    {
                        Imei = imei.Imei,
                        SerialNumber = imei.SerialNumber,
                        ProductId = od.ProductId,
                        CustomerId = order.CustomerId,
                        OrderDetailId = od.Id,
                        StartDate = now,
                        EndDate = now.AddMonths(months),
                        WarrantyType = $"Chính hãng Apple {months} tháng",
                        Status = "Active",
                        Notes = $"Kích hoạt tự động theo đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"}",
                        CreatedAt = now
                    });
                }
            }
        }

        var oldStatus = order.OrderStatus;
        order.OrderStatus = "Shipping";
        order.ShippingUnit = string.IsNullOrWhiteSpace(shippingUnit) ? "Viettel Post" : shippingUnit;
        order.TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? $"VNPOST{order.Id}{now:mmss}" : trackingNumber;
        order.EstimatedDeliveryDate = now.AddDays(2);

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "FULFILL_ORDER",
            EntityName = "Orders",
            RecordId = order.Id.ToString(),
            OldValues = $"Status={oldStatus}",
            NewValues = $"Status=Shipping, Đơn vị={order.ShippingUnit}, Mã vận đơn={order.TrackingNumber}",
            UserName = User.Identity?.Name ?? "Admin",
            Timestamp = now
        });

        await _context.SaveChangesAsync();
        SuccessMessage = $"Đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"} đã được gán số IMEI, xuất kho và bàn giao đơn vị vận chuyển {order.ShippingUnit}!";
        return RedirectToPage(new { id = orderId });
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

        // 1. Kiểm tra trạng thái kết thúc (Terminal State)
        if (order.OrderStatus is "Completed" or "Cancelled" or "Hoàn tất" or "Đã hủy")
        {
            ErrorMessage = "Đơn hàng đã ở trạng thái kết thúc, không thể chỉnh sửa!";
            return RedirectToPage(new { id = orderId });
        }

        var currentNorm = NormalizeStatus(order.OrderStatus);
        var targetNorm = NormalizeStatus(newStatus);

        // 2. Kiểm tra State Machine:
        // Pending -> Confirmed hoặc Cancelled
        // Confirmed -> Shipping hoặc Cancelled
        // Shipping -> Completed hoặc Cancelled
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
                return RedirectToPage(new { id = orderId });
            }
        }

        // 3. Yêu cầu 4: Kiểm tra ràng buộc IMEI trước khi sang Shipping hoặc Completed
        if (targetNorm is "Shipping" or "Completed")
        {
            bool allImeisAssigned = order.OrderDetails.All(od => od.ProductImeiId.HasValue && od.ProductImeiId > 0);
            if (!allImeisAssigned)
            {
                ErrorMessage = "Không thể chuyển trạng thái sang Đang giao hoặc Hoàn tất khi chưa gán đủ 100% mã IMEI cho toàn bộ máy trong đơn hàng!";
                return RedirectToPage(new { id = orderId });
            }
        }

        // 4. Yêu cầu 3: Khóa không cho chọn "Chờ hoàn tiền" & "Đã hoàn tiền" khi đơn đang hoạt động
        if (targetNorm != "Cancelled" && (paymentStatus is "Chờ hoàn tiền" or "Đã hoàn tiền"))
        {
            ErrorMessage = "Chỉ đơn hàng đã hủy mới được phép chọn trạng thái Chờ hoàn tiền hoặc Đã hoàn tiền!";
            return RedirectToPage(new { id = orderId });
        }

        var now = DateTime.Now;
        var oldStatus = order.OrderStatus;
        var oldPayment = order.PaymentStatus;
        order.OrderStatus = targetNorm;

        // 5. Yêu cầu 1: Ràng buộc Đơn hoàn tất bắt buộc phải "Đã thanh toán"
        if (targetNorm == "Completed")
        {
            order.PaymentStatus = "Đã thanh toán";

            // Tự động kích hoạt bảo hành điện tử nếu có máy gán IMEI
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
                                Notes = $"Kích hoạt tự động khi đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"} hoàn tất giao hàng",
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

            // Hoàn lại số lượng tồn kho Product.Quantity
            foreach (var od in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(od.ProductId);
                if (prod != null)
                {
                    prod.Quantity += od.Quantity;
                }
            }

            // Giải phóng mã IMEI về trạng thái InStock nếu hủy đơn
            var orderImeis = await _context.ProductImeis.Where(pi => pi.OrderId == order.Id).ToListAsync();
            foreach (var imei in orderImeis)
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
        SuccessMessage = $"Cập nhật trạng thái đơn hàng {order.OrderCode ?? $"#IPS{order.Id:D6}"} thành công: {targetNorm} (Thanh toán: {order.PaymentStatus})!";
        return RedirectToPage(new { id = orderId });
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
