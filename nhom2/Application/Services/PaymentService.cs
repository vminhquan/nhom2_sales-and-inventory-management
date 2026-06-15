using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using nhom2.Application.DTOs;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Data;

namespace nhom2.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomerService _customerService;
    private readonly ICustomer _customerRepository;
    private readonly IProductClient _productClient;
    private readonly PayOsClient _payOs;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ApplicationDbContext context,
        ICustomerService customerService,
        ICustomer customerRepository,
        IProductClient productClient,
        PayOsClient payOs,
        IConfiguration configuration)
    {
        _context = context;
        _customerService = customerService;
        _customerRepository = customerRepository;
        _productClient = productClient;
        _payOs = payOs;
        _configuration = configuration;
    }

    public async Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto dto)
    {
        Validate(dto);
        var requestedItems = dto.OrderItems
            .GroupBy(item => item.ProductId)
            .Select(group => new OrderItemDto
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();
        var orderItems = new List<OrderItem>();
        foreach (var item in requestedItems)
        {
            var product = await _productClient.GetProductByIdAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"Product {item.ProductId} khong ton tai");
            if (product.Quantity < item.Quantity)
                throw new InvalidOperationException($"San pham {product.Name} khong du ton kho");
            orderItems.Add(new OrderItem(item.Quantity, product.SellingPrice)
            {
                ProductId = product.Id,
                ProductName = product.Name
            });
        }

        var customer = await GetOrCreateCustomerAsync(dto);
        var systemUserId = _configuration.GetValue<int?>("Checkout:SystemUserId")
            ?? throw new InvalidOperationException("Checkout:SystemUserId is not configured.");
        var order = new Order
        {
            UserId = systemUserId,
            CustomerId = customer.Id,
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.PendingPayment,
            OrderItems = orderItems
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 100_000L + order.Id;
        var frontendUrl = GetRequired("Checkout:FrontendBaseUrl");
        var backendUrl = GetRequired("Checkout:BackendBaseUrl");
        var payment = new PaymentTransaction
        {
            OrderId = order.Id,
            OrderCode = orderCode,
            ExpiresAt = expiresAt
        };
        _context.PaymentTransactions.Add(payment);
        await _context.SaveChangesAsync();

        try
        {
            payment.CheckoutUrl = await _payOs.CreatePaymentLinkAsync(
                orderCode,
                checked((int)order.TotalAmount),
                $"Thanh toan DH {order.Id}",
                $"{frontendUrl}/payment/success?orderCode={orderCode}",
                $"{backendUrl}/api/payments/cancel-return?orderCode={orderCode}",
                new DateTimeOffset(expiresAt).ToUnixTimeSeconds(),
                order.OrderItems.Select(item => (object)new
                {
                    name = item.ProductName,
                    quantity = item.Quantity,
                    price = checked((int)item.Price)
                }).ToList());
            await _context.SaveChangesAsync();
        }
        catch
        {
            order.Status = OrderStatus.PaymentFailed;
            order.LastModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw;
        }

        return Map(payment);
    }

    public async Task<PaymentStatusDto?> GetStatusAsync(long orderCode)
    {
        var payment = await LoadAsync(orderCode);
        if (payment is null)
            return null;
        if (payment.Order.Status == OrderStatus.PendingPayment && payment.ExpiresAt <= DateTime.UtcNow)
        {
            payment.Order.Status = OrderStatus.PaymentExpired;
            payment.Order.LastModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return MapStatus(payment);
    }

    public async Task CancelAsync(long orderCode)
    {
        var payment = await LoadAsync(orderCode);
        if (payment is null || payment.Order.Status != OrderStatus.PendingPayment)
            return;
        payment.Order.Status = OrderStatus.PaymentCancelled;
        payment.Order.LastModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task HandleWebhookAsync(JsonElement payload)
    {
        if (!_payOs.VerifyWebhook(payload))
            throw new UnauthorizedAccessException("Chu ky webhook PayOS khong hop le");

        var data = payload.GetProperty("data");
        if (data.GetProperty("code").GetString() != "00")
            return;
        var orderCode = data.GetProperty("orderCode").GetInt64();

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var payment = await LoadAsync(orderCode)
            ?? throw new KeyNotFoundException("Khong tim thay payment");
        if (payment.Order.Status == OrderStatus.Paid)
        {
            await transaction.CommitAsync();
            return;
        }
        if (payment.Order.Status != OrderStatus.PendingPayment)
        {
            await transaction.CommitAsync();
            return;
        }

        var claimed = await _context.Orders
            .Where(order => order.Id == payment.OrderId
                && order.Status == OrderStatus.PendingPayment)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatus.ProcessingPayment)
                .SetProperty(order => order.LastModifiedAt, DateTime.UtcNow));
        if (claimed == 0)
        {
            await transaction.CommitAsync();
            return;
        }
        payment.Order.Status = OrderStatus.ProcessingPayment;

        var reserved = new List<(int ProductId, int Quantity)>();
        try
        {
            foreach (var item in payment.Order.OrderItems)
            {
                await _productClient.ReserveStockAsync(new ReserveStockRequest
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
                reserved.Add((item.ProductId, item.Quantity));
            }
        }
        catch
        {
            foreach (var item in reserved)
                await _productClient.ReleaseStockAsync(item.ProductId, item.Quantity);
            payment.Order.Status = OrderStatus.PaymentFailed;
            payment.Order.LastModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            throw;
        }

        payment.Order.Status = OrderStatus.Paid;
        payment.Order.AmountPaid = payment.Order.TotalAmount;
        payment.Order.LastModifiedAt = DateTime.UtcNow;
        payment.CompletedAt = DateTime.UtcNow;
        payment.PayOsTransactionReference = data.TryGetProperty("reference", out var reference)
            ? reference.GetString()
            : null;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ExpirePendingPaymentsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var orders = await _context.PaymentTransactions
            .Where(payment => payment.ExpiresAt <= now
                && payment.Order.Status == OrderStatus.PendingPayment)
            .Select(payment => payment.Order)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            order.Status = OrderStatus.PaymentExpired;
            order.LastModifiedAt = now;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> GetOrCreateCustomerAsync(CreatePaymentLinkDto dto)
    {
        var customer = await _customerRepository.GetByPhoneAsync(dto.Phone.Trim());
        if (customer is not null)
            return customer;
        var created = await _customerService.CreateAsync(new CreateCustomerDto
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        });
        return await _customerRepository.GetByIdAsync(created.Id)
            ?? throw new InvalidOperationException("Khong the tao khach hang");
    }

    private Task<PaymentTransaction?> LoadAsync(long orderCode) =>
        _context.PaymentTransactions
            .Include(payment => payment.Order)
            .ThenInclude(order => order.OrderItems)
            .FirstOrDefaultAsync(payment => payment.OrderCode == orderCode);

    private static void Validate(CreatePaymentLinkDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName)
            || string.IsNullOrWhiteSpace(dto.Phone)
            || string.IsNullOrWhiteSpace(dto.Address)
            || dto.OrderItems.Count == 0
            || dto.OrderItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            throw new ArgumentException("Thong tin dat hang khong hop le");
    }

    private string GetRequired(string key)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} is not configured.")
            : value.TrimEnd('/');
    }

    private static PaymentLinkResponseDto Map(PaymentTransaction payment) => new()
    {
        OrderId = payment.OrderId,
        OrderCode = payment.OrderCode,
        CheckoutUrl = payment.CheckoutUrl,
        ExpiresAt = payment.ExpiresAt
    };

    private static PaymentStatusDto MapStatus(PaymentTransaction payment) => new()
    {
        OrderId = payment.OrderId,
        OrderCode = payment.OrderCode,
        Status = payment.Order.Status.ToString(),
        ExpiresAt = payment.ExpiresAt
    };
}
