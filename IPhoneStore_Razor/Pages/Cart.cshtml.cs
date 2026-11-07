using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IPhoneStore_Razor.Models;
using IPhoneStore_Razor.Services;

namespace IPhoneStore_Razor.Pages;

public class CartModel : PageModel
{
    private readonly CartService _cartService;

    public CartModel(CartService cartService)
    {
        _cartService = cartService;
    }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }

    public void OnGet()
    {
        LoadCart();
    }

    public IActionResult OnPostUpdate(int productId, int quantity)
    {
        _cartService.UpdateQuantity(productId, quantity);
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int productId)
    {
        _cartService.RemoveFromCart(productId);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        _cartService.ClearCart();
        return RedirectToPage();
    }

    private void LoadCart()
    {
        CartItems = _cartService.GetCart();
        TotalAmount = _cartService.GetTotalAmount();
    }
}
