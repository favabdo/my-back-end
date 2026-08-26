using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}
