using nhom2.Application.DTOs;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;

namespace nhom2.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomer _customerRepository;
    private readonly IOrder _orderRepository;

    public CustomerService(ICustomer customerRepository, IOrder orderRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<CustomerResponseDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        var result = new List<CustomerResponseDto>();

        foreach (var customer in customers)
            result.Add(await MapAsync(customer, includeHistory: false));

        return result;
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer is null ? null : await MapAsync(customer, includeHistory: true);
    }

    public async Task<CustomerResponseDto> CreateAsync(CreateCustomerDto dto)
    {
        Validate(dto.FullName, dto.Phone);

        if (await _customerRepository.GetByPhoneAsync(dto.Phone.Trim()) is not null)
            throw new InvalidOperationException("Số điện thoại khách hàng đã tồn tại");

        var customer = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = Normalize(dto.Email),
            Address = Normalize(dto.Address),
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        return await MapAsync(customer, includeHistory: true);
    }

    public async Task<CustomerResponseDto> UpdateAsync(UpdateCustomerDto dto)
    {
        Validate(dto.FullName, dto.Phone);

        var customer = await _customerRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Customer với ID {dto.Id}");
        var samePhone = await _customerRepository.GetByPhoneAsync(dto.Phone.Trim());

        if (samePhone is not null && samePhone.Id != dto.Id)
            throw new InvalidOperationException("Số điện thoại khách hàng đã tồn tại");

        customer.FullName = dto.FullName.Trim();
        customer.Phone = dto.Phone.Trim();
        customer.Email = Normalize(dto.Email);
        customer.Address = Normalize(dto.Address);
        customer.LastModifiedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer);
        return await MapAsync(customer, includeHistory: true);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Customer với ID {id}");

        await _customerRepository.DeleteAsync(customer);
    }

    private async Task<CustomerResponseDto> MapAsync(Customer customer, bool includeHistory)
    {
        var orders = await _orderRepository.GetAllOrdersByCustomerId(customer.Id);
        var activeOrders = orders.Where(order => order.Status != OrderStatus.Cancelled).ToList();

        return new CustomerResponseDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            TotalSpent = activeOrders.Sum(order => order.TotalAmount),
            CurrentDebt = activeOrders.Sum(order => order.DebtAmount),
            OrderCount = activeOrders.Count,
            CreatedAt = customer.CreatedAt,
            LastModifiedAt = customer.LastModifiedAt,
            PurchaseHistory = includeHistory
                ? orders.Select(order => new CustomerOrderSummaryDto
                {
                    OrderId = order.Id,
                    Status = order.Status.ToString(),
                    Total = order.TotalAmount,
                    AmountPaid = order.AmountPaid,
                    DebtAmount = order.DebtAmount,
                    CreatedAt = order.CreatedAt
                }).ToList()
                : new List<CustomerOrderSummaryDto>()
        };
    }

    private static void Validate(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Tên khách hàng không được để trống");

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Số điện thoại không được để trống");
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
