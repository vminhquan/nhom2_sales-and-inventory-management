using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Application.DTOs;
using nhom2.Application.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    
    // business logic liên quan đến Order
    // tầng Application - xử lý logic giữa API và Repository
    public class OrderService : IOrderService
    {
        private readonly IOrder _orderRepository;
        private readonly IUserClient _userClient;

        public OrderService(IOrder orderRepository, IUserClient userClient)
        {
            _orderRepository = orderRepository;
            _userClient = userClient;
        }

        
        // Lấy tất cả đơn hàng

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrders();
            return orders.Select(MapToDto).ToList();
        }

        
        // Lấy đơn hàng theo ID

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepository.GetOrderById(id);
            return order != null ? MapToDto(order) : null;
        }

        
        // Lấy tất cả đơn hàng của một user

        public async Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _orderRepository.GetAllOrdersByUserId(userId);
            return orders.Select(MapToDto).ToList();
        }

        
        /// Tạo đơn hàng mới

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            // Validation
            if (createOrderDto.UserId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (!createOrderDto.OrderItems.Any())
                throw new ArgumentException("Đơn hàng phải có ít nhất 1 sản phẩm");

            // Kiểm tra User tồn tại từ User service
            var user = await _userClient.GetUserByIdAsync(createOrderDto.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User với ID {createOrderDto.UserId} không tồn tại");

            // Tạo Order mới
            var order = new Order
            {
                UserId = createOrderDto.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            // Thêm OrderItems và validate Product
            foreach (var itemDto in createOrderDto.OrderItems)
            {
                // Kiểm tra Product tồn tại từ Mock data
                var product = MockProductData.GetProductById(itemDto.ProductId);
                if (product == null)
                    throw new KeyNotFoundException($"Product với ID {itemDto.ProductId} không tồn tại");

                // Kiểm tra stock có đủ không
                if (!MockProductData.HasStock(itemDto.ProductId, itemDto.Quantity))
                    throw new InvalidOperationException($"Product {product.Name} chỉ còn {product.Stock} sản phẩm");

                var orderItem = new OrderItem(itemDto.Quantity, itemDto.Price)
                {
                    ProductId = itemDto.ProductId
                };
                order.OrderItems.Add(orderItem);
            }

            // Lưu vào database
            await _orderRepository.AddOrder(order);

            return MapToDto(order);
        }

        
        public async Task<OrderResponseDto> UpdateOrderAsync(UpdateOrderDto updateOrderDto)
        {
            var order = await _orderRepository.GetOrderById(updateOrderDto.Id);
            
            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy Order với ID {updateOrderDto.Id}");

            if (updateOrderDto.OrderItems == null || !updateOrderDto.OrderItems.Any())
                throw new ArgumentException("Đơn hàng phải có ít nhất 1 sản phẩm");

            order.OrderItems.Clear();

            foreach (var itemDto in updateOrderDto.OrderItems)
            {
                var product = MockProductData.GetProductById(itemDto.ProductId);
                if (product == null)
                    throw new KeyNotFoundException($"Product với ID {itemDto.ProductId} không tồn tại");

                if (!MockProductData.HasStock(itemDto.ProductId, itemDto.Quantity))
                    throw new InvalidOperationException($"Product {product.Name} chỉ còn {product.Stock} sản phẩm");

                var orderItem = new OrderItem(itemDto.Quantity, itemDto.Price)
                {
                    ProductId = itemDto.ProductId
                };

                order.OrderItems.Add(orderItem);
            }

            order.LastModifiedAt = DateTime.UtcNow;

            await _orderRepository.UpdateOrder(order);

            return MapToDto(order);
        }

        // Cập nhật trạng thái đơn hàng
        public async Task<OrderResponseDto> UpdateOrderStatusAsync(UpdateOrderStatusDto updateOrderStatusDto)
        {
            var order = await _orderRepository.GetOrderById(updateOrderStatusDto.Id);

            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy Order với ID {updateOrderStatusDto.Id}");

            if (!Enum.IsDefined(typeof(OrderStatus), updateOrderStatusDto.Status))
                throw new ArgumentException($"Trạng thái '{updateOrderStatusDto.Status}' không hợp lệ");

            var newStatus = updateOrderStatusDto.Status;

            if (!IsValidTransition(order.Status, newStatus))
                throw new InvalidOperationException($"Không thể chuyển trạng thái từ '{order.Status}' sang '{newStatus}'");

            order.Status = newStatus;
            order.LastModifiedAt = DateTime.UtcNow;

            await _orderRepository.UpdateOrder(order);

            return MapToDto(order);
        }

        // Xóa đơn hàng
        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _orderRepository.GetOrderById(id);
            
            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy Order với ID {id}");

            // Chỉ cho phép xóa nếu đơn hàng chưa được xử lý
            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Chỉ có thể xóa đơn hàng ở trạng thái Pending");

            await _orderRepository.DeleteOrder(id);
            return true;
        }

        
        // Luồng tối giản: Pending -> Processing -> Shipped -> Completed; Cancelled là kết thúc
        private static bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == newStatus)
                return true;

            return currentStatus switch
            {
                OrderStatus.Pending => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,
                OrderStatus.Processing => newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled,
                OrderStatus.Shipped => newStatus == OrderStatus.Completed,
                OrderStatus.Completed => false,
                OrderStatus.Cancelled => false,
                _ => false
            };
        }

        // Chuyển đổi Order entity thành OrderResponseDto
        private OrderResponseDto MapToDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                Status = order.Status.ToString(),
                Total = order.OrderItems.Sum(oi => oi.SubTotal),
                CreatedAt = order.CreatedAt,
                LastModifiedAt = order.LastModifiedAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    SubTotal = oi.SubTotal
                }).ToList()
            };
        }
    }
}
