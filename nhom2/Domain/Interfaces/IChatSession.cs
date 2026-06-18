using nhom2.Domain.Entities;

namespace nhom2.Domain.Interfaces;

public interface IChatSession
{
    Task<ChatSession> GetOrCreateActiveAsync(int customerUserId, string? customerEmail);
    Task<ChatSession?> GetActiveAsync(int customerUserId);
    Task<List<ChatMessage>> GetMessagesAsync(int sessionId, int limit = 30);
    Task AddMessageAsync(ChatMessage message);
    Task EndActiveAsync(int customerUserId);
}
