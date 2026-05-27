using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    /// <summary>
    /// Service để xử lý business logic liên quan đến Order
    /// Đây là tầng Application - xử lý logic giữa API và Repository
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrder _orderRepository;

        public OrderService(IOrder orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Lấy tất cả đơn hàng
        /// </summary>
        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllOrders();
            return orders.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepository.GetOrderById(id);
            return order != null ? MapToDto(order) : null;
        }

        /// <summary>
        /// Lấy tất cả đơn hàng của một user
        /// </summary>
        public async Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _orderRepository.GetAllOrdersByUserId(userId);
            return orders.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            // Validation
            if (createOrderDto.UserId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (!createOrderDto.OrderItems.Any())
                throw new ArgumentException("Đơn hàng phải có ít nhất 1 sản phẩm");

            // Tạo Order mới
            var order = new Order
            {
                UserId = createOrderDto.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            // Thêm OrderItems
            foreach (var itemDto in createOrderDto.OrderItems)
            {
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

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        public async Task<OrderResponseDto> UpdateOrderAsync(UpdateOrderDto updateOrderDto)
        {
            var order = await _orderRepository.GetOrderById(updateOrderDto.Id);
            
            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy Order với ID {updateOrderDto.Id}");

            // Cập nhật trạng thái nếu có
            if (!string.IsNullOrEmpty(updateOrderDto.Status))
            {
                if (Enum.TryParse<OrderStatus>(updateOrderDto.Status, out var newStatus))
                {
                    order.Status = newStatus;
                }
                else
                {
                    throw new ArgumentException($"Trạng thái '{updateOrderDto.Status}' không hợp lệ");
                }
            }

            order.LastModifiedAt = DateTime.UtcNow;

            await _orderRepository.UpdateOrder(order);

            return MapToDto(order);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
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

        /// <summary>
        /// Chuyển đổi Order entity thành OrderResponseDto
        /// </summary>
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
