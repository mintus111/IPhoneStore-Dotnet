using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.ViewModels;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditLogsModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public AuditLogsModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public AuditLogsViewModel Vm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? actionFilter, string? tableFilter, string? searchKeyword, int currentPage = 1)
    {
        const int pageSize = 15;
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
            query = query.Where(a => a.Action == actionFilter);

        if (!string.IsNullOrWhiteSpace(tableFilter))
            query = query.Where(a => a.EntityName == tableFilter);

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(kw)) ||
                                     (a.RecordId != null && a.RecordId.ToLower().Contains(kw)) ||
                                     (a.NewValues != null && a.NewValues.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        var logs = await query.OrderByDescending(a => a.Timestamp)
            .Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

        var availableActions = await _context.AuditLogs
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var availableTables = await _context.AuditLogs
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        Vm = new AuditLogsViewModel
        {
            Logs = logs,
            ActionFilter = actionFilter,
            TableFilter = tableFilter,
            SearchKeyword = searchKeyword,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            AvailableActions = availableActions,
            AvailableTables = availableTables
        };

        return Page();
    }
}
