using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface ICustomerService
{
    Task<List<CustomerResponseDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(int id);
    Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerResponseDto> UpdateAsync(UpdateCustomerDto dto);
    Task DeleteAsync(int id);
}
