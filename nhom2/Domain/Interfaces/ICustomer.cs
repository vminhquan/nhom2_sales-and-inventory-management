using nhom2.Domain.Entities;

namespace nhom2.Domain.Interfaces;

public interface ICustomer
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(Customer customer);
}
