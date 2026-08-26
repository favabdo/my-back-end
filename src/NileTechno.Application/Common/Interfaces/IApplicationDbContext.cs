using Microsoft.EntityFrameworkCore;
using NileTechno.Domain.Entities;

namespace NileTechno.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderHistoryEntry> OrderHistoryEntries { get; }

    DbSet<Review> Reviews { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<ShippingZone> ShippingZones { get; }

    DbSet<CartItem> CartItems { get; }
    DbSet<WishlistItem> WishlistItems { get; }

    DbSet<AbandonedCart> AbandonedCarts { get; }
    DbSet<AbandonedCartItem> AbandonedCartItems { get; }

    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<StoreSettings> StoreSettingsList { get; }
    DbSet<UserAddress> UserAddresses { get; }
    DbSet<StockOverride> StockOverrides { get; }
    DbSet<AnalyticsSearch> AnalyticsSearches { get; }
    DbSet<AnalyticsProductView> AnalyticsProductViews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
