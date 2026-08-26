namespace NileTechno.Application.Features.Products.DTOs;

public class CustomerProductCardDto
{
    public string ItemCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public decimal Stock { get; init; }
    public decimal Price { get; set; }
}
