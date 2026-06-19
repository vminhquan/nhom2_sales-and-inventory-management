using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface IProductClient
{
    Task<List<ProductDto>> GetProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request);
    Task ReleaseStockAsync(int productId, int quantity, string? referenceId = null,
        int? productVariantId = null, int? productVariantColorId = null);
    Task<bool> HasProductsForSupplierAsync(int supplierId);
}
