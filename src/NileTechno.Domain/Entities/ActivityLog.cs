using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}
