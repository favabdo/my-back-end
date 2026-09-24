using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
}
