using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Infrastructure.Services;

/// <summary>
/// Picks up Ec_ErpPostings rows left Pending/Failed (ERP was down, transient error, etc.)
/// and retries posting them as ERP sales invoices.
/// </summary>
public class ErpPostingRetryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;
    private readonly ILogger<ErpPostingRetryService> _logger;

    public ErpPostingRetryService(IServiceScopeFactory scopeFactory, IConfiguration configuration,
        ILogger<ErpPostingRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var section = configuration.GetSection("ErpPosting");
        _enabled = !bool.TryParse(section["Enabled"], out var enabled) || enabled;
        var seconds = int.TryParse(section["RetryIntervalSeconds"], out var v) && v > 0 ? v : 60;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
            return;

        // Let the schema bootstrapper create Ec_ErpPostings first.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var poster = (ErpSalesPostingService)scope.ServiceProvider.GetRequiredService<IErpSalesPostingService>();
                var orderIds = await poster.GetRetryableOrderIdsAsync(stoppingToken);

                foreach (var orderId in orderIds)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await poster.PostOrderAsync(orderId, stoppingToken);
                }

                if (orderIds.Count > 0)
                    _logger.LogInformation("ERP posting retry pass finished ({Count} orders).", orderIds.Count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ERP posting retry pass failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
