namespace NileTechno.Application.Common.Interfaces;

public interface IErpSalesPostingService
{
    /// <summary>Posts (or re-posts) an order as an ERP sales invoice. Never throws.</summary>
    Task PostOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>Sets the ERP invoice (and its details) Status=0 so stock returns. Never throws.</summary>
    Task ReverseOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>Restores a reversed invoice back to Status=1. Never throws.</summary>
    Task RestoreOrderAsync(int orderId, CancellationToken ct = default);
}
