using Microsoft.EntityFrameworkCore;
using nhom2.Domain.Entities;
using nhom2.Infrastructure.Data;

namespace nhom2.Application.Services;

public class PaymentExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpirationWorker> _logger;

    public PaymentExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                await context.Orders
                    .Where(order => order.Status == OrderStatus.PendingPayment
                        && order.Payment != null
                        && order.Payment.ExpiresAt <= now)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(order => order.Status, OrderStatus.PaymentExpired)
                            .SetProperty(order => order.LastModifiedAt, now),
                        stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to expire pending payments. The worker will retry.");
            }
        }
    }
}
