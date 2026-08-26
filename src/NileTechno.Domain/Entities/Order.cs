using NileTechno.Domain.Common;
using NileTechno.Domain.Enums;

namespace NileTechno.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? CancelReason { get; set; }
    public string? InternalNote { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public string Governorate { get; set; } = string.Empty;
    public string AddressDetails { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string PaymentMethod { get; set; } = "cod";
    public string? CouponCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderHistoryEntry> History { get; set; } = new List<OrderHistoryEntry>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}

public class OrderHistoryEntry : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NewStatus { get; set; }
}
