using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Models.ViewModels;

public class InventoryViewModel
{
    public int InStockCount { get; set; }
    public int ReservedCount { get; set; }
    public int SoldCount { get; set; }
    public int DefectiveCount { get; set; }
    public List<ProductImei> Imeis { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();
    public List<Product> AllProducts { get; set; } = new();
    public string? SkuFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? SearchKeyword { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
}

public class BatchImportViewModel
{
    public List<Product> Products { get; set; } = new();
    public int SelectedProductId { get; set; }
    public string Vendor { get; set; } = "Synnex FPT";
    public string? Note { get; set; }
    public string ImeisText { get; set; } = string.Empty;
}

public class OrderDetailViewModel
{
    public Order Order { get; set; } = null!;
    public List<ProductImei> AvailableImeis { get; set; } = new();
}

public class WarrantyDashboardViewModel
{
    public int ActiveWarrantiesCount { get; set; }
    public int PendingClaimsCount { get; set; }
    public int ReplacedCountMonth { get; set; }
    public decimal WarrantyRatio { get; set; } = 1.4m;
    public List<Warranty> Warranties { get; set; } = new();
    public List<WarrantyClaim> Claims { get; set; } = new();
    public string? ActiveTab { get; set; } = "claims"; // claims, book
}

public class WarrantyClaimDetailViewModel
{
    public WarrantyClaim Claim { get; set; } = null!;
    public Warranty Warranty { get; set; } = null!;
    public List<ProductImei> AvailableReplacementImeis { get; set; } = new();
}

public class StaffManagementViewModel
{
    public List<ApplicationUser> StaffMembers { get; set; } = new();
    public Dictionary<string, string> UserRoles { get; set; } = new();
}

public class CustomerItemViewModel
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasAccount => !string.IsNullOrEmpty(UserId);
    public bool IsActive { get; set; } = true;
    public int OrderCount { get; set; }
    public int WarrantyCount { get; set; }
}

public class CustomerManagementViewModel
{
    public List<CustomerItemViewModel> Customers { get; set; } = new();
    public string? SearchKeyword { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveAccounts { get; set; }
    public int LockedAccounts { get; set; }
    public int GuestCustomers { get; set; }
    public int TotalOrders { get; set; }
}

public class AuditLogsViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string? SearchKeyword { get; set; }
    public string? ActionFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
}
