using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Domain.Interfaces;

namespace nhom2.Api.Internal;

[ApiController]
[Route("api/internal/suppliers")]
[AllowAnonymous]
public class InternalSupplierController : ControllerBase
{
    private readonly ISupplier _supplierRepository;
    private readonly IConfiguration _configuration;

    public InternalSupplierController(
        ISupplier supplierRepository,
        IConfiguration configuration)
    {
        _supplierRepository = supplierRepository;
        _configuration = configuration;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!HasValidApiKey())
            return Unauthorized(new { success = false, message = "Internal API key không hợp lệ" });

        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier is null
            ? NotFound(new { success = false, message = "Không tìm thấy nhà cung cấp" })
            : Ok(new
            {
                success = true,
                data = new
                {
                    supplier.Id,
                    supplier.Name,
                    supplier.ContactName,
                    supplier.Phone,
                    supplier.Email,
                    supplier.Address
                }
            });
    }

    private bool HasValidApiKey()
    {
        var expected = _configuration["Services:InternalApiKey"];
        var actual = Request.Headers["X-Internal-Api-Key"].ToString();
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected, actual, StringComparison.Ordinal);
    }
}
