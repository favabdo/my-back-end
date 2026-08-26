using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class AnalyticsSearch : BaseEntity
{
    public string Term { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AnalyticsProductView : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;
    public int Count { get; set; }
}
