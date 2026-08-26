using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class StoreSettings : BaseEntity
{
    public string StoreName { get; set; } = "NileTechno Store";
    public string StoreTitle { get; set; } = "متجر NileTechno الإلكتروني";
    public string PromoTagline { get; set; } = "تسوق بأمان مع NileTechno";
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public decimal FreeShippingMin { get; set; }
    public string? AnnouncementText { get; set; }
    public bool AnnouncementEnabled { get; set; }
}

public class UserAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}
