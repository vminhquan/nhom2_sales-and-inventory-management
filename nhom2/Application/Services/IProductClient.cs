using nhom2.Application.DTOs;
using nhom2.Application.Mock;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    public interface IProductClient
    {
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request);
    }
}