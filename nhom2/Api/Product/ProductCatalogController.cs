using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Application.Services;

namespace nhom2.Api.Product;

[ApiController]
[Route("api/product-catalog")]
[Authorize(Roles = "Admin,SalesStaff")]
public class ProductCatalogController : ControllerBase
{
    private readonly IProductClient _productClient;

    public ProductCatalogController(IProductClient productClient)
    {
        _productClient = productClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(new { success = true, data = await _productClient.GetProductsAsync() });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var product = await _productClient.GetProductByIdAsync(id);
            return product is null
                ? NotFound(new { success = false, message = "Không tìm thấy sản phẩm" })
                : Ok(new { success = true, data = product });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }
}
