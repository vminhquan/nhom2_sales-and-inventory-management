namespace nhom2.Application.DTOs;

public class OrderEventDto
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal DebtAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public List<OrderEventItemDto> Items { get; set; } = new();
}

public class OrderEventItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
