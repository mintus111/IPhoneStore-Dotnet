using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Mvc.Data;
using IPhoneStore_Mvc.Models.Entities;

namespace IPhoneStore_Mvc.Controllers;

public class WarrantyController : Controller
{
    private readonly IPhoneStoreDbContext _context;

    public WarrantyController(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    // GET: /Warranty (hoặc /Warranty/Index?search=...)
    public async Task<IActionResult> Index(string? search = null)
    {
        // 1. Bắt buộc đăng nhập mới cho phép tra cứu bảo hành
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            ViewBag.IsGuest = true;
            ViewBag.SearchQuery = search?.Trim();
            return View(new List<Warranty>());
        }

        ViewBag.IsGuest = false;
        ViewBag.SearchQuery = search?.Trim();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

        List<Warranty> warranties = new();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var query = search.Trim();

            // Tìm kiếm theo IMEI hoặc theo Số điện thoại khách hàng
            warranties = await _context.Warranties
                .Include(w => w.Product)
                .Include(w => w.Customer)
                .Include(w => w.WarrantyClaims)
                .Where(w => w.Imei == query || (w.Customer != null && w.Customer.PhoneNumber == query))
                .OrderByDescending(w => w.StartDate)
                .ToListAsync();

            ViewBag.HasSearched = true;
        }
        else if (customer != null)
        {
            // Nếu khách đã đăng nhập và chưa tìm kiếm, tự động hiển thị danh sách bảo hành của tài khoản
            warranties = await _context.Warranties
                .Include(w => w.Product)
                .Include(w => w.Customer)
                .Include(w => w.WarrantyClaims)
                .Where(w => w.CustomerId == customer.Id)
                .OrderByDescending(w => w.StartDate)
                .ToListAsync();
        }

        return View(warranties);
    }
}
