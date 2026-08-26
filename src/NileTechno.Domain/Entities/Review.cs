using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class Review : BaseEntity
{
    public string ExternalId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CustomerName { get; set; } = "عميل مميز";
    public bool Approved { get; set; }
    public string Date { get; set; } = string.Empty;
}
