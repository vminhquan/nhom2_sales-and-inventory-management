using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Application.DTOs;
using nhom2.Application.Services;

namespace nhom2.Api.Supplier;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "Admin,SalesStaff,WarehouseKeeper")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(new { success = true, data = await _supplierService.GetAllAsync() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        return supplier is null
            ? NotFound(new { success = false, message = "Không tìm thấy nhà cung cấp" })
            : Ok(new { success = true, data = supplier });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SalesStaff")]
    public async Task<IActionResult> Create(CreateSupplierDto dto)
    {
        try
        {
            var supplier = await _supplierService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id },
                new { success = true, data = supplier });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,SalesStaff")]
    public async Task<IActionResult> Update(int id, UpdateSupplierDto dto)
    {
        try
        {
            dto.Id = id;
            return Ok(new { success = true, data = await _supplierService.UpdateAsync(dto) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _supplierService.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
