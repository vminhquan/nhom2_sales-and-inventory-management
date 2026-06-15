namespace nhom2.Domain.Entities;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? PayOsTransactionReference { get; set; }
    public Order Order { get; set; } = null!;
}
