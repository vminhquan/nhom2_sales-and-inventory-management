using nhom2.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    /// Interface định nghĩa các method cần có trong OrderService
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId);
        Task<List<OrderResponseDto>> GetOrdersByCustomerEmailAsync(string email);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto);
        Task<OrderResponseDto> UpdateOrderAsync(UpdateOrderDto updateOrderDto);
        Task<OrderResponseDto> UpdateOrderStatusAsync(UpdateOrderStatusDto updateOrderStatusDto);
        Task<bool> DeleteOrderAsync(int id);
    }
}
