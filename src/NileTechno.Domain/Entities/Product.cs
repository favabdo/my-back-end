using NileTechno.Domain.Common;

namespace NileTechno.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }

    public int Stock { get; set; }
    public bool Featured { get; set; }
    public bool IsNew { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public List<string> GalleryUrls { get; set; } = new();

    public double Rating { get; set; }
    public int ReviewsCount { get; set; }

    public List<string> Colors { get; set; } = new();
    public List<string> Sizes { get; set; } = new();

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}
