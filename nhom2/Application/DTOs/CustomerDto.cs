namespace nhom2.Application.DTOs;

public class CreateCustomerDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
    public int Id { get; set; }
}

public class CustomerResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal CurrentDebt { get; set; }
    public int OrderCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public List<CustomerOrderSummaryDto> PurchaseHistory { get; set; } = new();
}

public class CustomerOrderSummaryDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal DebtAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
