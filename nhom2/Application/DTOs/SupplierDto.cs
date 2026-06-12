namespace nhom2.Application.DTOs;

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class UpdateSupplierDto : CreateSupplierDto
{
    public int Id { get; set; }
}

public class SupplierResponseDto : UpdateSupplierDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
