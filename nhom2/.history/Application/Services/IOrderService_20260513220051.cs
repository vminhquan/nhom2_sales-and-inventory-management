using nhom2.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    /// <summary>
    /// Interface định nghĩa các method cần có trong OrderService
    /// </summary>
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(int userId);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto);
        Task<OrderResponseDto> UpdateOrderAsync(UpdateOrderDto updateOrderDto);
        Task<bool> DeleteOrderAsync(int id);
    }
}
