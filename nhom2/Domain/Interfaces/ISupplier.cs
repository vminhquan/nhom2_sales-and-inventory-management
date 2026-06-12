using nhom2.Domain.Entities;

namespace nhom2.Domain.Interfaces;

public interface ISupplier
{
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
}
