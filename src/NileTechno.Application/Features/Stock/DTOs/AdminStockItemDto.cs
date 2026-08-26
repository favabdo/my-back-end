namespace NileTechno.Application.Features.Stock.DTOs;

public class AdminStockItemDto
{
    public string ItemCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public decimal TransPkgQty1 { get; init; }
    public decimal ReorderQty { get; init; }
    public string StoreCode { get; init; } = string.Empty;
    public string StoreName { get; init; } = string.Empty;
    public string GroupId { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
}
