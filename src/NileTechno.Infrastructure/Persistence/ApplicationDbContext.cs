using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Infrastructure.Identity;

namespace NileTechno.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderHistoryEntry> OrderHistoryEntries => Set<OrderHistoryEntry>();

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<ShippingZone> ShippingZones => Set<ShippingZone>();

    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    public DbSet<AbandonedCart> AbandonedCarts => Set<AbandonedCart>();
    public DbSet<AbandonedCartItem> AbandonedCartItems => Set<AbandonedCartItem>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<StoreSettings> StoreSettingsList => Set<StoreSettings>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<StockOverride> StockOverrides => Set<StockOverride>();
    public DbSet<AnalyticsSearch> AnalyticsSearches => Set<AnalyticsSearch>();
    public DbSet<AnalyticsProductView> AnalyticsProductViews => Set<AnalyticsProductView>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.Total).HasColumnType("decimal(18,2)");

            e.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(o => o.History)
                .WithOne(h => h.Order)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Review>(e =>
        {
            e.HasIndex(r => r.ExternalId);
            e.HasIndex(r => r.ProductId);
            e.Property(r => r.ProductId).HasMaxLength(100);
            e.Property(r => r.ExternalId).HasMaxLength(200);
        });

        builder.Entity<StockOverride>(e =>
        {
            e.HasIndex(s => s.ItemCode).IsUnique();
            e.Property(s => s.Quantity).HasColumnType("decimal(18,2)");
        });

        builder.Entity<AnalyticsSearch>(e =>
        {
            e.HasIndex(s => s.Term).IsUnique();
        });

        builder.Entity<AnalyticsProductView>(e =>
        {
            e.HasIndex(s => s.ProductId).IsUnique();
        });

        builder.Entity<Coupon>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.DiscountPercent).HasColumnType("decimal(5,2)");
        });

        builder.Entity<ShippingZone>(e =>
        {
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Price).HasColumnType("decimal(18,2)");
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.ProductId });
        });

        builder.Entity<WishlistItem>(e =>
        {
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        });

        builder.Entity<AbandonedCart>(e =>
        {
            e.Property(a => a.Total).HasColumnType("decimal(18,2)");
            e.HasMany(a => a.Items)
                .WithOne(i => i.AbandonedCart)
                .HasForeignKey(i => i.AbandonedCartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AbandonedCartItem>(e =>
        {
            e.Property(i => i.Price).HasColumnType("decimal(18,2)");
        });

        builder.Entity<StoreSettings>(e =>
        {
            e.Property(s => s.FreeShippingMin).HasColumnType("decimal(18,2)");
        });
    }
}
