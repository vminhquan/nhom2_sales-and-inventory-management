using Microsoft.AspNetCore.Mvc;
using nhom2.Application.Mock;

namespace nhom2.Api.Mock
{
    [ApiController]
    [Route("api/products")]
    public class MockProductController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            var products = MockProductData.GetAllProducts();
            return Ok(new { success = true, data = products });
        }

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = MockProductData.GetProductById(id);
            if (product == null)
                return NotFound(new { success = false, message = "Product not found" });

            return Ok(new { success = true, data = product });
        }
    }
}
