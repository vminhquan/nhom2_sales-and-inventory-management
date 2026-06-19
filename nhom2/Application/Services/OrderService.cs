using nhom2.Application.DTOs;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;

namespace nhom2.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrder _orderRepository;
    private readonly ICustomer _customerRepository;
    private readonly IUserClient _userClient;
    private readonly IProductClient _productClient;
    private readonly IOrderEventPublisher _eventPublisher;

    public OrderService(
        IOrder orderRepository,
        ICustomer customerRepository,
        IUserClient userClient,
        IProductClient productClient,
        IOrderEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _userClient = userClient;
        _productClient = productClient;
        _eventPublisher = eventPublisher;
    }

    public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
    {
        return (await _orderRepository.GetAllOrders()).Select(MapToDto).ToList();
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _orderRepository.GetOrderById(id);
        return order is null ? null : MapToDto(order);
    }

    public async Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId)
    {
        return (await _orderRepository.GetAllOrdersByUserId(userId)).Select(MapToDto).ToList();
    }

    public async Task<List<OrderResponseDto>> GetOrdersByCustomerEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không hợp lệ");

        return (await _orderRepository.GetAllOrdersByCustomerEmail(email)).Select(MapToDto).ToList();
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        ValidateOrderInput(dto.UserId, dto.OrderItems, dto.DiscountAmount, dto.AmountPaid);

        if (await _userClient.GetUserByIdAsync(dto.UserId) is null)
            throw new KeyNotFoundException($"User với ID {dto.UserId} không tồn tại");

        var customer = await GetCustomerAsync(dto.CustomerId);
        var requestedItems = AggregateItems(dto.OrderItems);
        var reservations = new List<ReserveStockResponse>();
        Order? order = null;

        try
        {
            foreach (var item in requestedItems)
            {
                reservations.Add(await _productClient.ReserveStockAsync(new ReserveStockRequest
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductVariantColorId = item.ProductVariantColorId,
                    Quantity = item.Quantity
                }));
            }

            order = new Order
            {
                UserId = dto.UserId,
                CustomerId = customer?.Id,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                PaymentMethod = OrderPaymentMethod.Cash,
                DiscountAmount = dto.DiscountAmount,
                AmountPaid = dto.AmountPaid,
                OrderItems = reservations.Select(reservation => new OrderItem(
                    requestedItems.Single(item => item.ProductId == reservation.Product.Id
                        && item.ProductVariantId == reservation.Product.ProductVariantId
                        && item.ProductVariantColorId == reservation.Product.ProductVariantColorId).Quantity,
                    reservation.Product.SellingPrice)
                {
                    ProductId = reservation.Product.Id,
                    ProductVariantId = reservation.Product.ProductVariantId,
                    ProductVariantColorId = reservation.Product.ProductVariantColorId,
                    ProductName = reservation.Product.Name,
                    VariantName = reservation.Product.VariantName,
                    ColorName = reservation.Product.ColorName,
                    Sku = reservation.Product.Sku
                }).ToList()
            };

            ValidateAmounts(order.Subtotal, order.DiscountAmount, order.AmountPaid);
            await _orderRepository.AddOrder(order);
            await _eventPublisher.PublishAsync(CreateEvent(order, "order.created"));

            return MapToDto(order);
        }
        catch
        {
            if (order?.Id > 0)
                await TryDeleteOrderAsync(order.Id);

            await ReleaseReservationsAsync(reservations);
            throw;
        }
    }

    public async Task<OrderResponseDto> UpdateOrderAsync(UpdateOrderDto dto)
    {
        var order = await _orderRepository.GetOrderById(dto.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Order với ID {dto.Id}");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể sửa sản phẩm của đơn hàng Pending");

        ValidateOrderInput(order.UserId, dto.OrderItems, dto.DiscountAmount, dto.AmountPaid);

        var requestedItems = AggregateItems(dto.OrderItems);
        var oldQuantities = order.OrderItems
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var newQuantities = requestedItems.ToDictionary(item => item.ProductId, item => item.Quantity);
        var productIds = oldQuantities.Keys.Union(newQuantities.Keys).ToList();
        var products = new Dictionary<int, ProductDto>();
        var addedStock = new List<ReserveStockResponse>();
        var orderUpdated = false;

        try
        {
            foreach (var productId in productIds)
            {
                var oldQuantity = oldQuantities.GetValueOrDefault(productId);
                var newQuantity = newQuantities.GetValueOrDefault(productId);
                var delta = newQuantity - oldQuantity;

                if (delta > 0)
                {
                    var reservation = await _productClient.ReserveStockAsync(new ReserveStockRequest
                    {
                        ProductId = productId,
                        Quantity = delta
                    });
                    addedStock.Add(reservation);
                    products[productId] = reservation.Product;
                }
                else if (newQuantity > 0)
                {
                    products[productId] = await _productClient.GetProductByIdAsync(productId)
                        ?? throw new KeyNotFoundException($"Product với ID {productId} không tồn tại");
                }
            }

            order.OrderItems.Clear();
            foreach (var item in requestedItems)
            {
                var product = products[item.ProductId];
                order.OrderItems.Add(new OrderItem(item.Quantity, product.SellingPrice)
                {
                    ProductId = product.Id,
                    ProductName = product.Name
                });
            }

            order.DiscountAmount = dto.DiscountAmount;
            order.AmountPaid = dto.AmountPaid;
            order.LastModifiedAt = DateTime.UtcNow;
            ValidateAmounts(order.Subtotal, order.DiscountAmount, order.AmountPaid);

            await _orderRepository.UpdateOrder(order);
            orderUpdated = true;

            foreach (var productId in productIds)
            {
                var releasedQuantity = oldQuantities.GetValueOrDefault(productId)
                    - newQuantities.GetValueOrDefault(productId);
                if (releasedQuantity > 0)
                    await _productClient.ReleaseStockAsync(productId, releasedQuantity);
            }

            await _eventPublisher.PublishAsync(CreateEvent(order, "order.updated"));
            return MapToDto(order);
        }
        catch
        {
            if (!orderUpdated)
                await ReleaseReservationsAsync(addedStock);
            throw;
        }
    }

    public async Task<OrderResponseDto> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
    {
        var order = await _orderRepository.GetOrderById(dto.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Order với ID {dto.Id}");

        if (!Enum.IsDefined(dto.Status))
            throw new ArgumentException($"Trạng thái '{dto.Status}' không hợp lệ");

        if (!IsValidTransition(order.Status, dto.Status))
        {
            throw new InvalidOperationException(
                $"Không thể chuyển trạng thái từ '{order.Status}' sang '{dto.Status}'");
        }

        var oldStatus = order.Status;
        var shouldReleaseStock = oldStatus != OrderStatus.Cancelled
            && dto.Status == OrderStatus.Cancelled;
        var previousAmountPaid = order.AmountPaid;

        if (shouldReleaseStock)
            await ReleaseOrderStockAsync(order);

        try
        {
            order.Status = dto.Status;
            if (order.PaymentMethod == OrderPaymentMethod.Cash && dto.Status == OrderStatus.Completed)
                order.AmountPaid = order.TotalAmount;
            order.LastModifiedAt = DateTime.UtcNow;
            await _orderRepository.UpdateOrder(order);
        }
        catch
        {
            if (shouldReleaseStock)
                await ReserveOrderStockAsync(order);
            order.AmountPaid = previousAmountPaid;
            throw;
        }

        await _eventPublisher.PublishAsync(CreateEvent(order, "order.status-changed"));
        return MapToDto(order);
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _orderRepository.GetOrderById(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy Order với ID {id}");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể xóa đơn hàng ở trạng thái Pending");

        await ReleaseOrderStockAsync(order);
        try
        {
            await _orderRepository.DeleteOrder(id);
        }
        catch
        {
            await ReserveOrderStockAsync(order);
            throw;
        }

        order.Status = OrderStatus.Cancelled;
        order.LastModifiedAt = DateTime.UtcNow;
        await _eventPublisher.PublishAsync(CreateEvent(order, "order.deleted"));
        return true;
    }

    private async Task<Customer?> GetCustomerAsync(int? customerId)
    {
        if (!customerId.HasValue)
            return null;

        return await _customerRepository.GetByIdAsync(customerId.Value)
            ?? throw new KeyNotFoundException($"Customer với ID {customerId.Value} không tồn tại");
    }

    private static List<OrderItemDto> AggregateItems(IEnumerable<OrderItemDto> items)
    {
        return items
            .GroupBy(item => new { item.ProductId, item.ProductVariantId, item.ProductVariantColorId })
            .Select(group => new OrderItemDto
            {
                ProductId = group.Key.ProductId,
                ProductVariantId = group.Key.ProductVariantId,
                ProductVariantColorId = group.Key.ProductVariantColorId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();
    }

    private static void ValidateOrderInput(
        int userId,
        IReadOnlyCollection<OrderItemDto>? items,
        decimal discountAmount,
        decimal amountPaid)
    {
        if (userId <= 0)
            throw new ArgumentException("UserId phải lớn hơn 0");

        if (items is null || items.Count == 0)
            throw new ArgumentException("Đơn hàng phải có ít nhất 1 sản phẩm");

        if (items.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            throw new ArgumentException("ProductId và số lượng phải lớn hơn 0");

        if (discountAmount < 0)
            throw new ArgumentException("Chiết khấu không được là số âm");

        if (amountPaid < 0)
            throw new ArgumentException("Số tiền đã thanh toán không được là số âm");
    }

    private static void ValidateAmounts(decimal subtotal, decimal discountAmount, decimal amountPaid)
    {
        if (discountAmount > subtotal)
            throw new ArgumentException("Chiết khấu không được lớn hơn tạm tính");

        var total = subtotal - discountAmount;
        if (amountPaid > total)
            throw new ArgumentException("Số tiền thanh toán không được lớn hơn tổng đơn hàng");
    }

    private async Task ReleaseOrderStockAsync(Order order)
    {
        foreach (var item in order.OrderItems.GroupBy(item => new
                 { item.ProductId, item.ProductVariantId, item.ProductVariantColorId }))
            await _productClient.ReleaseStockAsync(item.Key.ProductId, item.Sum(value => value.Quantity),
                productVariantId: item.Key.ProductVariantId,
                productVariantColorId: item.Key.ProductVariantColorId);
    }

    private async Task ReserveOrderStockAsync(Order order)
    {
        foreach (var item in order.OrderItems.GroupBy(item => new
                 { item.ProductId, item.ProductVariantId, item.ProductVariantColorId }))
        {
            await _productClient.ReserveStockAsync(new ReserveStockRequest
            {
                ProductId = item.Key.ProductId,
                ProductVariantId = item.Key.ProductVariantId,
                ProductVariantColorId = item.Key.ProductVariantColorId,
                Quantity = item.Sum(value => value.Quantity)
            });
        }
    }

    private async Task ReleaseReservationsAsync(IEnumerable<ReserveStockResponse> reservations)
    {
        foreach (var reservation in reservations.Reverse())
        {
            try
            {
                await _productClient.ReleaseStockAsync(
                    reservation.Product.Id,
                    reservation.PreviousQuantity - reservation.CurrentQuantity);
            }
            catch
            {
                // Preserve the original failure. Stock recovery can be retried operationally.
            }
        }
    }

    private async Task TryDeleteOrderAsync(int orderId)
    {
        try
        {
            await _orderRepository.DeleteOrder(orderId);
        }
        catch
        {
            // Preserve the original failure from the downstream service.
        }
    }

    private static bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == newStatus)
            return true;

        return currentStatus switch
        {
            OrderStatus.Pending => newStatus is OrderStatus.Processing
                or OrderStatus.Shipped
                or OrderStatus.Cancelled,
            OrderStatus.PendingPayment => false,
            OrderStatus.ProcessingPayment => false,
            OrderStatus.Paid => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.PaymentCancelled => false,
            OrderStatus.PaymentExpired => false,
            OrderStatus.PaymentFailed => false,
            OrderStatus.Processing => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus is OrderStatus.Completed or OrderStatus.Cancelled,
            OrderStatus.Completed => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.FullName,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
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
                ProductVariantId = item.ProductVariantId,
                ProductVariantColorId = item.ProductVariantColorId,
                ProductName = item.ProductName,
                VariantName = item.VariantName,
                ColorName = item.ColorName,
                Sku = item.Sku,
                Quantity = item.Quantity,
                Price = item.Price,
                SubTotal = item.SubTotal
            }).ToList()
        };
    }

    private static OrderEventDto CreateEvent(Order order, string eventType)
    {
        return new OrderEventDto
        {
            EventType = eventType,
            OrderId = order.Id,
            UserId = order.UserId,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.FullName,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            AmountPaid = order.AmountPaid,
            DebtAmount = order.DebtAmount,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(item => new OrderEventItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                Subtotal = item.SubTotal
            }).ToList()
        };
    }
}
