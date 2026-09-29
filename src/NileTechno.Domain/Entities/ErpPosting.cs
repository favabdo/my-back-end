namespace NileTechno.Domain.Entities;

/// <summary>
/// Outbox row linking an Ec_Orders order to the ERP sales invoice (wh_TransHeader)
/// posted for it. Status: Pending | Posted | Failed | Reversed | Skipped.
/// </summary>
public class ErpPosting
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public long? ErpHeaderId { get; set; }
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
