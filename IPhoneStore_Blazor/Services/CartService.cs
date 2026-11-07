using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using IPhoneStore_Blazor.Models;
using IPhoneStore_Blazor.Models.Entities;

namespace IPhoneStore_Blazor.Services;

/// <summary>
/// Giỏ hàng iPhone cho Blazor Server: lưu trong ProtectedLocalStorage của trình duyệt
/// (Session không dùng được vì Blazor chạy qua SignalR circuit).
/// </summary>
public class CartService
{
    private const string StorageKey = "IPhoneStore_Cart";
    private readonly ProtectedLocalStorage _storage;
    private List<CartItem> _items = new();
    private bool _loaded;

    public CartService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public event Action? Changed;
    public bool IsLoaded => _loaded;

    /// <summary>Gọi sau lần render đầu tiên (cần JS interop).</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        try
        {
            var result = await _storage.GetAsync<List<CartItem>>(StorageKey);
            _items = result.Success && result.Value != null ? result.Value : new List<CartItem>();
            _loaded = true;
        }
        catch
        {
            // Nếu lỗi JS interop (đang prerender), không đặt _loaded = true để lần sau có thể đọc lại
            _items = new List<CartItem>();
        }
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        try { await _storage.SetAsync(StorageKey, _items); } catch { /* bỏ qua */ }
        Changed?.Invoke();
    }

    public List<CartItem> GetCart() => _items;

    public async Task<(bool Success, string Message)> AddToCartAsync(Product product, int quantity = 1)
    {
        await EnsureLoadedAsync();

        if (product == null) return (false, "Sản phẩm không tồn tại!");
        
        var availableStock = product.Quantity > 0 ? product.Quantity : 99;

        var existing = _items.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            if (product.Quantity > 0 && existing.Quantity + quantity > product.Quantity)
            {
                if (existing.Quantity < product.Quantity)
                {
                    existing.Quantity = product.Quantity;
                    await SaveAsync();
                    return (true, $"Đã cập nhật số lượng {product.Name} trong giỏ lên mức tối đa ({product.Quantity} máy)!");
                }
                return (false, $"Kho chỉ còn {product.Quantity} máy, bạn đã có {existing.Quantity} máy trong giỏ!");
            }
            existing.Quantity += quantity;
        }
        else
        {
            if (product.Quantity > 0 && quantity > product.Quantity)
            {
                quantity = product.Quantity;
            }

            _items.Add(new CartItem
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

        await SaveAsync();
        return (true, $"Đã thêm {product.Name} vào giỏ hàng!");
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var item = _items.FirstOrDefault(c => c.ProductId == productId);
        if (item == null) return;
        if (quantity <= 0) _items.Remove(item);
        else item.Quantity = quantity;
        await SaveAsync();
    }

    public async Task RemoveFromCartAsync(int productId)
    {
        _items.RemoveAll(c => c.ProductId == productId);
        await SaveAsync();
    }

    public async Task ClearCartAsync()
    {
        _items = new List<CartItem>();
        await SaveAsync();
    }

    public decimal GetTotalAmount() => _items.Sum(i => i.TotalPrice);
    public int GetCount() => _items.Sum(i => i.Quantity);
}
