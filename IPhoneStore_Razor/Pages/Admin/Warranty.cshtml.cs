using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff")]
public class WarrantyModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public WarrantyModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public WarrantyDashboardViewModel Vm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? tab)
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var activeCount = await _context.Warranties.CountAsync(w => w.Status == "Active" && w.EndDate >= now);
        var pendingClaims = await _context.WarrantyClaims.CountAsync(c => c.Status == "Pending" || c.Status == "Tiếp nhận");
        var replacedMonth = await _context.WarrantyClaims.CountAsync(c => c.ActionTaken == "Replace" && c.ClaimDate >= startOfMonth);

        var claims = await _context.WarrantyClaims
            .Include(c => c.Warranty)
                .ThenInclude(w => w!.Product)
            .Include(c => c.Warranty)
                .ThenInclude(w => w!.Customer)
            .OrderByDescending(c => c.ClaimDate)
            .ToListAsync();

        var warranties = await _context.Warranties
            .Include(w => w.Customer)
            .Include(w => w.Product)
            .OrderByDescending(w => w.CreatedAt)
            .Take(50)
            .ToListAsync();

        Vm = new WarrantyDashboardViewModel
        {
            ActiveWarrantiesCount = activeCount,
            PendingClaimsCount = pendingClaims,
            ReplacedCountMonth = replacedMonth,
            WarrantyRatio = 1.2m,
            Claims = claims,
            Warranties = warranties,
            ActiveTab = tab ?? "claims"
        };

        return Page();
    }

    public async Task<IActionResult> OnGetCheckImeiAsync(string imei)
    {
        if (string.IsNullOrWhiteSpace(imei))
            return new JsonResult(new { success = false, message = "Vui lòng nhập số IMEI." });

        imei = imei.Trim();
        var warranty = await _context.Warranties
            .Include(w => w.Product)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.Imei == imei);

        if (warranty == null)
        {
            return new JsonResult(new {
                success = false,
                message = $"Không tìm thấy hợp đồng bảo hành điện tử nào cho IMEI {imei}. Thiết bị có thể chưa kích hoạt hoặc không phải hàng AAR."
            });
        }

        var isExpired = warranty.EndDate < DateTime.Now;
        var daysLeft = (warranty.EndDate - DateTime.Now).Days;
        var daysSinceStart = (DateTime.Now - warranty.StartDate).Days;
        var isWithin30Days = daysSinceStart >= 0 && daysSinceStart <= 30;

        return new JsonResult(new {
            success = true,
            imei = warranty.Imei,
            productName = warranty.Product?.Name ?? "Thiết bị iPhone",
            sku = warranty.Product?.Sku ?? "N/A",
            customerName = warranty.Customer?.FullName ?? "Khách lẻ",
            phoneNumber = warranty.Customer?.PhoneNumber ?? "N/A",
            startDate = warranty.StartDate.ToString("dd/MM/yyyy"),
            endDate = warranty.EndDate.ToString("dd/MM/yyyy"),
            isExpired = isExpired,
            daysLeft = daysLeft,
            daysSinceStart = daysSinceStart,
            isWithin30Days = isWithin30Days,
            status = warranty.Status
        });
    }

    public async Task<IActionResult> OnPostCreateClaimAsync(string imei, string issueDescription, string? technicianNotes)
    {
        if (string.IsNullOrWhiteSpace(imei))
        {
            ErrorMessage = "Vui lòng cung cấp mã IMEI thiết bị.";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(issueDescription))
        {
            ErrorMessage = "Vui lòng nhập mô tả chi tiết triệu chứng lỗi của máy.";
            return RedirectToPage();
        }

        imei = imei.Trim();
        var warranty = await _context.Warranties
            .Include(w => w.Product)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.Imei == imei);

        if (warranty == null)
        {
            ErrorMessage = $"Không tìm thấy thông tin bảo hành của thiết bị có IMEI {imei}.";
            return RedirectToPage();
        }

        var existingClaim = await _context.WarrantyClaims
            .Where(c => c.WarrantyId == warranty.Id && (c.Status == "Pending" || c.Status == "Processing" || c.Status == "Tiếp nhận" || c.Status == "Approved"))
            .FirstOrDefaultAsync();

        if (existingClaim != null)
        {
            ErrorMessage = $"Thiết bị này đang có phiếu bảo hành đang xử lý ({existingClaim.ClaimCode}). Không thể tạo trùng!";
            return RedirectToPage();
        }

        var countToday = await _context.WarrantyClaims.CountAsync(c => c.ClaimDate.Date == DateTime.Today);
        var claimCode = $"BH{DateTime.Now:yyMMdd}-{(countToday + 1):D3}";

        var claim = new WarrantyClaim
        {
            WarrantyId = warranty.Id,
            ClaimCode = claimCode,
            ClaimDate = DateTime.Now,
            IssueDescription = issueDescription.Trim(),
            TechnicianNotes = technicianNotes?.Trim(),
            Status = "Pending"
        };

        _context.WarrantyClaims.Add(claim);

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "CREATE_CLAIM",
            EntityName = "WarrantyClaim",
            RecordId = claimCode,
            NewValues = $"Tiếp nhận yêu cầu bảo hành mới {claimCode} cho IMEI {imei}. Lỗi: {issueDescription}",
            UserName = User.Identity?.Name ?? "Admin",
            Timestamp = DateTime.Now
        });

        await _context.SaveChangesAsync();

        SuccessMessage = $"Đã tiếp nhận hồ sơ bảo hành {claimCode} thành công!";
        return RedirectToPage("/Admin/WarrantyClaimDetail", new { id = claim.Id });
    }
}
