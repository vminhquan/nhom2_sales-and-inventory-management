using Microsoft.AspNetCore.Mvc;
using nhom2.Application.Mock;

namespace nhom2.Api.Mock
{
    /// <summary>
    /// Mock API Controller cho Product
    /// Được sử dụng để test khi Product Service từ nhóm 1 chưa sẵn sàng
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MockProductController : ControllerBase
    {
        /// <summary>
        /// GET: api/mockproduct/{id}
        /// Lấy product theo ID từ mock data
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = MockProductData.GetProductById(id);
            if (product == null)
                return NotFound(new { message = $"Product với ID {id} không tìm thấy" });

            return Ok(new { success = true, data = product });
        }

        /// <summary>
        /// GET: api/mockproduct
        /// Lấy tất cả products từ mock data
        /// </summary>
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            var products = MockProductData.GetAllProducts();
            return Ok(new { success = true, data = products });
        }

        /// <summary>
        /// GET: api/mockproduct/check-stock/{productId}/{quantity}
        /// Kiểm tra product có còn stock không
        /// </summary>
        [HttpGet("check-stock/{productId}/{quantity}")]
        public IActionResult CheckStock(int productId, int quantity)
        {
            var hasStock = MockProductData.HasStock(productId, quantity);
            var product = MockProductData.GetProductById(productId);

            if (product == null)
                return NotFound(new { message = $"Product với ID {productId} không tìm thấy" });

            return Ok(new 
            { 
                success = true, 
                data = new 
                { 
                    productId, 
                    quantity, 
                    availableStock = product.Stock, 
                    hasStock 
                } 
            });
        }
    }
}
