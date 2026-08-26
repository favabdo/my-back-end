using NileTechno.Domain.Entities;

namespace NileTechno.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Order> Orders { get; }
    IRepository<Review> Reviews { get; }
    IRepository<Coupon> Coupons { get; }
    IRepository<ShippingZone> ShippingZones { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<WishlistItem> WishlistItems { get; }
    IRepository<AbandonedCart> AbandonedCarts { get; }
    IRepository<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
