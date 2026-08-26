using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NileTechno.Application.Common.Interfaces;
using NileTechno.Domain.Entities;
using NileTechno.Domain.Enums;

namespace NileTechno.API.Controllers;

[Route("api/orders")]
public class OrdersCompatController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ICurrentUserService _currentUser;

    public OrdersCompatController(IApplicationDbContext db, IEmailService email, ICurrentUserService currentUser)
    {
        _db = db;
        _email = email;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.History)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return Ok(orders.Select(Map));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCompatRequest body, CancellationToken ct)
    {
        var uniqueHex = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
        var orderNumber = "ORD-" + DateTime.UtcNow.ToString("HHmmss") + "-" + uniqueHex;
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = body.CustomerName ?? body.Name ?? "",
            CustomerEmail = body.CustomerEmail ?? body.Email ?? "",
            CustomerPhone = body.CustomerPhone ?? body.Phone ?? "",
            Governorate = body.Governorate ?? "",
            AddressDetails = body.Address ?? body.AddressDetails ?? "",
            PaymentMethod = body.PaymentMethod ?? "cod",
            CouponCode = body.CouponCode,
            Total = body.Total ?? 0,
            Subtotal = body.Subtotal ?? body.Total ?? 0,
            ShippingCost = body.ShippingCost ?? 0,
            DiscountAmount = body.DiscountAmount ?? 0,
            Status = OrderStatus.Pending
        };

        if (body.Items is not null)
        {
            foreach (var item in body.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductName = item.Name ?? item.Title ?? "",
                    ProductImage = item.Image ?? "",
                    UnitPrice = item.Price ?? 0,
                    Quantity = item.Quantity <= 0 ? 1 : item.Quantity,
                    SelectedColor = item.SelectedColor,
                    SelectedSize = item.SelectedSize
                });
            }
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, order = Map(order) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.OrderId) || string.IsNullOrWhiteSpace(body.NewStatus))
            return BadRequest(new { error = "orderId و newStatus مطلوبان" });

        var order = await FindOrderAsync(body.OrderId, ct);
        if (order is null)
            return NotFound(new { error = "الطلب غير موجود" });

        order.Status = ParseStatus(body.NewStatus);
        if (!string.IsNullOrWhiteSpace(body.CancelReason))
            order.CancelReason = body.CancelReason;

        order.History.Add(new OrderHistoryEntry
        {
            Action = $"تغيير حالة الطلب إلى: {body.NewStatus}",
            Actor = _currentUser.Email ?? "مدير النظام",
            Type = "STATUS_CHANGE",
            NewStatus = body.NewStatus
        });

        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
            await _email.SendOrderStatusEmailAsync(order.CustomerEmail, order.CustomerName, order.OrderNumber, body.NewStatus, ct);

        return Ok(new { success = true, order = Map(order), emailSent = !string.IsNullOrWhiteSpace(order.CustomerEmail) });
    }

    [Authorize(Roles = "Admin,MainAdmin")]
    [HttpPost("bulk-update-status")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateOrderStatusRequest body, CancellationToken ct)
    {
        if (body.OrderIds is null || body.OrderIds.Count == 0 || string.IsNullOrWhiteSpace(body.NewStatus))
            return BadRequest(new { error = "orderIds (مصفوفة) و newStatus مطلوبان" });

        var updated = new List<object>();
        foreach (var id in body.OrderIds)
        {
            var order = await FindOrderAsync(id, ct);
            if (order is null)
                continue;

            order.Status = ParseStatus(body.NewStatus);
            if (!string.IsNullOrWhiteSpace(body.CancelReason))
                order.CancelReason = body.CancelReason;

            order.History.Add(new OrderHistoryEntry
            {
                Action = $"تحديث جماعي لحالة الطلب إلى: {body.NewStatus}",
                Actor = _currentUser.Email ?? "مدير النظام",
                Type = "BULK_STATUS_CHANGE",
                NewStatus = body.NewStatus
            });

            if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
                await _email.SendOrderStatusEmailAsync(order.CustomerEmail, order.CustomerName, order.OrderNumber, body.NewStatus, ct);

            updated.Add(Map(order));
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, updatedCount = updated.Count, orders = updated });
    }

    private async Task<Order?> FindOrderAsync(string orderId, CancellationToken ct) =>
        await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderId || o.Id.ToString() == orderId, ct);

    private static OrderStatus ParseStatus(string value)
    {
        var key = value.Trim().Replace("-", "").Replace("_", "").ToUpperInvariant();
        return key switch
        {
            "PENDING" or "CREATED" or "RECEIVED" => OrderStatus.Pending,
            "PROCESSING" or "PREPARING" or "CONFIRMED" or "APPROVED" => OrderStatus.Processing,
            "PACKED" or "READY" => OrderStatus.Packed,
            "SHIPPED" or "INTRANSIT" => OrderStatus.Shipped,
            "OUTFORDELIVERY" => OrderStatus.OutForDelivery,
            "DELIVERED" => OrderStatus.Delivered,
            "CANCELED" or "CANCELLED" => OrderStatus.Canceled,
            "REFUNDED" => OrderStatus.Refunded,
            _ => OrderStatus.Pending
        };
    }

    private static object Map(Order o) => new
    {
        id = o.Id,
        orderNumber = o.OrderNumber,
        status = o.Status.ToString().ToUpperInvariant(),
        cancelReason = o.CancelReason,
        customerName = o.CustomerName,
        customerEmail = o.CustomerEmail,
        customerPhone = o.CustomerPhone,
        governorate = o.Governorate,
        addressDetails = o.AddressDetails,
        paymentMethod = o.PaymentMethod,
        total = o.Total,
        date = o.CreatedAt,
        items = o.Items.Select(i => new
        {
            name = i.ProductName,
            price = i.UnitPrice,
            quantity = i.Quantity,
            image = i.ProductImage
        }),
        history = o.History.Select(h => new
        {
            action = h.Action,
            actor = h.Actor,
            type = h.Type,
            newStatus = h.NewStatus,
            timestamp = h.CreatedAt
        })
    };
}

public class CreateOrderCompatRequest
{
    public string? Name { get; set; }
    public string? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Phone { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Governorate { get; set; }
    public string? Address { get; set; }
    public string? AddressDetails { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CouponCode { get; set; }
    public decimal? Total { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? ShippingCost { get; set; }
    public decimal? DiscountAmount { get; set; }
    public List<CreateOrderItemCompatRequest>? Items { get; set; }
}

public class CreateOrderItemCompatRequest
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Image { get; set; }
    public decimal? Price { get; set; }
    public int Quantity { get; set; }
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string? OrderId { get; set; }
    public string? NewStatus { get; set; }
    public string? CancelReason { get; set; }
}

public class BulkUpdateOrderStatusRequest
{
    public List<string>? OrderIds { get; set; }
    public string? NewStatus { get; set; }
    public string? CancelReason { get; set; }
}
