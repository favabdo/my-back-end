using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class StockOverride : BaseEntity
{
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
