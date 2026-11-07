using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin,Manager,Staff")]
public class BatchImportModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public BatchImportModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public BatchImportViewModel Vm { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Vm.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Vm.Products = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

        if (Vm.SelectedProductId == 0)
        {
            ErrorMessage = "Vui lòng chọn model iPhone cần nhập lô IMEI!";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Vm.ImeisText))
        {
            ErrorMessage = "Danh sách IMEI không được để trống!";
            return Page();
        }

        var lines = Vm.ImeisText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();

        if (!lines.Any())
        {
            ErrorMessage = "Vui lòng nhập ít nhất một số IMEI hợp lệ!";
            return Page();
        }

        var invalidImeis = new List<string>();
        var cleanImeis = new List<string>();
        var seenInBatch = new HashSet<string>();
        var duplicateInBatch = new List<string>();

        foreach (var line in lines)
        {
            if (!Regex.IsMatch(line, @"^\d{15}$"))
            {
                invalidImeis.Add(line);
                continue;
            }

            if (!seenInBatch.Add(line))
            {
                duplicateInBatch.Add(line);
                continue;
            }

            cleanImeis.Add(line);
        }

        if (invalidImeis.Any())
        {
            ErrorMessage = $"Phát hiện {invalidImeis.Count} IMEI không đúng định dạng 15 số (VD: {invalidImeis.First()}). Vui lòng kiểm tra lại!";
            return Page();
        }

        if (duplicateInBatch.Any())
        {
            ErrorMessage = $"Phát hiện {duplicateInBatch.Count} IMEI bị trùng lặp ngay trong danh sách nhập!";
            return Page();
        }

        var existingInDb = await _context.ProductImeis
            .Where(pi => cleanImeis.Contains(pi.Imei))
            .Select(pi => pi.Imei)
            .ToListAsync();

        if (existingInDb.Any())
        {
            ErrorMessage = $"Phát hiện {existingInDb.Count} IMEI đã tồn tại trong hệ thống kho (VD: {existingInDb.First()})!";
            return Page();
        }

        var product = await _context.Products.FindAsync(Vm.SelectedProductId);
        if (product == null) return NotFound();

        var now = DateTime.Now;
        foreach (var imei in cleanImeis)
        {
            _context.ProductImeis.Add(new ProductImei
            {
                ProductId = product.Id,
                Imei = imei,
                SerialNumber = "S" + imei.Substring(7),
                Status = "InStock",
                ImportDate = now
            });
        }

        product.Quantity += cleanImeis.Count;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "BATCH_IMPORT_IMEI",
            EntityName = "ProductImeis",
            RecordId = product.Id.ToString(),
            NewValues = $"Nhập lô {cleanImeis.Count} IMEI cho model {product.Name}. NCC: {Vm.Vendor}",
            UserName = User.Identity?.Name ?? "Admin",
            Timestamp = now
        });

        await _context.SaveChangesAsync();

        SuccessMessage = $"Đã nhập kho thành công lô {cleanImeis.Count} chiếc iPhone {product.Name} từ nhà phân phối {Vm.Vendor}!";
        return RedirectToPage("/Admin/Inventory");
    }
}
