using nhom2.Application.DTOs;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;

namespace nhom2.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplier _supplierRepository;

    public SupplierService(ISupplier supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<List<SupplierResponseDto>> GetAllAsync()
    {
        return (await _supplierRepository.GetAllAsync()).Select(Map).ToList();
    }

    public async Task<SupplierResponseDto?> GetByIdAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier is null ? null : Map(supplier);
    }

    public async Task<SupplierResponseDto> CreateAsync(CreateSupplierDto dto)
    {
        Validate(dto);
        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            ContactName = dto.ContactName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = Normalize(dto.Email),
            Address = Normalize(dto.Address),
            Notes = Normalize(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier);
        return Map(supplier);
    }

    public async Task<SupplierResponseDto> UpdateAsync(UpdateSupplierDto dto)
    {
        Validate(dto);
        var supplier = await _supplierRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Supplier với ID {dto.Id}");

        supplier.Name = dto.Name.Trim();
        supplier.ContactName = dto.ContactName.Trim();
        supplier.Phone = dto.Phone.Trim();
        supplier.Email = Normalize(dto.Email);
        supplier.Address = Normalize(dto.Address);
        supplier.Notes = Normalize(dto.Notes);
        supplier.LastModifiedAt = DateTime.UtcNow;

        await _supplierRepository.UpdateAsync(supplier);
        return Map(supplier);
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Supplier với ID {id}");
        await _supplierRepository.DeleteAsync(supplier);
    }

    private static void Validate(CreateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Tên nhà cung cấp không được để trống");

        if (string.IsNullOrWhiteSpace(dto.ContactName))
            throw new ArgumentException("Người liên hệ không được để trống");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            throw new ArgumentException("Số điện thoại không được để trống");
    }

    private static SupplierResponseDto Map(Supplier supplier)
    {
        return new SupplierResponseDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            LastModifiedAt = supplier.LastModifiedAt
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
