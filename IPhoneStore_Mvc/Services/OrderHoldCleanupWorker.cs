using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Services;

/// <summary>
/// Background Service tự động quét và giải phóng đơn hàng Pending quá 15 phút không thanh toán
/// (Chuẩn nghiệp vụ bán lẻ Thế Giới Di Động / TopZone / FPT Shop)
/// </summary>
public class OrderHoldCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderHoldCleanupWorker> _logger;

    public OrderHoldCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<OrderHoldCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation(">>> OrderHoldCleanupWorker đã khởi động thành công (Quét định kỳ mỗi 60 giây).");
        }
        catch
        {
            Console.WriteLine(">>> OrderHoldCleanupWorker đã khởi động thành công (Quét định kỳ mỗi 60 giây).");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredOrdersAsync(stoppingToken);
                // Chờ 60 giây trước lượt quét tiếp theo
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Thoát êm khi ứng dụng dừng (Graceful Shutdown)
                break;
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong tiến trình quét đơn hàng quá hạn của OrderHoldCleanupWorker.");
                }
                catch
                {
                    Console.WriteLine($"[OrderHoldCleanupWorker Error]: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task CleanupExpiredOrdersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IPhoneStoreDbContext>();

        var timeoutThreshold = DateTime.Now.AddMinutes(-15);

        // Tìm các đơn hàng Pending, thanh toán trực tuyến (không phải COD) quá hạn 15 phút
        var expiredOrders = await context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.OrderStatus == "Pending" && o.PaymentMethod != "COD" && o.OrderDate < timeoutThreshold)
            .ToListAsync(stoppingToken);

        if (expiredOrders.Count == 0) return;

        _logger.LogInformation("Phát hiện {Count} đơn hàng quá hạn 15 phút cần hủy và hoàn kho tự động.", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            order.OrderStatus = "Cancelled";
            order.Notes = string.IsNullOrEmpty(order.Notes)
                ? "[Hủy tự động bởi Hệ thống]: Quá hạn 15 phút giữ chỗ chưa thanh toán."
                : $"{order.Notes} | [Hủy tự động bởi Hệ thống]: Quá hạn 15 phút giữ chỗ chưa thanh toán.";

            // 1. Hoàn lại số lượng tồn kho Products
            foreach (var detail in order.OrderDetails)
            {
                var product = await context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.Quantity += detail.Quantity;
                }
            }

            // 2. Thu hồi các mã IMEI đang bị giữ chỗ (Reserved) về lại kho (InStock)
            var reservedImeis = await context.ProductImeis
                .Where(pi => pi.OrderId == order.Id && pi.Status == "Reserved")
                .ToListAsync(stoppingToken);

            foreach (var imei in reservedImeis)
            {
                imei.Status = "InStock";
                imei.OrderId = null;
            }

            // 3. Ghi vết vào AuditLogs
            context.AuditLogs.Add(new AuditLog
            {
                UserId = null,
                UserName = "OrderHoldCleanupWorker",
                Action = "SYSTEM_CLEANUP",
                EntityName = "Orders",
                RecordId = order.Id.ToString(),
                NewValues = $"{{\"OrderId\":{order.Id},\"Action\":\"AutoCancel\",\"ReleasedImeis\":{reservedImeis.Count}}}",
                IpAddress = "127.0.0.1",
                Timestamp = DateTime.Now
            });

            _logger.LogInformation("Đã hủy đơn #{OrderId}, hoàn lại tồn kho và giải phóng {Imeis} IMEI.", order.Id, reservedImeis.Count);
        }

        await context.SaveChangesAsync(stoppingToken);
    }
}
