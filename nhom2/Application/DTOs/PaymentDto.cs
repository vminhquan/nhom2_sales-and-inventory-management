namespace nhom2.Application.DTOs;

using System.Text.Json.Serialization;

public class CreatePaymentLinkDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public List<OrderItemDto> OrderItems { get; set; } = new();

    [JsonIgnore]
    public int? AuthenticatedCustomerUserId { get; set; }

    [JsonIgnore]
    public string? AuthenticatedCustomerEmail { get; set; }
}

public class PaymentLinkResponseDto
{
    public int OrderId { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class PaymentStatusDto
{
    public int OrderId { get; set; }
    public long OrderCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
