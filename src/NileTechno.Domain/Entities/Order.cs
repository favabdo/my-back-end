using NileTechno.Domain.Enums;

namespace NileTechno.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? CancelReason { get; set; }
    public string? InternalNote { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerNotes { get; set; }

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

public class OrderItem
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}

public class OrderHistoryEntry
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? NewStatus { get; set; }
}
