using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff")]
public class WarrantyClaimDetailModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public WarrantyClaimDetailModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public WarrantyClaimDetailViewModel Vm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var claim = await _context.WarrantyClaims
            .Include(c => c.Warranty)
                .ThenInclude(w => w!.Product)
            .Include(c => c.Warranty)
                .ThenInclude(w => w!.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        var warranty = claim.Warranty ?? await _context.Warranties
            .Include(w => w.Product)
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.Id == claim.WarrantyId);

        if (warranty == null) return NotFound();

        var replacementImeis = await _context.ProductImeis
            .Where(pi => pi.ProductId == warranty.ProductId && pi.Status == "InStock")
            .ToListAsync();

        Vm = new WarrantyClaimDetailViewModel
        {
            Claim = claim,
            Warranty = warranty,
            AvailableReplacementImeis = replacementImeis
        };

        return Page();
    }

    public async Task<IActionResult> OnPostProcessClaimAsync(
        int claimId,
        string resolutionType,
        int? replacementImeiId,
        string? technicianNotes)
    {
        var claim = await _context.WarrantyClaims
            .Include(c => c.Warranty)
            .FirstOrDefaultAsync(c => c.Id == claimId);

        if (claim == null) return NotFound();

        var now = DateTime.Now;
        claim.ActionTaken = resolutionType;
        claim.TechnicianNotes = technicianNotes;
        claim.ResolvedDate = now;

        if (resolutionType == "Replace")
        {
            if (!replacementImeiId.HasValue || replacementImeiId.Value == 0)
            {
                ErrorMessage = "Vui lòng chọn máy IMEI mới nguyên seal từ kho để thực hiện chính sách 1 Đổi 1!";
                return RedirectToPage("/Admin/WarrantyClaimDetail", new { id = claimId });
            }

            var newImei = await _context.ProductImeis.FindAsync(replacementImeiId.Value);
            if (newImei == null || newImei.Status != "InStock")
            {
                ErrorMessage = "IMEI thay thế không hợp lệ hoặc không còn trong kho!";
                return RedirectToPage("/Admin/WarrantyClaimDetail", new { id = claimId });
            }

            claim.NewImei = newImei.Imei;
            claim.Status = "Completed";
            newImei.Status = "Sold";

            if (claim.Warranty != null)
            {
                var oldImeiEntity = await _context.ProductImeis
                    .FirstOrDefaultAsync(pi => pi.Imei == claim.Warranty.Imei);

                if (oldImeiEntity != null)
                {
                    oldImeiEntity.Status = "Defective";
                }

                claim.Warranty.OriginalImei = claim.Warranty.Imei;
                claim.Warranty.Imei = newImei.Imei;
                claim.Warranty.SerialNumber = newImei.SerialNumber;
                claim.Warranty.Notes += $" | 1 Đổi 1 ngày {now:dd/MM/yyyy} theo phiếu {claim.ClaimCode ?? claim.Id.ToString()}";
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "WARRANTY_REPLACE_1TO1",
                EntityName = "WarrantyClaims",
                RecordId = claim.Id.ToString(),
                OldValues = $"Lỗi={claim.IssueDescription}, Máy cũ IMEI={claim.Warranty?.OriginalImei}",
                NewValues = $"Đổi máy mới IMEI={newImei.Imei}, Serial={newImei.SerialNumber}",
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = now
            });

            SuccessMessage = $"Đã thực hiện xuất kho máy mới IMEI {newImei.Imei} cho khách hàng theo chuẩn 1 Đổi 1 của Apple!";
        }
        else if (resolutionType == "Repair")
        {
            claim.Status = "Completed";
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "WARRANTY_REPAIR",
                EntityName = "WarrantyClaims",
                RecordId = claim.Id.ToString(),
                NewValues = $"Sửa chữa linh kiện chính hãng: {technicianNotes}",
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = now
            });

            SuccessMessage = $"Hồ sơ {claim.ClaimCode ?? claim.Id.ToString()} đã được duyệt xử lý bảo hành sửa chữa!";
        }
        else
        {
            claim.Status = "Rejected";
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "WARRANTY_REJECT",
                EntityName = "WarrantyClaims",
                RecordId = claim.Id.ToString(),
                NewValues = $"Từ chối bảo hành: {technicianNotes}",
                UserName = User.Identity?.Name ?? "Admin",
                Timestamp = now
            });

            SuccessMessage = $"Đã cập nhật trạng thái Từ chối bảo hành do không đủ điều kiện!";
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/WarrantyClaimDetail", new { id = claimId });
    }
}
