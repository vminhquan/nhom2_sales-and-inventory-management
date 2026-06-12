using Microsoft.EntityFrameworkCore;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Data;

namespace nhom2.Infrastructure.Repositories;

public class CustomerRepo : ICustomer
{
    private readonly ApplicationDbContext _context;

    public CustomerRepo(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Customer>> GetAllAsync()
    {
        return _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FullName)
            .ToListAsync();
    }

    public Task<Customer?> GetByIdAsync(int id)
    {
        return _context.Customers.FirstOrDefaultAsync(customer => customer.Id == id);
    }

    public Task<Customer?> GetByPhoneAsync(string phone)
    {
        return _context.Customers.FirstOrDefaultAsync(customer => customer.Phone == phone);
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Customer customer)
    {
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}
