using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class AbandonedCart : BaseEntity
{
    public Guid? UserId { get; set; }
    public string CustomerName { get; set; } = "زائر المتجر";
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string Governorate { get; set; } = "غير محدد";
    public decimal Total { get; set; }

    public ICollection<AbandonedCartItem> Items { get; set; } = new List<AbandonedCartItem>();
}

public class AbandonedCartItem : BaseEntity
{
    public Guid AbandonedCartId { get; set; }
    public AbandonedCart? AbandonedCart { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Image { get; set; }
}
