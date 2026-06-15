using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Application.DTOs;
using nhom2.Application.Services;

namespace nhom2.Api.Payment;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    [HttpPost("links")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateLink(CreatePaymentLinkDto dto)
    {
        try
        {
            return Ok(new { success = true, data = await _paymentService.CreatePaymentLinkAsync(dto) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{orderCode:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(long orderCode)
    {
        var status = await _paymentService.GetStatusAsync(orderCode);
        return status is null
            ? NotFound(new { success = false, message = "Khong tim thay payment" })
            : Ok(new { success = true, data = status });
    }

    [HttpGet("cancel-return")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelReturn([FromQuery] long orderCode)
    {
        await _paymentService.CancelAsync(orderCode);
        var frontend = _configuration["Checkout:FrontendBaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Checkout:FrontendBaseUrl is not configured.");
        return Redirect($"{frontend}/payment/cancelled?orderCode={orderCode}");
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload)
    {
        try
        {
            await _paymentService.HandleWebhookAsync(payload);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }
}
