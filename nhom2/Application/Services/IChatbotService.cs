using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public interface IChatbotService
{
    Task<ChatSessionDto> GetSessionAsync(int customerUserId, string? customerEmail);
    Task<ChatResponseDto> SendAsync(int customerUserId, string? customerEmail, string message);
    Task EndSessionAsync(int customerUserId);
}
