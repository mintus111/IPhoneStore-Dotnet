using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Data;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly IPhoneStoreDbContext _context;

    public CategoriesModel(IPhoneStoreDbContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; set; } = new();

    [BindProperty]
    public Category InputCategory { get; set; } = new();

    [TempData]
    public string? AlertMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InputCategory.Name))
        {
            AlertMessage = "Vui lòng nhập tên dòng máy iPhone!";
            return RedirectToPage();
        }

        if (InputCategory.Id == 0)
        {
            InputCategory.IsActive = true;
            _context.Categories.Add(InputCategory);
            AlertMessage = $"Đã thêm dòng máy: {InputCategory.Name}";
        }
        else
        {
            var existing = await _context.Categories.FindAsync(InputCategory.Id);
            if (existing != null)
            {
                existing.Name = InputCategory.Name;
                existing.Description = InputCategory.Description;
                existing.DisplayOrder = InputCategory.DisplayOrder;
                existing.IsActive = InputCategory.IsActive;
                AlertMessage = $"Đã cập nhật dòng máy: {existing.Name}";
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            AlertMessage = $"Đã xóa dòng máy {category.Name}";
        }
        return RedirectToPage();
    }
}
