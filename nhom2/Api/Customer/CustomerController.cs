using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Application.DTOs;
using nhom2.Application.Services;

namespace nhom2.Api.Customer;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,SalesStaff")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(new { success = true, data = await _customerService.GetAllAsync() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return customer is null
            ? NotFound(new { success = false, message = "Không tìm thấy khách hàng" })
            : Ok(new { success = true, data = customer });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        try
        {
            var customer = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id },
                new { success = true, data = customer });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerDto dto)
    {
        try
        {
            dto.Id = id;
            return Ok(new { success = true, data = await _customerService.UpdateAsync(dto) });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _customerService.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
