using System.Text.Json;
using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface IPaymentService
{
    Task<OrderResponseDto> CreateCashOrderAsync(CreatePaymentLinkDto dto);
    Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto dto);
    Task<PaymentStatusDto?> GetStatusAsync(long orderCode);
    Task CancelAsync(long orderCode);
    Task HandleWebhookAsync(JsonElement payload);
    Task ExpirePendingPaymentsAsync(CancellationToken cancellationToken);
}
