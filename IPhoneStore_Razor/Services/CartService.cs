using System.Text.Json;
using Microsoft.AspNetCore.Http;
using IPhoneStore_Razor.Models;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Services;

/// <summary>
/// Service quản lý Giỏ hàng iPhone lưu trong Session
/// </summary>
public class CartService
{
    private const string CartSessionKey = "IPhoneStore_Cart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public List<CartItem> GetCart()
    {
        if (Session == null) return new List<CartItem>();

        var json = Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<CartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
        catch
        {
            return new List<CartItem>();
        }
    }

    private void SaveCart(List<CartItem> cart)
    {
        if (Session == null) return;
        var json = JsonSerializer.Serialize(cart);
        Session.SetString(CartSessionKey, json);
    }

    public (bool Success, string Message) AddToCart(Product product, int quantity = 1)
    {
        if (product == null)
            return (false, "Sản phẩm không tồn tại!");

        if (product.Quantity <= 0)
            return (false, "iPhone này hiện đã hết hàng!");

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == product.Id);

        if (existing != null)
        {
            if (existing.Quantity + quantity > product.Quantity)
            {
                return (false, $"Kho chỉ còn {product.Quantity} máy, bạn đã có {existing.Quantity} máy trong giỏ!");
            }
            existing.Quantity += quantity;
        }
        else
        {
            if (quantity > product.Quantity)
            {
                return (false, $"Kho chỉ còn {product.Quantity} máy!");
            }

            cart.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ModelSeries = product.ModelSeries,
                Color = product.Color,
                Storage = product.Storage,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });
        }

        SaveCart(cart);
        return (true, $"Đã thêm {product.Name} vào giỏ hàng!");
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            SaveCart(cart);
        }
    }

    public void RemoveFromCart(int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCart(cart);
    }

    public void ClearCart()
    {
        SaveCart(new List<CartItem>());
    }

    public decimal GetTotalAmount()
    {
        return GetCart().Sum(item => item.TotalPrice);
    }

    public int GetCount()
    {
        return GetCart().Sum(item => item.Quantity);
    }
}
