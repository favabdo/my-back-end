using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class CartItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}
