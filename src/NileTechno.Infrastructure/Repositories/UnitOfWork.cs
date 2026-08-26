using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Infrastructure.Persistence;

namespace NileTechno.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Products = new Repository<Product>(_context);
        Categories = new Repository<Category>(_context);
        Orders = new Repository<Order>(_context);
        Reviews = new Repository<Review>(_context);
        Coupons = new Repository<Coupon>(_context);
        ShippingZones = new Repository<ShippingZone>(_context);
        CartItems = new Repository<CartItem>(_context);
        WishlistItems = new Repository<WishlistItem>(_context);
        AbandonedCarts = new Repository<AbandonedCart>(_context);
        ActivityLogs = new Repository<ActivityLog>(_context);
    }

    public IRepository<Product> Products { get; }
    public IRepository<Category> Categories { get; }
    public IRepository<Order> Orders { get; }
    public IRepository<Review> Reviews { get; }
    public IRepository<Coupon> Coupons { get; }
    public IRepository<ShippingZone> ShippingZones { get; }
    public IRepository<CartItem> CartItems { get; }
    public IRepository<WishlistItem> WishlistItems { get; }
    public IRepository<AbandonedCart> AbandonedCarts { get; }
    public IRepository<ActivityLog> ActivityLogs { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
