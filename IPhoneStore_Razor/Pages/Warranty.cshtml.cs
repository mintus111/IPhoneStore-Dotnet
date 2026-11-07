using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages;

public class WarrantyModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public WarrantyModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public bool HasSearched { get; set; }

    public List<Warranty> Warranties { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            HasSearched = true;
            var q = SearchQuery.Trim();
            Warranties = await _context.Warranties
                .Include(w => w.Product)
                .Include(w => w.Customer)
                .Where(w => w.Imei == q || (w.Customer != null && w.Customer.PhoneNumber == q))
                .OrderByDescending(w => w.StartDate)
                .ToListAsync();
        }
        else if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer != null)
                {
                    Warranties = await _context.Warranties
                        .Include(w => w.Product)
                        .Where(w => w.CustomerId == customer.Id)
                        .OrderByDescending(w => w.StartDate)
                        .ToListAsync();
                }
            }
        }
    }
}
