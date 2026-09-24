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
    public DbSet<LoginAccount> LoginAccounts => Set<LoginAccount>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<StockOverride> StockOverrides => Set<StockOverride>();
    public DbSet<AnalyticsSearch> AnalyticsSearches => Set<AnalyticsSearch>();
    public DbSet<AnalyticsProductView> AnalyticsProductViews => Set<AnalyticsProductView>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e => e.ToTable("Ec_AspNetUsers"));
        builder.Entity<IdentityRole<Guid>>(e => e.ToTable("Ec_AspNetRoles"));
        builder.Entity<IdentityUserRole<Guid>>(e => e.ToTable("Ec_AspNetUserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(e => e.ToTable("Ec_AspNetUserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(e => e.ToTable("Ec_AspNetUserLogins"));
        builder.Entity<IdentityUserToken<Guid>>(e => e.ToTable("Ec_AspNetUserTokens"));
        builder.Entity<IdentityRoleClaim<Guid>>(e => e.ToTable("Ec_AspNetRoleClaims"));

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
            e.ToTable("Ec_Orders");
            e.Property(o => o.Id).ValueGeneratedOnAdd();
            e.Property(o => o.CustomerNotes).HasColumnName("Notes");
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
            e.ToTable("Ec_OrderItems");
            e.Property(i => i.Id).ValueGeneratedOnAdd();
            e.Property(i => i.ProductId).HasMaxLength(50);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });

        builder.Entity<OrderHistoryEntry>(e =>
        {
            e.ToTable("Ec_OrderHistoryEntries");
            e.Property(h => h.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<Review>(e =>
        {
            e.ToTable("Ec_Reviews");
            e.HasIndex(r => r.ExternalId);
            e.HasIndex(r => r.ProductId);
            e.Property(r => r.ProductId).HasMaxLength(100);
            e.Property(r => r.ExternalId).HasMaxLength(200);
        });

        builder.Entity<StockOverride>(e =>
        {
            e.ToTable("Ec_StockOverrides");
            e.HasIndex(s => s.ItemCode).IsUnique();
            e.Property(s => s.Quantity).HasColumnType("decimal(18,2)");
        });

        builder.Entity<AnalyticsSearch>(e =>
        {
            e.ToTable("Ec_AnalyticsSearches");
            e.HasIndex(s => s.Term).IsUnique();
        });

        builder.Entity<AnalyticsProductView>(e =>
        {
            e.ToTable("Ec_AnalyticsProductViews");
            e.HasIndex(s => s.ProductId).IsUnique();
        });

        builder.Entity<Coupon>(e =>
        {
            e.ToTable("Ec_Coupons");
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.DiscountPercent).HasColumnType("decimal(5,2)");
        });

        builder.Entity<ShippingZone>(e =>
        {
            e.ToTable("Ec_ShippingZones");
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Price).HasColumnType("decimal(18,2)");
        });

        builder.Entity<CartItem>(e =>
        {
            e.ToTable("Ec_CartItems");
            e.Property(c => c.UserId).HasMaxLength(64);
            e.Property(c => c.ProductId).HasMaxLength(50);
            e.HasIndex(c => new { c.UserId, c.ProductId, c.SelectedColor, c.SelectedSize });
        });

        builder.Entity<WishlistItem>(e =>
        {
            e.ToTable("Ec_WishlistItems");
            e.Property(w => w.UserId).HasMaxLength(64);
            e.Property(w => w.ProductId).HasMaxLength(50);
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
        });

        builder.Entity<UserAddress>(e =>
        {
            e.ToTable("Ec_UserAddresses");
            e.Property(a => a.UserId).HasMaxLength(64);
        });

        builder.Entity<AbandonedCart>(e =>
        {
            e.ToTable("Ec_AbandonedCarts");
            e.Property(a => a.UserId).HasMaxLength(64);
            e.Property(a => a.Total).HasColumnType("decimal(18,2)");
            e.HasMany(a => a.Items)
                .WithOne(i => i.AbandonedCart)
                .HasForeignKey(i => i.AbandonedCartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AbandonedCartItem>(e =>
        {
            e.ToTable("Ec_AbandonedCartItems");
            e.Property(i => i.Price).HasColumnType("decimal(18,2)");
        });

        builder.Entity<ActivityLog>(e =>
        {
            e.ToTable("Ec_ActivityLogs");
        });

        builder.Entity<StoreSettings>(e =>
        {
            e.ToTable("Ec_StoreSettingsList");
            e.Property(s => s.FreeShippingMin).HasColumnType("decimal(18,2)");
        });

        builder.Entity<LoginAccount>(e =>
        {
            e.ToTable("Ec_LoginAccounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).UseIdentityColumn(1, 1);
            e.HasIndex(a => a.NormalizedEmail).IsUnique();
            e.Property(a => a.Email).HasMaxLength(256);
            e.Property(a => a.NormalizedEmail).HasMaxLength(256);
            e.Property(a => a.AuthProvider).HasMaxLength(32);
            e.Property(a => a.GoogleSubject).HasMaxLength(128);
            e.Property(a => a.FullName).HasMaxLength(200);
            e.Property(a => a.Phone).HasMaxLength(32);
        });
    }
}
