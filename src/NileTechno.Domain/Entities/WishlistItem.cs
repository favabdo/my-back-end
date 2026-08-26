using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}
