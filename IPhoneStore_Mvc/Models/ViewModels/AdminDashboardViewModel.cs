using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Models.ViewModels;

public class AdminDashboardViewModel
{
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

    // Kho hàng & Cảnh báo tồn
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public List<Product> LowStockProducts { get; set; } = new();

    // Thống kê IMEI toàn hệ thống
    public int TotalImeis { get; set; }
    public int InStockImeis { get; set; }
    public int ReservedImeis { get; set; }
    public int SoldImeis { get; set; }
    public int DefectiveImeis { get; set; }

    // Người dùng & Bảo hành
    public int TotalCustomers { get; set; }
    public int ActiveWarranties { get; set; }

    // Sản phẩm bán chạy nhất
    public List<BestSellerItem> TopSellingProducts { get; set; } = new();

    // Đơn hàng gần đây
    public List<Order> RecentOrders { get; set; } = new();

    // Nhật ký kiểm toán gần đây
    public List<AuditLog> RecentAuditLogs { get; set; } = new();

    // Danh sách sản phẩm cho Dropdown Nhập lô IMEI
    public List<Product> AllActiveProducts { get; set; } = new();
}

public class BestSellerItem
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
