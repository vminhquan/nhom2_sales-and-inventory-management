using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface ISupplierService
{
    Task<List<SupplierResponseDto>> GetAllAsync();
    Task<SupplierResponseDto?> GetByIdAsync(int id);
    Task<SupplierResponseDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierResponseDto> UpdateAsync(UpdateSupplierDto dto);
    Task DeleteAsync(int id);
}
