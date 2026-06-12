using Microsoft.EntityFrameworkCore;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Data;

namespace nhom2.Infrastructure.Repositories;

public class SupplierRepo : ISupplier
{
    private readonly ApplicationDbContext _context;

    public SupplierRepo(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Supplier>> GetAllAsync()
    {
        return _context.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ToListAsync();
    }

    public Task<Supplier?> GetByIdAsync(int id)
    {
        return _context.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id);
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Supplier supplier)
    {
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }
}
