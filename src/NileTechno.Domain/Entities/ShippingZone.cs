using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class ShippingZone : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;
}
