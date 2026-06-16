using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using nhom2.Domain.Entities;

namespace nhom2.Domain.Interfaces
{
    public interface IOrder
    {
        Task<Order?> GetOrderById(int id);
        Task<List<Order>> GetAllOrdersByUserId(int userId);
        Task<List<Order>> GetAllOrdersByCustomerId(int customerId);
        Task<List<Order>> GetAllOrdersByCustomerEmail(string email);
        Task<List<Order>> GetAllOrders();
        Task AddOrder(Order order);
        Task UpdateOrder(Order order);
        Task DeleteOrder(int orderId);
    }
}
