namespace IPhoneStore_Mvc.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ModelSeries { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;

    public decimal TotalPrice => Price * Quantity;
}
