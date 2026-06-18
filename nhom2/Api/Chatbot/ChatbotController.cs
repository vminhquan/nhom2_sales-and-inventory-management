using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhom2.Application.DTOs;
using nhom2.Application.Services;

namespace nhom2.Api.Chatbot;

[ApiController]
[Route("api/chatbot")]
[Authorize(Roles = "Customer")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetSession()
    {
        var identity = GetCustomerIdentity();
        var session = await _chatbotService.GetSessionAsync(identity.UserId, identity.Email);
        return Ok(new { success = true, data = session });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> Send(ChatRequestDto dto)
    {
        try
        {
            var identity = GetCustomerIdentity();
            var response = await _chatbotService.SendAsync(identity.UserId, identity.Email, dto.Message);
            return Ok(new { success = true, data = response });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Chatbot chưa được cấu hình OPENAI_API_KEY trên server."
                });
            }

            if (ex.Message.Contains("OpenAI API error", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Chatbot chưa gọi được OpenAI API. Vui lòng kiểm tra OPENAI_API_KEY trên server."
                });
            }

            return StatusCode(503, new { success = false, message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("session/end")]
    public async Task<IActionResult> EndSession()
    {
        var identity = GetCustomerIdentity();
        await _chatbotService.EndSessionAsync(identity.UserId);
        return Ok(new { success = true });
    }

    private (int UserId, string? Email) GetCustomerIdentity()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idValue, out var userId))
            throw new UnauthorizedAccessException("JWT không chứa user id hợp lệ.");

        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
        return (userId, email);
    }
}
