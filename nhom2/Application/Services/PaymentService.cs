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
    private readonly IUserClient _userClient;
    private readonly PayOsClient _payOs;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext context,
        ICustomerService customerService,
        ICustomer customerRepository,
        IProductClient productClient,
        IUserClient userClient,
        PayOsClient payOs,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _customerService = customerService;
        _customerRepository = customerRepository;
        _productClient = productClient;
        _userClient = userClient;
        _payOs = payOs;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateCashOrderAsync(CreatePaymentLinkDto dto)
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
        var totalQuantity = requestedItems.Sum(item => item.Quantity);
        var reserved = new List<(int ProductId, int Quantity)>();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            foreach (var item in requestedItems)
            {
                var reservation = await _productClient.ReserveStockAsync(new ReserveStockRequest
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ReferenceId = $"cash:{dto.AuthenticatedCustomerUserId}:{item.ProductId}:{Guid.NewGuid():N}"
                });
                reserved.Add((item.ProductId, item.Quantity));
                orderItems.Add(new OrderItem(item.Quantity, reservation.Product.SellingPrice)
                {
                    ProductId = reservation.Product.Id,
                    ProductName = reservation.Product.Name
                });
            }

            var customer = await GetOrCreateCustomerAsync(dto);
            var membership = !string.IsNullOrWhiteSpace(customer.Email)
                ? await _userClient.GetCustomerMembershipByEmailAsync(customer.Email)
                : null;
            var systemUserId = _configuration.GetValue<int?>("Checkout:SystemUserId")
                ?? throw new InvalidOperationException("Checkout:SystemUserId is not configured.");
            var order = new Order
            {
                UserId = dto.AuthenticatedCustomerUserId ?? systemUserId,
                CustomerId = customer.Id,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                DiscountAmount = CalculateMemberDiscount(orderItems, totalQuantity, membership),
                AmountPaid = 0,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapOrder(order);
        }
        catch
        {
            foreach (var item in reserved)
            {
                await _productClient.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    $"cash:{dto.AuthenticatedCustomerUserId}:{item.ProductId}:rollback");
            }
            throw;
        }
    }

    public async Task<PaymentLinkResponseDto> CreatePaymentLinkAsync(CreatePaymentLinkDto dto)
    {
        Validate(dto);
        _payOs.EnsureConfigured();
        _ = GetRequired("Checkout:FrontendBaseUrl");
        _ = GetRequired("Checkout:BackendBaseUrl");

        var requestedItems = dto.OrderItems
            .GroupBy(item => item.ProductId)
            .Select(group => new OrderItemDto
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();
        var orderItems = new List<OrderItem>();
        var totalQuantity = requestedItems.Sum(item => item.Quantity);
        foreach (var item in requestedItems)
        {
            var product = await _productClient.GetProductByIdAsync(item.ProductId)
                ?? throw new KeyNotFoundException($"Product {item.ProductId} không tồn tại");
            if (product.Quantity < item.Quantity)
                throw new InvalidOperationException($"Sản phẩm {product.Name} không đủ tồn kho");
            orderItems.Add(new OrderItem(item.Quantity, product.SellingPrice)
            {
                ProductId = product.Id,
                ProductName = product.Name
            });
        }

        var customer = await GetOrCreateCustomerAsync(dto);
        var membership = !string.IsNullOrWhiteSpace(customer.Email)
            ? await _userClient.GetCustomerMembershipByEmailAsync(customer.Email)
            : null;
        var systemUserId = _configuration.GetValue<int?>("Checkout:SystemUserId")
            ?? throw new InvalidOperationException("Checkout:SystemUserId is not configured.");
        var order = new Order
        {
            UserId = dto.AuthenticatedCustomerUserId ?? systemUserId,
            CustomerId = customer.Id,
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.PendingPayment,
            DiscountAmount = CalculateMemberDiscount(orderItems, totalQuantity, membership),
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
                $"{frontendUrl}/#/payment/success?orderCode={orderCode}",
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

        if (payment.Order.Status == OrderStatus.PendingPayment)
        {
            try
            {
                var payOsStatus = await _payOs.GetPaymentStatusAsync(orderCode);
                if (payOsStatus.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await CompletePaymentAsync(orderCode, payOsStatus.Reference);
                }
                else if (payOsStatus.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    await CancelAsync(orderCode);
                }

                _context.ChangeTracker.Clear();
                payment = await LoadAsync(orderCode);
                if (payment is null)
                    return null;
            }
            catch (Exception ex) when (ex is HttpRequestException
                or PayOsException
                or PayOsConfigurationException
                or JsonException)
            {
                _logger.LogWarning(
                    ex,
                    "Could not reconcile PayOS status for order code {OrderCode}.",
                    orderCode);
            }
        }

        return payment is null ? null : MapStatus(payment);
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
            throw new UnauthorizedAccessException("Chữ ký webhook PayOS không hợp lệ");

        var data = payload.GetProperty("data");
        if (data.GetProperty("code").GetString() != "00")
            return;
        var orderCode = data.GetProperty("orderCode").GetInt64();
        var reference = data.TryGetProperty("reference", out var referenceElement)
            ? referenceElement.GetString()
            : null;

        await CompletePaymentAsync(orderCode, reference);
    }

    private async Task CompletePaymentAsync(long orderCode, string? reference)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var payment = await LoadAsync(orderCode)
            ?? throw new KeyNotFoundException("Không tìm thấy payment");
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
                    Quantity = item.Quantity,
                    ReferenceId = $"payos:{orderCode}:{item.ProductId}:reserve"
                });
                reserved.Add((item.ProductId, item.Quantity));
            }
        }
        catch
        {
            foreach (var item in reserved)
                await _productClient.ReleaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    $"payos:{orderCode}:{item.ProductId}:rollback");
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
        payment.PayOsTransactionReference = reference;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        if (!string.IsNullOrWhiteSpace(payment.Order.Customer?.Email))
        {
            try
            {
                await _userClient.NotifyPaidOrderAsync(payment.Order.Customer.Email, payment.Order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update customer tier for order {OrderId}.", payment.Order.Id);
            }
        }
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
        if (!string.IsNullOrWhiteSpace(dto.AuthenticatedCustomerEmail))
        {
            var normalizedEmail = dto.AuthenticatedCustomerEmail.Trim().ToLower();
            var existingByEmail = await _context.Customers
                .FirstOrDefaultAsync(customer => customer.Email != null
                    && customer.Email.ToLower() == normalizedEmail);
            if (existingByEmail is not null)
            {
                existingByEmail.FullName = dto.FullName.Trim();
                existingByEmail.Phone = dto.Phone.Trim();
                existingByEmail.Email = dto.AuthenticatedCustomerEmail.Trim();
                existingByEmail.Address = dto.Address.Trim();
                existingByEmail.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingByEmail;
            }

            var existingByPhone = await _customerRepository.GetByPhoneAsync(dto.Phone.Trim());
            if (existingByPhone is not null)
            {
                existingByPhone.FullName = dto.FullName.Trim();
                existingByPhone.Email = dto.AuthenticatedCustomerEmail.Trim();
                existingByPhone.Address = dto.Address.Trim();
                existingByPhone.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingByPhone;
            }

            var customerForAccount = new Customer
            {
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim(),
                Email = dto.AuthenticatedCustomerEmail.Trim(),
                Address = dto.Address.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customerForAccount);
            await _context.SaveChangesAsync();
            return customerForAccount;
        }

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
            ?? throw new InvalidOperationException("Không thể tạo khách hàng");
    }

    private Task<PaymentTransaction?> LoadAsync(long orderCode) =>
        _context.PaymentTransactions
            .Include(payment => payment.Order)
            .ThenInclude(order => order.Customer)
            .Include(payment => payment.Order)
            .ThenInclude(order => order.OrderItems)
            .FirstOrDefaultAsync(payment => payment.OrderCode == orderCode);

    private static decimal CalculateMemberDiscount(
        IReadOnlyCollection<OrderItem> orderItems,
        int totalQuantity,
        CustomerMembershipDto? membership)
    {
        if (totalQuantity < 3 || membership is null || membership.DiscountPercent <= 0)
            return 0;

        var subtotal = orderItems.Sum(item => item.SubTotal);
        return Math.Round(subtotal * membership.DiscountPercent / 100m, 0);
    }

    private static void Validate(CreatePaymentLinkDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName)
            || string.IsNullOrWhiteSpace(dto.Phone)
            || string.IsNullOrWhiteSpace(dto.Address)
            || dto.OrderItems.Count == 0
            || dto.OrderItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            throw new ArgumentException("Thông tin đặt hàng không hợp lệ");
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

    private static OrderResponseDto MapOrder(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.FullName,
        Status = order.Status.ToString(),
        Subtotal = order.Subtotal,
        DiscountAmount = order.DiscountAmount,
        Total = order.TotalAmount,
        AmountPaid = order.AmountPaid,
        DebtAmount = order.DebtAmount,
        CreatedAt = order.CreatedAt,
        LastModifiedAt = order.LastModifiedAt,
        OrderItems = order.OrderItems.Select(item => new OrderItemResponseDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Price = item.Price,
            SubTotal = item.SubTotal
        }).ToList()
    };

    private static PaymentStatusDto MapStatus(PaymentTransaction payment) => new()
    {
        OrderId = payment.OrderId,
        OrderCode = payment.OrderCode,
        Status = payment.Order.Status.ToString(),
        ExpiresAt = payment.ExpiresAt
    };
}
